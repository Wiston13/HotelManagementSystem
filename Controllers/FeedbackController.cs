using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagementSystem.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _clock;

        public FeedbackController(HotelManagementContext context, TaipeiClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Branches = await _context.Branches.OrderBy(b => b.BranchId).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(FeedbackCreateViewModel input)
        {
            if (!ModelState.IsValid)
            {
                var errorMsg = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "輸入資料有誤。";
                return BadRequest(new { success = false, message = errorMsg });
            }

            // 嚴格檢查分館是否存在
            var branchExists = await _context.Branches.AnyAsync(b => b.BranchId == input.BranchId);
            if (!branchExists)
            {
                return BadRequest(new { success = false, message = "選擇的分館不存在。" });
            }

            var feedback = new Feedback
            {
                BranchId = input.BranchId,
                CustomerName = input.CustomerName,
                Email = input.Email,
                Phone = input.Phone,
                Content = input.Content,
                CreatedAt = _clock.Now
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "您的意見已成功送出！" });
        }
    }
}