using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Controllers;
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

       
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "顧客意見管理";

            
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Branch)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            
            return View("~/Views/Feedback/Index.cshtml", feedbacks);
        }
    }
}