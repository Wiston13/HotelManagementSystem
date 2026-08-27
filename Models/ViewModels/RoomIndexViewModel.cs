using HotelManagementSystem.Models.Entities;
using System.Collections.Generic;

namespace HotelManagementSystem.Models.ViewModels
{
    public class RoomIndexViewModel
    {
        public List<Branch> Branches { get; set; } = new();
        public List<RoomType> RoomTypes { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();
    }
}