using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Models;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication;
using SIMS_Assignment.Services;

namespace SIMS_WEB.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuth _auth;
        private readonly IDataStorage _storage;

        public AccountController(IAuth auth, IDataStorage storage)
        {
            _auth = auth;
            _storage = storage;
        }

        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return string.Empty;

            return role.Trim().ToLowerInvariant() switch
            {
                "admin" => "Admin",
                "faculty" or "lecturer" => "Faculty",
                "student" => "Student",
                _ => role.Trim()
            };
        }

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
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

            var normalizedRole = NormalizeRole(model.Role);

            // Verify authentication via IAuth which reads users.csv
            bool authenticated = await _auth.LoginAsync(model.Username, model.Password);
            if (authenticated)
            {
                var user = await _storage.GetUserByNameAsync(model.Username);
                if (user != null && NormalizeRole(user.Role) == normalizedRole)
                {
                    store.FailedAttempts.Remove(key);
                    HttpContext.Session.SetString("Username", user.Name);
                    HttpContext.Session.SetString("Role", normalizedRole);

                    // If it is a student, associate their StudentId in session
                    if (normalizedRole == "Student")
                    {
                        var studentObj = store.Students.FirstOrDefault(s => s.FullName.Equals(user.Name, StringComparison.OrdinalIgnoreCase)
                                                                           || s.StudentId.Equals(user.Name, StringComparison.OrdinalIgnoreCase));
                        if (studentObj != null)
                        {
                            HttpContext.Session.SetString("StudentId", studentObj.StudentId);
                        }
                    }

                    return normalizedRole switch
                    {
                        "Admin"   => RedirectToAction("Index", "Dashboard"),
                        "Faculty" => RedirectToAction("Index", "Grades"),
                        "Student" => RedirectToAction("Index", "StudentDashboard"),
                        _         => RedirectToAction("Index", "Dashboard")
                    };
                }
                else
                {
                    model.ErrorMessage = "Vai trò chọn không đúng với tài khoản đăng ký";
                }
            }
            else
            {
                model.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không hợp lệ";
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
                if (string.IsNullOrEmpty(model.ErrorMessage))
                {
                    model.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không hợp lệ";
                }
                model.FailedAttempts = attempts;
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _storage.GetUserByNameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            var normalizedRole = NormalizeRole(model.Role);
            SIMS_Assignment.Models.User newUser = normalizedRole switch
            {
                "Admin" => new SIMS_Assignment.Models.Admin { Name = model.Username, Role = normalizedRole, Email = model.Email, FullName = model.Username },
                "Faculty" => new SIMS_Assignment.Models.Lecturer { Name = model.Username, Role = normalizedRole, Email = model.Email, FullName = model.Username },
                _ => new SIMS_Assignment.Models.Student { Name = model.Username, Role = normalizedRole, Email = model.Email, FullName = model.Username }
            };

            var success = await _auth.RegisterAsync(newUser, model.Password);
            if (!success)
            {
                ModelState.AddModelError("", "Đăng ký không thành công. Vui lòng thử lại.");
                return View(model);
            }

            TempData["Success"] = $"Đăng ký thành công! Bạn có thể đăng nhập với tài khoản {model.Username}.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser([FromQuery] string? role)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login");

            var vm = new CreateUserViewModel();
            if (!string.IsNullOrEmpty(role))
            {
                vm.Role = role;
            }

            var store = SimsDataStore.Instance;
            var users = await _storage.GetAllUsersAsync();
            var nextStudent = GenerateNextStudentId(store.Students.Select(s => s.StudentId));
            var nextLecturer = GenerateNextLecturerId(users.Where(u => u.Role == "Faculty" || u.Role == "Lecturer").Select(u => u.Name));

            ViewBag.NextStudentId = nextStudent;
            ViewBag.NextLecturerId = nextLecturer;

            var normalizedRole = NormalizeRole(role ?? "");
            if (normalizedRole == "Student")
            {
                vm.Username = nextStudent;
            }
            else if (normalizedRole == "Faculty")
            {
                vm.Username = nextLecturer;
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login");

            var store = SimsDataStore.Instance;
            var users = await _storage.GetAllUsersAsync();
            var nextStudent = GenerateNextStudentId(store.Students.Select(s => s.StudentId));
            var nextLecturer = GenerateNextLecturerId(users.Where(u => u.Role == "Faculty" || u.Role == "Lecturer").Select(u => u.Name));

            ViewBag.NextStudentId = nextStudent;
            ViewBag.NextLecturerId = nextLecturer;

            if (!ModelState.IsValid) return View(model);

            var existingUser = await _storage.GetUserByNameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            var normalizedRole = NormalizeRole(model.Role);
            SIMS_Assignment.Models.User newUser = normalizedRole switch
            {
                "Admin" => new SIMS_Assignment.Models.Admin { Name = model.Username, Role = normalizedRole, Email = model.Email, FullName = model.FullName },
                "Faculty" => new SIMS_Assignment.Models.Lecturer { Name = model.Username, Role = normalizedRole, Email = model.Email, FullName = model.FullName },
                _ => new SIMS_Assignment.Models.Student { Name = model.Username, Role = normalizedRole, Email = model.Email, FullName = model.FullName }
            };

            var success = await _auth.RegisterAsync(newUser, model.Password);
            if (!success)
            {
                ModelState.AddModelError("", "Tạo tài khoản không thành công. Vui lòng thử lại.");
                return View(model);
            }

            // Sync student profile immediately if role is Student (so it has email right away)
            if (normalizedRole == "Student")
            {
                var studentManager = HttpContext.RequestServices.GetService(typeof(IStudentManager)) as IStudentManager;
                if (studentManager != null)
                {
                    await studentManager.GetAllAsync();
                }
            }

            // Recalculate next IDs after successful creation
            var updatedUsers = await _storage.GetAllUsersAsync();
            var updatedNextStudent = GenerateNextStudentId(store.Students.Select(s => s.StudentId));
            var updatedNextLecturer = GenerateNextLecturerId(updatedUsers.Where(u => u.Role == "Faculty" || u.Role == "Lecturer").Select(u => u.Name));

            ViewBag.NextStudentId = updatedNextStudent;
            ViewBag.NextLecturerId = updatedNextLecturer;

            model.IsSuccess = true;
            model.Message = $"Đã tạo tài khoản {model.Username} ({normalizedRole}) thành công!";
            ModelState.Clear();
            return View(new CreateUserViewModel { Message = model.Message, IsSuccess = true });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public static string GenerateNextStudentId(IEnumerable<string> existingIds)
        {
            int maxNum = 0;
            foreach (var id in existingIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                var cleanId = id.Replace(" ", "").Trim();
                if (cleanId.StartsWith("SV", StringComparison.OrdinalIgnoreCase))
                {
                    var numStr = cleanId.Substring(2);
                    if (int.TryParse(numStr, out var num))
                    {
                        if (num > maxNum) maxNum = num;
                    }
                }
            }
            return $"SV{(maxNum + 1).ToString("D7")}";
        }

        public static string GenerateNextLecturerId(IEnumerable<string> existingIds)
        {
            int maxNum = 0;
            foreach (var id in existingIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                var cleanId = id.Replace(" ", "").Trim();
                if (cleanId.StartsWith("GV", StringComparison.OrdinalIgnoreCase))
                {
                    var numStr = cleanId.Substring(2);
                    if (int.TryParse(numStr, out var num))
                    {
                        if (num > maxNum) maxNum = num;
                    }
                }
            }
            return $"GV{(maxNum + 1).ToString("D7")}";
        }
    }
}
