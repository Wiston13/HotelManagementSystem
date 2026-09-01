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

            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                throw new InvalidOperationException(
                    "找不到 n8n FAQ Webhook URL 設定。");
            }

            var request = new FaqAskRequest
            {
                Message = message
            };

            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(
                    webhookUrl,
                    request);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<FaqAskResponse>();
        }
    }
}