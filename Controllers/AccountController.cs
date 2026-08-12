using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace HotelManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string Username, string Password)
        {
            // 1. 後端帳號格式驗證
            if (string.IsNullOrEmpty(Username) || !Regex.IsMatch(Username, @"^\d{10}$"))
            {
                ViewBag.ErrorMessage = "帳號驗證失敗，必須剛好為 10 碼純數字！";
                return View();
            }

            // 2. 後端密碼格式驗證
            if (string.IsNullOrEmpty(Password) || !Regex.IsMatch(Password, @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,25}$"))
            {
                ViewBag.ErrorMessage = "密碼驗證失敗，必須包含英文與數字，且長度為 8-25 碼！";
                return View();
            }

            // =======================================================
            // 3. 登入帳密比對邏輯（此處設定一組測試帳密）
            // 帳號：1234567890 (剛好10碼數字)
            // 密碼：admin123 (包含英數，長度8碼)
            // =======================================================
            if (Username == "1234567890" && Password == "admin123")
            {
                //  成功登入，導向您的「總管理畫面」
                return RedirectToAction("Employees", "Staff");
            }
            //  一般員工 (新建立的判斷軌道)
            else if (Username == "8888888888" && Password == "staff123")
            {
                // 成功登入，導向「員工專屬管理頁面」
                //  這裡假設您有一個 EmployeeController，裡面的 Action 叫 Index
                return RedirectToAction("Index", "Home");
            }
            //  帳密輸入錯誤
            else
            {
                ViewBag.ErrorMessage = "員工帳號或密碼輸入錯誤，請重新輸入！";
                return View();
            }
        
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // 1. 清除登入狀態 (以 Cookie 驗證為例)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. 導向剛才建立的登出完成畫面
            return RedirectToAction("LogoutPage");
        }

        // 3. 新增一個專門用來顯示登出畫面的 Action
        [HttpGet]
        public IActionResult LogoutPage()
        {
            return View("Logout");
        }

        public IActionResult Employees() { return View(); }
    }
}
