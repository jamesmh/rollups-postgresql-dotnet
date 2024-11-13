using System.Data.Common;
using Npgsql;
using RollupsPostgresqlDotnet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<DbConnection>((provider) =>
    new NpgsqlConnection("Server=localhost;Port=5432;User Id=postgres_user;Password=123456;Database=postgres_db;"));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.WriteLine("Migrations started");
await Migrations.RunAsync(app.Services.GetRequiredService<DbConnection>());
Console.WriteLine("Migrations completed");

app.Run();
