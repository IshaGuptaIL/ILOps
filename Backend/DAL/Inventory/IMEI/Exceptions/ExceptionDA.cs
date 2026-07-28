using DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DAL.Inventory.IMEI.Exceptions
{
    public class ExceptionDA : IExceptions
    {
        private readonly string _connectionString;

        public ExceptionDA(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("bvactivation_Connection");
        }

        public async Task<IEnumerable<ExceptionBO>> GetExceptionsAsync(string poNumber = null)
        {
            var list = new List<ExceptionBO>();
            using var conn = new SqlConnection(_connectionString);
            string sql = "SELECT ID, VBCode, VBDescription, PONumber, RecNo, ErrorWhile, [RowCount], Resolved FROM tblErrors WHERE (@PO IS NULL OR PONumber = @PO)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@PO", string.IsNullOrEmpty(poNumber) ? DBNull.Value : poNumber);

            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new ExceptionBO
                {
                    ID = r["ID"] != DBNull.Value ? Convert.ToInt32(r["ID"]) : 0,
                    VBCode = r["VBCode"]?.ToString(),
                    VBDescription = r["VBDescription"]?.ToString(),
                    PONumber = r["PONumber"]?.ToString(),
                    RecNo = r["RecNo"] != DBNull.Value ? Convert.ToInt32(r["RecNo"]) : (int?)null,
                    ErrorWhile = r["ErrorWhile"]?.ToString(),
                    RowCount = r["RowCount"] != DBNull.Value ? Convert.ToInt32(r["RowCount"]) : (int?)null,
                    Resolved = r["Resolved"] != DBNull.Value && Convert.ToBoolean(r["Resolved"])
                });
            }
            return list;
        }

        public async Task<bool> ResolveExceptionAsync(int id, string userId)
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = "UPDATE tblErrors SET Resolved = 1 WHERE ID = @ID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@ID", id);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteExceptionAsync(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = "DELETE FROM tblErrors WHERE ID = @ID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@ID", id);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ClearAllExceptionsAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = "DELETE FROM tblErrors";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<IEnumerable<tblIMEILengthExceptions>> GetIMEILengthExceptionsAsync()
        {
            var list = new List<tblIMEILengthExceptions>();
            using var conn = new SqlConnection(_connectionString);
            string sql = "SELECT ExceptionPart, IMEILength, AllowAlpha FROM tblIMEILengthExceptions ORDER BY ExceptionPart";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;

            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new tblIMEILengthExceptions
                {
                    ExceptionPart = r["ExceptionPart"].ToString(),
                    IMEILength = r["IMEILength"] != DBNull.Value ? Convert.ToInt32(r["IMEILength"]) : (int?)null,
                    AllowAlpha = r["AllowAlpha"] != DBNull.Value && Convert.ToBoolean(r["AllowAlpha"])
                });
            }
            return list;
        }

        public async Task<bool> SaveIMEILengthExceptionAsync(tblIMEILengthExceptions exception)
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = @"
                IF EXISTS (SELECT 1 FROM tblIMEILengthExceptions WHERE ExceptionPart = @Part)
                    UPDATE tblIMEILengthExceptions SET IMEILength = @Len, AllowAlpha = @Alpha WHERE ExceptionPart = @Part
                ELSE
                    INSERT INTO tblIMEILengthExceptions (ExceptionPart, IMEILength, AllowAlpha) VALUES (@Part, @Len, @Alpha)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@Part", exception.ExceptionPart);
            cmd.Parameters.AddWithValue("@Len", exception.IMEILength.HasValue ? exception.IMEILength.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Alpha", exception.AllowAlpha.HasValue ? exception.AllowAlpha.Value : DBNull.Value);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteIMEILengthExceptionAsync(string part)
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = "DELETE FROM tblIMEILengthExceptions WHERE ExceptionPart = @Part";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@Part", part);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
