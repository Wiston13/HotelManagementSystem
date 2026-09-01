using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels;
using HotelManagementSystem.Models.ViewModels.Announcement;
using Microsoft.AspNetCore.Mvc;


namespace HotelManagementSystem.Controllers
{
    public class AnnouncementController : Controller
    {
        private readonly HotelManagementContext _context;

        public AnnouncementController(HotelManagementContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new AnnouncementIndexViewModel
            {
                Announcements = _context.Announcements
                    .OrderByDescending(a => a.CreatedAt)
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AnnouncementIndexViewModel model)
        {
            if (model.Create.StartAt.HasValue &&
                model.Create.EndAt.HasValue &&
                model.Create.EndAt <= model.Create.StartAt)
            {
                ModelState.AddModelError(
                    "Create.EndAt",
                    "結束時間必須晚於開始時間");
            }

            if (!ModelState.IsValid)
            {
                model.Announcements = _context.Announcements
                    .OrderByDescending(a => a.CreatedAt)
                    .ToList();

                return View("Index", model);
            }

            var announcement = new Announcement
            {
                Title = model.Create.Title,
                Content = model.Create.Content,
                StartAt = model.Create.StartAt!.Value,
                EndAt = model.Create.EndAt!.Value,
                IsActive = model.Create.IsActive
            };

            _context.Announcements.Add(announcement);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "公告新增成功";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(AnnouncementIndexViewModel model)
        {
            if (model.Edit.StartAt.HasValue &&
                model.Edit.EndAt.HasValue &&
                model.Edit.EndAt <= model.Edit.StartAt)
            {
                ModelState.AddModelError(
                    "Edit.EndAt",
                    "結束時間必須晚於開始時間");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            TempData["SuccessMessage"] = "公告修改資料驗證成功";

            return RedirectToAction(nameof(Index));
        }
    }
}