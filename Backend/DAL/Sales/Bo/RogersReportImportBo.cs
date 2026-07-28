using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DAL.Sales.Interface;

namespace DAL.Sales.Bo
{
    public class RogersReportImportBo : IRogersReportImportBo
    {
        private readonly IRogersReportImportDa _da;

        public RogersReportImportBo(IRogersReportImportDa da)
        {
            _da = da;
        }

        public async Task<bool> ProcessAndImportFileAsync(Stream fileStream, string fileType, string fileName, CancellationToken cancellationToken)
        {
            var dataTable = new DataTable();
            
            using (var workbook = new XLWorkbook(fileStream))
            {
                var worksheet = workbook.Worksheet(1);
                bool firstRow = true;
                
                foreach (var row in worksheet.RowsUsed())
                {
                    if (firstRow)
                    {
                        foreach (var cell in row.Cells())
                        {
                            dataTable.Columns.Add(cell.Value.ToString());
                        }
                        firstRow = false;
                    }
                    else
                    {
                        dataTable.Rows.Add();
                        int i = 0;
                        foreach (var cell in row.Cells())
                        {
                            dataTable.Rows[dataTable.Rows.Count - 1][i] = cell.Value.ToString();
                            i++;
                        }
                    }
                }
            }

            string targetTable = fileType switch
            {
                "RM" => "RogersReportRMA-Import",
                "CM" => "RogersReportCM-Import",
                "Manual" => "RogersReportRMA-Manual-Import",
                _ => throw new ArgumentException("Invalid file type")
            };

            // Convert VBA logic:
            // e.g. CurrentDb.Execute("delete from [RogersReportRMA-Import]")
            await _da.ExecuteStoredProcedureOrQueryAsync($"DELETE FROM [{targetTable}]", cancellationToken);
            
            // Insert parsed data
            await _da.BulkInsertExcelDataAsync(dataTable, targetTable, cancellationToken);
            
            // Further queries like qryValidateCMRMA1, qryAppendCMRMA etc...
            // await _da.ExecuteStoredProcedureOrQueryAsync("EXEC sp_ProcessCMRMAImport", cancellationToken);

            return true;
        }

        public async Task<bool> GenerateCmSummaryAsync(CancellationToken cancellationToken)
        {
            // Simulate CM Summary VBA action
            await _da.ExecuteStoredProcedureOrQueryAsync("EXEC sp_GenerateCMSummary", cancellationToken);
            return true;
        }

        public async Task<bool> ProcessManualRmaImportAsync(CancellationToken cancellationToken)
        {
            // Simulate Manual RMA Import VBA action
            await _da.ExecuteStoredProcedureOrQueryAsync("EXEC sp_ProcessManualRmaImport", cancellationToken);
            return true;
        }

        public async Task<bool> DeleteBatchFilesAsync(string cmFile, string rmFile, string manualFile, CancellationToken cancellationToken)
        {
            return await _da.DeleteBatchFilesAsync(cmFile, rmFile, manualFile, cancellationToken);
        }

        public async Task<byte[]> GenerateTemplateAsync(string fileType)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add($"{fileType} Template");
                
                // Add Headers based on fileType
                if (fileType == "RM")
                {
                    worksheet.Cell(1, 1).Value = "RMA Number";
                    worksheet.Cell(1, 2).Value = "IMEI";
                    worksheet.Cell(1, 3).Value = "Credit Memo";
                }
                else if (fileType == "CM")
                {
                    worksheet.Cell(1, 1).Value = "CM Number";
                    worksheet.Cell(1, 2).Value = "Date";
                    worksheet.Cell(1, 3).Value = "Amount";
                }
                else 
                {
                    worksheet.Cell(1, 1).Value = "Manual ID";
                    worksheet.Cell(1, 2).Value = "Details";
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return await Task.FromResult(stream.ToArray());
                }
            }
        }
    }
}
