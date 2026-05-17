using System.Diagnostics;
using LLama;
using LLama.Common;
using LLama.Sampling;
using MathTime.Data;
using MathTime.Models;
using Microsoft.AspNetCore.Mvc;

namespace MathTime.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationContext _db;

        

        public HomeController(ApplicationContext context, ILogger<HomeController> logger)
        {
            _db = context;
            _logger = logger;
        }

        public ActionResult Index()
        {            
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [HttpPost]
        [HttpPost]
        public IActionResult ChatBot([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Сообщение не может быть пустым." });

            string message = request.Message.ToLower().Trim();

            // Список ключевых слов и ответов
            var faq = new List<(string[] keywords, string answer)>
    {
        (
            new[] { "регистрация", "зарегистрироваться", "создать аккаунт", "регаться" },
            "Для регистрации нажмите кнопку «Регистрация» на главной странице и заполните необходимые данные."
        ),

        (
            new[] { "вход", "войти", "авторизация", "логин" },
            "Для входа в систему используйте кнопку «Войти» и укажите Email и пароль."
        ),

        (
            new[] { "загрузить файл", "добавить файл", "загрузка", "загрузить материал" },
            "Перейдите в раздел «Файловый центр» или «Библиотека», заполните форму и нажмите кнопку «Загрузить»."
        ),

        (
            new[] { "скачать", "скачивание", "загрузить себе файл" },
            "Нажмите кнопку «Скачать» рядом с нужным материалом."
        ),

        (
            new[] { "создать статью", "добавить статью", "публикация статьи", "написать статью" },
            "Для создания статьи перейдите в раздел статей и заполните форму публикации."
        ),

        (
            new[] { "удалить статью", "удаление статьи" },
            "Удалить статью может только её автор или администратор."
        ),

        (
            new[] { "профиль", "изменить профиль", "редактировать профиль", "изменить данные" },
            "Для изменения данных перейдите в личный кабинет и нажмите кнопку «Редактировать»."
        ),

        (
            new[] { "код подтверждения", "не приходит код", "подтверждение почты", "код на почту" },
            "Проверьте папку «Спам» или воспользуйтесь функцией повторной отправки кода."
        ),

        (
            new[] { "пароль", "забыл пароль", "восстановить пароль" },
            "Обратитесь к администратору сайта для восстановления доступа."
        ),

        (
            new[] { "удалить файл", "кто может удалить файл", "удаление файла" },
            "Удалять файлы может автор файла или администратор системы."
        ),

        (
            new[] { "комментарий", "оставить комментарий", "написать комментарий" },
            "Для добавления комментария откройте статью и введите текст комментария."
        ),

        (
            new[] { "лайк", "поставить лайк", "оценить статью" },
            "Нажмите кнопку лайка под публикацией."
        ),

        (
            new[] { "библиотека", "учебники", "пособия" },
            "Библиотека предназначена для хранения учебников и учебных пособий."
        ),

        (
            new[] { "файловый центр", "материалы", "презентации", "конспекты" },
            "Файловый центр используется для обмена презентациями, конспектами и методическими материалами."
        )
    };

            // Поиск совпадений по ключевым словам
            foreach (var item in faq)
            {
                if (item.keywords.Any(k => message.Contains(k)))
                {
                    return Json(new
                    {
                        response = item.answer
                    });
                }
            }

            // Ответ по умолчанию
            return Json(new
            {
                response = "Извините, я не смог найти ответ на ваш вопрос. Попробуйте сформулировать его иначе."
            });
        }


    }
}

