using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MathTime.Models
{
    public class Article
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название статьи")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Введите содержание статьи")]
        public string Content { get; set; }

        // Имя автора для отображения
        public string Author { get; set; }

        // ID пользователя, который создал статью (для проверки прав)
        public int AuthorId { get; set; }

        public DateTime PublishedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
    }
}
