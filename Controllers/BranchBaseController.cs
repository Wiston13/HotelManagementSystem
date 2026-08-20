using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.Filters;
namespace HotelManagementSystem.Controllers
{
    [BranchAuthorize]
    public class BranchBaseController : Controller
    {
        
    }
}
