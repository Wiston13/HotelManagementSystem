using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels;
using HotelManagementSystem.Models.ViewModels.Announcement;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;


namespace HotelManagementSystem.Controllers
{
    public class AnnouncementController : SystemAdminControllerBase
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _clock;

        public AnnouncementController(
            HotelManagementContext context,
            TaipeiClock clock)
            : base(context)
        {
            _context = context;
            _clock = clock;
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
        public IActionResult Create(
                [Bind(Prefix = "Create")] AnnouncementCreateViewModel model)
        {
            if (model.StartAt.HasValue &&
                model.EndAt.HasValue &&
                model.EndAt <= model.StartAt)
            {
                ModelState.AddModelError(
                    "Create.EndAt",
                    "結束時間必須晚於開始時間");
            }

            if (!ModelState.IsValid)
            {
                var indexModel = new AnnouncementIndexViewModel
                {
                    Create = model,

                    Announcements = _context.Announcements
                        .OrderByDescending(a => a.CreatedAt)
                        .ToList()
                };

                ViewData["OpenModal"] = "Create";

                return View("Index", indexModel);
            }

            var announcement = new Announcement
            {
                Title = model.Title,
                Content = model.Content,
                StartAt = model.StartAt!.Value,
                EndAt = model.EndAt!.Value,
                IsActive = model.IsActive,
                ShowToGuest = model.ShowToGuest,
                CreatedAt = _clock.Now
            };

            _context.Announcements.Add(announcement);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "公告新增成功";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
    [Bind(Prefix = "Edit")] AnnouncementEditViewModel model)
        {
            if (model.StartAt.HasValue &&
                model.EndAt.HasValue &&
                model.EndAt <= model.StartAt)
            {
                ModelState.AddModelError(
                    "Edit.EndAt",
                    "結束時間必須晚於開始時間");
            }

            if (!ModelState.IsValid)
            {
                var indexModel = new AnnouncementIndexViewModel
                {
                    Edit = model,

                    Announcements = _context.Announcements
                        .OrderByDescending(a => a.CreatedAt)
                        .ToList()
                };

                ViewData["OpenModal"] = "Edit";

                return View("Index", indexModel);
            }

            var announcement = _context.Announcements
                .FirstOrDefault(a => a.AnnouncementId == model.AnnouncementId);

            if (announcement == null)
            {
                TempData["ErrorMessage"] = "找不到要修改的公告";

                return RedirectToAction(nameof(Index));
            }

            announcement.Title = model.Title;
            announcement.Content = model.Content;
            announcement.StartAt = model.StartAt!.Value;
            announcement.EndAt = model.EndAt!.Value;
            announcement.IsActive = model.IsActive;
            announcement.ShowToGuest = model.ShowToGuest;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "公告修改成功";

            return RedirectToAction(nameof(Index));
        }
    }
}