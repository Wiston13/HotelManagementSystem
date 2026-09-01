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
            ViewBag.Branches = await _context.Branches
                .OrderBy(b => b.BranchId)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SubmitFeedback([FromForm] Feedback input)
        {
            if (input.BranchId <= 0 ||
                string.IsNullOrWhiteSpace(input.CustomerName) ||
                string.IsNullOrWhiteSpace(input.Email) ||
                string.IsNullOrWhiteSpace(input.Content))
            {
                return BadRequest(new { success = false, message = "分館、姓名、信箱與顧客意見為必填欄位。" });
            }

            if (input.Content.Length > 500)
            {
                return BadRequest(new { success = false, message = "顧客意見內容不得超過 500 字。" });
            }

            var branchExists = await _context.Branches.AnyAsync(b => b.BranchId == input.BranchId);
            if (!branchExists)
            {
                return NotFound(new { success = false, message = "找不到對應的分館。" });
            }

            input.CreatedAt = _clock.Now;

            _context.Feedbacks.Add(input);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "您的意見已成功送出！" });
        }
    }
}
