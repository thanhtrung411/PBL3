using Microsoft.AspNetCore.Mvc;
using PBL3.Services.Interfaces;
using System.Threading.Tasks;

namespace PBL3.Controllers
{
    /// <summary>
    /// Controller test để kiểm tra kết nối database
    /// Tạm thời dùng để debug
    /// </summary>
    public class TestController : Controller
    {
        private readonly ILoaiPhongService _loaiPhongService;
        private readonly IBangGiaPhongService _bangGiaService;

        public TestController(
            ILoaiPhongService loaiPhongService,
            IBangGiaPhongService bangGiaService)
        {
            _loaiPhongService = loaiPhongService;
            _bangGiaService = bangGiaService;
        }

        /// <summary>
        /// Test 1: Xem tất cả loại phòng
        /// URL: /Test/GetAllRoomTypes
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllRoomTypes()
        {
            try
            {
                var loaiPhongs = await _loaiPhongService.GetAllLoaiPhongsAsync();

                if (loaiPhongs == null || loaiPhongs.Count == 0)
                {
                    return Ok(new { success = false, message = "Không có loại phòng nào!" });
                }

                return Ok(new 
                { 
                    success = true, 
                    message = $"Có {loaiPhongs.Count} loại phòng",
                    data = loaiPhongs 
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Test 2: Xem giá phòng hiện tại
        /// URL: /Test/GetCurrentPrice?roomId=STANDARD
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCurrentPrice(string roomId)
        {
            try
            {
                var price = await _bangGiaService.LayGiaPhongHienTaiAsync(roomId);

                return Ok(new 
                { 
                    success = true, 
                    roomId = roomId,
                    currentPrice = price,
                    message = price > 0 ? "Tìm thấy giá phòng" : "Không có giá phòng nào"
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Test 3: Trang HTML hiển thị tất cả test
        /// URL: /Test/Dashboard
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var loaiPhongs = await _loaiPhongService.GetAllLoaiPhongsAsync();
                return View(loaiPhongs);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }
    }
}
