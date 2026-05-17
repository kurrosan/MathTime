using System.Security.Claims;
using System.Security.Cryptography;
using MathTime.Data;
using MathTime.Models;
using MathTime.Services;
using MathTime.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathTime.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationContext _db;
        private readonly IPasswordHasher<User> _hasher;
        private readonly EmailService _email;

        public AccountController(ApplicationContext db, IPasswordHasher<User> hasher, EmailService email)
        {
            _db = db;
            _hasher = hasher;
            _email = email;
        }

        // =====================================================
        // LOGIN
        // =====================================================

        [HttpGet("/login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["LoginError"] = "Введите корректные данные.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == vm.Email);

            if (user == null ||
                _hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password)
                == PasswordVerificationResult.Failed)
            {
                TempData["LoginError"] = "Неверный email или пароль.";
                return RedirectToAction("Index", "Home");
            }

            if (user.IsBlocked)
            {
                TempData["LoginError"] = "Ваш аккаунт заблокирован.";
                return RedirectToAction("Index", "Home");
            }

            await SignInAsync(user, vm.RememberMe);

            return RedirectToAction("Index", "Home");
        }

        // ===============================
        // REGISTRATION
        // ===============================
        [HttpPost("/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["RegisterError"] = "Проверьте корректность введённых данных.";
                return RedirectToAction("Index", "Home");
            }

            if (vm.Password != vm.ConfirmPassword)
            {
                TempData["RegisterError"] = "Пароли не совпадают.";
                return RedirectToAction("Index", "Home");
            }

            if (await _db.Users.AnyAsync(u => u.Email == vm.Email))
            {
                TempData["RegisterError"] = "Пользователь с таким Email уже существует.";
                return RedirectToAction("Index", "Home");
            }

            var user = new User
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Phone = vm.Phone,
                Email = vm.Email,
                DateOfBirth = vm.DateOfBirth,
                Gender = vm.Gender,
                EmailVerified = false,
                Role = "User"
            };

            // Аватар
            if (vm.Avatar != null && vm.Avatar.Length > 0)
            {
                using var ms = new MemoryStream();
                await vm.Avatar.CopyToAsync(ms);
                user.Avatar = ms.ToArray();
            }

            user.PasswordHash = _hasher.HashPassword(user, vm.Password);

            // Генерация кода подтверждения
            user.VerificationCode = GenerateVerificationCode();

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await _email.SendEmailAsync(
     user.Email,
     "Подтверждение Email — MathTime",
     $"<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; text-align: center; background: #f7f8fc; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.1);\"><h2 style=\"color: #333; font-weight: 600; margin-bottom: 20px;\">Ваш код подтверждения</h2><h1 style=\"color: #4f46e5; font-size: 48px; letter-spacing: 2px; margin: 0;\">{user.VerificationCode}</h1><p style=\"color: #666; margin-top: 20px; font-size: 16px;\">Введите этот код, чтобы подтвердить свой аккаунт</p></div>"
 );

            // Входим сразу после регистрации
            await SignInAsync(user, true);

            return RedirectToAction("EmailConfirmation");
        }

        [HttpGet("/resend-code")]
        [Authorize]
        public async Task<IActionResult> ResendCode()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _db.Users.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Index", "Home");

            user.VerificationCode = GenerateVerificationCode();

            await _db.SaveChangesAsync();

            await _email.SendEmailAsync(
                user.Email,
                "Новый код подтверждения — MathTime",
                $@"
        <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; text-align: center; background: #f7f8fc; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.1);"">
            <h2 style=""color: #333; font-weight: 600; margin-bottom: 20px;"">
                Ваш код подтверждения
            </h2>

            <h1 style=""color: #4f46e5; font-size: 48px; letter-spacing: 2px; margin: 0;"">
                {user.VerificationCode}
            </h1>

            <p style=""color: #666; margin-top: 20px; font-size: 16px;"">
                Введите этот код, чтобы подтвердить свой аккаунт
            </p>
        </div>"
            );

            TempData["VerifyError"] = "Новый код отправлен!";
            return RedirectToAction("EmailConfirmation");
        }

        [HttpPost("/verify-email")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string code)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _db.Users.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Index", "Home");

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["VerifyError"] = "Введите код подтверждения.";
                return RedirectToAction("EmailConfirmation");
            }

            if (user.VerificationCode == code)
            {
                user.EmailVerified = true;
                user.VerificationCode = null;

                await _db.SaveChangesAsync();

                TempData["VerifySuccess"] = "Email успешно подтверждён!";
                return RedirectToAction("Profile");
            }

            TempData["VerifyError"] = "Неверный код подтверждения.";
            return RedirectToAction("EmailConfirmation");
        }
        private string GenerateVerificationCode()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[4];
            rng.GetBytes(bytes);
            int code = BitConverter.ToInt32(bytes, 0) % 900000 + 100000;
            return Math.Abs(code).ToString("D6");
        }

        // ===============================
        // LOGOUT
        // ===============================
        [Authorize]
        [HttpPost("/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ===============================
        // SIGN-IN HELPER
        // ===============================
        private async Task SignInAsync(User user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    AllowRefresh = true
                });
        }

        [Authorize]
        [HttpGet("/profile")]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var id))
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }

            // ===== Статистика для администратора =====
            if (User.IsInRole("Admin"))
            {
                ViewBag.TotalUsers = await _db.Users.CountAsync();
                ViewBag.BlockedUsers = await _db.Users.CountAsync(u => u.IsBlocked);
            }

            return View(user);
        }

        [Authorize]
        [HttpPost("/profile/avatar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvatar(IFormFile Avatar)
        {
            if (Avatar == null || Avatar.Length == 0)
                return RedirectToAction("Profile");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var id))
                return Unauthorized();

            var user = await _db.Users.FindAsync(id);

            if (user == null)
                return RedirectToAction("Index", "Home");

            using var ms = new MemoryStream();
            await Avatar.CopyToAsync(ms);

            user.Avatar = ms.ToArray();

            await _db.SaveChangesAsync();

            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpGet("/profile/edit")]
        public async Task<IActionResult> EditProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var id))
                return Unauthorized();

            var user = await _db.Users.FindAsync(id);

            if (user == null)
                return RedirectToAction("Profile");

            return View(user);
        }

        [Authorize]
        [HttpPost("/profile/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(
     string FirstName, string LastName, string Email,
     string Phone, DateTime DateOfBirth, string Gender,
     string? NewPassword, string? ConfirmPassword)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var id))
                return Unauthorized();

            var user = await _db.Users.FindAsync(id);

            if (user == null)
                return RedirectToAction("Profile");

            user.FirstName = FirstName;
            user.LastName = LastName;
            user.Email = Email;
            user.Phone = Phone;
            user.DateOfBirth = DateOfBirth;
            user.Gender = Gender;

            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                if (NewPassword != ConfirmPassword)
                {
                    TempData["EditError"] = "Пароли не совпадают.";
                    return RedirectToAction("EditProfile");
                }

                user.PasswordHash = _hasher.HashPassword(user, NewPassword);
            }

            await _db.SaveChangesAsync();
            await SignInAsync(user, true);

            return RedirectToAction("Profile");
        }

        // ===============================
        // AVATAR GETTER
        // ===============================
        [HttpGet("/avatar/{id}")]
        public async Task<IActionResult> Avatar(int id)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user?.Avatar == null)
                return File(System.IO.File.ReadAllBytes("wwwroot/images/avatar.png"), "image/png");

            return File(user.Avatar, "image/jpeg");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Like(int articleId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var existingLike = await _db.Likes
                .FirstOrDefaultAsync(l => l.ArticleId == articleId && l.UserId == userId);

            if (existingLike != null)
            {
                _db.Likes.Remove(existingLike);
            }
            else
            {
                _db.Likes.Add(new Like
                {
                    ArticleId = articleId,
                    UserId = userId
                });
            }

            await _db.SaveChangesAsync();

            var likeCount = await _db.Likes.CountAsync(l => l.ArticleId == articleId);

            return Json(new { success = true, count = likeCount });
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddComment(int articleId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Json(new { success = false, error = "Комментарий не может быть пустым" });

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdValue))
                return Unauthorized();

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var comment = new Comment
            {
                ArticleId = articleId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            var user = await _db.Users.FindAsync(userId);

            if (user == null)
                return Json(new { success = false, error = "Пользователь не найден" });

            return Json(new
            {
                success = true,
                comment = new
                {
                    content = comment.Content,
                    createdAt = comment.CreatedAt.ToString("g"),
                    userName = $"{user.FirstName} {user.LastName}"
                }
            });
        }

        // ===============================
        // ADMIN PANEL
        // ===============================
        [Authorize(Roles = "Admin")]
        [HttpGet("/admin/users")]
        public async Task<IActionResult> UserList()
        {
            var users = await _db.Users.ToListAsync();
            return View(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("/admin/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return RedirectToAction("UserList");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("/admin/block/{id}")]
        public async Task<IActionResult> BlockUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsBlocked = true;
            await _db.SaveChangesAsync();

            return RedirectToAction("UserList");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("/admin/unblock/{id}")]
        public async Task<IActionResult> UnblockUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsBlocked = false;
            await _db.SaveChangesAsync();

            return RedirectToAction("UserList");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/admin/edit/{id}")]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("/admin/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, User updated)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FirstName = updated.FirstName;
            user.LastName = updated.LastName;
            user.Email = updated.Email;
            user.Phone = updated.Phone;
            user.Gender = updated.Gender;
            user.DateOfBirth = updated.DateOfBirth;
            user.Role = updated.Role;

            await _db.SaveChangesAsync();
            return RedirectToAction("UserList");
        }

        [HttpGet("/email-confirmation")]
        [Authorize]
        public IActionResult EmailConfirmation()
        {
            return View();
        }
    }
}
