using ILOps_Inventory.Areas.Common.Models;   // UserMaster, UserRoleId yahan se
using ILOps_Inventory.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ILOps_Inventory.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _connectionString;

        public AccountController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                               ?? config.GetConnectionString("bvactivation_Connection")
                               ?? "Server=.;Database=ILOpsDB;Trusted_Connection=true;";
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("IsLoggedIn") == "true")
                return RedirectToAction("Index", "Dashboard");

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = ValidateUser(model.Email, model.Password);

            if (user != null)
            {
                // ✅ Session from DB
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetInt32("UserRoleId", user.UserRoleId);
                HttpContext.Session.SetInt32("UserId", user.Id);

                return RedirectToAction("Index", "Dashboard");
            }

            // Optional fallback static test user
            if (model.Email == "abc@gmail.com" && model.Password == "1234")
            {
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("UserEmail", model.Email);
                HttpContext.Session.SetString("UserName", "Test User");
                HttpContext.Session.SetInt32("UserRoleId", 1); // Super Admin
                HttpContext.Session.SetInt32("UserId", 999);

                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError("", "Invalid email or password!");
            return View(model);
        }

        private UserModel? ValidateUser(string email, string password)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using var cmd = new SqlCommand(@"
                    SELECT Id, FullName, Email, UserRoleId, IsActive 
                    FROM UserMaster 
                    WHERE Email = @Email AND Password = @Password AND IsActive = 1", conn);

                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new UserModel
                    {
                        Id = reader.GetInt16(0),
                        FullName = reader.GetString(1),
                        Email = reader.GetString(2),
                        UserRoleId = reader.GetInt16(3),
                        IsActive = reader.GetBoolean(4)
                    };
                }
            }
            catch
            {
            }

            return null;
        }

     
    }
}
