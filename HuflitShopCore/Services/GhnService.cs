using HuflitShopCore.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuflitShopCore.Services
{
    public sealed class GhnOptions
    {
        public const string SectionName = "GHN";
        public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api/";
        public string Token { get; set; } = string.Empty;
        public int ShopId { get; set; }
        public int ShopDistrictId { get; set; }
        public string ShopWardCode { get; set; } = string.Empty;
        public string ShopName { get; set; } = "Huflit Shop";
        public string ShopPhone { get; set; } = string.Empty;
        public string ShopAddress { get; set; } = string.Empty;
        public string ShopWardName { get; set; } = string.Empty;
        public string ShopDistrictName { get; set; } = string.Empty;
        public string ShopProvinceName { get; set; } = string.Empty;
        public int PaymentTypeId { get; set; } = 1;
        public string RequiredNote { get; set; } = "CHOXEMHANGKHONGTHU";
        public int DefaultLengthCm { get; set; } = 30;
        public int DefaultWidthCm { get; set; } = 20;
        public int DefaultHeightCm { get; set; } = 10;
    }

    public sealed class GhnApiException : Exception
    {
        public GhnApiException(string message) : base(message) { }
    }

    public sealed class GhnProvince
    {
        public int ProvinceId { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
    }

    public sealed class GhnDistrict
    {
        public int DistrictId { get; set; }
        public int ProvinceId { get; set; }
        public string DistrictName { get; set; } = string.Empty;
        public int SupportType { get; set; }
        public int Status { get; set; }
    }

    public sealed class GhnWard
    {
        public string WardCode { get; set; } = string.Empty;
        public int DistrictId { get; set; }
        public string WardName { get; set; } = string.Empty;
        public int SupportType { get; set; }
        public int Status { get; set; }
    }

    public sealed class GhnDestination
    {
        public string ProvinceName { get; init; } = string.Empty;
        public string DistrictName { get; init; } = string.Empty;
        public string WardName { get; init; } = string.Empty;
    }

    public sealed class GhnPackage
    {
        public int Weight { get; init; }
        public int Length { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
    }

    public sealed class GhnQuote
    {
        public int ServiceId { get; init; }
        public int ServiceTypeId { get; init; }
        public string ServiceName { get; init; } = string.Empty;
        public decimal ShippingFee { get; init; }
        public DateTime? ExpectedDeliveryTime { get; init; }
        public GhnPackage Package { get; init; } = new();
    }

    public sealed class GhnCreateResult
    {
        public string OrderCode { get; init; } = string.Empty;
        public decimal TotalFee { get; init; }
        public DateTime? ExpectedDeliveryTime { get; init; }
    }

    public sealed class GhnOrderStatusResult
    {
        public string OrderCode { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public decimal TotalFee { get; init; }
        public DateTime? Leadtime { get; init; }
    }

    public sealed class GhnService
    {
        private readonly HttpClient _httpClient;
        private readonly GhnOptions _options;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public GhnService(HttpClient httpClient, IOptions<GhnOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Token) && _options.ShopId > 0 && _options.ShopDistrictId > 0 && !string.IsNullOrWhiteSpace(_options.ShopWardCode);

        public async Task<IReadOnlyList<GhnProvince>> GetProvincesAsync(CancellationToken cancellationToken = default)
        {
            var data = await SendAsync<List<GhnProvince>>(HttpMethod.Get, "master-data/province", null, false, cancellationToken);
            return data
                .Where(x => !string.Equals(
                    x.ProvinceName.Trim(),
                    "Test - Alert - Tỉnh - 001",
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.ProvinceName)
                .ToList();
        }

        public async Task<IReadOnlyList<GhnDistrict>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default)
        {
            var data = await SendAsync<List<GhnDistrict>>(HttpMethod.Post, "master-data/district", new { province_id = provinceId }, false, cancellationToken);
            return data.Where(x => x.Status == 1 && x.SupportType != 0).OrderBy(x => x.DistrictName).ToList();
        }

        public async Task<IReadOnlyList<GhnWard>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default)
        {
            var data = await SendAsync<List<GhnWard>>(HttpMethod.Post, "master-data/ward", new { district_id = districtId }, false, cancellationToken);
            return data.Where(x => x.Status == 1 && x.SupportType != 0).OrderBy(x => x.WardName).ToList();
        }

        public async Task<GhnDestination> ResolveDestinationAsync(int provinceId, int districtId, string wardCode, CancellationToken cancellationToken = default)
        {
            var province = (await GetProvincesAsync(cancellationToken)).FirstOrDefault(x => x.ProvinceId == provinceId)
                ?? throw new GhnApiException("Tỉnh/Thành phố không hợp lệ trên GHN.");
            var district = (await GetDistrictsAsync(provinceId, cancellationToken)).FirstOrDefault(x => x.DistrictId == districtId)
                ?? throw new GhnApiException("Quận/Huyện không thuộc Tỉnh/Thành phố đã chọn.");
            var ward = (await GetWardsAsync(districtId, cancellationToken)).FirstOrDefault(x => x.WardCode == wardCode)
                ?? throw new GhnApiException("Phường/Xã không thuộc Quận/Huyện đã chọn.");

            return new GhnDestination
            {
                ProvinceName = province.ProvinceName,
                DistrictName = district.DistrictName,
                WardName = ward.WardName
            };
        }

        public GhnPackage BuildPackage(IEnumerable<Cart> items)
        {
            var rows = items.Select(x => new
            {
                x.Quantity,
                Product = x.ProductVariant?.Product
            }).Where(x => x.Product != null && x.Quantity > 0).ToList();

            if (rows.Count == 0) throw new GhnApiException("Không có sản phẩm hợp lệ để tính phí GHN.");

            var package = new GhnPackage
            {
                Weight = rows.Sum(x => Math.Max(300, x.Product!.WeightGrams) * x.Quantity),
                Length = _options.DefaultLengthCm,
                Width = _options.DefaultWidthCm,
                Height = _options.DefaultHeightCm
            };

            if (package.Weight is < 1 or > 50000 || package.Length is < 1 or > 200 || package.Width is < 1 or > 200 || package.Height is < 1 or > 200)
                throw new GhnApiException("Kiện hàng vượt giới hạn hàng nhẹ GHN (50 kg hoặc 200 cm mỗi chiều). Vui lòng kiểm tra thông số đóng gói.");

            return package;
        }

        public async Task<IReadOnlyList<GhnQuote>> GetQuotesAsync(int toDistrictId, string toWardCode, GhnPackage package, decimal insuranceValue, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();
            var services = await SendAsync<List<AvailableService>>(HttpMethod.Post, "v2/shipping-order/available-services", new
            {
                shop_id = _options.ShopId,
                from_district = _options.ShopDistrictId,
                to_district = toDistrictId
            }, false, cancellationToken);

            var quotes = new List<GhnQuote>();
            foreach (var service in services.Where(x => x.ServiceTypeId == 2))
            {
                var fee = await SendAsync<FeeData>(HttpMethod.Post, "v2/shipping-order/fee", new
                {
                    service_id = service.ServiceId,
                    service_type_id = service.ServiceTypeId,
                    from_district_id = _options.ShopDistrictId,
                    from_ward_code = _options.ShopWardCode,
                    to_district_id = toDistrictId,
                    to_ward_code = toWardCode,
                    length = package.Length,
                    width = package.Width,
                    height = package.Height,
                    weight = package.Weight,
                    insurance_value = Math.Min(5000000, Math.Max(0, (int)insuranceValue))
                }, true, cancellationToken);

                DateTime? eta = null;
                try
                {
                    var leadtime = await SendAsync<LeadtimeData>(HttpMethod.Post, "v2/shipping-order/leadtime", new
                    {
                        from_district_id = _options.ShopDistrictId,
                        from_ward_code = _options.ShopWardCode,
                        to_district_id = toDistrictId,
                        to_ward_code = toWardCode,
                        service_id = service.ServiceId
                    }, true, cancellationToken);
                    eta = DateTimeOffset.FromUnixTimeSeconds(leadtime.Leadtime).LocalDateTime;
                }
                catch (GhnApiException)
                {
                    // Báo phí vẫn dùng được nếu endpoint ETA của tài khoản test tạm thời không khả dụng.
                }

                quotes.Add(new GhnQuote
                {
                    ServiceId = service.ServiceId,
                    ServiceTypeId = service.ServiceTypeId,
                    ServiceName = string.IsNullOrWhiteSpace(service.ShortName) ? "GHN Hàng nhẹ" : $"GHN - {service.ShortName}",
                    ShippingFee = fee.Total,
                    ExpectedDeliveryTime = eta,
                    Package = package
                });
            }

            if (quotes.Count == 0) throw new GhnApiException("GHN không có dịch vụ hàng nhẹ phù hợp cho tuyến giao này.");
            return quotes.OrderBy(x => x.ShippingFee).ToList();
        }

        public async Task<GhnCreateResult> CreateOrderAsync(Order order, Shipment shipment, IReadOnlyCollection<OrderDetail> details, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();
            var codAmount = order.PaymentMethodId == "pm-cod" ? (int)Math.Min(50000000m, order.FinalAmount) : 0;
            var content = string.Join(", ", details.Select(x => x.ProductNameSnapshot).Distinct());
            if (content.Length > 2000) content = content[..2000];
            var data = await SendAsync<CreateOrderData>(HttpMethod.Post, "v2/shipping-order/create", new
            {
                payment_type_id = _options.PaymentTypeId,
                required_note = _options.RequiredNote,
                client_order_code = order.Id,
                from_name = _options.ShopName,
                from_phone = _options.ShopPhone,
                from_address = _options.ShopAddress,
                from_ward_name = _options.ShopWardName,
                from_district_name = _options.ShopDistrictName,
                from_province_name = _options.ShopProvinceName,
                to_name = order.ShippingFullName,
                to_phone = order.ShippingPhoneNumber,
                to_address = order.ShippingAddress,
                to_ward_code = order.ShippingWardCode,
                to_district_id = order.ShippingDistrictId,
                to_ward_name = order.ShippingWard,
                to_district_name = order.ShippingDistrict,
                to_province_name = order.ShippingCity,
                cod_amount = codAmount,
                content,
                length = shipment.LengthCm,
                width = shipment.WidthCm,
                height = shipment.HeightCm,
                weight = shipment.WeightGrams,
                insurance_value = (int)Math.Min(5000000m, order.TotalAmount),
                service_id = shipment.ServiceId,
                service_type_id = shipment.ServiceTypeId,
                items = details.Select(x => new
                {
                    name = x.ProductNameSnapshot,
                    code = x.ProductVariantId,
                    quantity = x.Quantity,
                    price = (int)x.PurchasedPrice
                })
            }, true, cancellationToken);

            return new GhnCreateResult
            {
                OrderCode = data.OrderCode,
                TotalFee = ParseDecimal(data.TotalFee),
                ExpectedDeliveryTime = data.ExpectedDeliveryTime
            };
        }

        public async Task CancelOrderAsync(string orderCode, CancellationToken cancellationToken = default)
        {
            var results = await SendAsync<List<CancelData>>(HttpMethod.Post, "v2/switch-status/cancel", new { order_codes = new[] { orderCode } }, true, cancellationToken);
            var result = results.FirstOrDefault();
            if (result == null || !result.Result) throw new GhnApiException(result?.Message ?? "GHN không thể hủy vận đơn.");
        }

        public async Task<GhnOrderStatusResult> GetOrderStatusAsync(string orderCode, CancellationToken cancellationToken = default)
        {
            var data = await SendAsync<OrderDetailData>(HttpMethod.Post, "v2/shipping-order/detail", new { order_code = orderCode }, false, cancellationToken);
            return new GhnOrderStatusResult
            {
                OrderCode = data.OrderCode,
                Status = data.Status,
                TotalFee = data.TotalFee,
                Leadtime = data.Leadtime
            };
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, bool includeShopId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.Token)) throw new GhnApiException("Chưa cấu hình GHN:Token.");
            using var request = new HttpRequestMessage(method, path);
            request.Headers.TryAddWithoutValidation("Token", _options.Token);
            if (includeShopId) request.Headers.TryAddWithoutValidation("ShopId", _options.ShopId.ToString());
            if (body != null) request.Content = JsonContent.Create(body);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new GhnApiException($"Không thể kết nối GHN: {ex.Message}");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new GhnApiException("GHN phản hồi quá thời gian cho phép. Vui lòng thử lại.");
            }

            using (response)
            {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            GhnResponse<T>? envelope;
            try { envelope = JsonSerializer.Deserialize<GhnResponse<T>>(json, _jsonOptions); }
            catch (JsonException) { throw new GhnApiException($"GHN trả về dữ liệu không hợp lệ (HTTP {(int)response.StatusCode})."); }

            if (!response.IsSuccessStatusCode || envelope == null || envelope.Code != 200 || envelope.Data == null)
                throw new GhnApiException(envelope?.Message ?? $"Không thể kết nối GHN (HTTP {(int)response.StatusCode}).");
            return envelope.Data;
            }
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured) throw new GhnApiException("Chưa cấu hình đủ GHN Token, ShopId, ShopDistrictId và ShopWardCode.");
        }

        private static decimal ParseDecimal(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
            return decimal.TryParse(value.ToString(), out var parsed) ? parsed : 0;
        }

        private sealed class GhnResponse<T>
        {
            public int Code { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }

        private sealed class AvailableService
        {
            [JsonPropertyName("service_id")]
            public int ServiceId { get; set; }
            [JsonPropertyName("short_name")]
            public string ShortName { get; set; } = string.Empty;
            [JsonPropertyName("service_type_id")]
            public int ServiceTypeId { get; set; }
        }

        private sealed class FeeData { public decimal Total { get; set; } }
        private sealed class LeadtimeData { public long Leadtime { get; set; } }
        private sealed class CreateOrderData
        {
            [JsonPropertyName("order_code")]
            public string OrderCode { get; set; } = string.Empty;
            [JsonPropertyName("total_fee")]
            public JsonElement TotalFee { get; set; }
            [JsonPropertyName("expected_delivery_time")]
            public DateTime? ExpectedDeliveryTime { get; set; }
        }
        private sealed class CancelData
        {
            public bool Result { get; set; }
            public string Message { get; set; } = string.Empty;
        }
        private sealed class OrderDetailData
        {
            [JsonPropertyName("order_code")]
            public string OrderCode { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            [JsonPropertyName("total_fee")]
            public decimal TotalFee { get; set; }
            public DateTime? Leadtime { get; set; }
        }
    }
}
