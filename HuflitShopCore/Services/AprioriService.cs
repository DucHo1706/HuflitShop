using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HuflitShopCore.Data;
using HuflitShopCore.Models;

namespace HuflitShopCore.Services
{
    /* 

     * 
     * 1. HÓA ĐƠN CŨ (TRANSACTIONS): 
     *    - Là lịch sử các đơn hàng khách hàng đã mua trước đó.
     * 
     * 2. LUẬT KẾT HỢP (ASSOCIATION RULE)
     *    - Là những "QUY LUẬT" đúc rút từ đống hóa đơn cũ đó.
     *    - Ví dụ: AI phát hiện thấy cứ 10 người mua [Áo sơ mi] thì có 8 người mua thêm [Cà vạt].
     *      Từ đó sinh ra một quy luật: Mua Áo sơ mi -> Gợi ý mua Cà vạt (độ tin cậy 80%).
     * 
     * 3. DANH SÁCH LUẬT (LIST OF RULES) 
     *    - Là một tập hợp tất cả các quy luật mà AI đã quét và rút ra được từ hàng ngàn đơn hàng lịch sử.
     *      * Luật 1: Mua Áo sơ mi -> Gợi ý mua Cà vạt (Tin cậy 80%)
     *      * Luật 2: Mua Quần tây -> Gợi ý mua Thắt lưng (Tin cậy 60%)
     *      * Luật 3: Mua Giày da -> Gợi ý mua Xi đánh giày (Tin cậy 90%)
     * 
     * 4. HỆ THỐNG DÙNG THẾ NÀO?
     *    - Khi khách hàng đang xem [Giày da] trên web, hệ thống mở "Danh sách luật" này ra tìm xem có luật nào 
     *      bắt đầu bằng [Giày da] không. 
     *    - Nó tìm thấy Luật 3, nên ngay lập tức đề xuất sản phẩm [Xi đánh giày] lên màn hình để dụ khách mua kèm!

     */

    // Giao diện định nghĩa các dịch vụ gợi ý sản phẩm sử dụng thuật toán trí tuệ nhân tạo Apriori
    public interface IAprioriService
    {
        // Gợi ý sản phẩm mua kèm khi đang xem một sản phẩm cụ thể
        Task<List<Product>> GetRecommendationsAsync(string currentProductId, int limit = 4);
        
        // Gợi ý sản phẩm mua kèm dựa trên danh sách sản phẩm hiện có trong giỏ hàng
        Task<List<Product>> GetCartRecommendationsAsync(List<string> cartProductIds, int limit = 4);
    }

    // Lớp triển khai thuật toán khai phá luật kết hợp Apriori
    public class AprioriService : IAprioriService
    {
        private readonly AppDbContext _context;
        
        // Cơ chế bộ nhớ đệm (Cache) để lưu trữ các luật kết hợp đã tính toán.
        // Giúp tránh việc quét cơ sở dữ liệu hóa đơn liên tục ở mỗi lượt tải trang của khách hàng.
        private static List<AssociationRule> _cachedRules = new();
        private static DateTime _cacheExpiration = DateTime.MinValue;
        private static readonly object _cacheLock = new();

        public AprioriService(AppDbContext context)
        {
            _context = context;
        }

        // Gợi ý sản phẩm mua kèm cho trang chi tiết sản phẩm.
        // currentProductId ID sản phẩm đang xem
        // limit Số lượng tối đa muốn lấy
        // Đệ quy 
        public async Task<List<Product>> GetRecommendationsAsync(string currentProductId, int limit = 4)
        {
            //  yêu cầu xử lý bằng cách coi như giỏ hàng hiện tại đang có 1 sản phẩm này
            return await GetCartRecommendationsAsync(new List<string> { currentProductId }, limit);
        }

        // Gợi ý sản phẩm mua kèm dựa trên danh sách các sản phẩm đang có trong giỏ hàng.
     
        // name="cartProductIds">Danh sách ID các sản phẩm trong giỏ
        // name="limit">Số lượng gợi ý tối đa
        public async Task<List<Product>> GetCartRecommendationsAsync(List<string> cartProductIds, int limit = 4)
        {
            // Nếu giỏ hàng trống, lấy danh sách các sản phẩm mới nhất làm mặc định
            if (cartProductIds == null || !cartProductIds.Any())
            {
                return await GetDefaultPopularProductsAsync(limit);
            }

            // Lấy danh sách  kết hợp 
            var rules = await GetOrCalculateRulesAsync();
            
            // Lọc tìm các  kết hợp thỏa mãn:
            // 1. Sản phẩm điều kiện phải nằm trong giỏ hàng của khách.
            // 2. Sản phẩm gợi ý  KHÔNG được nằm trong giỏ hàng (tránh gợi ý món khách đã chọn).
            var recommendedProductIds = rules
                .Where(r => cartProductIds.Contains(r.Antecedent) && !cartProductIds.Contains(r.Consequent))
                .OrderByDescending(r => r.Confidence) // Ưu tiên luật có độ tin cậy Confidence cao nhất
                .ThenByDescending(r => r.Support)    // Sau đó ưu tiên luật có độ hỗ trợ cao nhất
                .Select(r => r.Consequent)
                .Distinct()
                .Take(limit)
                .ToList();

            // Truy vấn thông tin chi tiết (bao gồm cả hình ảnh) của các sản phẩm được đề xuất
            var products = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => recommendedProductIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();

            // Sắp xếp các đối tượng sản phẩm trả về đúng theo thứ tự bảng xếp hạng gợi ý ở trên
            var orderedProducts = recommendedProductIds
                .Select(id => products.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .ToList();

            // Nếu thuật toán tìm được ít hơn số lượng yêu cầu,
            // hệ thống sẽ tự động bù đắp bằng các sản phẩm mới nhất .
            if (orderedProducts.Count < limit)
            {
                var padCount = limit - orderedProducts.Count;
                var currentIds = orderedProducts.Select(p => p.Id).Concat(cartProductIds).ToList();
                
                var padding = await _context.Products
                    .Include(p => p.ProductImages)
                    .Where(p => !p.IsDeleted && !currentIds.Contains(p.Id))
                    .OrderByDescending(p => p.CreatedAt) // Lấy các sản phẩm mới nhất làm dự phòng
                    .Take(padCount)
                    .ToListAsync();
                
                orderedProducts.AddRange(padding);
            }

            return orderedProducts;
        }

        // Lấy danh sách sản phẩm mặc định (mới nhất) làm dự phòng khi không có giỏ hàng hoặc thiếu gợi ý.

        private async Task<List<Product>> GetDefaultPopularProductsAsync(int limit)
        {
            return await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // Lấy luật kết hợp từ bộ đệm RAM. Nếu chưa có hoặc hết hạn (sau 30 phút), tiến hành tính toán lại.
        // 
        private async Task<List<AssociationRule>> GetOrCalculateRulesAsync()
        {
            // Sử dụng lock để đồng bộ luồng, tránh việc nhiều request cùng lúc chạy tính toán gây quá tải CPU
            lock (_cacheLock)
            {
                if (_cachedRules.Any() && DateTime.Now < _cacheExpiration)
                {
                    return _cachedRules;
                }
            }

            // Gọi thuật toán tính luật kết hợp với thiết lập mặc định (Support >= 2%, Confidence >= 10%)
            var rules = await CalculateAprioriRulesAsync(minSupport: 0.02, minConfidence: 0.1);

            // Từ các luật kết hợp mạnh vừa tìm được, tiến hành tự sinh mã giảm giá combo lưu vào DB
            try
            {
                await AutoGenerateComboPromotionsAsync(rules);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error auto-generating combo promotions: {ex.Message}");
            }

            // Lưu kết quả vào bộ đệm RAM và gia hạn 5 giây hoạt động phục vụ test nhanh
            lock (_cacheLock)
            {
                _cachedRules = rules;
                _cacheExpiration = DateTime.Now.AddSeconds(5);
            }

            return rules;
        }

        // Tự động tạo các chiến dịch khuyến mãi Combo giảm giá dựa trên phân tích luật kết hợp mạnh.
        // name="strongRules">Danh sách các luật kết hợp tìm được
        private async Task AutoGenerateComboPromotionsAsync(List<AssociationRule> strongRules)
        {
            var now = DateTime.Now;
            // Lấy danh sách các khuyến mãi Combo đang hoạt động
            var comboPromos = await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now && !string.IsNullOrEmpty(p.ComboProductIds))
                .ToListAsync();

            // Chọn ra tối đa 5 luật kết hợp mạnh nhất có Độ tin cậy mua kèm (Confidence) từ 15% trở lên
            var topRules = strongRules
                .Where(r => r.Confidence >= 0.15)
                .OrderByDescending(r => r.Confidence)
                .Take(5)
                .ToList();

            bool hasChanges = false;
            foreach (var rule in topRules)
            {
                // Kiểm tra xem database đã có sẵn chương trình khuyến mãi Combo cho cặp 2 sản phẩm này chưa
                bool exists = comboPromos.Any(p => 
                    p.ComboProductIds != null && p.ComboProductIds.Contains(rule.Antecedent) && p.ComboProductIds.Contains(rule.Consequent));

                if (!exists)
                {
                    // Tự sinh mã giảm giá ngẫu nhiên ví dụ: AI-COMBO-E4BA
                    string randomHex = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                    string promoCode = $"AI-COMBO-{randomHex}";

                    // Tạo bản ghi khuyến mãi mới (Giảm giá 10% khi mua cả 2 sản phẩm này cùng lúc)
                    var newPromo = new Promotion
                    {
                        Id = Guid.NewGuid().ToString(),
                        PromoCode = promoCode,
                        DiscountType = "Percentage",
                        DiscountValue = 10, // Mặc định giảm 10%
                        MinOrderAmount = 0,
                        StartDate = now,
                        EndDate = now.AddDays(7), // Mã có hạn dùng trong vòng 7 ngày
                        IsActive = true,
                        IsAutoApply = true, // Tự động áp dụng giảm giá khi giỏ hàng chứa cả 2 sản phẩm
                        ComboProductIds = $"{rule.Antecedent},{rule.Consequent}"
                    };

                    _context.Promotions.Add(newPromo);
                    comboPromos.Add(newPromo); // Thêm tạm vào danh sách để tránh sinh trùng lặp trong đợt quét này
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        // THUẬT TOÁN APRIORI: Tính toán các luật kết hợp dựa trên lịch sử mua hàng thực tế
        //  name="minSupport">Độ hỗ trợ tối thiểu (tỉ lệ xuất hiện chung trên tổng số đơn)
        // name="minConfidence">Độ tin cậy tối thiểu (xác suất mua kèm B sau khi chọn A)
        private async Task<List<AssociationRule>> CalculateAprioriRulesAsync(double minSupport, double minConfidence)
        {
            // Bước 1: Quét Database lấy thông tin các sản phẩm đã mua của từng hóa đơn
            var orderDetails = await _context.OrderDetails
                .Include(od => od.ProductVariant)
                .Where(od => od.ProductVariant != null)
                .Select(od => new { od.OrderId, od.ProductVariant.ProductId })
                .ToListAsync();

            // Gom nhóm các sản phẩm theo từng đơn hàng. Chỉ xét các hóa đơn có từ 2 sản phẩm trở lên.
            var transactions = orderDetails
                .GroupBy(od => od.OrderId)
                .Select(g => g.Select(od => od.ProductId).Distinct().ToList())
                .Where(list => list.Count > 1)
                .ToList();

            int totalTransactions = transactions.Count;
            if (totalTransactions == 0) return new List<AssociationRule>();

            // Bước 2: Tìm tập phổ biến 1 phần tử (L1)
            // Đếm tần suất xuất hiện của từng sản phẩm riêng lẻ trong các giao dịch
            var itemCounts = new Dictionary<string, int>();
            foreach (var transaction in transactions)
            {
                foreach (var item in transaction)
                {
                    if (itemCounts.ContainsKey(item))
                        itemCounts[item]++;
                    else
                        itemCounts[item] = 1;
                }
            }

            // Lọc giữ lại các sản phẩm có tỉ lệ xuất hiện đạt minSupport (2% trở lên)
            var L1 = itemCounts
                .Where(x => (double)x.Value / totalTransactions >= minSupport)
                .ToDictionary(x => x.Key, x => x.Value);

            // Bước 3: Tìm tập phổ biến 2 phần tử (L2)
            // Ghép đôi các sản phẩm trong danh sách L1 và đếm số lần cặp đôi này cùng xuất hiện trong một đơn hàng
            var pairCounts = new Dictionary<Tuple<string, string>, int>();
            var frequentItemsList = L1.Keys.ToList();

            for (int i = 0; i < frequentItemsList.Count; i++)
            {
                for (int j = i + 1; j < frequentItemsList.Count; j++)
                {
                    var itemA = frequentItemsList[i];
                    var itemB = frequentItemsList[j];

                    // Đếm số đơn hàng chứa cả sản phẩm A và sản phẩm B
                    int coOccurrenceCount = 0;
                    foreach (var transaction in transactions)
                    {
                        if (transaction.Contains(itemA) && transaction.Contains(itemB))
                        {
                            coOccurrenceCount++;
                        }
                    }

                    // Nếu tỉ lệ xuất hiện chung đạt minSupport, lưu lại cặp sản phẩm này
                    double support = (double)coOccurrenceCount / totalTransactions;
                    if (support >= minSupport)
                    {
                        var pair = Tuple.Create(itemA, itemB);
                        pairCounts[pair] = coOccurrenceCount;
                    }
                }
            }

            // Bước 4: Tạo ra các luật kết hợp (Association Rules) từ danh sách L2
            var rules = new List<AssociationRule>();
            foreach (var kvp in pairCounts)
            {
                var itemA = kvp.Key.Item1;
                var itemB = kvp.Key.Item2;
                int coOccurrenceCount = kvp.Value;
                double support = (double)coOccurrenceCount / totalTransactions;

                // Tạo luật A -> B (Nếu mua A sẽ gợi ý mua kèm B)
                if (L1.ContainsKey(itemA))
                {
                    double confidenceAtoB = (double)coOccurrenceCount / L1[itemA];
                    if (confidenceAtoB >= minConfidence)
                    {
                        rules.Add(new AssociationRule
                        {
                            Antecedent = itemA,
                            Consequent = itemB,
                            Support = support,
                            Confidence = confidenceAtoB
                        });
                    }
                }

                // Tạo luật B -> A (Nếu mua B sẽ gợi ý mua kèm A)
                if (L1.ContainsKey(itemB))
                {
                    double confidenceBtoA = (double)coOccurrenceCount / L1[itemB];
                    if (confidenceBtoA >= minConfidence)
                    {
                        rules.Add(new AssociationRule
                        {
                            Antecedent = itemB,
                            Consequent = itemA,
                            Support = support,
                            Confidence = confidenceBtoA
                        });
                    }
                }
            }

            return rules;
        }
    }

    // Định nghĩa cấu trúc của một Luật kết hợp được sinh ra từ thuật toán Apriori
    public class AssociationRule
    {
        public string Antecedent { get; set; } = string.Empty; // Vế trái (Sản phẩm khách hàng đã chọn)
        public string Consequent { get; set; } = string.Empty; // Vế phải (Sản phẩm gợi ý mua kèm)
        public double Support { get; set; }                    // Độ hỗ trợ của cặp sản phẩm này
        public double Confidence { get; set; }                 // Độ tin cậy của luật kết hợp này
    }
}
