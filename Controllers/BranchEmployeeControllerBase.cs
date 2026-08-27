using Microsoft.AspNetCore.Authorization;
using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
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

        protected string? CurrentEmployeeName =>
            User.Identity?.Name;

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

        protected string? CurrentBranchName =>
            User.FindFirst("BranchName")?.Value;

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var employeeNumber = CurrentEmployeeNumber;
            var isActive = !string.IsNullOrWhiteSpace(employeeNumber) &&
                await _context.Employees
                    .AsNoTracking()
                    .AnyAsync(e => e.EmployeeNumber == employeeNumber && e.IsActive);

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
