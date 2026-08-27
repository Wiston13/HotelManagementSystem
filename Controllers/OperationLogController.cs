using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class OperationLogController : SystemAdminControllerBase
    {
        private readonly HotelManagementSystem.Models.HotelManagementContext _context;

        public OperationLogController(HotelManagementSystem.Models.HotelManagementContext context)
        {
            _context = context;
        }

        // 🎯 一進網頁：預設回傳乾淨的空陣列，防止預設載入
        public async Task<IActionResult> Search()
        {
            ViewBag.ActionTypes = await _context.OperationTypes
                .OrderBy(t => t.OperationTypeId)
                .Select(t => t.OperationTypeName)
                .ToListAsync();

            var emptyLogs = Enumerable.Empty<dynamic>();
            return View(emptyLogs);
        }

        // 🎯 終極通電：支援操作者姓名/工號模糊查詢、操作對象房號/訂單號模糊查詢！
        [HttpGet]
        public async Task<IActionResult> GetFilteredLogs(
           string startDate, string endDate, string branch,
           string type, string operatorInput, string target)
        {
            var query = _context.OperationLogs.Include("OperationType").AsQueryable();

            // 1. 日期篩選
            if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var sDate))
            {
                query = query.Where(l => l.OperatedAt >= sDate);
            }
            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var eDate))
            {
                var eDateEnd = eDate.AddDays(1);
                query = query.Where(l => l.OperatedAt < eDateEnd);
            }

            // 2. 分館篩選
            if (!string.IsNullOrWhiteSpace(branch) && branch != "全部分館")
            {
                if (branch == "台北中山商旅") query = query.Where(l => l.TargetBranchId == 1);
                else if (branch == "台北信義商旅") query = query.Where(l => l.TargetBranchId == 2);
                else if (branch == "台中草悟商旅") query = query.Where(l => l.TargetBranchId == 3);
                else if (branch == "台南安平商旅") query = query.Where(l => l.TargetBranchId == 4);
                else if (branch == "高雄港灣商旅") query = query.Where(l => l.TargetBranchId == 5);
                else if (branch == "花蓮站前商旅") query = query.Where(l => l.TargetBranchId == 6);
            }

            // 3. 操作類型中文篩選
            if (!string.IsNullOrWhiteSpace(type) && type != "全部類型")
            {
                query = query.Where(l => l.OperationType.OperationTypeName == type);
            }

            // 4. 【操作者雙向大升級】：同時完美支援輸入「中文姓名」或「員工編號」！
            if (!string.IsNullOrWhiteSpace(operatorInput))
            {
                var op = operatorInput.Trim().ToLower();

                // 🚀 關鍵核心步驟：先去員工表撈出名字或編號有包含關鍵字的所有員工編號清單
                var matchedEmployeeNumbers = await _context.Employees
                    .Where(e => e.EmployeeName.ToLower().Contains(op) || e.EmployeeNumber.ToLower().Contains(op))
                    .Select(e => e.EmployeeNumber)
                    .ToListAsync();

                // 🚀 拿著這批員工編號，去操作紀錄表裡用 .Contains 進行大範圍通電捕捉！
                query = query.Where(l => matchedEmployeeNumbers.Contains(l.OperatorEmployeeNumber) || l.Description.ToLower().Contains(op));
            }

            // 5. 操作對象 / 識別編號篩選
            if (!string.IsNullOrWhiteSpace(target))
            {
                var tgt = target.Trim().ToLower();
                query = query.Where(l => l.TargetIdentifier.ToLower().Contains(tgt) || l.TargetType.ToLower().Contains(tgt) || l.Description.ToLower().Contains(tgt));
            }

            // 6. 撈取資料
            var rawLogs = await query
                .OrderByDescending(l => l.OperationLogId)
                .Take(200)
                .ToListAsync();

            // 7. 轉換成中文員工姓名大包裹
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
                    targetBranchId = l.TargetBranchId,
                    operatorName = operatorName,
                    operationTypeName = l.OperationType != null ? l.OperationType.OperationTypeName : "未知操作",
                    targetType = l.TargetType,
                    targetIdentifier = l.TargetIdentifier,
                    description = l.Description
                };
            });

            return Json(result);
        }
    }
}
