using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;

namespace HotelManagementSystem.Filters
{
    // 專門保護分館系統的攔截器（系統管理員嚴禁入內版）
    public class BranchAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. 從 Session 撈出登入者的角色
            var userRole = context.HttpContext.Session.GetString("UserRole");

            // 2. 漏洞防護 A：如果根本沒登入（Role 為空）
            if (string.IsNullOrEmpty(userRole))
            {
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    controller.TempData["ErrorMessage"] = "安全性防護：您尚未登入，請先登入分館員工帳號！";
                }

                // 強制驅逐回登入頁面
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 3. 漏洞防護 B：如果登入的是系統管理員（SystemAdmin），直接鐵腕攔截！
            if (userRole == "SystemAdmin")
            {
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    // 彈出專屬警告：提醒管理員去他該去的地方
                    controller.TempData["ErrorMessage"] = "權限限制：系統管理員帳號禁止訪問分館日常營運系統！";
                }

                // 強制把管理員轟回登入頁（或者你可以讓他留在原本的地方）
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 4. 漏洞防護 C：防呆機制，如果拿到了奇奇怪怪、不是 BranchEmployee 的權限字串
            if (userRole != "BranchEmployee")
            {
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    controller.TempData["ErrorMessage"] = "安全性防護：您的權限不足以訪問此分館系統！";
                }
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
