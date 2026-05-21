using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PBL3.Services.Interfaces;
using System.Threading.Tasks;

namespace PBL3.Controllers
{
    /// <summary>
    /// Controller test để kiểm tra kết nối database
    /// Tạm thời dùng để debug
    /// </summary>
    [Authorize]
    public class TestController : Controller
    {
        private readonly ILoaiPhongService _loaiPhongService;
        private readonly IBangGiaPhongService _bangGiaService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TestController> _logger;

        public TestController(
            ILoaiPhongService loaiPhongService,
            IBangGiaPhongService bangGiaService,
            IWebHostEnvironment environment,
            ILogger<TestController> logger)
        {
            _loaiPhongService = loaiPhongService;
            _bangGiaService = bangGiaService;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Test 1: Xem tất cả loại phòng
        /// URL: /Test/GetAllRoomTypes
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllRoomTypes()
        {
            if (!IsDebugRouteEnabled())
            {
                return NotFound();
            }

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
                _logger.LogWarning(ex, "Failed to load room types from debug endpoint.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Khong the tai du lieu kiem tra. Vui long thu lai sau."
                });
            }
        }

        /// <summary>
        /// Test 2: Xem giá phòng hiện tại
        /// URL: /Test/GetCurrentPrice?roomId=STANDARD
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCurrentPrice(string roomId)
        {
            if (!IsDebugRouteEnabled())
            {
                return NotFound();
            }

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
                _logger.LogWarning(ex, "Failed to load room price from debug endpoint. RoomId={RoomId}", roomId);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Khong the tai du lieu kiem tra. Vui long thu lai sau."
                });
            }
        }

        /// <summary>
        /// Test 3: Trang HTML hiển thị tất cả test
        /// URL: /Test/Dashboard
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsDebugRouteEnabled())
            {
                return NotFound();
            }

            try
            {
                var loaiPhongs = await _loaiPhongService.GetAllLoaiPhongsAsync();
                return View(loaiPhongs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load debug dashboard.");
                ViewBag.Error = "Khong the tai du lieu kiem tra. Vui long thu lai sau.";
                return View();
            }
        }

        private bool IsDebugRouteEnabled()
        {
            return _environment.IsDevelopment();
        }
    }
}
