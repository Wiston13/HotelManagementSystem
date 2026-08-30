using HotelManagementSystem.Models;
using HotelManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class EmployeeAccountController : BranchEmployeeControllerBase
    {
        private readonly HotelManagementContext _context;

        public EmployeeAccountController(HotelManagementContext context)
            : base(context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            // TODO: 依 CurrentEmployeeNumber 取得目前登入員工。
            // TODO: 使用現有 PasswordHasher<Employee> 驗證目前密碼。
            // TODO: 驗證 NewPassword 與 ConfirmNewPassword 一致。
            // TODO: 重新 Hash 新密碼並更新 PasswordHash。
            // TODO: SaveChangesAsync。
            // TODO: 成功後登出並導回 Account/Login。
            // TODO: 視正式規格決定是否新增密碼修改的 OperationLog 與 OperationType。

            return Json(new
            {
                success = false,
                message = "TODO"
            });
        }
    }
}
