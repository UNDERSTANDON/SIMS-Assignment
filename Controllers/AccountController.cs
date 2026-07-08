using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Models;

namespace SIMS_WEB.Controllers
{
    public class AccountController : Controller
    {
        // In-memory demo users — stub only for UI demo
        private static readonly Dictionary<string, (string password, string role, string email)> _users = new()
        {
            { "admin",     ("admin123",   "Admin",   "admin@univ.edu") },
            { "giaovien",  ("faculty123", "Faculty", "faculty@univ.edu") },
            { "sinhvien",  ("student123", "Student", "student@univ.edu") },
        };

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var store = SimsDataStore.Instance;
            string key = model.Username.ToLower();

            if (store.LockUntil.TryGetValue(key, out var lockTime) && lockTime > DateTime.Now)
            {
                model.IsLocked = true;
                model.LockRemainingSeconds = (int)(lockTime - DateTime.Now).TotalSeconds;
                return View(model);
            }



            if (_users.TryGetValue(key, out var user) &&
                user.password == model.Password &&
                user.role == model.Role)
            {
                store.FailedAttempts.Remove(key);
                HttpContext.Session.SetString("Username", model.Username);
                HttpContext.Session.SetString("Role", model.Role);

                return model.Role switch
                {
                    "Admin"   => RedirectToAction("Index", "Dashboard"),
                    "Faculty" => RedirectToAction("Index", "Grades"),
                    "Student" => RedirectToAction("Index", "StudentDashboard"),
                    _         => RedirectToAction("Index", "Dashboard")
                };
            }

            store.FailedAttempts.TryGetValue(key, out int attempts);
            attempts++;
            store.FailedAttempts[key] = attempts;

            if (attempts >= 5)
            {
                store.LockUntil[key] = DateTime.Now.AddMinutes(15);
                model.IsLocked = true;
                model.LockRemainingSeconds = 15 * 60;
            }
            else
            {
                model.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không hợp lệ";
                model.FailedAttempts = attempts;
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string key = model.Username.ToLower();
            if (_users.ContainsKey(key))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            _users[key] = (model.Password, model.Role, model.Email);
            TempData["Success"] = $"Đăng ký thành công! Bạn có thể đăng nhập với tài khoản {model.Username}.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login");

            return View(new CreateUserViewModel());
        }

        [HttpPost]
        public IActionResult CreateUser(CreateUserViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login");

            if (!ModelState.IsValid) return View(model);

            string key = model.Username.ToLower();
            if (_users.ContainsKey(key))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            _users[key] = (model.Password, model.Role, model.Email);
            model.IsSuccess = true;
            model.Message = $"Đã tạo tài khoản {model.Username} ({model.Role}) thành công!";
            ModelState.Clear();
            return View(new CreateUserViewModel { Message = model.Message, IsSuccess = true });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
