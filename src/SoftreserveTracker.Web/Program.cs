using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Infrastructure;
using SoftreserveTracker.Web.Services.Debug;
using SoftreserveTracker.Web.Services.Import;
using SoftreserveTracker.Web.Services.Items;
using SoftreserveTracker.Web.Services.Parsing;
using SoftreserveTracker.Web.Services.PlusOne;
using SoftreserveTracker.Web.Services.Players;
using SoftreserveTracker.Web.Services.Rosters;
using SoftreserveTracker.Web.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization();
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(SoftreserveTracker.Web.Resources.SharedResource)));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=App_Data/softreserve.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<RosterAccessFilter>();
builder.Services.AddScoped<IRosterService, RosterService>();
builder.Services.AddScoped<ISoftresCsvParser, SoftresCsvParser>();
builder.Services.AddScoped<IGargulJsonParser, GargulJsonParser>();
builder.Services.AddScoped<IFileArchiveService, FileArchiveService>();
builder.Services.AddScoped<IPlusOneCalculator, PlusOneCalculator>();
builder.Services.AddScoped<IPlayerClassLookup, PlayerClassLookup>();
builder.Services.AddScoped<IRaidImportService, RaidImportService>();
builder.Services.AddScoped<IKnownItemService, KnownItemService>();
builder.Services.AddScoped<IUploadFileClassifier, UploadFileClassifier>();
builder.Services.AddScoped<IDebugAdminService, DebugAdminService>();
builder.Services.AddScoped<DebugEnabledFilter>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("de"), new CultureInfo("en") };
    options.SetDefaultCulture("de");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
