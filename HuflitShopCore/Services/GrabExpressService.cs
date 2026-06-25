using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class GrabExpressServiceOption
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceCode { get; set; } = string.Empty;
        public decimal ShippingFee { get; set; }
        public string EstimatedTime { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
    }

    public class GrabBookResult
    {
        public string DeliveryId { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string TrackingUrl { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    public class GrabExpressService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GrabExpressService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task<List<GrabExpressServiceOption>> GetQuotesAsync(string destCity, string destDistrict, string destSpecificAddress, decimal orderTotal)
        {
            var shopCity = _configuration["GrabExpress:ShopCity"] ?? "Hồ Chí Minh";
            var shopDistrict = _configuration["GrabExpress:ShopDistrict"] ?? "Quận 10";
            var clientId = _configuration["GrabExpress:ClientId"];

            var options = new List<GrabExpressServiceOption>();

            // 1. Phương thức Giao hàng tiêu chuẩn (GHN/GHTK) - Luôn có
            decimal standardFee = 25000m;
            string standardEta = "1 - 2 ngày";
            
            // Nếu giao đi tỉnh khác ngoài HCM
            if (!string.IsNullOrEmpty(destCity) && !destCity.Contains(shopCity, StringComparison.OrdinalIgnoreCase))
            {
                standardFee = 40000m;
                standardEta = "3 - 5 ngày";
            }
            
            options.Add(new GrabExpressServiceOption
            {
                ServiceName = "Giao hàng Tiêu chuẩn (GHN/GHTK)",
                ServiceCode = "carrier-standard",
                ShippingFee = standardFee,
                EstimatedTime = standardEta
            });

            // 2. Các phương thức GrabExpress - Chỉ khả dụng nếu cùng Tỉnh/Thành phố
            if (!string.IsNullOrEmpty(destCity) && destCity.Contains(shopCity, StringComparison.OrdinalIgnoreCase))
            {
                // Nếu có API key thật và không dùng giả lập
                if (!string.IsNullOrEmpty(clientId) && clientId != "YOUR_CLIENT_ID")
                {
                    try
                    {
                        return await CallRealGrabExpressQuotesApiAsync(destSpecificAddress, destDistrict, destCity);
                    }
                    catch (Exception)
                    {
                        // Fallback sang mock nếu API thật bị lỗi để không làm gián đoạn thanh toán
                    }
                }

                // CHẾ ĐỘ GIẢ LẬP MOCK SANDBOX THÔNG MINH
                double distance = EstimateDistance(shopDistrict, destDistrict);
                
                // GrabExpress Hỏa tốc: 15k cho 2km đầu, 5k cho mỗi km tiếp theo
                decimal instantFee = 15000m;
                if (distance > 2)
                {
                    instantFee += (decimal)(distance - 2) * 5000m;
                }
                // Làm tròn hàng nghìn
                instantFee = Math.Round(instantFee / 1000m, 0) * 1000m;

                options.Add(new GrabExpressServiceOption
                {
                    ServiceName = "GrabExpress Hỏa Tốc (Xe máy)",
                    ServiceCode = "grab-instant",
                    ShippingFee = instantFee,
                    EstimatedTime = "30 - 50 phút"
                });

                options.Add(new GrabExpressServiceOption
                {
                    ServiceName = "Ahamove Hỏa Tốc (Xe máy)",
                    ServiceCode = "ahamove-instant",
                    ShippingFee = Math.Max(15000m, instantFee - 2000m),
                    EstimatedTime = "25 - 45 phút"
                });

                options.Add(new GrabExpressServiceOption
                {
                    ServiceName = "GrabExpress Tiết kiệm (Trong ngày)",
                    ServiceCode = "grab-sameday",
                    ShippingFee = 22000m,
                    EstimatedTime = "2 - 4 tiếng"
                });
            }

            return options;
        }

        public async Task<GrabBookResult> BookDeliveryAsync(string carrierCode, string receiverName, string receiverPhone, string fullAddress)
        {
            var clientId = _configuration["GrabExpress:ClientId"];
            if (!string.IsNullOrEmpty(clientId) && clientId != "YOUR_CLIENT_ID")
            {
                try
                {
                    return await CallRealGrabExpressBookApiAsync(carrierCode, receiverName, receiverPhone, fullAddress);
                }
                catch (Exception)
                {
                    // Fallback to mock on error
                }
            }

            // CHẾ ĐỘ GIẢ LẬP MOCK SANDBOX ĐẶT XE
            var random = new Random();
            var deliveryId = "GRAB-" + random.Next(10000000, 99999999);
            var trackingNumber = "GE-" + random.Next(100000, 999999);
            
            // Danh sách tài xế giả lập
            var drivers = new[] { "Nguyễn Văn Hùng", "Trần Minh Tâm", "Lê Hoàng Nam", "Phạm Quốc Bảo", "Nguyễn Tuấn Hải" };
            var plates = new[] { "59-K1 829.12", "59-T2 721.45", "50-Y1 902.34", "59-S3 881.02", "59-P1 665.89" };
            var driverIdx = random.Next(drivers.Length);

            return new GrabBookResult
            {
                Success = true,
                DeliveryId = deliveryId,
                TrackingNumber = trackingNumber,
                TrackingUrl = "/Order/Tracking?id=" + trackingNumber,
                DriverName = drivers[driverIdx],
                DriverPhone = "090" + random.Next(1000000, 9999999),
                LicensePlate = plates[driverIdx]
            };
        }

        private double EstimateDistance(string originDistrict, string destDistrict)
        {
            if (string.IsNullOrEmpty(destDistrict)) return 10.0;
            if (destDistrict.Contains(originDistrict, StringComparison.OrdinalIgnoreCase))
            {
                return 1.5; // Cùng quận
            }

            // Danh sách quận lân cận Quận 10
            var nearDistricts = new[] { "Quận 3", "Quận 1", "Quận 5", "Quận 11", "Tân Bình", "Phú Nhuận" };
            foreach (var d in nearDistricts)
            {
                if (destDistrict.Contains(d, StringComparison.OrdinalIgnoreCase))
                    return 4.5;
            }

            // Danh sách quận trung bình
            var midDistricts = new[] { "Quận 4", "Quận 6", "Quận 7", "Quận 8", "Bình Thạnh", "Gò Vấp", "Tân Phú" };
            foreach (var d in midDistricts)
            {
                if (destDistrict.Contains(d, StringComparison.OrdinalIgnoreCase))
                    return 8.0;
            }

            // Quận ở xa
            return 15.0;
        }

        // Khung API GrabExpress Sandbox thực tế
        private async Task<List<GrabExpressServiceOption>> CallRealGrabExpressQuotesApiAsync(string address, string district, string city)
        {
            await Task.Delay(100);
            throw new NotImplementedException("Chưa cấu hình API credentials thật. Sử dụng Mocking.");
        }

        private async Task<GrabBookResult> CallRealGrabExpressBookApiAsync(string carrierCode, string receiverName, string receiverPhone, string fullAddress)
        {
            await Task.Delay(100);
            throw new NotImplementedException("Chưa cấu hình API credentials thật. Sử dụng Mocking.");
        }
    }
}
