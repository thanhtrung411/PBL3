using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Receptionist;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class RoomController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReceptionistCheckInService _receptionistCheckInService;

        public RoomController(
            ApplicationDbContext context,
            IReceptionistCheckInService receptionistCheckInService)
        {
            _context = context;
            _receptionistCheckInService = receptionistCheckInService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ViewBag.RoomTypeOptions = await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(x => x.TenLoaiPhong)
                .Select(x => new RoomTypeOptionDto
                {
                    RoomTypeId = x.MaLoaiPhong.Trim(),
                    RoomTypeName = x.TenLoaiPhong
                })
                .ToListAsync(cancellationToken);
            var roomMap = await _receptionistCheckInService.GetRoomMapAsync(cancellationToken);
            return View(roomMap);
        }

        [HttpPost]
        public async Task<IActionResult> Maintenance(
            [FromBody] RoomMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _receptionistCheckInService.SetRoomMaintenanceAsync(
                request ?? new RoomMaintenanceRequest(),
                cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeType(
            [FromBody] RoomTypeChangeRequest request,
            CancellationToken cancellationToken)
        {
            var roomId = (request?.RoomId ?? string.Empty).Trim();
            var roomTypeId = (request?.RoomTypeId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(roomTypeId))
            {
                return Ok(new RoomTypeChangeResult
                {
                    Success = false,
                    Message = "Thiếu phòng hoặc loại phòng cần đổi."
                });
            }

            var room = await _context.Phongs
                .Include(x => x.MaLoaiPhongNavigation)
                .FirstOrDefaultAsync(x => x.MaPhong == roomId, cancellationToken);
            if (room == null)
            {
                return Ok(new RoomTypeChangeResult
                {
                    Success = false,
                    Message = "Không tìm thấy phòng."
                });
            }

            var nextRoomType = await _context.LoaiPhongs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaLoaiPhong == roomTypeId, cancellationToken);
            if (nextRoomType == null)
            {
                return Ok(new RoomTypeChangeResult
                {
                    Success = false,
                    Message = "Không tìm thấy loại phòng."
                });
            }

            if (string.Equals(room.MaLoaiPhong.Trim(), roomTypeId, StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new RoomTypeChangeResult
                {
                    Success = true,
                    RoomId = room.MaPhong,
                    RoomTypeId = room.MaLoaiPhong.Trim(),
                    RoomTypeName = room.MaLoaiPhongNavigation.TenLoaiPhong,
                    Message = "Loại phòng không thay đổi."
                });
            }

            room.MaLoaiPhong = nextRoomType.MaLoaiPhong;
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new RoomTypeChangeResult
            {
                Success = true,
                RoomId = room.MaPhong,
                RoomTypeId = nextRoomType.MaLoaiPhong.Trim(),
                RoomTypeName = nextRoomType.TenLoaiPhong,
                Message = $"Đã đổi phòng {room.SoPhong} sang loại {nextRoomType.TenLoaiPhong}."
            });
        }
    }

    public sealed class RoomTypeOptionDto
    {
        public string RoomTypeId { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;
    }

    public sealed class RoomTypeChangeRequest
    {
        public string? RoomId { get; set; }

        public string? RoomTypeId { get; set; }
    }

    public sealed class RoomTypeChangeResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string RoomId { get; set; } = string.Empty;

        public string RoomTypeId { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;
    }
}
