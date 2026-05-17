using System;
using System.ComponentModel.DataAnnotations;

namespace MathTime.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MaxLength(50)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = null!;

        [Required, MaxLength(50)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = null!;

        [Required, MaxLength(20)]
        [Phone]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = null!;

        [Required, EmailAddress, MaxLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают.")]
        [Display(Name = "Подтверждение пароля")]
        public string ConfirmPassword { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Дата рождения")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Display(Name = "Пол")]
        public string Gender { get; set; } = null!; // Male/Female/Other

        // Новое поле для загрузки аватара
        public IFormFile? Avatar { get; set; }
    }
}