using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathTime.Models
{
    [Table("Users")]
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, MaxLength(20)]
        public string Phone { get; set; }

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required, MaxLength(10)]
        public string Gender { get; set; }

        [Required, MaxLength(50)]
        public string Role { get; set; } = "User";
        public bool IsBlocked { get; set; } = false; // Для блокировки

        public bool EmailVerified { get; set; } = false;
        [MaxLength(6)]
        public string? VerificationCode { get; set; }
        public byte[]? Avatar { get; set; }

        [NotMapped]
        public string Name => $"{FirstName} {LastName}";

        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
