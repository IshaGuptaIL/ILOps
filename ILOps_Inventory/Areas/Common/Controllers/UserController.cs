using ILOps_Inventory.Areas.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace ILOps_Inventory.Areas.Common.Controllers
{
    [Area("Common")]
    public class UserController : Controller
    {
        private readonly string _connectionString;

        public UserController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("bvactivation_Connection") ?? "";
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var model = new UserIndexViewModel();
            int skip = (page - 1) * pageSize;

            // ✅ Total count for pager
            using var connCount = new SqlConnection(_connectionString);
            await connCount.OpenAsync();
            using var cmdCount = new SqlCommand("SELECT COUNT(*) FROM userMaster WHERE IsActive = 1", connCount);
            model.TotalUsers = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
            model.CurrentPage = page;
            model.PageSize = pageSize;
            model.TotalPages = (int)Math.Ceiling((double)model.TotalUsers / pageSize);

            // ✅ Paginated users
            using var connUsers = new SqlConnection(_connectionString);
            await connUsers.OpenAsync();


            using var cmdUsers = new SqlCommand(@"
        SELECT u.Id, u.FullName, u.Email, u.ContactNumber, u.Address, u.State, 
               u.ZipCode, u.Country, u.City, u.UserRoleId, 
               ISNULL(u.IsActive, 1) AS IsActive,
               ISNULL(u.CreatedDate, SYSDATETIME()) AS CreatedDate,
               ISNULL(r.Name, 'No Role') AS RoleName
        FROM userMaster u LEFT JOIN userRole r ON u.UserRoleId = r.Id 
        ORDER BY u.CreatedDate DESC
        OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY", connUsers);

            cmdUsers.Parameters.AddWithValue("@skip", skip);
            cmdUsers.Parameters.AddWithValue("@pageSize", pageSize);


            using var readerUsers = await cmdUsers.ExecuteReaderAsync();
            while (await readerUsers.ReadAsync())
            {
                model.Users.Add(new UserModel
                {
                    Id = Convert.ToInt32(readerUsers["Id"]),  // ✅ Safe for INT columns
                    FullName = readerUsers["FullName"]?.ToString() ?? "",
                    Email = readerUsers["Email"]?.ToString() ?? "",
                    ContactNumber = readerUsers["ContactNumber"]?.ToString() ?? "",  // Note: You used ContactNumber, not PhoneNumber
                    Address = readerUsers["Address"]?.ToString() ?? "",
                    State = readerUsers["State"]?.ToString() ?? "",
                    ZipCode = readerUsers["ZipCode"]?.ToString() ?? "",
                    Country = readerUsers["Country"]?.ToString() ?? "",
                    City = readerUsers["City"]?.ToString() ?? "",
                    UserRoleId = Convert.ToInt16(readerUsers["UserRoleId"]),  // ✅ Safe conversion
                    IsActive = Convert.ToBoolean(readerUsers["IsActive"]),
                    CreatedDate = readerUsers["CreatedDate"] == DBNull.Value
           ? DateTime.Now : Convert.ToDateTime(readerUsers["CreatedDate"]),
                    RoleName = readerUsers["RoleName"]?.ToString() ?? "No Role"
                });
            }

            // ✅ Active Roles for dropdown (separate conn)
            using var connRoles = new SqlConnection(_connectionString);
            await connRoles.OpenAsync();

            using var cmdRoles = new SqlCommand("SELECT Id, Name FROM userRole WHERE IsActive = 1 ORDER BY Name", connRoles);
            using var readerRoles = await cmdRoles.ExecuteReaderAsync();

            while (await readerRoles.ReadAsync())
            {
                model.Roles.Add(new UserRole
                {
                    Id = readerRoles.GetInt16("Id"),
                    Name = readerRoles["Name"]?.ToString() ?? ""
                });
            }

            return View("~/Areas/Common/Views/Role/User.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                SELECT u.Id, u.FullName, u.Email, u.ContactNumber, u.Address, u.State, 
                       u.ZipCode, u.Country, u.City, u.UserRoleId, 
                       ISNULL(r.Name, 'No Role') AS RoleName, u.IsActive, u.CreatedDate
                FROM userMaster u LEFT JOIN userRole r ON u.UserRoleId = r.Id
                WHERE u.Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var readerUsers = await cmd.ExecuteReaderAsync();
            if (await readerUsers.ReadAsync())
            {
                return Json(new  // ✅ Consistent camelCase JSON
                {
                    id = Convert.ToInt32(readerUsers["Id"]),
                    fullName = readerUsers["FullName"]?.ToString() ?? "",
                    email = readerUsers["Email"]?.ToString() ?? "",
                    ContactNumber = readerUsers["ContactNumber"]?.ToString() ?? "",
                    address = readerUsers["Address"]?.ToString() ?? "",
                    state = readerUsers["State"]?.ToString() ?? "",
                    zipCode = readerUsers["ZipCode"]?.ToString() ?? "",
                    country = readerUsers["Country"]?.ToString() ?? "",
                    city = readerUsers["City"]?.ToString() ?? "",
                    RoleName = readerUsers["RoleName"].ToString(),
                    userRoleId = Convert.ToInt16(readerUsers["UserRoleId"]),
                    isActive = Convert.ToBoolean(readerUsers["IsActive"]),

                });
            }
            return Json(null);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] UserModel model)
        {
            // ✅ Trim & validate
            model.FullName = (model.FullName ?? "").Trim();
            model.Email = (model.Email ?? "").Trim();
            model.ContactNumber = (model.ContactNumber ?? "").Trim();
            model.Address = (model.Address ?? "").Trim();
            model.State = (model.State ?? "").Trim();
            model.ZipCode = (model.ZipCode ?? "").Trim();
            model.Country = (model.Country ?? "").Trim();
            model.City = (model.City ?? "").Trim();
            model.Password ??= "";

            if (string.IsNullOrWhiteSpace(model.FullName))
                ModelState.AddModelError("FullName", "Full Name is required");
            if (string.IsNullOrWhiteSpace(model.Email) || !new EmailAddressAttribute().IsValid(model.Email))
                ModelState.AddModelError("Email", "Valid Email is required");
            if (model.UserRoleId <= 0)
                ModelState.AddModelError("UserRoleId", "Please select a role");

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(k => k.Key, v => string.Join(", ", v.Value.Errors.Select(e => e.ErrorMessage)));
                return Json(new { success = false, message = "Validation failed", errors });
            }

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                INSERT INTO userMaster (FullName, Email, ContactNumber, Address, State, ZipCode, Country, City, Password, UserRoleId, IsActive, CreatedDate)
                VALUES (@FullName, @Email, @ContactNumber, @Address, @State, @ZipCode, @Country, @City, @Password, @UserRoleId, 1, SYSDATETIME());
                SELECT SCOPE_IDENTITY();", conn);

            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@ContactNumber", (object)model.ContactNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object)model.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@State", (object)model.State ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ZipCode", (object)model.ZipCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Country", (object)model.Country ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@City", (object)model.City ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Password", model.Password);  // TODO: Hash in production
            cmd.Parameters.AddWithValue("@UserRoleId", model.UserRoleId);  // ✅ Sirf ID save

            var userId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return Json(new { success = true, userId });
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromForm] UserModel model)
        {
            // ✅ Trim & validate (no password for update)
            model.FullName = (model.FullName ?? "").Trim();
            model.Email = (model.Email ?? "").Trim();
            model.ContactNumber = (model.ContactNumber ?? "").Trim();
            model.Address = (model.Address ?? "").Trim();
            model.State = (model.State ?? "").Trim();
            model.ZipCode = (model.ZipCode ?? "").Trim();
            model.Country = (model.Country ?? "").Trim();
            model.City = (model.City ?? "").Trim();

            if (string.IsNullOrWhiteSpace(model.FullName))
                ModelState.AddModelError("FullName", "Full Name is required");
            if (string.IsNullOrWhiteSpace(model.Email) || !new EmailAddressAttribute().IsValid(model.Email))
                ModelState.AddModelError("Email", "Valid Email is required");
            if (model.UserRoleId <= 0)
                ModelState.AddModelError("UserRoleId", "Please select a role");

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(k => k.Key, v => string.Join(", ", v.Value.Errors.Select(e => e.ErrorMessage)));
                return Json(new { success = false, message = "Validation failed", errors });
            }

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                UPDATE userMaster SET FullName=@FullName, Email=@Email, ContactNumber=@ContactNumber, 
                                       Address=@Address, State=@State, ZipCode=@ZipCode, 
                                       Country=@Country, City=@City, UserRoleId=@UserRoleId, IsActive=@IsActive
                WHERE Id=@Id", conn);

            cmd.Parameters.AddWithValue("@Id", model.Id);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@ContactNumber", (object)model.ContactNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object)model.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@State", (object)model.State ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ZipCode", (object)model.ZipCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Country", (object)model.Country ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@City", (object)model.City ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserRoleId", model.UserRoleId);  // ✅ Sirf ID update
            cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

            await cmd.ExecuteNonQueryAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // ✅ Clean permissions first
            using var cmdPerm = new SqlCommand("DELETE FROM UserPermission WHERE UserId = @id", conn);
            cmdPerm.Parameters.AddWithValue("@id", id);
            await cmdPerm.ExecuteNonQueryAsync();

            // ✅ Soft delete
            using var cmd = new SqlCommand("UPDATE userMaster SET IsActive=0 WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();

            return Json(new { success = true });
        }
    }
}
