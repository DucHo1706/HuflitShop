using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace HuflitShopCore.Helpers
{
    public static class DbErrorHandler
    {
        /// <summary>
        /// Thực thi lưu thay đổi Database và tự động bắt lỗi khóa ngoại (Foreign Key/Relationship)
        /// </summary>
        /// <returns>Trả về Tuple gồm: (Thành công hay không, Câu thông báo lỗi nếu có)</returns>
        public static async Task<(bool Success, string ErrorMessage)> ExecuteAsync(Func<Task> dbAction)
        {
            try
            {
                await dbAction();
                return (true, string.Empty);
            }
            catch (DbUpdateException ex)
            {
                var innerMsg = ex.InnerException?.Message ?? string.Empty;
                if (innerMsg.Contains("REFERENCE constraint") || innerMsg.Contains("FOREIGN KEY"))
                {
                    return (false, "Dữ liệu này đang được sử dụng ở nơi khác (đã có hóa đơn, phiếu nhập, sản phẩm...) nên hệ thống chặn không cho phép xóa!");
                }
                return (false, "Lỗi cập nhật dữ liệu: " + innerMsg);
            }
            catch (Exception ex)
            {
                return (false, "Lỗi hệ thống: " + ex.Message);
            }
        }
    }
}
