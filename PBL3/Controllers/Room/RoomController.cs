using Microsoft.AspNetCore.Mvc;

public class RoomController : Controller
{
    public IActionResult Index()
    {
        // Sau này bạn sẽ lấy dữ liệu từ DB ở đây
        // var rooms = _context.Rooms.ToList();
        // return View(rooms);
        return View();
    }
}