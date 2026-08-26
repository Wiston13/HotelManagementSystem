using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using HotelManagementSystem.Filters;

namespace HotelManagementSystem.Controllers
{
    [AdminAuthorize]
    public class AdminBaseController : Controller
    {
        public SessionProxy Session => new SessionProxy(HttpContext);
    }

    public class SessionProxy
    {
        private readonly HttpContext _context;
        public SessionProxy(HttpContext context) => _context = context;

        public string? this[string key]
        {
            get => _context.Session.GetString(key);
            set => _context.Session.SetString(key, value ?? string.Empty);
        }
    }
}
