using DAL.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.RunRate
{
    public class RunRateDA :IRunRate
    {

        private readonly string _pgConn;

        public RunRateDA(IConfiguration config)
        {
            _pgConn = config.GetConnectionString("spire_Connection");
        }

        public async Task<List<RunRateItem>> GetWFHInventoryAsync()
        {

            var result = new List<RunRateItem>();

            var sql = @"
        SELECT part_no, description, onhand_qty
        FROM INVENTORY
        WHERE whse = @Whse AND misc_1 = @Misc1
    ";

            try
            {
                await using var conn = new NpgsqlConnection(_pgConn);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Whse", "CO");
                cmd.Parameters.AddWithValue("@Misc1", "WORK FROM HOME");

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var item = new RunRateItem
                    {
                        Code = reader["part_no"]?.ToString(),
                        Description = reader["description"]?.ToString(),
                        OnHand = reader["onhand_qty"] != DBNull.Value ? Convert.ToDecimal(reader["onhand_qty"]) : 0
                    };

                    result.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching WFH Inventory: {ex.Message}");
                throw; // API will return 500 so Angular shows error
            }

            return result;
        }
           
    }

}



