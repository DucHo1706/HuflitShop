# HuflitShopCore - Luồng đặt hàng (Customer) + giả lập thanh toán

Tài liệu này mô tả quy trình end-to-end theo đúng logic hiện có trong:
- `Controllers/ProductController.cs` (AddCart, BuyNow)
- `Controllers/CartController.cs` (Hiển thị/Update/Remove giỏ)
- `Controllers/OrderController.cs` (Checkout/PlaceOrder/Success)
- Các view đã tạo mới:
  - `Views/Cart/Cart.cshtml`
  - `Views/Order/Checkout.cshtml`
  - `Views/Order/Success.cshtml`

---

## 1) Chọn sản phẩm → thêm vào giỏ
### Nút thao tác
- `Home/Index.cshtml` có nút "Thêm Vào Giỏ" gọi `ProductController.AddCart(productId)`
- Trang sản phẩm có nút "Mua ngay" gọi `ProductController.BuyNow(productId)`

### Logic lưu giỏ
`ProductController.AddCart / BuyNow`:
- Tìm `ProductVariant` thỏa `ProductId` và có hàng (`IsActive && StockQuantity > 0`).
- Upsert vào bảng `Carts` theo key:
  - Nếu **đăng nhập**: `UserId = userId`, `SessionId = null`
  - Nếu **khách vãng lai**: `UserId = null`, `SessionId = GuestCartId (cookie)`

---

## 2) Xem giỏ hàng
Route: `GET /Cart/Cart`
- Nếu user đăng nhập: lấy cart theo `UserId`
- Nếu không đăng nhập: lấy cart theo `GuestCartId` cookie

View: `Views/Cart/Cart.cshtml`
- Hiển thị danh sách item + tổng tiền (`ViewBag.Items`, `ViewBag.Total`)
- Cho phép:
  - **UpdateQuantity** (POST) → `CartController.UpdateQuantity(cartId, quantity)`
  - **Remove** (POST) → `CartController.Remove(cartId)`

---

## 3) Checkout (nhập địa chỉ khách hàng)
Route: `GET /Order/Checkout`
- Bắt buộc đăng nhập (`[Authorize]`)
- Lấy toàn bộ item trong giỏ của user
- Tính tổng tiền → `ViewBag.Total`
- Trả view với model `Address`

View: `Views/Order/Checkout.cshtml`
- Form POST gọi `OrderController.PlaceOrder(Address address)`
- Các trường input:
  - `SpecificAddress`
  - `City`
  - `District`

---

## 4) Xác nhận đặt hàng + giả lập thanh toán thành công
Route: `POST /Order/PlaceOrder`
- Validates ModelState của `Address`
- Tạo `Order`:
  - `OrderStatus = 0` (theo enum comment trong model)
  - `PaymentStatus = 1` (**đã set để “thanh toán thành công giả lập” ngay khi bấm**)
  - `TotalAmount`, `FinalAmount` = tổng tiền trong giỏ
  - Shipping lấy từ `address` và claims (`Name`, `Phone`)
- Tạo `OrderDetail` cho từng cart item (snapshot ProductName/Size/Color)
- Xóa toàn bộ item trong giỏ sau khi tạo order
- Redirect sang: `GET /Order/Success?id=...`

---

## 5) Trang thành công
Route: `GET /Order/Success?id=ORDER_ID`
- Load `Order` theo `id`
- Hiển thị:
  - `OrderId`
  - `FinalAmount`
  - `ShippingCity`, `ShippingDistrict`, `ShippingAddress`

View: `Views/Order/Success.cshtml`

---

## Ghi chú quan trọng về “thanh toán thành công”
- Hiện tại hệ thống **chưa tích hợp cổng thanh toán**.
- Vì vậy “thanh toán thành công” được **giả lập** bằng cách set:
  - `Order.PaymentStatus = 1` trong `OrderController.PlaceOrder`.

Nếu sau này tích hợp cổng thanh toán, bạn có thể tách flow thành:
- PlaceOrder tạo order với `PaymentStatus = 0`
- Redirect về provider
- Provider webhook callback → update `PaymentStatus = 1`

