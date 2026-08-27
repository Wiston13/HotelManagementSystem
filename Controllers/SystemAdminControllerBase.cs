using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelManagementSystem.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    public abstract class SystemAdminControllerBase : Controller
    {
        protected string? CurrentEmployeeNumber =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        protected string? CurrentEmployeeName =>
            User.Identity?.Name;
    }
}