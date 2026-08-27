using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAL.Models;
using DAL.Sales.Interface;
using Microsoft.EntityFrameworkCore;

namespace DAL.Sales.Da
{
    public class IMEISearchDa : IIMEISearchDa
    {
        private readonly AppDBContext _context;

        public IMEISearchDa(AppDBContext context)
        {
            _context = context;
        }

        public async Task<List<TblRMA>> SearchRmaAsync(string criteria, string query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await _context.tblRMA
                    .AsNoTracking()
                    .OrderByDescending(x => x.ID)
                    .Take(100)
                    .ToListAsync(cancellationToken);
            }

            var trimmedQuery = query.Trim();
            var normCriteria = (criteria ?? "").Trim().ToLower();

            IQueryable<TblRMA> q = _context.tblRMA.AsNoTracking();

            if (normCriteria == "receive waybill" || normCriteria == "receive_waybill")
            {
                q = q.Where(x => x.ExtraInfo != null && (x.ExtraInfo == trimmedQuery || x.ExtraInfo.Contains(trimmedQuery)));
            }
            else if (normCriteria == "return waybill" || normCriteria == "return_waybill")
            {
                q = q.Where(x => x.ReturnWaybill != null && (x.ReturnWaybill == trimmedQuery || x.ReturnWaybill.Contains(trimmedQuery)));
            }
            else
            {
                // Default IMEI search
                q = q.Where(x => x.IMEI != null && (x.IMEI == trimmedQuery || x.IMEI.Contains(trimmedQuery)));
            }

            return await q.OrderBy(x => x.ID).ToListAsync(cancellationToken);
        }

        public async Task<List<TblRMAResponses>> SearchRogersResponsesAsync(string imei, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(imei))
            {
                return await _context.tblRMAResponses
                    .AsNoTracking()
                    .OrderByDescending(x => x.ID)
                    .Take(100)
                    .ToListAsync(cancellationToken);
            }

            var trimmedImei = imei.Trim();
            return await _context.tblRMAResponses
                .AsNoTracking()
                .Where(x => x.IMEI != null && (x.IMEI == trimmedImei || x.IMEI.Contains(trimmedImei)))
                .OrderBy(x => x.ID)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TblRogersReportCMRMA>> SearchRogersReportCmRmaAsync(string imei, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(imei))
            {
                return await _context.tblRogersReportCMRMA
                    .AsNoTracking()
                    .OrderByDescending(x => x.ID)
                    .Take(100)
                    .ToListAsync(cancellationToken);
            }

            var trimmedImei = imei.Trim();
            return await _context.tblRogersReportCMRMA
                .AsNoTracking()
                .Where(x => x.IMEIRMA != null && (x.IMEIRMA == trimmedImei || x.IMEIRMA.Contains(trimmedImei)))
                .OrderBy(x => x.ID)
                .ToListAsync(cancellationToken);
        }

        public async Task<IMEISearchResultDto> SearchAllAsync(string criteria, string query, CancellationToken cancellationToken)
        {
            var result = new IMEISearchResultDto();

            result.RmaResults = await SearchRmaAsync(criteria, query, cancellationToken);

            string searchImei = query;
            if ((criteria ?? "").ToLower().Contains("waybill") && result.RmaResults.Any())
            {
                // Extract IMEIs from found RMA results
                var foundImeis = result.RmaResults.Where(r => !string.IsNullOrEmpty(r.IMEI)).Select(r => r.IMEI!).Distinct().ToList();
                if (foundImeis.Any())
                {
                    result.RogersResponses = await _context.tblRMAResponses
                        .AsNoTracking()
                        .Where(x => x.IMEI != null && foundImeis.Contains(x.IMEI))
                        .OrderBy(x => x.ID)
                        .ToListAsync(cancellationToken);

                    result.CmRmaResults = await _context.tblRogersReportCMRMA
                        .AsNoTracking()
                        .Where(x => x.IMEIRMA != null && foundImeis.Contains(x.IMEIRMA))
                        .OrderBy(x => x.ID)
                        .ToListAsync(cancellationToken);

                    return result;
                }
            }

            result.RogersResponses = await SearchRogersResponsesAsync(searchImei, cancellationToken);
            result.CmRmaResults = await SearchRogersReportCmRmaAsync(searchImei, cancellationToken);

            return result;
        }
    }
}
