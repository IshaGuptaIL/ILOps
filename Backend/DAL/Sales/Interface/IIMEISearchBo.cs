using System.Threading;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.Sales.Interface
{
    public interface IIMEISearchBo
    {
        Task<IMEISearchResultDto> SearchAsync(string criteria, string query, CancellationToken cancellationToken);
    }
}
