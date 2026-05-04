using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.AdvantageVoice
{
    public interface IAdvantageVoice
    {

        Task<List<AdvantageImportVM>> GetPendingImportsAsync(int userId);
        Task<bool> ImportExcelDataAsync(Stream fileStream, int userId);
        Task<List<AdvantageImportVM>> ValidateDataAsync(int userId);
        Task<bool> SubmitOrdersAsync(int userId);
        byte[] GenerateTemplate();

    }
}
