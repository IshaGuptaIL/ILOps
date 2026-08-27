using DAL.Common.Jwt;
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
using DAL.Inventory.IMEI.Exceptions;
using DAL.Inventory.IMEI.HardwareIMEI;
using DAL.Inventory.IMEI.RecieveIMEI;
using DAL.Inventory.IMEI.Report;
using DAL.Inventory.InventoryEdit;
using DAL.Inventory.InventoryType;
using DAL.Inventory.ModifyInventory;
using DAL.Inventory.OutputInvoice;
using DAL.Inventory.RogerAR;
using DAL.Inventory.RunRate;
using DAL.Inventory.SpareLight;
using DAL.Inventory.SpareLight.DA;
using DAL.Inventory.PriceProtection;
using DAL.Inventory.PriceProtection.ImeiSearch;
using DAL.Inventory.PriceProtection.OutputToExcel;
using DAL.Inventory.PriceProtection.RogerOverPayments;
using DAL.Models;
using DAL.Sales.ARCollections;
using DAL.Sales.BO;
using DAL.Sales.CustomerSales;
using DAL.Sales.HydroSales;
using DAL.Sales.RogersInvoiceSpire;
using LegacyApp.DAL.Sales.RogersSalesReporting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using DAL.Inventory.PriceProtection.ApplyCredit_ReviewClaims;
using DAL.Sales.RMAReporting;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Kestrel & Request Timeouts (10 Minutes)
// =======================
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromMinutes(10)
    };
});

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
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.CommandTimeout(600)));




// =======================
// JWT Authentication
// =======================
var jwtKey = "ILOps_Super_Secret_Key_For_JWT_Authentication_1234567890!!!";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});


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
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
    
    // Add JWT Authentication support to Swagger UI
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste your JWT token directly below (you DO NOT need to type 'Bearer')."
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// =======================
// Application Services
// =======================

// Login
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddTransient<ILogin, LoginDA>();
builder.Services.AddTransient<IModifyInventory, ModifyInventoryDA>();
builder.Services.AddTransient<IUser,UserDA>();
builder.Services.AddTransient<ICostValidation,CostValidationDA>();
builder.Services.AddTransient<Iiemi, ImeiDA>();
builder.Services.AddScoped<IRecieveImei, RecieveImeiDA>();
builder.Services.AddScoped<IReports,ReportsDA>();
builder.Services.AddScoped<IInvoiceCredit,InvoiceCreditDA>();
builder.Services.AddScoped<IExceptions, ExceptionDA>();
builder.Services.AddScoped<ICount, CountDA>();
builder.Services.AddScoped<ICountAnalysis,CountAnalysisDA>();
builder.Services.AddScoped<IInventoryType, InventoryTypeDA>();
builder.Services.AddTransient<IOutputInvoice,OutputInvoiceDA>();
builder.Services.AddTransient<ICustomSearch,CustomSearchDA>();
builder.Services.AddTransient<IRunRate,RunRateDA>();
builder.Services.AddTransient<ISpareLight, SpareLightDA>();
builder.Services.AddTransient<IRoger, RogerDA>();
builder.Services.AddTransient<ISku, SkuDA>();
builder.Services.AddTransient<ISalesTaxReport,SalesTaxReportDA>();
builder.Services.AddTransient<IAdvantageVoice,AdvantageVoiceDA>();
builder.Services.AddTransient<IInventoryEdit, InventoryEditDA>();
builder.Services.AddTransient<ICustomerSales,CustomerSalesDA>();
builder.Services.AddTransient<IHydroSales,HydroSalesDA>();
builder.Services.AddTransient<IRogerSalesReportingDAL, RogerSalesReportingDAL>();
builder.Services.AddTransient<IARCollectionsDA, ARCollectionsDA>();
builder.Services.AddTransient<IRogersInvoiceSpireDA, RogersInvoiceSpireDA>();
builder.Services.AddTransient<IPriceProtection, PriceProtectionDA>();
builder.Services.AddTransient<IImeiSearch, ImeiSearchDA>();
builder.Services.AddTransient<IOutputToExcel, OutputToExcelDA>();
builder.Services.AddTransient<IRogerOverPayments, RogerOverPaymentsDA>();
builder.Services.AddTransient<IApplyCreditReviewClaims,ApplyCreditReviewClaimsDA>();
builder.Services.AddTransient<IRMAReportingDA, RMAReportingDA>();

builder.Services.AddHttpClient<ISpireClient, SpireClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var baseUrl = config["SpireApi:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("SpireApi:BaseUrl missing");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(10);
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
    client.Timeout = TimeSpan.FromMinutes(10);
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
    var dbContext = sp.GetRequiredService<AppDBContext>();

    string user =
        config["SpireApi:UserName"]
        ?? throw new InvalidOperationException("SpireApi:UserName missing");

    string pass =
        config["SpireApi:Password"]
        ?? throw new InvalidOperationException("SpireApi:Password missing");

    string pgConn =
        config.GetConnectionString("spire_Connection")
        ?? throw new InvalidOperationException("spire_Connection missing");

    return new SpireDA(client, logger, user, pass, pgConn, dbContext);
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

app.UseRequestTimeouts();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

// =======================
// Generate Sample Excel Files for Testing
// =======================
try
{
    string targetDir = @"c:\Users\DELL\Downloads\My Code";
    if (System.IO.Directory.Exists(targetDir))
    {
        string scanPath = System.IO.Path.Combine(targetDir, "Sample_ScanList.xlsx");
        string packPath = System.IO.Path.Combine(targetDir, "Sample_PackingSlip.xlsx");

        if (!System.IO.File.Exists(scanPath))
        {
            using (var package = new ExcelPackage(new System.IO.FileInfo(scanPath)))
            {
                var ws = package.Workbook.Worksheets.Add("ScanList");
                ws.Cells[1, 1].Value = "359411001234567";
                ws.Cells[2, 1].Value = "359411001234568";
                ws.Cells[3, 1].Value = "359411001234569";
                ws.Cells[4, 1].Value = "359411001234570";
                ws.Cells[5, 1].Value = "359411001234571";
                package.Save();
            }
        }

        if (!System.IO.File.Exists(packPath))
        {
            using (var package = new ExcelPackage(new System.IO.FileInfo(packPath)))
            {
                var ws = package.Workbook.Worksheets.Add("PackingSlip");
                ws.Cells[1, 1].Value = "359411001234567";
                ws.Cells[2, 1].Value = "359411001234568";
                ws.Cells[3, 1].Value = "359411001234569";
                ws.Cells[4, 1].Value = "359411001234570";
                ws.Cells[5, 1].Value = "359411001234571";
                package.Save();
            }
        }
        System.Console.WriteLine("Sample Excel files created at: " + targetDir);
    }
}
catch (System.Exception ex)
{
    System.Console.WriteLine("Error generating sample Excels: " + ex.Message);
}

app.Run();
