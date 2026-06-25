# 🛍️ HuflitShopCore - Hệ Thống Thương Mại Điện Tử Thời Trang & Logistics Tích Hợp

HuflitShopCore là một nền tảng thương mại điện tử thời trang cao cấp được xây dựng trên nền tảng **ASP.NET Core MVC**. Hệ thống tích hợp các tính năng e-commerce hiện đại, giải thuật khai thác dữ liệu hành vi người dùng (Datamining) để gợi ý sản phẩm cá nhân hóa (AI Recommendations), quản lý kho hàng thông minh theo cơ chế FIFO, hệ thống khuyến mãi nâng cao và tích hợp dịch vụ giao hàng hỏa tốc (GrabExpress/Ahamove) giả lập bản đồ hành trình thời gian thực.

---

## 🔑 Tài Khoản Thử Nghiệm (Testing Accounts)

* **Tài khoản Admin (Quản trị viên):** 
  * Email: `admin@huflit.com`
  * Mật khẩu: `123`
* **Tài khoản Khách hàng:** 
  * Email: `khach@gmail.com`
  * Mật khẩu: `123`

---

## 🚀 Công Nghệ Sử Dụng (Technology Stack)

### 1. Backend & Business Logic
* **Framework:** ASP.NET Core MVC 8.0 (.NET 8)
* **Ngôn ngữ:** C# (C Sharp)
* **ORM:** Entity Framework Core (EF Core)
* **Cơ sở dữ liệu:** Microsoft SQL Server (Hỗ trợ SQLite cho môi trường phát triển & kiểm thử nhẹ)
* **Xử lý tác vụ ngầm:** Task Parallel Library (TPL) cho các giả lập xử lý giao hàng ngầm trên Server.

### 2. Frontend & Giao diện người dùng
* **Framework:** Bootstrap 5, Vanilla CSS3 (Sử dụng hệ thống biến CSS đồng bộ, Glassmorphic UI)
* **Logic Client-side:** Javascript (ES6+, AJAX JQuery) cho tải động giỏ hàng, bản đồ và popups.
* **Thư viện icon:** FontAwesome 6, Google Material Icons.

### 3. API & Dịch vụ bên thứ ba
* **Lưu trữ hình ảnh:** Cloudinary API (Tải lên và tối ưu hóa hình ảnh sản phẩm tự động)
* **Cổng thanh toán:** VNPAY Sandbox (Thanh toán trực tuyến qua ATM, QR Code)
* **Vận chuyển & Bản đồ:** Giả lập tích hợp API GrabExpress / Ahamove để báo giá động và hiển thị bản đồ hành trình.

---

## 🏛️ Kiến Trúc Hệ Thống (Architecture)

Duyệt qua cấu trúc dự án:
* **Areas/Admin:** Chứa toàn bộ Controller và View phục vụ chức năng quản trị (Quản lý sản phẩm, đơn hàng, kho hàng FIFO, báo cáo doanh thu, chương trình khuyến mãi).
* **Controllers:** Điều hướng và xử lý request của khách hàng (Home, Product, Cart, Order, Profile).
* **Services Layer:** Tách biệt hoàn toàn logic nghiệp vụ khỏi Controller:
  * `OrderService.cs`: Tạo đơn hàng, tính toán khuyến mãi tự động, phân bổ giảm giá.
  * `PromotionService.cs`: Quản lý, xác thực và lưu trữ các chương trình khuyến mãi.
  * `GrabExpressService.cs`: Giả lập báo giá, đặt tài xế và mô phỏng lộ trình xe.
  * `StockReceiptService.cs`: Nhập kho và tính toán giá vốn sản phẩm theo phương pháp FIFO.
* **Data (DbContext):** `AppDbContext.cs` quản lý các cấu hình bảng, quan hệ thực thể, và seeding dữ liệu ban đầu.

---

## 🌟 Tính Năng Từ A Đến Z (Core Features)

### 1. Phía Khách Hàng (Client-Side E-Commerce)
* **Danh mục sản phẩm & Chi tiết:** Hiển thị sản phẩm theo danh mục, lọc giá, chọn phân loại kích thước (Size) và màu sắc (Color) trực quan.
* **Giỏ hàng linh hoạt:** Thêm, sửa số lượng sản phẩm trực tiếp bằng AJAX không cần tải lại trang. Tự động kiểm tra tồn kho của từng biến thể trước khi cho phép thêm.
* **Đặt hàng & Thanh toán đa phương thức:**
  * Hỗ trợ thanh toán khi nhận hàng (COD).
  * Tích hợp cổng thanh toán **VNPAY**, tự động tạo URL thanh toán an toàn và xử lý callback trạng thái đơn hàng trực tuyến.

### 2. Phía Quản Trị (Admin Dashboard)
* **Quản lý danh mục & Biến thể:** Quản lý sản phẩm đa cấp, quản lý kho biến thể (Color, Size).
* **Quản lý nhập kho FIFO:** Nhập kho sản phẩm theo từng lô hàng, ghi nhận giá vốn khác nhau của từng đợt nhập.
* **Duyệt đơn hàng & Tác vụ ngầm:** Admin phê duyệt đơn hàng, lập tức kích hoạt tiến trình giả lập tài xế lấy hàng và cập nhật lộ trình giao hàng ngầm trên Server.
* **Báo cáo tài chính & Tồn kho:** Báo cáo doanh thu, giá vốn hàng bán, và lợi nhuận thực tế dựa trên giải thuật FIFO.

### 3. Các tính năng Nâng cao (Advanced Features)

#### A. Tích hợp GrabExpress / Ahamove & Bản đồ theo dõi thời gian thực
* **Báo giá vận chuyển tự động:** Khi khách hàng chọn Tỉnh/Thành phố và Quận/Huyện tại trang checkout, hệ thống gọi API giả lập GrabExpress để tính toán phí ship dựa trên vị trí địa lý.
* **Bản đồ theo dõi (Live Tracking Page):** Khách hàng có thể truy cập trang theo dõi đơn hàng để xem vị trí tài xế di chuyển trên bản đồ Canvas động, kèm các mốc trạng thái (Đang tìm tài xế -> Đang lấy hàng -> Đang giao -> Đã giao thành công).

#### B. Hệ thống Khuyến mãi nâng cao (Advanced Promotion System)
* **Khuyến mãi tự động (IsAutoApply = true):** Tự động áp dụng giảm giá trực tiếp lên sản phẩm hoặc combo trong giỏ hàng mà không cần khách nhập code.
* **Mua chung theo Combo:** Cấu hình danh sách sản phẩm yêu cầu mua chung (`ComboProductIds`). Khi khách mua đủ toàn bộ sản phẩm trong combo, hệ thống tự động trừ tiền giảm giá combo tương ứng.
* **Voucher giới hạn sản phẩm cụ thể (`ApplicableProductId`):** Khóa voucher chỉ cho phép áp dụng khi giỏ hàng chứa sản phẩm quy định. Số tiền giảm giá (nếu tính theo %) chỉ được tính trên giá trị của sản phẩm đó trong giỏ chứ không tính trên tổng đơn hàng.

#### C. Thu thập dữ liệu hành vi người dùng (Datamining & AI Recommendations)
* **Instant View Logging:** Ghi nhận lượt xem sản phẩm của người dùng đã đăng nhập ngay khi họ tải trang chi tiết sản phẩm (thời gian xem ban đầu mặc định là 1 giây).
* **JavaScript Behavior Tracking:** Theo dõi thời gian xem thực tế của người dùng bằng cách đếm số giây họ ở lại tab chi tiết sản phẩm. Sử dụng thuộc tính `keepalive: true` trong API `fetch` để đảm bảo dữ liệu ping hành vi gửi về server thành công ngay cả khi người dùng đóng tab hoặc chuyển trang đột ngột.
* **Gợi ý Cá nhân hóa (AI recommendations):** Phân tích dữ liệu thời gian xem để tìm ra các danh mục (Category) được người dùng quan tâm nhất, xếp hạng danh mục và đề xuất các sản phẩm tương ứng lên mục "DÀNH RIÊNG CHO BẠN" tại trang chủ.

#### D. Popup kích cầu giỏ hàng bỏ quên (Abandoned Cart Detection)
* Khi khách hàng thêm sản phẩm vào giỏ nhưng không thanh toán và để giỏ hàng tồn tại quá **30 giây** (thời gian cấu hình ngắn để phục vụ demo), trang Giỏ hàng khi tải lại sẽ hiển thị một Popup Glassmorphic tặng mã voucher kích cầu mua sắm `QUENGIORHANG` (giảm 10%).

---

## 📐 Các Giải Thuật Cốt Lõi (Core Algorithms & Logic)

### 1. Thuật toán gợi ý cá nhân hóa dựa trên thời gian xem (AI Recommendation)
Giải thuật phân tích hành vi thời gian xem của người dùng để đề xuất sản phẩm:
* **Bước 1:** Truy vấn tất cả lịch sử xem sản phẩm (`ProductViewsLog`) của người dùng hiện tại từ cơ sở dữ liệu.
* **Bước 2:** Phân tích chuỗi dữ liệu trong cột `UserAgent` (chứa định dạng `[Browser] | Duration: [Seconds]s`) để trích xuất số giây xem thực tế của từng sản phẩm.
* **Bước 3:** Nhóm (Group by) các lượt xem theo danh mục sản phẩm (Category) và tính tổng thời gian xem của người dùng trên mỗi danh mục.
* **Bước 4:** Sắp xếp các danh mục theo tổng thời gian xem giảm dần (danh mục quan tâm nhất xếp đầu).
* **Bước 5:** Duyệt qua danh sách danh mục đã sắp xếp, lấy ra các sản phẩm mới nhất thuộc từng danh mục này để đưa vào danh sách gợi ý (đảm bảo không lấy sản phẩm trùng lặp).
* **Bước 6 (Padding):** Nếu duyệt qua tất cả danh mục đã xem mà danh sách gợi ý vẫn chưa đủ 8 sản phẩm, hệ thống tự động truy vấn thêm các sản phẩm mới nhất từ hệ thống để điền đầy cho đủ 8 sản phẩm trước khi trả về giao diện.

### 2. Thuật toán quản lý kho hàng & Tính lợi nhuận theo FIFO (First-In, First-Out)
* **Nguyên lý:** Hàng hóa nhập kho trước sẽ được xuất kho trước. Mỗi lô hàng nhập vào được lưu trữ thông tin số lượng nhập, số lượng đã xuất, và đơn giá nhập riêng biệt trong bảng `InventoryLots`.
* **Khi bán hàng:** Khi đơn hàng được duyệt, hệ thống sẽ tiến hành xuất kho:
  * Duyệt các lô hàng của sản phẩm đó có lượng tồn (`Quantity - ReleasedQuantity > 0`) theo thứ tự thời gian nhập sớm nhất.
  * Khấu trừ số lượng mua từ các lô này. Nếu lô đầu tiên không đủ số lượng, phần còn lại tiếp tục khấu trừ sang lô tiếp theo. Ghi nhận mối quan hệ lô hàng xuất vào bảng `OrderDetailLots`.
* **Tính giá vốn hàng bán (COGS):** Giá vốn của sản phẩm trong đơn hàng được tính bằng tổng tích số lượng xuất nhân với đơn giá nhập của từng lô tương ứng. Doanh thu trừ đi giá vốn này sẽ cho ra lợi nhuận thực tế chính xác của doanh nghiệp.

### 3. Thuật toán xác thực và phân bổ giảm giá của Voucher
Khi người dùng nhập voucher giảm giá tại Checkout:
* **Xác định giỏ hàng:** Hệ thống lấy các sản phẩm đang thanh toán (hỗ trợ cả giỏ hàng thông thường và sản phẩm mua ngay qua biến thể).
* **Xác thực điều kiện áp dụng:**
  * Kiểm tra xem giá trị đơn hàng tối thiểu (`MinOrderAmount`) có lớn hơn tổng giá trị hàng sau khi đã trừ đi các chương trình khuyến mãi tự động (giảm trực tiếp, combo) hay không.
  * Nếu voucher giới hạn sản phẩm (`ApplicableProductId`), kiểm tra xem sản phẩm đó có tồn tại trong đơn hàng không.
  * Nếu voucher giới hạn combo (`ComboProductIds`), kiểm tra xem toàn bộ các sản phẩm cấu thành combo có xuất hiện đầy đủ trong đơn hàng không.
* **Tính toán số tiền giảm giá:**
  * Đối với voucher giới hạn sản phẩm/combo: Lọc ra các sản phẩm trong checkout khớp với điều kiện. Tính tổng giá trị của nhóm sản phẩm này (sau khi đã áp dụng giảm giá trực tiếp tự động). Áp dụng phần trăm (%) hoặc giá trị cố định của voucher lên tổng con này để ra số tiền giảm giá thực tế (không tính trên sản phẩm không liên quan).
  * Đối với voucher thường: Áp dụng trực tiếp lên tổng giá trị đơn hàng.
  * Giảm giá tối đa được giới hạn bởi `MaxDiscountAmount` và không vượt quá tổng giá trị hàng hóa được áp dụng.
* **Phân bổ giảm giá (`DiscountAllocation`):** Để giữ tính toàn vẹn dữ liệu kế toán, tổng số tiền giảm giá của đơn hàng được phân bổ tỉ lệ xuống từng chi tiết đơn hàng (`OrderDetail`) theo công thức:
  $$\text{Giảm giá phân bổ cho SP } i = \text{Tổng giảm giá} \times \frac{\text{Giá trị hàng SP } i}{\text{Tổng giá trị đơn hàng}}$$
  Sai số làm tròn số thập phân cuối cùng được bù đắp vào sản phẩm cuối cùng của đơn hàng.

---

## 🛠️ Hướng Dẫn Cài Đặt & Chạy Dự Án (Getting Started)

### 1. Yêu cầu hệ thống
* .NET SDK 8.0 trở lên.
* Microsoft SQL Server LocalDB hoặc SQL Server Express.
* Công cụ dòng lệnh `dotnet ef` (để chạy migration).

### 2. Cấu hình ứng dụng
Mở file [appsettings.json](file:///d:/WEB/HuflitShopCore/HuflitShopCore/appsettings.json) và cấu hình các thông số sau:
* `ConnectionStrings:DefaultConnection`: Chuỗi kết nối tới cơ sở dữ liệu SQL Server của bạn.
* `CloudinarySettings`: Cấu hình tài khoản Cloudinary (`CloudName`, `ApiKey`, `ApiSecret`) để upload ảnh.
* `Vnpay`: Cấu hình cổng thanh toán thử nghiệm VNPAY.

### 3. Khởi tạo cơ sở dữ liệu
Chạy các lệnh sau trong thư mục chứa dự án `HuflitShopCore` để cập nhật cơ sở dữ liệu và seed dữ liệu mẫu:
```bash
dotnet ef database update
```

### 4. Chạy dự án
Chạy lệnh sau để khởi động máy chủ thử nghiệm cục bộ:
```bash
dotnet run --project HuflitShopCore
```
Sau khi khởi chạy thành công, truy cập ứng dụng thông qua trình duyệt tại địa chỉ mặc định: `https://localhost:7107` hoặc `http://localhost:5191`.
