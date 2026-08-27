using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelManagementSystem.Controllers
{
    [Authorize(Roles = "BranchEmployee")]
    public abstract class BranchEmployeeControllerBase : Controller
    {
        private readonly HotelManagementContext _context;

        protected BranchEmployeeControllerBase(HotelManagementContext context)
        {
            _context = context;
        }

        protected string? CurrentEmployeeNumber =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        protected int? CurrentBranchId
        {
            get
            {
                var value = User.FindFirst("BranchId")?.Value;

                return int.TryParse(value, out var branchId)
                    ? branchId
                    : null;
            }
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var employeeNumber = CurrentEmployeeNumber;
            var branchId = CurrentBranchId;

            var isActive =
                !string.IsNullOrWhiteSpace(employeeNumber) &&
                branchId.HasValue &&
                await _context.Employees
                    .AsNoTracking()
                    .AnyAsync(e =>
                        e.EmployeeNumber == employeeNumber &&
                        e.IsActive &&
                        e.Role == "BranchEmployee" &&
                        e.BranchId == branchId.Value);

            if (!isActive)
            {
                await HttpContext.SignOutAsync("HotelCookie");
                context.Result = RedirectToAction("Login", "Account");
                return;
            }

            await next();
        }
    }
}
