using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.Sales.Interface
{
    public interface IIMEISearchDa
    {
        Task<List<TblRMA>> SearchRmaAsync(string criteria, string query, CancellationToken cancellationToken);
        Task<List<TblRMAResponses>> SearchRogersResponsesAsync(string imei, CancellationToken cancellationToken);
        Task<List<TblRogersReportCMRMA>> SearchRogersReportCmRmaAsync(string imei, CancellationToken cancellationToken);
        Task<IMEISearchResultDto> SearchAllAsync(string criteria, string query, CancellationToken cancellationToken);
    }
}
