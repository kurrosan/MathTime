using System.ComponentModel.DataAnnotations;

namespace MathTime.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int ArticleId { get; set; }
        public Article Article { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        [Required(ErrorMessage = "Введите текст комментария")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
