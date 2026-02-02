using ILOps_Inventory.Areas.Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace ILOps_Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    public class InventoryTypeController : Controller
    {
        private readonly string _connectionString;

        public InventoryTypeController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("bvactivation_Connection");
        }

        public IActionResult Index()
        {
            return View("~/Areas/Inventory/Views/Inventory/InventoryType.cshtml",
                new List<CostEntry>());
        }

        // ✅ PAGED DATA
        [HttpGet]
        public IActionResult GetData(string entryType, int page = 1, int pageSize = 10)
        {
            var (data, totalCount) = LoadDataByTypePaged(entryType ?? "HCC", page, pageSize);

            return Json(new
            {
                data,
                totalCount,
                page,
                pageSize
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        // ✅ ADD
        [HttpPost]
        public IActionResult Add([FromBody] JsonElement body)
        {
            string name = body.GetProperty("Name").GetString();
            string type = body.GetProperty("TableType").GetString();

            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, error = "Name required" });

            string query = @"INSERT INTO tblMan (Name, InventoryType)
                             VALUES (@Name, @Type)";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Name", name.Trim());
            cmd.Parameters.AddWithValue("@Type", type);

            conn.Open();
            cmd.ExecuteNonQuery();

            return Json(new { success = true });
        }

        // ✅ UPDATE
        [HttpPatch]
        public IActionResult Update([FromBody] JsonElement body)
        {
            int id = body.GetProperty("Id").GetInt32();
            string name = body.GetProperty("Name").GetString();

            if (id <= 0 || string.IsNullOrWhiteSpace(name))
                return Json(new { success = false });

            string query = @"UPDATE tblMan SET Name=@Name WHERE Id=@Id";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Name", name.Trim());

            conn.Open();
            cmd.ExecuteNonQuery();

            return Json(new { success = true });
        }

        // ✅ DB PAGING
        private (List<CostEntry>, int) LoadDataByTypePaged(string type, int page, int pageSize)
        {
            var list = new List<CostEntry>();
            int total;

            string query = @"
                SELECT COUNT(*) FROM tblMan WHERE InventoryType = @Type;

                SELECT Id, Name, IsActive
                FROM tblMan
                WHERE InventoryType = @Type
                ORDER BY Name
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            ";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Type", type);
            cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            reader.Read();
            total = reader.GetInt32(0);

            reader.NextResult();
            while (reader.Read())
            {
                list.Add(new CostEntry
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    IsActive = reader.GetBoolean("IsActive")
                });
            }

            return (list, total);
        }
    }
}
