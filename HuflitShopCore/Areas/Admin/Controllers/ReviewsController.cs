using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReviewsController : Controller
    {
        private readonly ReviewService _reviewService;

        public ReviewsController(ReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        public async Task<IActionResult> Index()
        {
            var reviews = await _reviewService.GetAllReviewsAsync();
            return View(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _reviewService.DeleteReviewAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reply(string id, string replyText)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var success = await _reviewService.AddReplyAsync(id, replyText);
            if (success)
            {
                TempData["SuccessMessage"] = "Phản hồi đánh giá thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi phản hồi.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}