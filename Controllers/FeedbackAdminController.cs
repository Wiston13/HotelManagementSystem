using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Controllers; // 請根據你們專案中 SystemAdminControllerBase 的實際命名空間調整
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagementSystem.Controllers
{
    public class FeedbackAdminController : SystemAdminControllerBase
    {
        private readonly HotelManagementContext _context;

        public FeedbackAdminController(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        // 對應後台管理列表 (純唯讀檢視)
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

            // 指向原本的 Index View
            return View("~/Views/Feedback/Index.cshtml", feedbacks);
        }
    }
}