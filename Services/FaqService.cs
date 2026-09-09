using HotelManagementSystem.Models.ViewModels;
using System.Net.Http.Json;

namespace HotelManagementSystem.Services
{
    public class FaqService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public FaqService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<FaqAskResponse?> AskAsync(string message)
        {
            string? webhookUrl =
                _configuration["N8n:FaqWebhookUrl"];
            string? apiKey =
                _configuration["N8n:ApiKey"];

            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                throw new InvalidOperationException(
                    "找不到 n8n FAQ Webhook URL 設定。");
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "找不到 n8n FAQ API Key 設定。");
            }
            var payload = new FaqAskRequest
            {
                Message = message
            };

            using var httpRequest =
            new HttpRequestMessage(
            HttpMethod.Post,
            webhookUrl);
            
            httpRequest.Headers.Add(
                "X-FAQ-API-Key",
                apiKey);
            httpRequest.Headers.Add(
            "ngrok-skip-browser-warning",
            "true");

            httpRequest.Content =
                JsonContent.Create(payload);

            using HttpResponseMessage response =
                await _httpClient.SendAsync(httpRequest);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<FaqAskResponse>();
        }
    }
}