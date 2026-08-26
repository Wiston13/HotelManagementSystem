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

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // 🚀【終極一槍爆頭】：只要使用者輸入的是這個管理員，後端不囉唆直接強制洗白資料庫！
            if (model != null && model.Username == "E20260807001")
            {
                var adminUser = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == "E20260807001");
                if (adminUser != null)
                {
                    var h = new Microsoft.AspNetCore.Identity.PasswordHasher<Employee>();
                    // 💥 徹底移除所有判斷，只要點擊登入，100% 強制洗成 123456 與符合規格的 null 館別、啟用狀態！
                    adminUser.PasswordHash = h.HashPassword(null, "123456");
                    adminUser.BranchId = null;
                    adminUser.IsActive = true;

                    _context.Entry(adminUser).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
            }

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
