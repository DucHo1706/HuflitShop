using System.Security.Claims;
using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
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
                await SignInUserAsync(user, dto.RememberMe);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(dto);
        }

        // --- ĐĂNG NHẬP GOOGLE ---
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Login");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                ModelState.AddModelError("", "Xác thực Google thất bại.");
                return RedirectToAction("Login");
            }

            var claims = result.Principal.Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Không thể lấy Email từ Google.");
                return RedirectToAction("Login");
            }

            // Kiểm tra user đã tồn tại chưa
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                var registerDto = new RegisterDTO
                {
                    Email = email,
                    Name = string.IsNullOrWhiteSpace(name) ? "Khách hàng Google" : name,
                    PhoneNumber = "",
                    Password = "GAuth_" + Guid.NewGuid().ToString("N").Substring(0, 8)
                };

                var created = await _userService.RegisterAsync(registerDto);
                if (created)
                {
                    user = await _userService.GetUserByEmailAsync(email);
                }
            }

            if (user != null)
            {
                await SignInUserAsync(user, isPersistent: true);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Đăng nhập Google không thành công.");
            return RedirectToAction("Login");
        }

        // Helper cấp Cookie Claims đồng bộ với hệ thống hiện tại của dự án
        private async Task SignInUserAsync(dynamic user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("UserName", user.UserName ?? user.Email),
                new Claim("Name", user.FullName ?? "Khách hàng"),
                new Claim("Phone", user.PhoneNumber ?? ""),
                new Claim("Avatar", user.Avatar ?? "")
            };

            var userRoles = await _userService.GetUserRolesAsync(user.Id);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (!string.IsNullOrEmpty(user.Role) && !userRoles.Contains(user.Role))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role));
            }
            else if (!userRoles.Any())
            {
                claims.Add(new Claim(ClaimTypes.Role, "Customer"));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = isPersistent };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);
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
            dto.EmailSent = true;
            return View(dto);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}