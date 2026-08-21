using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // 🚀 1. 記得引用微軟官方的資料庫加強工具
using HotelManagementSystem.Models;  // 🚀 2. 引用妳們專案的資料庫模型 (Context)
using System.Threading.Tasks;

namespace HotelManagementSystem.Controllers
{
    public class StaffController : AdminBaseController
    {
        // 🎯 3. 新增：宣告並接上資料庫的連線欄位
        private readonly HotelManagementContext _context;

        // 🎯 4. 新增：透過建構子，讓微軟自動把資料庫電力注入進來
        public StaffController(HotelManagementContext context)
        {
            _context = context;
        }

        // 🎯 5. 升級：改為非同步的 async Task 處理機制
        public async Task<IActionResult> Employees()
        {
            // 妳原本寫的超棒偵錯紀錄，完美保留！
            Console.WriteLine(HttpContext.Session.GetString("UserRole"));
            Console.WriteLine(HttpContext.Session.GetString("UserName"));
            Console.WriteLine(HttpContext.Session.GetString("BranchId"));

            // 🚀 【核心大通電】：去資料庫把 dbo.Employees 表格內的所有員工抓出來
            var employeeList = await _context.Employees.ToListAsync();

            // 🚀 【倒給前端】：把這串裝滿資料庫數據的清單，正式丟進 View 的括號內！
            return View(employeeList);
        }
    }
}
