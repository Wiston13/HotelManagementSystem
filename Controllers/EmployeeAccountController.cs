using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class EmployeeAccountController : BranchEmployeeControllerBase
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _clock;

        public EmployeeAccountController(HotelManagementContext context, TaipeiClock taipeiClock)
            : base(context)
        {
            _context = context;
            _clock = taipeiClock;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            // TODO: 依 CurrentEmployeeNumber 取得目前登入員工。
            // TODO: 使用現有 PasswordHasher<Employee> 驗證目前密碼。
            // TODO: 驗證 NewPassword 與 ConfirmNewPassword 一致。
            // TODO: 重新 Hash 新密碼並更新 PasswordHash。
            // TODO: SaveChangesAsync。
            // TODO: 成功後登出並導回 Account/Login。
            // TODO: 視正式規格決定是否新增密碼修改的 OperationLog 與 OperationType。
            if (model == null || string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword) || string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
            {
                return Json(new { success = false, message = "請輸入密碼" });
            }

            if (!CurrentBranchId.HasValue)
            {
                await HttpContext.SignOutAsync("HotelCookie");

                return Json(new
                {
                    success = false,
                    redirectUrl = "/Account/Login"
                });
            }

            var staff = await _context.Employees.Where(b => b.EmployeeNumber == CurrentEmployeeNumber).FirstOrDefaultAsync();

            if (staff == null)
            {
                await HttpContext.SignOutAsync("HotelCookie");

                return Json(new
                {
                    success = false,
                    redirectUrl = "/Account/Login"
                });
            }

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Employee>();
            var verifyResult = hasher.VerifyHashedPassword(staff, staff.PasswordHash, model.CurrentPassword);

            if (verifyResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                return Json(new { success = false, message = "原密碼錯誤" });
            }

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                return Json(new { success = false, message = "新密碼與確認密碼不符" });
            }

            if (model.NewPassword == model.CurrentPassword)
            {
                return Json(new
                {
                    success = false,
                    message = "新密碼不可與原密碼相同"
                });
            }

            staff.PasswordHash = hasher.HashPassword(staff, model.NewPassword);

            var operationLog = new OperationLog
            {
                TargetBranchId = CurrentBranchId.Value,
                OperatedAt = _clock.Now,
                OperatorEmployeeNumber = CurrentEmployeeNumber!,
                OperationTypeId = 25,
                TargetType = "Employee",
                TargetIdentifier = staff.EmployeeNumber,
                Description = $"員工修改自己的登入密碼。"
            };

            _context.OperationLogs.Add(operationLog);

            await _context.SaveChangesAsync();
            await HttpContext.SignOutAsync("HotelCookie");

            return Json(new
            {
                success = true,
                redirectUrl = "/Account/Login"
            });
        }
    }
}
