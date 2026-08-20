using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.Filters;
namespace HotelManagementSystem.Controllers
{
    [AdminAuthorize]
    public class AdminBaseController : Controller
    {
    
    }
}
