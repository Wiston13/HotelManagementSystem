using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;

namespace HotelManagementSystem.Controllers
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. 檢查 Session 裡面有沒有管理員的身分
            var userRole = context.HttpContext.Session.GetString("UserRole");

            // 2. 如果沒登入，或是登入的人不是系統管理員，立刻攔截
            if (string.IsNullOrEmpty(userRole) || userRole != "SystemAdmin")
            {
                // 3. 設定錯誤訊息
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    controller.TempData["ErrorMessage"] = "防護機制：您無權限訪問該頁面，請先登入管理員帳號！";
                }

                // 4. 強制退貨，彈回登入頁面
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }

            base.OnActionExecuting(context);
        }
    }
}
