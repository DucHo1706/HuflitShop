# Tiêu chuẩn và Quy tắc Kiến trúc dự án HuflitShop

1. **Mô hình kiến trúc**: Tuân thủ mô hình MVC (Model-View-Controller) của C#. Không chuyển sang làm ứng dụng dưới dạng API.
2. **Giao diện (UI/HTML)**: Đặt chung code HTML/Razor trong thư mục `Views` gắn liền với Controller.
3. **Khóa chính (Primary Key)**: Các Entity sẽ sử dụng `Guid.NewGuid().ToString()` trong C# để tạo ID tự động dạng chuỗi (phù hợp với `VARCHAR(50)` trong Database) để dễ dàng quản lý và kiểm soát hơn thay vì tự động sinh mã không kiểm soát.
4. **Quản lý Users & Authentication**: Tách rời sự phụ thuộc vào base `IdentityUser` mặc định. Tự định nghĩa lớp `AppUser` riêng để dễ quản lý dữ liệu, kiểm soát ID.
5. **Phân chia Task**: Mỗi lần chỉ giải quyết một task nhỏ, độc lập, không dồn lại xử lý cùng một lúc. Hoàn thành rồi mới chuyển sang task tiếp theo.
6. **Cấu trúc thư mục (Dependency Injection)**: Đơn giản hóa cấu trúc thư mục. Không tạo quá nhiều thư mục/file rườm rà cho Interface/DI nếu điều đó làm cấu trúc phức tạp, để tránh bị rắc rối khi liên kết file chính.
7. **Quy chuẩn tổ chức thư mục (Folder Structure)**: Tuân thủ cấu trúc gọn nhẹ, rõ ràng:
   - `Models/`: Tầng Entity (chỉ chứa các class ánh xạ trực tiếp với Database).
   - `DTO/` (Data Transfer Object): Chứa các class bọc/đóng gói dữ liệu để chuyển đổi giữa Controller và View, hoặc để nhận dữ liệu từ các form (thay thế khái niệm ViewModels).
   - `Services/`: Tầng xử lý nghiệp vụ (Business Logic). Tương tác trực tiếp với `AppDbContext`. Loại bỏ Repositories nếu không thực sự cần thiết để tránh rườm rà.
   - `Controllers/`: Chỉ làm nhiệm vụ điều hướng, nhận Request, gọi Service, lấy `DTO` và trả ra View.
   - `Views/`: Giao diện hệ thống (HTML/Razor).
   - `Data/`: Chứa DbContext và Migrations.
   - `Helpers/`: Chứa thư viện, hàm tiện ích, hằng số (Constants) dùng chung.