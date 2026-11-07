using Microsoft.EntityFrameworkCore;
using PersonalLedger.Data;
using PersonalLedger.Services.Import;
using PersonalLedger.Services.Import.Handlers;
using PersonalLedger.Services;
using PersonalLedger.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<LedgerDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("LedgerDatabase")));

// Transaction import
builder.Services.AddSingleton<
    ITransactionImportHandler,
    CapitalOneCheckingSavingsHandler>();

builder.Services.AddSingleton<
    ITransactionImportHandler,
    CapitalOneCreditCardHandler>();

builder.Services.AddSingleton<TransactionImportFactory>();

builder.Services.AddScoped<
    ITransactionQueryService,
    TransactionQueryService>();

builder.Services.AddHostedService<TransactionImportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Transaction}/{action=Index}/{id?}");


app.Run();
