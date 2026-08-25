using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        [HttpGet]
        public async Task<IActionResult> ResetAdminPassword()
        {
            var admin = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == "E20260807001");
            if (admin != null)
            {
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Employee>();
                // 🚀 強制將他的密碼打碎加密，重設為 "123456"（妳也可以改成妳們原本要的密碼）
                admin.PasswordHash = hasher.HashPassword(admin, "123456");

                await _context.SaveChangesAsync();
                return Content("最高管理員 E20260807001 的密碼已成功在資料庫中洗成雜湊碼！初始密碼為 123456");
            }
            return Content("在資料庫中找不到 E20260807001 帳號");
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
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
            
                return Json(new { success = false, message = "登入資料異常，請聯絡系統管理員" });
            }

            
            HttpContext.Session.SetString("EmployeeNumber", employee.EmployeeNumber);
            HttpContext.Session.SetString("UserRole", employee.Role);
            HttpContext.Session.SetString("UserName", employee.EmployeeName);

            if (employee.BranchId.HasValue)
            {
                HttpContext.Session.SetString("BranchId", employee.BranchId.Value.ToString());
            }
            else
            {
                HttpContext.Session.Remove("BranchId");
            }

            string targetUrl = "/EmployeeHome/Index";

            if (employee.Role == "SystemAdmin")
            {
                targetUrl = "/Staff/Employees";
            }
            else if (employee.Role == "BranchEmployee")
            {
                targetUrl = "/EmployeeHome/Index";
            }

            return Json(new { success = true, redirectUrl = targetUrl });
        }
    }
}
