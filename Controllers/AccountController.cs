using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.Models; // 引用你剛剛建立的 LoginModel
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HotelManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        // 1. 宣告資料庫 Context 欄位
        private readonly HotelManagementContext _context;

        // 2. 透過建構子注入資料庫 Context
        public AccountController(HotelManagementContext context)
        {
            _context = context;
        }

        // 3. 保留原本的 GET 方法：負責開啟登入畫面
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 4. 新增 POST 方法：負責接收前端資料並驗證
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // 檢查前端傳來的欄位有沒有漏填
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "請填寫帳號與密碼" });
            }

            // 步驟一：去資料庫撈取員工資料 (對應 dbo.Employees 表)
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumber == model.Username);

            // 步驟二：檢查帳號是否存在
            if (employee == null)
            {
                return Json(new { success = false, message = "帳號或密碼錯誤" });
            }

            // 步驟三：檢查帳號是否啟用 (SQL 的 bit 在 C# 會自動對應成 bool)
            if (employee.IsActive == false)
            {
                return Json(new { success = false, message = "此帳號已被停用" });
            }

            // 步驟四：驗證密碼雜湊值
            // ⚠️ 註：目前先用 == 比對。如果專案有套用加密（如 BCrypt），請改成對應的解密套件語法
            if (employee.PasswordHash != model.Password)
            {
                return Json(new { success = false, message = "帳號或密碼錯誤" });
            }

            // 步驟五：登入成功，將員工權限與姓名存入 Session 供後續頁面檢查
            HttpContext.Session.SetString("UserRole", employee.Role);
            HttpContext.Session.SetString("UserName", employee.EmployeeName);

            // 🚀 步驟六：多角色與多館別分流邏輯核心（嚴格優先權版本）
            string targetUrl = "/EmployeeHome/Index"; // 預設防呆網址

            // 🌟 【第一優先】先判定是不是系統管理員（用 Role 或是特定的員工編號卡死）
            if (employee.Role == "SystemAdmin" || employee.EmployeeNumber == "E20260807001")
            {
                // 🎯 系統管理員：絕對優先跳轉至「旅宿管理系統」
                targetUrl = "/Staff/Employees";
            }
            // 🌟 【第二優先】如果是管理員，上面就處理完了；走到這裡的絕對是「一般員工」
            else if (employee.Role == "BranchEmployee")
            {
                // 🎯 依據 BranchId 判斷各自的分館主畫面
                switch (employee.BranchId)
                {
                    case 1:
                        targetUrl = "/EmployeeHome";   // 台北分館員工
                        break;
                    case 2:
                        targetUrl = "/TaichungHome/Index"; // 台中分館員工
                        break;
                    case 3:
                        targetUrl = "/KaohsiungHome/Index"; // 高雄分館員工
                        break;
                    default:
                        targetUrl = "/EmployeeHome/Index";  // 未知館別防呆
                        break;
                }
            }

            // 🌟 最終大總結：將精確分流後的跳轉網址，用唯一的 return 回傳給前端
            return Json(new { success = true, redirectUrl = targetUrl });
        }
    }
}
