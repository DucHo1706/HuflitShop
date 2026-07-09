using HuflitShopCore.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace HuflitShopCore.Services
{
    public class AIChatService
    {
        private readonly AppDbContext _context;
        private readonly string _apiKey;
        private static readonly HttpClient _httpClient = new HttpClient();

        public AIChatService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _apiKey = configuration["GoogleAI:ApiKey"];

            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("YOUR_API_KEY_HERE"))
            {
                throw new InvalidOperationException("Chưa cấu hình Google AI API Key trong appsettings.json");
            }
        }

        public async Task<string> GetAIResponseAsync(string userMessage, string userId = null)
        {
            try
            {
                var lowerMsg = userMessage.ToLower();

                // 1. Phân tích ý định khách hàng
                bool isAskingOrder = lowerMsg.Contains("đơn") || lowerMsg.Contains("order") || lowerMsg.Contains("trạng thái") || lowerMsg.Contains("kiểm tra");
                bool isAskingProduct = lowerMsg.Contains("váy") || lowerMsg.Contains("áo") || lowerMsg.Contains("quần") || lowerMsg.Contains("size") || lowerMsg.Contains("màu") || lowerMsg.Contains("tìm") || lowerMsg.Contains("phối") || lowerMsg.Contains("mặc");

                string dynamicContext = "";

                // 2. Tra cứu Đơn Hàng
                if (isAskingOrder && !string.IsNullOrEmpty(userId))
                {
                    var recentOrders = await _context.Orders
                        .Where(o => o.UserId == userId)
                        .OrderByDescending(o => o.OrderDate)
                        .Take(3)
                        .Select(o => $"- Mã đơn: #{o.Id.Substring(0, 8).ToUpper()} | Ngày: {o.OrderDate:dd/MM/yyyy} | Tổng tiền: {o.FinalAmount:N0}đ | Mã trạng thái: {o.OrderStatus}")
                        .ToListAsync();

                    dynamicContext += "THÔNG TIN ĐƠN HÀNG CỦA KHÁCH TRONG HỆ THỐNG (Mã trạng thái: 0-Chờ duyệt, 1-Đang giao, 2-Hoàn tất, 3-Đã hủy):\n";
                    dynamicContext += recentOrders.Any() ? string.Join("\n", recentOrders) : "Khách hàng chưa có đơn hàng nào.\n";
                    dynamicContext += "\n";
                }
                else if (isAskingOrder && string.IsNullOrEmpty(userId))
                {
                    dynamicContext += "HỆ THỐNG: Khách hàng chưa đăng nhập, hãy yêu cầu khách đăng nhập hoặc cung cấp mã đơn.\n\n";
                }

                // 3. Tra cứu Sản Phẩm
                if (isAskingProduct)
                {
                    var products = await _context.Products
                        .OrderBy(p => p.ProductName)
                        .Where(p => !p.IsDeleted)
                        .Select(p => $"- {p.ProductName} | Giá: {p.CurrentPrice:N0}đ")
                        .Take(40)
                        .ToListAsync();

                    dynamicContext += "DANH SÁCH SẢN PHẨM HIỆN CÓ TẠI SHOP:\n";
                    dynamicContext += products.Any() ? string.Join("\n", products) : "Hiện không có sản phẩm nào khả dụng.\n";
                    dynamicContext += "\n";
                }

                // 4. Xây dựng Prompt
                string prompt = $@"
Bạn là nhân viên tư vấn thời trang AI (Stylist AI) chuyên nghiệp của HuflitShop. Tên bạn là 'Trợ lý HuflitShop'.
Nhiệm vụ của bạn:
1. Gợi ý phối đồ (Mix & Match) cho khách theo ngữ cảnh (đi tiệc, đi làm, thời tiết).
2. Kiểm tra tồn kho, kích cỡ và báo giá dựa trên dữ liệu thật.
3. Giải đáp tình trạng đơn hàng nếu khách hỏi.
4. Trả lời về các chính sách của cửa hàng.

QUY TẮC CỐT LÕI:
- Trả lời ngắn gọn, lịch sự, xưng 'dạ/vâng' và gọi khách là 'bạn' hoặc 'chị/anh'.
- KHÔNG BAO GIỜ bịa đặt sản phẩm hoặc đơn hàng. Chỉ tư vấn dựa vào [DỮ LIỆU THỰC TẾ CỦA SHOP] bên dưới.
- Bảng Size quy chuẩn: Size S (40-47kg), Size M (48-54kg), Size L (55-60kg).
- Chính sách: Miễn phí đổi trả 7 ngày. Phí ship đồng giá 30k, Freeship đơn từ 500k.

[DỮ LIỆU THỰC TẾ CỦA SHOP]:
{dynamicContext}

Câu hỏi của khách hàng: ""{userMessage}""
Hãy trả lời khách hàng ngay bây giờ:
";

                // 5. GỌI API GEMINI 1.5 FLASH
                var payload = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = prompt } } }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // ĐẢM BẢO MODEL LÀ gemini-1.5-flash VÀ XOÁ KHOẢNG TRẮNG CỦA API KEY
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro-latest:generateContent?key={_apiKey.Trim()}";

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetail = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GOOGLE ERROR DETAIL]: {errorDetail}");
                    return $"Dạ, hệ thống AI báo lỗi {response.StatusCode}. Xin vui lòng chờ nhân viên hỗ trợ ạ!";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseString);

                var aiText = jsonDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                return aiText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GEMINI API ERROR]: {ex.Message}");
                return "Dạ, hệ thống AI của shop đang bảo trì cập nhật mẫu mới. Chị vui lòng chờ xíu nhé ạ!";
            }
        }
    }
}