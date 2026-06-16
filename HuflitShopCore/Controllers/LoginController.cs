using System.Security.Claims;
using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HuflitShopCore.Controllers
{
    public class LoginController : Controller
    {
        private readonly UserService _userService;
        public LoginController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login() => View(new LoginDTO());

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var user = await _userService.AuthenticateAsync(dto.Email, dto.Password);
            if (user != null)
            {
                // Tạo danh sách các thông tin (Claims) để lưu vào Cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim("UserName", user.UserName), 
                    new Claim("Name", user.FullName), 
                    new Claim("Phone", user.PhoneNumber ?? ""), 
                    new Claim("Avatar", user.Avatar ?? ""), 
                    new Claim(ClaimTypes.Role, user.Role ?? "Customer") 
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = dto.RememberMe };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(dto);
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterDTO());

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var result = await _userService.RegisterAsync(dto);
            if (result) return RedirectToAction("Login");

            ModelState.AddModelError("", "Email đã tồn tại.");
            return View(dto);
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordDTO());

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var user = await _userService.GetUserByEmailAsync(dto.Email);
            if (user != null)
            {
                // Logic gửi email token ở đây
                dto.EmailSent = true;
            }
            return View(dto);
        }

        [HttpGet]
        public IActionResult ResetPassword(string uid, string token)
        {
            return View(new ResetPasswordDTO { UserId = uid, Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var result = await _userService.ResetPasswordAsync(dto.UserId, dto.NewPassword);
            if (result)
            {
                dto.IsSuccess = true;
                return View(dto);
            }
            ModelState.AddModelError("", "Đã xảy ra lỗi khi đặt lại mật khẩu.");
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string uid, string token, string email)
        {
            var dto = new EmailConfirmDTO { Email = email };
            if (!string.IsNullOrEmpty(uid))
            {
                var result = await _userService.ConfirmEmailAsync(uid);
                if (result) dto.EmailVerified = true;
            }
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmEmail(EmailConfirmDTO dto)
        {
            // Tái gửi email xác thực
            dto.EmailSent = true;
            return View(dto);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}