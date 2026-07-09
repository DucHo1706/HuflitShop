using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HuflitShopCore.Data;
using HuflitShopCore.Models;

namespace HuflitShopCore.Services
{
    public interface IAprioriService
    {
        Task<List<Product>> GetRecommendationsAsync(string currentProductId, int limit = 4);
        Task<List<Product>> GetCartRecommendationsAsync(List<string> cartProductIds, int limit = 4);
    }

    public class AprioriService : IAprioriService
    {
        private readonly AppDbContext _context;
        // Cache the association rules to prevent recalculating on every request
        private static List<AssociationRule> _cachedRules = new();
        private static DateTime _cacheExpiration = DateTime.MinValue;
        private static readonly object _cacheLock = new();

        public AprioriService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetRecommendationsAsync(string currentProductId, int limit = 4)
        {
            return await GetCartRecommendationsAsync(new List<string> { currentProductId }, limit);
        }

        public async Task<List<Product>> GetCartRecommendationsAsync(List<string> cartProductIds, int limit = 4)
        {
            if (cartProductIds == null || !cartProductIds.Any())
            {
                return await GetDefaultPopularProductsAsync(limit);
            }

            var rules = await GetOrCalculateRulesAsync();
            
            // Find rules where the antecedent (ItemA) is in the user's cart,
            // and the consequent (ItemB) is NOT in the user's cart.
            var recommendedProductIds = rules
                .Where(r => cartProductIds.Contains(r.Antecedent) && !cartProductIds.Contains(r.Consequent))
                .OrderByDescending(r => r.Confidence)
                .ThenByDescending(r => r.Support)
                .Select(r => r.Consequent)
                .Distinct()
                .Take(limit)
                .ToList();

            var products = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => recommendedProductIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();

            // Order products according to the recommendation ranking
            var orderedProducts = recommendedProductIds
                .Select(id => products.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .ToList();

            // If not enough recommendations, pad with popular/new products
            if (orderedProducts.Count < limit)
            {
                var padCount = limit - orderedProducts.Count;
                var currentIds = orderedProducts.Select(p => p.Id).Concat(cartProductIds).ToList();
                
                var padding = await _context.Products
                    .Include(p => p.ProductImages)
                    .Where(p => !p.IsDeleted && !currentIds.Contains(p.Id))
                    .OrderByDescending(p => p.CreatedAt) // newest fallback
                    .Take(padCount)
                    .ToListAsync();
                
                orderedProducts.AddRange(padding);
            }

            return orderedProducts;
        }

        private async Task<List<Product>> GetDefaultPopularProductsAsync(int limit)
        {
            return await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        private async Task<List<AssociationRule>> GetOrCalculateRulesAsync()
        {
            lock (_cacheLock)
            {
                if (_cachedRules.Any() && DateTime.Now < _cacheExpiration)
                {
                    return _cachedRules;
                }
            }

            // Recalculate rules (minSupport: 2%, minConfidence: 10% to ensure results for small dev datasets)
            var rules = await CalculateAprioriRulesAsync(minSupport: 0.02, minConfidence: 0.1);

            // Tự động sinh mã giảm giá combo cho các luật kết hợp mạnh
            try
            {
                await AutoGenerateComboPromotionsAsync(rules);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error auto-generating combo promotions: {ex.Message}");
            }

            lock (_cacheLock)
            {
                _cachedRules = rules;
                _cacheExpiration = DateTime.Now.AddMinutes(30); // Cache for 30 minutes
            }

            return rules;
        }

        private async Task AutoGenerateComboPromotionsAsync(List<AssociationRule> strongRules)
        {
            var now = DateTime.Now;
            var comboPromos = await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now && !string.IsNullOrEmpty(p.ComboProductIds))
                .ToListAsync();

            // Chọn các luật kết hợp mạnh (Độ tin cậy >= 15%) để tạo combo
            var topRules = strongRules
                .Where(r => r.Confidence >= 0.15)
                .OrderByDescending(r => r.Confidence)
                .Take(5)
                .ToList();

            bool hasChanges = false;
            foreach (var rule in topRules)
            {
                // Kiểm tra xem đã có combo nào cho 2 sản phẩm này chưa
                bool exists = comboPromos.Any(p => 
                    p.ComboProductIds.Contains(rule.Antecedent) && p.ComboProductIds.Contains(rule.Consequent));

                if (!exists)
                {
                    string randomHex = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                    string promoCode = $"AI-COMBO-{randomHex}";

                    var newPromo = new Promotion
                    {
                        Id = Guid.NewGuid().ToString(),
                        PromoCode = promoCode,
                        DiscountType = "Percentage",
                        DiscountValue = 10, // Giảm 10% cho combo
                        MinOrderAmount = 0,
                        StartDate = now,
                        EndDate = now.AddDays(7), // Có hiệu lực trong 7 ngày
                        IsActive = true,
                        IsAutoApply = true,
                        ComboProductIds = $"{rule.Antecedent},{rule.Consequent}"
                    };

                    _context.Promotions.Add(newPromo);
                    comboPromos.Add(newPromo); // Thêm vào danh sách tạm để tránh tạo trùng trong cùng 1 đợt quét
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task<List<AssociationRule>> CalculateAprioriRulesAsync(double minSupport, double minConfidence)
        {
            // 1. Fetch transactions (orders containing multiple items)
            var orderDetails = await _context.OrderDetails
                .Include(od => od.ProductVariant)
                .Where(od => od.ProductVariant != null)
                .Select(od => new { od.OrderId, od.ProductVariant.ProductId })
                .ToListAsync();

            var transactions = orderDetails
                .GroupBy(od => od.OrderId)
                .Select(g => g.Select(od => od.ProductId).Distinct().ToList())
                .Where(list => list.Count > 1)
                .ToList();

            int totalTransactions = transactions.Count;
            if (totalTransactions == 0) return new List<AssociationRule>();

            // 2. Count support for 1-itemsets (L1 candidates)
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

            // Filter L1 frequent itemsets
            var L1 = itemCounts
                .Where(x => (double)x.Value / totalTransactions >= minSupport)
                .ToDictionary(x => x.Key, x => x.Value);

            // 3. Generate candidate 2-itemsets (C2) and count support (L2)
            var pairCounts = new Dictionary<Tuple<string, string>, int>();
            var frequentItemsList = L1.Keys.ToList();

            for (int i = 0; i < frequentItemsList.Count; i++)
            {
                for (int j = i + 1; j < frequentItemsList.Count; j++)
                {
                    var itemA = frequentItemsList[i];
                    var itemB = frequentItemsList[j];

                    // Count co-occurrence
                    int coOccurrenceCount = 0;
                    foreach (var transaction in transactions)
                    {
                        if (transaction.Contains(itemA) && transaction.Contains(itemB))
                        {
                            coOccurrenceCount++;
                        }
                    }

                    double support = (double)coOccurrenceCount / totalTransactions;
                    if (support >= minSupport)
                    {
                        var pair = Tuple.Create(itemA, itemB);
                        pairCounts[pair] = coOccurrenceCount;
                    }
                }
            }

            // 4. Generate association rules from L2
            var rules = new List<AssociationRule>();
            foreach (var kvp in pairCounts)
            {
                var itemA = kvp.Key.Item1;
                var itemB = kvp.Key.Item2;
                int coOccurrenceCount = kvp.Value;
                double support = (double)coOccurrenceCount / totalTransactions;

                // Rule A -> B
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

                // Rule B -> A
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

    public class AssociationRule
    {
        public string Antecedent { get; set; } = string.Empty;
        public string Consequent { get; set; } = string.Empty;
        public double Support { get; set; }
        public double Confidence { get; set; }
    }
}
