using System.Collections.Generic;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.Inventory.IMEI.Exceptions
{
    public interface IExceptions
    {
        Task<IEnumerable<ExceptionBO>> GetExceptionsAsync(string poNumber = null);
        Task<bool> ResolveExceptionAsync(int id, string userId);
        Task<bool> DeleteExceptionAsync(int id);
        Task<bool> ClearAllExceptionsAsync();

        Task<IEnumerable<tblIMEILengthExceptions>> GetIMEILengthExceptionsAsync();
        Task<bool> SaveIMEILengthExceptionAsync(tblIMEILengthExceptions exception);
        Task<bool> DeleteIMEILengthExceptionAsync(string part);
    }
}
