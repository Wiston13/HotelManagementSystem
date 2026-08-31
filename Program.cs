using HotelManagementSystem.Models;
using HotelManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Options;
using HotelManagementSystem.Services.Email;

var builder = WebApplication.CreateBuilder(args);

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
        "N8n:WebhookSecret 尚未設定。")
    .ValidateOnStart();

builder.Services.AddHttpClient<
    IBookingEmailService,
    N8nBookingEmailService>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
