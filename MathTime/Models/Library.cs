using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathTime.Models
{
    public class Library
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = null!;

        [Required]
        public string StoredFileName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = null!;

        [Required]
        public long Size { get; set; }

        // ✅ ДОБАВЛЕНО ПОЛЕ КАТЕГОРИИ
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = null!;

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Связь с пользователем
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}