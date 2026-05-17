using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MathTime.Data;
using MathTime.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathTime.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class ArticlesController : Controller
    {
        private readonly ApplicationContext _context;

        public ArticlesController(ApplicationContext context)
        {
            _context = context;
        }

        // ===============================
        // Получение списка статей
        // ===============================
        public async Task<IActionResult> Index()
        {
            var list = await _context.Articles
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(list);
        }

        // ===============================
        // Создание статьи
        // ===============================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] Article dto)
        {
            ModelState.Remove("Author");

            if (string.IsNullOrWhiteSpace(dto.Content))
                ModelState.AddModelError("Content", "Введите содержание статьи");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var article = new Article
            {
                Title = dto.Title,
                Content = dto.Content,
                Author = $"{user.FirstName} {user.LastName}".Trim(),
                AuthorId = user.Id,
                PublishedAt = DateTime.Now
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ===============================
        // Получение статьи
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetArticle(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Likes)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (article == null) return NotFound();

            return Json(new
            {
                id = article.Id,
                title = article.Title,
                author = article.Author,
                content = article.Content,
                publishedAt = article.PublishedAt,
                likes = article.Likes.Count,
                comments = article.Comments.Select(c => new
                {
                    id = c.Id,
                    content = c.Content,
                    userName = $"{c.User.FirstName} {c.User.LastName}".Trim(),
                    userId = c.UserId,
                    createdAt = c.CreatedAt.ToString("g")
                })
            });
        }

        // ===============================
        // Добавление комментария
        // ===============================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddComment(int articleId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest(new { success = false, error = "Комментарий не может быть пустым." });

            var article = await _context.Articles.FindAsync(articleId);
            if (article == null) return NotFound();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            var comment = new Comment
            {
                ArticleId = articleId,
                Content = content,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);

            return Json(new
            {
                success = true,
                comment = new
                {
                    id = comment.Id,
                    content = comment.Content,
                    userName = $"{user.FirstName} {user.LastName}".Trim(),
                    createdAt = comment.CreatedAt.ToString("g")
                }
            });
        }

        // ===============================
        // Лайк
        // ===============================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Like(int articleId)
        {
            var article = await _context.Articles
                .Include(a => a.Likes)
                .FirstOrDefaultAsync(a => a.Id == articleId);

            if (article == null) return NotFound();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            var existingLike = article.Likes.FirstOrDefault(l => l.UserId == userId);

            if (existingLike != null)
                _context.Likes.Remove(existingLike);
            else
                article.Likes.Add(new Like { UserId = userId });

            await _context.SaveChangesAsync();
            return Json(new { success = true, count = article.Likes.Count });
        }

        // ===============================
        // Редактирование статьи
        // ===============================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditArticle(int id, string title, string content)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return NotFound();

            // Получаем текущего пользователя
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            // Проверка прав: либо автор статьи, либо админ
            if (article.AuthorId != userId && !User.IsInRole("Admin"))
                return Forbid();

            article.Title = title;
            article.Content = content;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ===============================
        // Удаление статьи
        // ===============================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Comments) // можно оставить для логики
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null) return NotFound();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            // Проверка прав: либо автор статьи, либо админ
            if (article.AuthorId != userId && !User.IsInRole("Admin"))
                return Forbid();

            // Мягкое удаление
            article.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ===============================
        // Редактирование комментария
        // ===============================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditComment(int id, string content)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            if (comment.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            comment.Content = content;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ===============================
        // Удаление комментария
        // ===============================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            if (comment.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
