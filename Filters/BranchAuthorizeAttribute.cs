using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;

namespace HotelManagementSystem.Filters
{
    public class BranchAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userRole = context.HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userRole))
            {
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    controller.TempData["ErrorMessage"] = "安全性防護：您尚未登入，請先登入分館員工帳號！";
                }

                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (userRole == "SystemAdmin")
            {
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    controller.TempData["ErrorMessage"] = "權限限制：系統管理員帳號禁止訪問分館日常營運系統！";
                }

                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

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
