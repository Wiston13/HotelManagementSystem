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

// 設定 Cookie 隱私權政策，確保 Session 功能在各瀏覽器安全性限制下均能正常放行
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false; // 關閉強制隱私同意檢查
    options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// 配置 Session 快取服務
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 逾時時間設定為 30 分鐘
    options.Cookie.HttpOnly = true;                        // 提高安全性，防範 XSS 讀取 Cookie
    options.Cookie.IsEssential = true;                     // 標記為核心必要 Cookie，不受一般隱私政策阻擋
});

// 註冊自訂業務邏輯與系統時鐘服務
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

// 啟用 Session 追蹤中介軟體（必須位於 UseRouting 之後，且在授權與路由對接之前）
app.UseSession();

app.UseAuthorization();

// .NET 9 最新靜態資源優化快取對接
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}") // 將預設首頁改為登入頁面
    .WithStaticAssets();

app.Run();
