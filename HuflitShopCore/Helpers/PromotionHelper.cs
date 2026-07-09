using HuflitShopCore.Models;

namespace HuflitShopCore.Helpers
{
    public static class PromotionHelper
    {
        /// <summary>
        /// Calculates the discounted price for a product based on active auto-apply promotions.
        /// </summary>
        /// <param name="originalPrice">The original price of the product.</param>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="activeAutoPromos">A list of active auto-apply promotions.</param>
        /// <returns>The final discounted price.</returns>
        public static decimal CalculateDiscountedPrice(decimal originalPrice, string productId, List<Promotion> activeAutoPromos)
        {
            var prodPromo = activeAutoPromos.FirstOrDefault(p => p.ApplicableProductId == productId);
            if (prodPromo == null)
            {
                return originalPrice;
            }

            if (string.Equals(prodPromo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(prodPromo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
            {
                decimal discount = originalPrice * (prodPromo.DiscountValue / 100);
                if (prodPromo.MaxDiscountAmount.HasValue && discount > prodPromo.MaxDiscountAmount.Value)
                {
                    discount = prodPromo.MaxDiscountAmount.Value;
                }
                return Math.Max(0, originalPrice - discount);
            }

            return Math.Max(0, originalPrice - prodPromo.DiscountValue);
        }
    }
}
