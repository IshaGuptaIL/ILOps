using ILOps_Inventory.Areas.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace ILOps_Inventory.Areas.Common.Controllers
{
    [Area("Common")]
    public class RolePermissionController : Controller
    {
        private readonly string _connectionString;

        public RolePermissionController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("bvactivation_Connection");
        }

    

        public IActionResult Index()
        {
            ViewData["HideWelcome"] = true;

            var model = new RolePermissionViewModel
            {
                ActiveRoles = GetActiveRolesSync(),
                Menus = GetMenusSync()
            };

            return View("~/Areas/Common/Views/Role/RolePermission.cshtml", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SavePermissions()
        {
            System.Diagnostics.Debug.WriteLine("🔥🚀🚀 METHOD ENTRY - 100% HIT!");
            Console.WriteLine("🔥🚀🚀 METHOD ENTRY - 100% HIT!");

            try
            {
                var roleIdStr = Request.Form["roleId"].FirstOrDefault();
                var menus = Request.Form["selectedMenus"].ToArray();

                System.Diagnostics.Debug.WriteLine($"🔥 RoleId='{roleIdStr}' Menus={menus.Length}");
                Console.WriteLine($"🔥 RoleId='{roleIdStr}' Menus={menus.Length}");

                if (short.TryParse(roleIdStr, out short roleId) && roleId > 0)
                {
                    // 🔥 DELETE OLD permissions for this role
                    ExecuteWithConnectionSync(conn =>
                    {
                        using var cmd = new SqlCommand("DELETE FROM RolePermissions WHERE RoleId = @RoleId", conn);
                        cmd.Parameters.AddWithValue("@RoleId", roleId);
                        cmd.ExecuteNonQuery();
                    });

                    // 🔥 INSERT NEW permissions
                    int savedCount = 0;
                    ExecuteWithConnectionSync(conn =>
                    {
                        foreach (var menuIdStr in menus)
                        {
                            if (int.TryParse(menuIdStr, out int menuId))
                            {
                                using var cmd = new SqlCommand(
                                    "INSERT INTO RolePermissions (RoleId, MenuId) VALUES (@RoleId, @MenuId)", conn);
                                cmd.Parameters.AddWithValue("@RoleId", roleId);
                                cmd.Parameters.AddWithValue("@MenuId", menuId);
                                savedCount += cmd.ExecuteNonQuery();
                            }
                        }
                    });

                    TempData["Debug"] = $"✅ {savedCount} permissions SAVED for Role '{roleId}'!";
                    System.Diagnostics.Debug.WriteLine($"✅ DATABASE SAVE SUCCESS: {savedCount} rows");
                }
                else
                {
                    TempData["Debug"] = "❌ Select valid Role first!";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR: {ex.Message}");
                TempData["Debug"] = $"❌ ERROR: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpGet]
        public IActionResult GetRolePermissions(string roleId)
        {
            if (!short.TryParse(roleId, out short parsedRoleId))
                return Json(new List<int>());

            var permittedMenus = new List<int>();
            ExecuteWithConnectionSync(conn =>
            {
                using var cmd = new SqlCommand("SELECT MenuId FROM RolePermissions WHERE RoleId = @RoleId", conn);
                cmd.Parameters.AddWithValue("@RoleId", parsedRoleId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    permittedMenus.Add(reader.GetInt32(0));
                }
            });
            return Json(permittedMenus);
        }

        [HttpGet]
        public IActionResult GetUserMenuPermissions()
        {
            var roleId = HttpContext.Session.GetInt32("UserRoleId");
            if (!roleId.HasValue)
                return Json(new List<object>());

            var permittedMenus = new List<object>();

            try
            {
                ExecuteWithConnectionSync(conn =>
                {
                    using var cmd = new SqlCommand(@"
                SELECT 
                    umm.Id, umm.MenuName, umm.Icon, umm.Controller, umm.ParentId
                FROM userMasterMenus umm
                INNER JOIN RolePermissions rp ON umm.Id = rp.MenuId
                WHERE rp.RoleId = @RoleId", conn);

                    cmd.Parameters.AddWithValue("@RoleId", roleId.Value);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        // Id, ParentId = smallint → GetInt16, phir Convert.ToInt32
                        short idValue = reader.GetInt16(0);
                        string menuName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        string icon = reader.IsDBNull(2) ? "bi-house-door" : reader.GetString(2);
                        string? controller = reader.IsDBNull(3) ? null : reader.GetString(3);
                        short parentRaw = reader.IsDBNull(4) ? (short)0 : reader.GetInt16(4);

                        permittedMenus.Add(new
                        {
                            Id = (int)idValue,
                            MenuName = menuName,
                            Icon = icon,
                            Controller = controller,
                            ParentId = (int)parentRaw
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetUserMenuPermissions ERROR: " + ex.Message);
            }

            return Json(permittedMenus);
        }

        // ===== Helper methods =====

        private List<UserRole> GetActiveRolesSync()
        {
            var roles = new List<UserRole>();
            ExecuteWithConnectionSync(conn =>
            {
                using var cmd = new SqlCommand("SELECT Id, Name FROM userRole WHERE IsActive = 1 ORDER BY Name", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    roles.Add(new UserRole
                    {
                        Id = reader.GetInt16(0),
                        Name = reader.GetString(1)
                    });
                }
            });
            return roles;
        }

        private List<Menu> GetMenusSync()
        {
            var menus = new List<Menu>();
            ExecuteWithConnectionSync(conn =>
            {
                using var cmd = new SqlCommand("SELECT Id, MenuName, ParentId, Controller FROM userMasterMenus", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    menus.Add(new Menu
                    {
                        MenuId = reader.GetInt16(0),
                        MenuName = reader.GetString(1),
                        ParentMenuId = reader.IsDBNull(2) ? (short?)null : reader.GetInt16(2),
                        Controller = reader.IsDBNull(3) ? null : reader.GetString(3)
                    });
                }
            });
            return menus;
        }

        private void ExecuteWithConnectionSync(Action<SqlConnection> action)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            action(conn);
        }

        // ===== ViewModel =====
        public class RolePermissionViewModel
        {
            public short RoleId { get; set; }
            public List<UserRole> ActiveRoles { get; set; } = new();
            public List<Menu> Menus { get; set; } = new();
            public List<int> SelectedMenus { get; set; } = new();
        }
    }
}
