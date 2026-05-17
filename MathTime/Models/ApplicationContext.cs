using MathTime.Models;
using Microsoft.EntityFrameworkCore;

namespace MathTime.Data
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Article> Articles => Set<Article>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Like> Likes => Set<Like>();
        public DbSet<FileItem> Files => Set<FileItem>();
        public DbSet<Library> Library => Set<Library>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----------------------------
            // Таблицы и индексы
            // ----------------------------

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Article>().ToTable("Articles");
            modelBuilder.Entity<Comment>().ToTable("Comments");
            modelBuilder.Entity<Library>().ToTable("Library");
            modelBuilder.Entity<Like>().ToTable("Likes");
            modelBuilder.Entity<FileItem>().ToTable("FileItems"); // сделал множественное число для консистентности

            // ----------------------------
            // Связи
            // ----------------------------

            // Comment → Article (Cascade delete)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Article)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Like → User (Restrict delete)
            modelBuilder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Like → Article (Cascade delete)
            modelBuilder.Entity<Like>()
                .HasOne(l => l.Article)
                .WithMany(a => a.Likes)
                .HasForeignKey(l => l.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Article)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);



            // ----------------------------
            // Дополнительные индексы (опционально)
            // ----------------------------
            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.ArticleId);
            modelBuilder.Entity<Like>()
                .HasIndex(l => l.ArticleId);
            modelBuilder.Entity<Like>()
                .HasIndex(l => l.UserId);
        }
    }
}
