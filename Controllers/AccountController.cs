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

            string targetUrl;

            if (employee.Role == "SystemAdmin")
            {
                targetUrl = "/Staff/Employees";
            }
            else if (employee.Role == "BranchEmployee")
            {
                targetUrl = "/EmployeeHome/Index";
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "帳號角色設定異常"
                });
            }

            //「這個人有哪些資料」
            // Authentication 裡最好只保存足以識別與授權目前使用者的最小資訊。
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, employee.EmployeeNumber),
                new Claim(ClaimTypes.Name, employee.EmployeeName),
                new Claim(ClaimTypes.Role, employee.Role)
            };

            if (employee.BranchId is int branchId)
            {
                claims.Add(new Claim("BranchId", branchId.ToString()));
            }

            //「這些資料組成一個已驗證身分」
            var identity = new ClaimsIdentity(claims, "HotelCookie");

            //將登入身分包成 ASP.NET Core 可作為目前 User 使用的 ClaimsPrincipal。
            var principal = new ClaimsPrincipal(identity);

            // 使用 HotelCookie 完成登入，並將 Authentication Cookie 寫入 Response。
            await HttpContext.SignInAsync("HotelCookie", principal);

            return Json(new { success = true, redirectUrl = targetUrl });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
