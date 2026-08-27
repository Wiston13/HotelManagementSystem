using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class RoomTypeController : SystemAdminControllerBase
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Save(
            int roomTypeId,
            int branchId,
            string roomTypeName,
            int maxOccupancy,
            string bedType,
            decimal nightlyPrice,
            bool isActive,
            string imageUrl,
            string? description)
        {
            if (roomTypeId == 0)
            {
                // TODO: 新增房型
            }
            else
            {
                // TODO: 修改房型
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
