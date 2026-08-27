using System;
using System.Threading;
using System.Threading.Tasks;
using DAL.Models;
using DAL.Sales.Interface;

namespace DAL.Sales.Bo
{
    public class IMEISearchBo : IIMEISearchBo
    {
        private readonly IIMEISearchDa _da;

        public IMEISearchBo(IIMEISearchDa da)
        {
            _da = da;
        }

        public async Task<IMEISearchResultDto> SearchAsync(string criteria, string query, CancellationToken cancellationToken)
        {
            return await _da.SearchAllAsync(criteria, query, cancellationToken);
        }
    }
}
