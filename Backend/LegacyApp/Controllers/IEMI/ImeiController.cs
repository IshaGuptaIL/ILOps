using DAL.Common.Login;
using DAL.Inventory.IMEI;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Provides IMEI lookup and associated Rogers invoice history retrieval.
/// Used by Find By IMEI search forms to link hardware serial numbers to purchasing and invoicing records.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImeiController : ControllerBase
{
    private readonly Iiemi _imei;

    public ImeiController(Iiemi imei)
    {
        _imei = imei;
    }

    /// <summary>
    /// Searches HardwareReceived table by IMEI serial number to locate matching PO and BV receipt details.
    /// Used for rapid hardware tracing and verification.
    /// </summary>
    [HttpGet("find")]
    public async Task<ApiResposne> FindByImei(string imei)
        => await _imei.FindByImeiAsync(imei);

    /// <summary>
    /// Retrieves all Rogers invoice records (amounts, reference numbers, dates) associated with a BV Receipt No.
    /// Displays transaction breakdown and calculates balance variance.
    /// </summary>
    [HttpGet("rogers-invoices")]
    public async Task<ApiResposne> GetRogersInvoices(string bvReceiptNo)
        => await _imei.GetRogersInvoicesAsync(bvReceiptNo);
}
