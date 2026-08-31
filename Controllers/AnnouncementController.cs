using HotelManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;


namespace HotelManagementSystem.Controllers
{
    public class AnnouncementController : Controller
    {
        public IActionResult Index()
        {
            return View(new AnnouncementCreateViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AnnouncementCreateViewModel model)
        {
            if (model.StartAt.HasValue &&
                model.EndAt.HasValue &&
                model.EndAt <= model.StartAt)
            {
                ModelState.AddModelError(
                    nameof(model.EndAt),
                    "結束時間必須晚於開始時間");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            TempData["SuccessMessage"] = "公告資料驗證成功";

            return RedirectToAction(nameof(Index));
        }

    }
}
