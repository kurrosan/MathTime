using MathTime.Data;
using MathTime.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MathTime.Controllers
{
    public class LibraryController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly IWebHostEnvironment _env;

        public LibraryController(ApplicationContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =====================================================
        // 📌 INDEX + ФИЛЬТР (поиск + категория)
        // =====================================================
        public async Task<IActionResult> Index(string searchString, string category)
        {
            var query = _context.Library
                .Include(f => f.User)
                .AsQueryable();

            // 🔎 Поиск по названию
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(f => f.FileName.Contains(searchString));
            }

            // 📂 Фильтр по категории
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(f => f.Category == category);
            }

            var files = await query
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            return View(files);
        }

        // =====================================================
        // 📤 ЗАГРУЗКА
        // =====================================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile uploadedFile, string displayName, string category)
        {
            if (uploadedFile == null || uploadedFile.Length == 0)
                return RedirectToAction(nameof(Index));

            if (string.IsNullOrEmpty(category))
                return RedirectToAction(nameof(Index));

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var storedFileName = Guid.NewGuid() + Path.GetExtension(uploadedFile.FileName);
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await uploadedFile.CopyToAsync(stream);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var fileItem = new Library
            {
                FileName = displayName,
                StoredFileName = storedFileName,
                ContentType = uploadedFile.ContentType,
                Size = uploadedFile.Length,
                Category = category, // ✅ добавлено
                UploadedAt = DateTime.Now,
                UserId = userId
            };

            _context.Library.Add(fileItem);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // 📥 СКАЧАТЬ
        // =====================================================
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var file = await _context.Library.FindAsync(id);
            if (file == null)
                return NotFound();

            var path = Path.Combine(_env.WebRootPath, "uploads", file.StoredFileName);

            if (!System.IO.File.Exists(path))
                return NotFound();

            var extension = Path.GetExtension(file.StoredFileName);

            var downloadName = file.FileName.EndsWith(extension)
                ? file.FileName
                : file.FileName + extension;

            return PhysicalFile(path, file.ContentType, downloadName);
        }

        // =====================================================
        // 🗑 УДАЛИТЬ
        // =====================================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var file = await _context.Library
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
                return NotFound();

            // Безопасно получаем ID пользователя
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Forbid();

            if (!int.TryParse(userIdClaim, out int currentUserId))
                return Forbid();

            // Проверка владельца или администратора
            if (file.UserId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            // Удаляем файл с диска
            var path = Path.Combine(_env.WebRootPath, "uploads", file.StoredFileName);

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            // Удаляем из базы
            _context.Library.Remove(file);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}