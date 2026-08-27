using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class StaffController : Controller
    {
        private readonly HotelManagementContext _context;

        public StaffController(HotelManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Employees()
        {
            var employeeList = await _context.Employees
                .Where(e => e.Role == "BranchEmployee")
                .ToListAsync();

            return View(employeeList);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee(string employeeName, string passwordHash, int branchId)
        {
            if (string.IsNullOrWhiteSpace(employeeName))
            {
                return Json(new { success = false, message = "員工姓名不可為空！" });
            }

            try
            {
                string datePrefix = "E" + DateTime.Today.ToString("yyyyMMdd");

                var lastEmployee = await _context.Employees
                    .Where(e => e.EmployeeNumber.StartsWith(datePrefix))
                    .OrderByDescending(e => e.EmployeeNumber)
                    .FirstOrDefaultAsync();

                int nextSerialNumber = 1;

                if (lastEmployee != null && lastEmployee.EmployeeNumber.Length >= 12)
                {
                    string lastThreeDigits = lastEmployee.EmployeeNumber.Substring(lastEmployee.EmployeeNumber.Length - 3);
                    if (int.TryParse(lastThreeDigits, out int currentNumber))
                    {
                        nextSerialNumber = currentNumber + 1;
                    }
                }

                string newEmployeeNumber = datePrefix + nextSerialNumber.ToString("D3");

                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Employee>();

                Employee newEmp = new Employee();
                newEmp.EmployeeName = employeeName;
                newEmp.EmployeeNumber = newEmployeeNumber;
                newEmp.BranchId = branchId;
                newEmp.Role = "BranchEmployee";
                newEmp.IsActive = true;

                string rawPassword = string.IsNullOrWhiteSpace(passwordHash) ? "123456" : passwordHash;
                newEmp.PasswordHash = hasher.HashPassword(newEmp, rawPassword);

                _context.Employees.Add(newEmp);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"員工建立成功！系統自動配發工號：{newEmployeeNumber}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "資料庫寫入失敗：" + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmployee(string employeeNumber, string employeeName, string passwordHash, int branchId, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(employeeNumber))
            {
                return Json(new { success = false, message = "員工編號不可為空！" });
            }

            try
            {
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber);
                if (emp == null)
                {
                    return Json(new { success = false, message = "找不到該名員工資料！" });
                }

                emp.EmployeeName = employeeName;
                emp.BranchId = branchId;
                emp.IsActive = isActive;

                if (!string.IsNullOrWhiteSpace(passwordHash))
                {
                    var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Employee>();
                    emp.PasswordHash = hasher.HashPassword(emp, passwordHash);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "員工資料修改成功！" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "資料庫更新失敗：" + ex.Message });
            }
        }
    }
}
