using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.InventoryType
{
    public class InventoryTypeDA : IInventoryType
    {
        private readonly string _connectionString;

        public InventoryTypeDA(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("bvactivation_Connection");
        }

        public async Task<(List<InventoryBO> data, int totalCount)> GetPagedDataAsync(string type, int page, int pageSize)
        {
            var list = new List<InventoryBO>();
            int total = 0;

            using var conn = new SqlConnection(_connectionString);
            // COUNT aur DATA dono ek hi baar mein fetch karenge
            string query = @"
        SELECT COUNT(*) FROM tblMan WHERE InventoryType = @Type;

        SELECT Id, Name, InventoryType, IsActive
        FROM tblMan
        WHERE InventoryType = @Type
        ORDER BY Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Type", type);
            cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync()) total = reader.GetInt32(0);

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    list.Add(new InventoryBO
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        InventoryType = reader.GetString(2),
                        IsActive = reader.GetBoolean(3)
                    });
                }
            }
            return (list, total);
        }

        public async Task<bool> AddGroupAsync(InventoryBO model)
        {
            using var conn = new SqlConnection(_connectionString);
            string query = "INSERT INTO tblMan (Name, InventoryType, IsActive) VALUES (@Name, @Type, 1)";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Name", model.Name);
            cmd.Parameters.AddWithValue("@Type", model.InventoryType);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateGroupAsync(InventoryBO model)
        {
            using var conn = new SqlConnection(_connectionString);
            string query = "UPDATE tblMan SET Name = @Name WHERE Id = @Id";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Name", model.Name);
            cmd.Parameters.AddWithValue("@Id", model.Id);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}