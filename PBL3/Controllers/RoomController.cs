using Microsoft.AspNetCore.Mvc;
using PBL3.Services.Receptionist;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class RoomController : Controller
    {
        private readonly IReceptionistCheckInService _receptionistCheckInService;

        public RoomController(IReceptionistCheckInService receptionistCheckInService)
        {
            _receptionistCheckInService = receptionistCheckInService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
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
    }
}
