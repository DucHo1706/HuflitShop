using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HuflitShopCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


public class AIChatService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly AppDbContext _context;

    public AIChatService(HttpClient httpClient, IConfiguration configuration, AppDbContext context)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleAI:ApiKey"] ?? string.Empty;
        _context = context;
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
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.ProductName)
                    .Take(40)
                    .Select(p => $"- {p.ProductName} | Giá: {p.CurrentPrice:N0}đ | Link: /Product/Details/{p.Id}")
                    .ToListAsync();

                dynamicContext += "DANH SÁCH SẢN PHẨM HIỆN CÓ TẠI SHOP:\n";
                dynamicContext += products.Any() ? string.Join("\n", products) : "Hiện không có sản phẩm nào khả dụng.\n";
                dynamicContext += "\n";
            }

            // 4. Xây dựng Prompt
            string prompt = $@"
Bạn là nhân viên tư vấn thời trang AI (Stylist AI) chuyên nghiệp của HuflitShop. Tên bạn là 'Trợ lý Huflit'.
Nhiệm vụ của bạn: Gợi ý phối đồ, báo giá và kiểm tra tồn kho.

QUY TẮC CỐT LÕI:
- Trả lời bằng tiếng Việt, ngắn gọn, lịch sự, xưng 'dạ/vâng' và gọi khách là 'bạn' hoặc 'chị/anh'.
- KHÔNG BAO GIỜ bịa đặt sản phẩm. Chỉ tư vấn dựa vào [DỮ LIỆU THỰC TẾ CỦA SHOP] bên dưới.
- BẮT BUỘC GẮN LINK: Khi nhắc đến tên một sản phẩm, bạn PHẢI tạo một đường dẫn có thể click được theo cú pháp Markdown là [Tên sản phẩm](Link).

[DỮ LIỆU THỰC TẾ CỦA SHOP]:
{dynamicContext}

Câu hỏi của khách hàng: ""{userMessage}""
Hãy trả lời khách hàng ngay bây giờ:
";

            // 5. CẤU HÌNH GỌI GROQ API (MIỄN PHÍ)
            var requestUrl = "https://api.groq.com/openai/v1/chat/completions";

            var payload = new
            {
                model = "llama-3.1-8b-instant", 
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.7
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // Thiết lập Header Authorization cho Groq
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey.Trim()}");

            var response = await _httpClient.PostAsync(requestUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetail = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[LỖI GROQ API - STATUS: {response.StatusCode}]: {errorDetail}");
                return $"Dạ, hệ thống AI báo lỗi {response.StatusCode}. Xin vui lòng thử lại sau ạ!";
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseString);

            // Bóc tách dữ liệu theo chuẩn OpenAI / Groq
            var aiText = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return aiText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LỖI HỆ THỐNG]: {ex.Message}");
            return "Dạ, hệ thống AI của shop đang bảo trì cập nhật mẫu mới. Bạn vui lòng chờ xíu nhé ạ!";
        }
    }
}