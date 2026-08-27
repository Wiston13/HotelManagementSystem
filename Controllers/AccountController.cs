using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly HotelManagementContext _context;

        public AccountController(HotelManagementContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel? model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Json(new { success = false, message = "請輸入帳號與密碼" });
            }

            var employee = await _context.Employees
                .Include(e => e.Branch)
                .FirstOrDefaultAsync(e => e.EmployeeNumber == model.Username);

            if (employee == null)
            {
                return Json(new { success = false, message = "帳號或密碼錯誤" });
            }

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Employee>();
            var verifyResult = hasher.VerifyHashedPassword(employee, employee.PasswordHash, model.Password);

            if (verifyResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                return Json(new { success = false, message = "帳號或密碼錯誤" });
            }

            if (employee.IsActive == false)
            {
                return Json(new { success = false, message = "此帳號已停用，請聯絡系統管理員" });
            }

            bool isSystemAdminValid = employee.Role == "SystemAdmin" && employee.BranchId == null;
            bool isBranchEmployeeValid = employee.Role == "BranchEmployee" && employee.BranchId != null;

            if (!isSystemAdminValid && !isBranchEmployeeValid)
            {
                return Json(new { success = false, message = "帳號或密碼錯誤" });
            }

            string targetUrl = employee.Role == "SystemAdmin"
                ? "/Staff/Employees"
                : "/EmployeeHome/Index";

            // 建立授權所需的最小 Claims，供角色與分館資料範圍判斷使用。
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, employee.EmployeeNumber),
                new Claim(ClaimTypes.Name, employee.EmployeeName),
                new Claim(ClaimTypes.Role, employee.Role)
            };

            if (employee.BranchId is int branchId)
            {
                claims.Add(new Claim("BranchId", branchId.ToString()));

                if (employee.Branch != null)
                {
                    claims.Add(new Claim("BranchName", employee.Branch.BranchName));
                }
            }

            var identity = new ClaimsIdentity(claims, "HotelCookie");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("HotelCookie", principal);

            return Json(new { success = true, redirectUrl = targetUrl });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("HotelCookie");

            return RedirectToAction(nameof(Login));
        }
    }
}
