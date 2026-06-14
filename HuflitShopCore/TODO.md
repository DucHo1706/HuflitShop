# HuflitShopCore - User flow refactor (Shop end-to-end)

## Mục tiêu
- Bổ sung nút **Logout** cho user.
- Refactor trang chủ (root HomeController) để **load sản phẩm** và hiển thị các thông tin cơ bản (giỏ hàng).
- Hoàn thiện luồng user thương mại:
  1) Chọn sản phẩm
  2) Thêm vào giỏ hàng
  3) Checkout/đặt hàng
  4) Thêm địa chỉ
  5) Tạo order thành công + hiển thị trang giao hàng/hoàn tất

## Ràng buộc kỹ thuật (để không đi quá lỗ)
1. **Không đổi schema DB** (migration/ALTER) trừ khi thật sự bắt buộc và có lý do rõ ràng.
2. Ưu tiên dùng **DTO/Model/DbContext/Services** hiện có trong `HuflitShopCore`.
3. Không đổi routing bừa. Nếu cần, chỉ thêm action overload để tương thích view đang dùng.
4. Không introduce thư viện mới (trừ khi project hiện tại đã dùng sẵn pattern tương tự).
5. Mỗi bước chỉ làm 1 cụm chức năng (layout/logout, home-load, cart, order/checkout, address/order-confirm).
6. Luôn build sau mỗi cụm thay đổi.

## Checklist công việc
- [ ] 1) Gắn nút Logout: tìm layout/header và auth partial, kiểm tra middleware/auth scheme.
- [ ] 2) Refactor Home (root):
  - [ ] load danh sách sản phẩm từ DB
  - [ ] render danh sách trên `Views/Home/Index.cshtml`
  - [ ] hiển thị badge/summary giỏ hàng nếu có
- [ ] 3) Giỏ hàng:
  - [ ] kiểm tra `Cart` model & logic services/controller
  - [ ] hoàn thiện AddToCart/UpdateQuantity/Remove
- [ ] 4) Checkout & Đặt hàng:
  - [ ] flow chọn địa chỉ (tạo mới Address nếu cần)
  - [ ] tạo `Order` + `OrderDetail`
  - [ ] cập nhật trạng thái order/Giao hàng theo model hiện có
- [ ] 5) Trang thành công/đơn hàng:
  - [ ] hiển thị orderId, địa chỉ, trạng thái giao hàng
- [ ] 6) Smoke test bằng các route chính.

## Các file dự kiến sẽ chạm
- `HuflitShopCore/HuflitShopCore/Controllers/HomeController.cs`
- `HuflitShopCore/HuflitShopCore/Views/Home/Index.cshtml`
- `HuflitShopCore/HuflitShopCore/Views/Shared/_Layout*.cshtml`
- `HuflitShopCore/HuflitShopCore/Views/Shared/*Login*Partial*.cshtml`
- `HuflitShopCore/HuflitShopCore/Controllers/*Cart*Controller*.cs`
- `HuflitShopCore/HuflitShopCore/Controllers/*Order*Controller*.cs`
- `HuflitShopCore/HuflitShopCore/Services/*`


