using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class RoomController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Save(
            int roomId,
            int branchId,
            string roomNumber,
            int roomTypeId,
            int floor,
            string supplyStatus,
            string? disabledReason)
        {
            if (roomId == 0)
            {
                // TODO: 新增房間
            }
            else
            {
                // TODO: 修改房間
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
