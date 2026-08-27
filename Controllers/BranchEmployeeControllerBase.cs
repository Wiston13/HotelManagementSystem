using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelManagementSystem.Controllers
{
    [Authorize(Roles = "BranchEmployee")]
    public abstract class BranchEmployeeControllerBase : Controller
    {
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
    }
}