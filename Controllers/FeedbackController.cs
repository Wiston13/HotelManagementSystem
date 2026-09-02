using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "顧客意見管理";

            var userBranchIdClaim = User.FindFirst("BranchId")?.Value;
            IQueryable<Feedback> query = _context.Feedbacks.Include(f => f.Branch);

            if (int.TryParse(userBranchIdClaim, out int branchId))
            {
                query = query.Where(f => f.BranchId == branchId);
            }

            var feedbacks = await query
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(feedbacks);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Branches = await _context.Branches.OrderBy(b => b.BranchId).ToListAsync();
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SubmitFeedback([FromForm] Feedback input)
        {
            if (input.BranchId <= 0 || string.IsNullOrWhiteSpace(input.CustomerName) ||
                string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Content))
            {
                return BadRequest(new { success = false, message = "必填欄位未填。" });
            }

            input.CreatedAt = _clock.Now;
            input.Status = "未處理"; 

            _context.Feedbacks.Add(input);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "您的意見已成功送出！" });
        }

 
        [HttpPost]
        public async Task<IActionResult> UpdateFeedbackDetails(int id, string status, string? adminReply)
        {
            if (status != "未處理" && status != "已處理")
            {
                return BadRequest(new { success = false, message = "狀態只能為未處理或已處理。" });
            }

            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound(new { success = false, message = "找不到該筆顧客意見。" });
            }

            feedback.Status = status;
            feedback.AdminReply = adminReply; 
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "處理狀態與備註已成功儲存！" });
        }
    }
}
