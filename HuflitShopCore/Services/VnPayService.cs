using HuflitShopCore.Helpers;
using HuflitShopCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class VnPayService
    {
        private readonly IConfiguration _configuration;
        private readonly OrderService _orderService;

        public VnPayService(IConfiguration configuration, OrderService orderService)
        {
            _configuration = configuration;
            _orderService = orderService;
        }

        public string CreatePaymentUrl(Order order, string returnUrl, string remoteIpAddress)
        {
            string tmnCode = _configuration["VNPAY:TmnCode"] ?? "AMYATERD";
            string hashSecret = _configuration["VNPAY:HashSecret"] ?? "NZPOHSDQOSQWZKNDDHPXPHRYNCPKNXPC";
            string baseUrl = _configuration["VNPAY:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", tmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(order.FinalAmount * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", string.IsNullOrEmpty(remoteIpAddress) ? "127.0.0.1" : remoteIpAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {order.Id}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnpay.AddRequestData("vnp_TxnRef", order.Id);

            return vnpay.CreateRequestUrl(baseUrl, hashSecret);
        }

        public async Task<(string OrderId, bool Success)> ProcessVnPayReturnAsync(IQueryCollection queryData)
        {
            var hashSecret = _configuration["VNPAY:HashSecret"] ?? "NZPOHSDQOSQWZKNDDHPXPHRYNCPKNXPC";
            var vnpay = new VnPayLibrary();

            foreach (var key in queryData.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, queryData[key]!);
                }
            }

            string orderId = vnpay.GetResponseData("vnp_TxnRef");
            string responseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string secureHash = queryData["vnp_SecureHash"]!;

            bool checkSignature = vnpay.ValidateSignature(secureHash, hashSecret);

            if (checkSignature)
            {
                var order = await _orderService.GetOrderForSuccessAsync(orderId);
                if (order != null)
                {
                    if (responseCode == "00")
                    {
                        order.PaymentStatus = 1; // Đã thanh toán
                        await _orderService.UpdateOrderAsync(order);
                        return (orderId, true);
                    }
                    else
                    {
                        await _orderService.UpdateOrderStatusAsync(orderId, 4); // Hủy đơn và trả lại kho
                        return (orderId, false);
                    }
                }
            }

            return (orderId, false);
        }
    }
}
