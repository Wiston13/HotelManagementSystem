using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Models; 
using HotelManagementSystem.Models.Entities;

namespace HotelManagementSystem.Controllers
{
    public class RoomTypeController : Controller
    {
        private readonly HotelManagementContext _context;

        // 1. 注入資料庫
        public RoomTypeController(HotelManagementContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 2. 撈出分館清單 (給畫面上方的下拉選單用)
            ViewBag.Branches = await _context.Branches.ToListAsync();

            // 3. 撈出所有房型資料 (給下方的表格用)
            var roomTypes = await _context.RoomTypes.ToListAsync();
            return View(roomTypes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // 4. 把落落長的參數改成直接接收 RoomType 物件
        public async Task<IActionResult> Save(RoomType model)
        {
            if (model.RoomTypeId == 0)
            {
                // 新增房型
                _context.RoomTypes.Add(model);
            }
            else
            {
                // 修改房型
                _context.RoomTypes.Update(model);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}