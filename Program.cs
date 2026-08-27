using HotelManagementSystem.Models;
using HotelManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// 註冊 MVC 控制器與檢視服務
builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication("HotelCookie")
    .AddCookie("HotelCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddSingleton<TaipeiClock>();

builder.Services.AddDbContext<HotelManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HMSDBConnection")));

builder.Services.AddScoped<NoShowService>();
builder.Services.AddScoped<RoomAvailabilityService>();

var app = builder.Build();

// 配置 HTTP 請求管道中介軟體 (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 啟用 Cookie 政策中介軟體
app.UseCookiePolicy();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// .NET 9 最新靜態資源優化快取對接
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
