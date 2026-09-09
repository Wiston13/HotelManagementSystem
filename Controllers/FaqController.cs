using HotelManagementSystem.Models.ViewModels;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class FaqController : Controller
    {
        private readonly ILogger<FaqController> _logger;
        private readonly FaqService _faqService;

        public FaqController(FaqService faqService,ILogger<FaqController> logger)
        {
            _faqService = faqService;
            _logger = logger;
        }

        [HttpPost]
        [EnableRateLimiting("FaqPolicy")]
        public async Task<IActionResult> Ask(
            [FromBody] FaqAskRequest? request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest(new FaqAskResponse
                {
                    Success = false,
                    Reply = "請輸入想詢問的問題。"
                });
            }

            string message = request.Message.Trim();

            if (message.Length > 50)
            {
                return BadRequest(new FaqAskResponse
                {
                    Success = false,
                    Reply = "問題內容不能超過 50 個字。"
                });
            }
            try
            {
                FaqAskResponse? response =
                await _faqService.AskAsync(message);

                if (response == null)
                {
                    return StatusCode(
                        StatusCodes.Status502BadGateway,
                        new FaqAskResponse
                        {
                            Success = false,
                            Reply = "客服目前暫時無法回覆，請稍後再試。"
                        });
                }
                return Json(response);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "呼叫 n8n FAQ Webhook 時發生錯誤。");

                return StatusCode(StatusCodes.Status503ServiceUnavailable, new FaqAskResponse
                {
                    Success = false,
                    Reply = "客服目前暫時無法回覆，請稍後再試。"
                });
            }
            

            
        }
    }
}
