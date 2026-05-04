using DAL.Common.Login;
using DAL.Common.Spire;
using DAL.Common.User;
using DAL.Inventory.AddInventory;
using DAL.Inventory.AdvantageVoice;
using DAL.Inventory.CostValidation;
using DAL.Inventory.Count;
using DAL.Inventory.CountAnalysis;
using DAL.Inventory.CustomSearch;
using DAL.Inventory.IMEI;
using DAL.Inventory.IMEI.Credit;
using DAL.Inventory.IMEI.HardwareIMEI;
using DAL.Inventory.IMEI.RecieveIMEI;
using DAL.Inventory.IMEI.Report;
using DAL.Inventory.InventoryType;
using DAL.Inventory.ModifyInventory;
using DAL.Inventory.OutputInvoice;
using DAL.Inventory.RogerAR;
using DAL.Inventory.RunRate;
using DAL.Inventory.SpareLight;
using DAL.Inventory.SpareLight.DA;
using DAL.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Form options
// =======================
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

// =======================
// DB Context (SQL Server)
// =======================
var connectionString =
    builder.Configuration.GetConnectionString("bvactivation_Connection");

builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(connectionString));




// =======================
// CORS
// =======================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>true)
              .AllowAnyHeader()
              .AllowCredentials()
              .AllowAnyMethod();
    });
});

// =======================
// Controllers + Swagger
// =======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
// Application Services
// =======================

// Login
builder.Services.AddTransient<ILogin, LoginDA>();
builder.Services.AddTransient<IModifyInventory, ModifyInventoryDA>();
builder.Services.AddTransient<IUser,UserDA>();
builder.Services.AddTransient<ICostValidation,CostValidationDA>();
builder.Services.AddTransient<Iiemi, ImeiDA>();
builder.Services.AddScoped<IRecieveImei, RecieveImeiDA>();
builder.Services.AddScoped<IReports,ReportsDA>();
builder.Services.AddScoped<IInvoiceCredit,InvoiceCreditDA>();
builder.Services.AddScoped<ICount, CountDA>();
builder.Services.AddScoped<ICountAnalysis,CountAnalysisDA>();
builder.Services.AddScoped<IInventoryType, InventoryTypeDA>();
builder.Services.AddTransient<IOutputInvoice,OutputInvoiceDA>();
builder.Services.AddTransient<ICustomSearch,CustomSearchDA>();
builder.Services.AddTransient<IRunRate,RunRateDA>();
builder.Services.AddTransient<ISpareLight, SpareLightDA>();
builder.Services.AddTransient<IRoger, RogerDA>();
builder.Services.AddTransient<IAdvantageVoice,AdvantageVoiceDA>();

builder.Services.AddHttpClient<ISpireClient, SpireClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var baseUrl = config["SpireApi:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("SpireApi:BaseUrl missing");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        AllowAutoRedirect = false,
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
});
builder.Services.AddScoped<IHardwareService, HardwareService>();

// =======================
// Spire HttpClient (IMPORTANT)
// =======================
builder.Services.AddHttpClient("SpireClient", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var baseUrl = config["SpireApi:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("SpireApi:BaseUrl missing");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        // ONLY if Spire SSL causes issues
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
});

// =======================
// SpireDA
// =======================
builder.Services.AddScoped<SpireDA>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("SpireClient");

    var logger = sp.GetRequiredService<ILogger<SpireDA>>();
    var config = sp.GetRequiredService<IConfiguration>();

    string user =
        config["SpireApi:UserName"]
        ?? throw new InvalidOperationException("SpireApi:UserName missing");

    string pass =
        config["SpireApi:Password"]
        ?? throw new InvalidOperationException("SpireApi:Password missing");

    string pgConn =
        config.GetConnectionString("spire_Connection")
        ?? throw new InvalidOperationException("spire_Connection missing");

    return new SpireDA(client, logger, user, pass, pgConn);
});

// =======================
// Inventory
// =======================
builder.Services.AddScoped<IAddInventory, AddInventoryDA>();

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// =======================
// Build App
// =======================
var app = builder.Build();

app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
