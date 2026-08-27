using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Models.Entities;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace HotelManagementSystem.Controllers
{
    public class OperationLogController : AdminBaseController
    {
        private readonly HotelManagementSystem.Models.HotelManagementContext _context;

        public OperationLogController(HotelManagementSystem.Models.HotelManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Search()
        {
            ViewBag.ActionTypes = await _context.OperationTypes
                .OrderBy(t => t.OperationTypeId)
                .Select(t => t.OperationTypeName)
                .ToListAsync();

           
            ViewBag.Branches = await _context.Branches
                .OrderBy(b => b.BranchId)
                .ToListAsync();

            var emptyLogs = Enumerable.Empty<dynamic>();
            return View(emptyLogs);
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredLogs(
           string startDate, string endDate, int? branchId,
           string type, string operatorInput, string target)
        {
            try
            {
               
                var query = _context.OperationLogs
                    .Include(l => l.OperationType)
                    .Include(l => l.TargetBranch)
                    .AsQueryable();

                
                if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var sDate))
                {
                    query = query.Where(l => l.OperatedAt >= sDate);
                }
                if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var eDate))
                {
                    var eDateEnd = eDate.AddDays(1);
                    query = query.Where(l => l.OperatedAt < eDateEnd);
                }

                
                if (branchId.HasValue)
                {
                    query = query.Where(l => l.TargetBranchId == branchId.Value);
                }

              
                if (!string.IsNullOrWhiteSpace(type) && type != "全部類型")
                {
                    query = query.Where(l => l.OperationType.OperationTypeName == type);
                }

                
                if (!string.IsNullOrWhiteSpace(operatorInput))
                {
                    var op = operatorInput.Trim().ToLower();

                    var matchedEmployeeNumbers = await _context.Employees
                        .Where(e => e.EmployeeName.ToLower().Contains(op) || e.EmployeeNumber.ToLower().Contains(op))
                        .Select(e => e.EmployeeNumber)
                        .ToListAsync();

                    query = query.Where(l => matchedEmployeeNumbers.Contains(l.OperatorEmployeeNumber) || l.Description.ToLower().Contains(op));
                }

                
                if (!string.IsNullOrWhiteSpace(target))
                {
                    var tgt = target.Trim().ToLower();
                    query = query.Where(l => l.TargetIdentifier.ToLower().Contains(tgt) || l.TargetType.ToLower().Contains(tgt) || l.Description.ToLower().Contains(tgt));
                }

               
                var rawLogs = await query
                    .OrderByDescending(l => l.OperationLogId)
                    .ToListAsync();

                
                var employeeMap = await _context.Employees
                    .ToDictionaryAsync(e => e.EmployeeNumber, e => e.EmployeeName);

                var result = rawLogs.Select(l =>
                {
                    string operatorName = employeeMap.ContainsKey(l.OperatorEmployeeNumber)
                        ? employeeMap[l.OperatorEmployeeNumber]
                        : l.OperatorEmployeeNumber;

                    return new
                    {
                        operatedAt = l.OperatedAt,
                   
                        branchName = l.TargetBranch != null ? l.TargetBranch.BranchName : "系統核心",
                        operatorName = operatorName,
                        operationTypeName = l.OperationType != null ? l.OperationType.OperationTypeName : "未知操作",
                        targetType = l.TargetType,
                        targetIdentifier = l.TargetIdentifier,
                        description = l.Description
                    };
                });

                return Json(result);
            }
            catch (Exception)
            {
             
                return Json(new { success = false, message = "操作失敗，請稍後再試。" });
            }
        }
    }
}
