# Cấu hình GHN test

Ứng dụng đang dùng môi trường development của GHN:

`https://dev-online-gateway.ghn.vn/shiip/public-api/`

Không ghi Token vào `appsettings.json`. Từ thư mục gốc của repository, lưu cấu hình bằng .NET User Secrets:

```powershell
dotnet user-secrets set "GHN:Token" "TOKEN_TEST_CUA_BAN" --project HuflitShopCore
dotnet user-secrets set "GHN:ShopId" "SHOP_ID_CUA_BAN" --project HuflitShopCore
dotnet user-secrets set "GHN:ShopDistrictId" "MA_QUAN_HUYEN_KHO" --project HuflitShopCore
dotnet user-secrets set "GHN:ShopWardCode" "MA_PHUONG_XA_KHO" --project HuflitShopCore
dotnet user-secrets set "GHN:ShopPhone" "SO_DIEN_THOAI_KHO" --project HuflitShopCore
dotnet user-secrets set "GHN:ShopAddress" "DIA_CHI_KHO" --project HuflitShopCore
dotnet user-secrets set "GHN:ShopWardName" "TEN_PHUONG_XA_KHO" --project HuflitShopCore
dotnet user-secrets set "GHN:ShopDistrictName" "TEN_QUAN_HUYEN_KHO" --project HuflitShopCore
dotnet user-secrets set "GHN:ShopProvinceName" "TEN_TINH_THANH_KHO" --project HuflitShopCore
```

Sản phẩm quần áo chỉ cần nhập `WeightGrams`. Kích thước kiện hàng không nằm trên từng sản phẩm; hệ thống dùng mặc định `30 x 20 x 10 cm` từ các khóa `GHN:DefaultLengthCm`, `GHN:DefaultWidthCm`, `GHN:DefaultHeightCm`.

Luồng kiểm thử:

1. Khách chọn tỉnh, quận/huyện và phường/xã tại Checkout; hệ thống lấy địa chỉ và phí thật từ GHN test.
2. Khi đặt hàng, server gọi lại GHN để kiểm tra phí, không tin phí gửi từ trình duyệt.
3. Admin mở chi tiết đơn, duyệt/đóng gói rồi chọn **Tạo vận đơn GHN**.
4. Admin có thể chọn **Đồng bộ GHN** hoặc mở trang tra cứu bằng mã vận đơn.

Webhook nhận trạng thái tại `POST /api/webhooks/ghn`. Nếu đặt `GHN:WebhookToken`, cấu hình callback GHN theo dạng:

`https://ten-mien-cua-ban/api/webhooks/ghn?token=WEBHOOK_TOKEN_CUA_BAN`

Sau khi đổi cấu hình, khởi động lại ứng dụng.
