using HotelManagementSystem.Models;
using HotelManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using HotelManagementSystem.Options;
using HotelManagementSystem.Services.Email;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// 提供短時間的記憶體快取，用於防止重複補寄確認信
builder.Services.AddMemoryCache();

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
builder.Services.AddHttpClient<FaqService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType =
            "application/json; charset=utf-8";

builder.Services
    .AddOptions<N8nOptions>()
    .Bind(builder.Configuration.GetSection(N8nOptions.SectionName))
    .Validate(
        options =>
            Uri.TryCreate(
                options.WebhookUrl,
                UriKind.Absolute,
                out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps),
        "N8n:WebhookUrl 必須是有效的 HTTP 或 HTTPS 網址。")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.HeaderName),
        "N8n:HeaderName 尚未設定。")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.WebhookSecret),
        "N8n:WebhookSecret 尚未設定。");
    //.ValidateOnStart();

builder.Services.AddHttpClient<
    IBookingEmailService,
    N8nBookingEmailService>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode =
            StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType =
                "application/json; charset=utf-8";

            await context.HttpContext.Response.WriteAsJsonAsync(
                new
                {
                    success = false,
                    reply = "詢問次數過多，請稍候一分鐘再試。"
                },
                cancellationToken);
        };
        options.AddPolicy("FaqPolicy", httpContext =>
        {
            string clientIp =
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";
            // 設定1分鐘詢問次數上限
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: clientIp,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 8,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
    });
    var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
