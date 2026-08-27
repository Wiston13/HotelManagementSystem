using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class StaffController : SystemAdminControllerBase
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _clock;

        public StaffController(HotelManagementContext context)
            : base(context)
        {
            _context = context;
            _clock = new TaipeiClock();
        }

        public async Task<IActionResult> Employees()
        {
            
            var employeeList = await _context.Employees
                .Include(e => e.Branch)
                .Where(e => e.Role == "BranchEmployee")
                .ToListAsync();

          
            ViewBag.Branches = await _context.Branches
                .OrderBy(b => b.BranchId)
                .ToListAsync();

            return View(employeeList);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee(string employeeName, string password, int branchId)
        {
           
            if (string.IsNullOrWhiteSpace(employeeName))
            {
                return Json(new { success = false, message = "員工姓名不可為空！" });
            }

            
            if (string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "請輸入初始密碼" });
            }

            
            var branchExists = await _context.Branches.AnyAsync(b => b.BranchId == branchId);
            if (!branchExists)
            {
                return Json(new { success = false, message = "選取的分館不存在，請重新選擇。" });
            }

            try
            {
                
                string datePrefix = "E" + _clock.Today.ToString("yyyyMMdd");

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

                Employee newEmp = new Employee
                {
                    EmployeeName = employeeName,
                    EmployeeNumber = newEmployeeNumber,
                    BranchId = branchId,
                    Role = "BranchEmployee",
                    IsActive = true
                };
                newEmp.PasswordHash = hasher.HashPassword(newEmp, password);

                _context.Employees.Add(newEmp);

                
                string operatorEmployeeNumber = HttpContext.Session.GetString("EmployeeNumber") ?? "";
                string description = $"建立分館員工 {newEmp.EmployeeNumber}({newEmp.EmployeeName})。";

                OperationLog log = new OperationLog
                {
                    TargetBranchId = newEmp.BranchId.GetValueOrDefault(0),
                    OperatedAt = _clock.Now,
                    OperatorEmployeeNumber = operatorEmployeeNumber,
                    OperationTypeId = 13, // EmployeeCreated
                    TargetType = "Employee",
                    TargetIdentifier = newEmp.EmployeeNumber ?? "",
                    Description = description
                };

                _context.OperationLogs.Add(log);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"員工建立成功！系統自動配發工號：{newEmployeeNumber}" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "操作失敗，請稍後再試。" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmployee(string employeeNumber, string employeeName, string password, int branchId, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(employeeNumber))
            {
                return Json(new { success = false, message = "員工編號不可為空！" });
            }

      
            if (string.IsNullOrWhiteSpace(employeeName))
            {
                return Json(new { success = false, message = "員工姓名不可為空！" });
            }

            
            var branchExists = await _context.Branches.AnyAsync(b => b.BranchId == branchId);
            if (!branchExists)
            {
                return Json(new { success = false, message = "選取的分館不存在，請重新選擇。" });
            }

            try
            {
               
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber && e.Role == "BranchEmployee");
                if (emp == null)
                {
                    return Json(new { success = false, message = "找不到該名分館員工資料！" });
                }

                
                string oldName = emp.EmployeeName ?? "";
                int? oldBranchId = emp.BranchId;
                bool oldIsActive = emp.IsActive;
                bool isPasswordReset = !string.IsNullOrWhiteSpace(password);

               
                emp.EmployeeName = employeeName;
                emp.BranchId = branchId;
                emp.IsActive = isActive;

                if (isPasswordReset)
                {
                    var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Employee>();
                    emp.PasswordHash = hasher.HashPassword(emp, password);
                }

                string operatorEmployeeNumber = HttpContext.Session.GetString("EmployeeNumber") ?? "";
                int currentBranchId = emp.BranchId.GetValueOrDefault(0);
                string targetIdentifier = emp.EmployeeNumber ?? "";

                
                if (oldName != emp.EmployeeName || oldBranchId != emp.BranchId)
                {
                    _context.OperationLogs.Add(new OperationLog
                    {
                        TargetBranchId = currentBranchId,
                        OperatedAt = _clock.Now,
                        OperatorEmployeeNumber = operatorEmployeeNumber,
                        OperationTypeId = 14, // EmployeeUpdated
                        TargetType = "Employee",
                        TargetIdentifier = targetIdentifier,
                        Description = $"修改員工資料：{emp.EmployeeNumber}({emp.EmployeeName})。"
                    });
                }

                if (oldIsActive && !emp.IsActive)
                {
                    _context.OperationLogs.Add(new OperationLog
                    {
                        TargetBranchId = currentBranchId,
                        OperatedAt = _clock.Now,
                        OperatorEmployeeNumber = operatorEmployeeNumber,
                        OperationTypeId = 15, // EmployeeDisabled
                        TargetType = "Employee",
                        TargetIdentifier = targetIdentifier,
                        Description = $"停用員工帳號：{emp.EmployeeNumber}({emp.EmployeeName})。"
                    });
                }

                if (!oldIsActive && emp.IsActive)
                {
                    _context.OperationLogs.Add(new OperationLog
                    {
                        TargetBranchId = currentBranchId,
                        OperatedAt = _clock.Now,
                        OperatorEmployeeNumber = operatorEmployeeNumber,
                        OperationTypeId = 16, // EmployeeEnabled
                        TargetType = "Employee",
                        TargetIdentifier = targetIdentifier,
                        Description = $"啟用員工帳號：{emp.EmployeeNumber}({emp.EmployeeName})。"
                    });
                }

                if (isPasswordReset)
                {
                    _context.OperationLogs.Add(new OperationLog
                    {
                        TargetBranchId = currentBranchId,
                        OperatedAt = _clock.Now,
                        OperatorEmployeeNumber = operatorEmployeeNumber,
                        OperationTypeId = 17, // EmployeePasswordReset
                        TargetType = "Employee",
                        TargetIdentifier = targetIdentifier,
                        Description = $"重設員工密碼：{emp.EmployeeNumber}({emp.EmployeeName})。"
                    });
                }

             
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "員工資料修改成功！" });
            }
            catch (Exception)
            {
             
                return Json(new { success = false, message = "操作失敗，請稍後再試。" });
            }
        }
    }
}
