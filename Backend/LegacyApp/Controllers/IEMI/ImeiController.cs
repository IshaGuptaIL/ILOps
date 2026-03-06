using DAL.Common.Login;
using DAL.Inventory.IMEI;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ImeiController : ControllerBase
{
    private readonly Iiemi _imei;

    public ImeiController(Iiemi imei)
    {
        _imei = imei;
    }

    [HttpGet("find")]
    public async Task<ApiResposne> FindByImei(string imei)
        => await _imei.FindByImeiAsync(imei);

    [HttpGet("rogers-invoices")]
    public async Task<ApiResposne> GetRogersInvoices(string bvReceiptNo)
        => await _imei.GetRogersInvoicesAsync(bvReceiptNo);
}
