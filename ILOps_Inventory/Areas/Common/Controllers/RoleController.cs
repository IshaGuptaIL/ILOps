using ILOps_Inventory.Areas.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;

namespace ILOps_Inventory.Areas.Common.Controllers
{
    [Area("Common")]
    public class RoleController : Controller
    {
        private readonly string _connectionString;

 
        // GET: /Common/Role (Main page with list)
        public RoleController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("bvactivation_Connection");
        }

        public IActionResult Index()
        {
            ViewData["HideWelcome"] = true;
            var roles = GetUserRolesSync();
            return View("~/Areas/Common/Views/Role/ManageRole.cshtml", roles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(short id)
        {
            try
            {
                // ✅ STEP 1: Check role exists
                var role = GetUserRoleByIdSync(id);
                if (role == null)
                {
                    TempData["Error"] = $"Role ID {id} not found";
                    return RedirectToAction("Index");
                }

                bool newStatus = !role.IsActive;

                // ✅ STEP 2: Direct UPDATE (No transaction needed for simple toggle)
                ExecuteWithConnectionSync(conn =>
                {
                    using var cmd = new SqlCommand("UPDATE userRole SET IsActive = @Active WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Active", newStatus);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    // ✅ DEBUG: Check rows affected
                    if (rowsAffected == 0)
                        throw new Exception($"No rows updated for Role ID: {id}");
                });

                TempData["Success"] = newStatus ? $"Role '{role.Name}' ACTIVATED!" : $"Role '{role.Name}' DEACTIVATED!";
            }
            catch (Exception ex)
            {
                // ✅ SHOW REAL ERROR
                TempData["Error"] = $"Toggle failed: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Role name required" });

            name = name.Trim();
            if (CheckRoleExistsSync(name))
                return Json(new { success = false, message = "Role exists" });

            try
            {
                ExecuteWithConnectionSync(conn =>
                {
                    using var transaction = conn.BeginTransaction();
                    try
                    {
                        using var cmd = new SqlCommand(@"
                    INSERT INTO userRole (Name, IsActive) 
                    OUTPUT INSERTED.Id 
                    VALUES (@Name, @Active)", conn, transaction);

                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Active", true); // ✅ DEFAULT IsActive = true

                        short newId = (short)cmd.ExecuteScalar();
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                });
                return Json(new { success = true, message = "Role created!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private List<UserRole> GetUserRolesSync()
        {
            var roles = new List<UserRole>();
            try
            {
                ExecuteWithConnectionSync(conn =>
                {
                    // ✅ CHANGED: ORDER BY Id DESC (newest first)
                    using var cmd = new SqlCommand("SELECT Id, Name, IsActive FROM userRole ORDER BY Id DESC", conn);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        roles.Add(new UserRole
                        {
                            Id = reader.GetInt16("Id"),
                            Name = reader.GetString("Name"),
                            IsActive = reader.GetBoolean("IsActive")
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Load failed: {ex.Message}";
            }
            return roles;
        }

        private UserRole? GetUserRoleByIdSync(short id)
        {
            UserRole? role = null;
            ExecuteWithConnectionSync(conn =>
            {
                using var cmd = new SqlCommand("SELECT Id, Name, IsActive FROM userRole WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    role = new UserRole
                    {
                        Id = reader.GetInt16("Id"),
                        Name = reader.GetString("Name"),
                        IsActive = reader.GetBoolean("IsActive")
                    };
                }
            });
            return role;
        }

        private bool CheckRoleExistsSync(string name)
        {
            int count = 0;
            ExecuteWithConnectionSync(conn =>
            {
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM userRole WHERE LOWER(LTRIM(RTRIM(Name))) = LOWER(@Name)", conn);
                cmd.Parameters.AddWithValue("@Name", name);
                count = (int)cmd.ExecuteScalar();
            });
            return count > 0;
        }

        private void ExecuteWithConnectionSync(Action<SqlConnection> action)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            action(conn);
        }

        // Simplified - other methods same as before
        private short GetNextRoleIdSync(SqlConnection conn, SqlTransaction trans)
        {
            using var cmd = new SqlCommand("SELECT ISNULL(MAX(Id), 0) + 1 FROM userRole", conn, trans);
            return Convert.ToInt16(cmd.ExecuteScalar());
        }

        private void InsertUserRoleSync(SqlConnection conn, SqlTransaction trans, short id, string name, bool active)
        {
            using var cmd = new SqlCommand("INSERT userRole (Id, Name, IsActive) VALUES (@Id, @Name, @Active)", conn, trans);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Active", active);
            cmd.ExecuteNonQuery();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateRole(short id, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return Json(new { success = false, message = "Name required" });

                name = name.Trim();
                if (CheckRoleNameExistsSync(id, name))
                    return Json(new { success = false, message = "Name already exists" });

                ExecuteWithConnectionSync(conn =>
                {
                    using var cmd = new SqlCommand("UPDATE userRole SET Name = @Name WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", name);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        throw new Exception("No role updated");
                });

                return Json(new { success = true, message = "Role updated!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private void UpdateUserRoleNameSync(SqlConnection conn, SqlTransaction trans, short id, string name)
        {
            using var cmd = new SqlCommand("UPDATE userRole SET Name = @Name WHERE Id = @Id", conn, trans);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.ExecuteNonQuery();
        }

        private bool CheckRoleNameExistsSync(short id, string name)
        {
            int count = 0;
            ExecuteWithConnectionSync(conn =>
            {
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM userRole WHERE Id != @Id AND LOWER(LTRIM(RTRIM(Name))) = LOWER(@Name)", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Name", name);
                count = (int)cmd.ExecuteScalar();
            });
            return count > 0;
        }
    }
}
