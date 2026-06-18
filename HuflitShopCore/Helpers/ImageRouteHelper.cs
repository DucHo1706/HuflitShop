using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace HuflitShopCore.Helpers
{
    public static class ImageRouteHelper
    {
        private static readonly ConcurrentDictionary<string, string> SuffixToPathMap = new(StringComparer.OrdinalIgnoreCase);
        private static string _webRootPath = string.Empty;

        public static void Initialize(string webRootPath)
        {
            _webRootPath = webRootPath;
            var productsDir = Path.Combine(webRootPath, "img", "products");
            
            if (Directory.Exists(productsDir))
            {
                var files = Directory.GetFiles(productsDir);
                foreach (var file in files)
                {
                    var filename = Path.GetFileName(file);
                    // Extract the suffix (e.g., "5c223852288.jpeg" from "chân váy 5c223852288.jpeg")
                    var lastSpaceIndex = filename.LastIndexOf(' ');
                    var suffix = lastSpaceIndex >= 0 ? filename[(lastSpaceIndex + 1)..] : filename;
                    
                    SuffixToPathMap[suffix] = "/img/products/" + filename;
                }
            }
        }

        public static string Resolve(string? publicId)
        {
            if (string.IsNullOrEmpty(publicId) || publicId.Contains("Tên_Cloud_Của_Bạn"))
            {
                return "/Client/img/default-product.jpg";
            }

            if (publicId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                publicId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return publicId;
            }

            // Extract the filename from the path
            var filename = Path.GetFileName(publicId);
            
            // Extract suffix (the last part after the last space, or the whole filename)
            var lastSpaceIndex = filename.LastIndexOf(' ');
            var suffix = lastSpaceIndex >= 0 ? filename[(lastSpaceIndex + 1)..] : filename;

            // Try to match the suffix in our dictionary
            if (SuffixToPathMap.TryGetValue(suffix, out var realPath))
            {
                return realPath;
            }

            // Fallback: If it's a relative path starting with / or ~/ or just img, return it cleaned
            if (publicId.StartsWith("~/"))
            {
                return publicId[1..];
            }
            if (publicId.StartsWith('/'))
            {
                return publicId;
            }
            
            // If not found in local files, resolve as Cloudinary asset
            return $"https://res.cloudinary.com/dsamboqwp/image/upload/{publicId}";
        }
    }
}
