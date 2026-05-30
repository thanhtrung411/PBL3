using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PBL3.Services.Interfaces;
using PBL3.Services.Receptionist;
using System.Security.Claims;

namespace PBL3.Controllers;

[Authorize]
public class ReceptionistController : Controller
{
    private readonly IReceptionistCheckInService _receptionistCheckInService;

    public ReceptionistController(IReceptionistCheckInService receptionistCheckInService)
    {
        _receptionistCheckInService = receptionistCheckInService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View("~/Views/Test/Receptionist.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Today(CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetTodayArrivalsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> RoomMap(CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetRoomMapAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> ServiceUsage(CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetServiceUsageAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> ServiceUsage(
        [FromBody] ReceptionistAddServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.AddServiceUsageAsync(
            request ?? new ReceptionistAddServiceRequest(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetCheckoutListAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(
        [FromBody] ReceptionistCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.CheckoutAsync(
            request ?? new ReceptionistCheckoutRequest(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(string code, CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.LookupBookingAsync(code, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Rooms(string bookingCode, CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetRoomSelectionAsync(bookingCode, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(
        [FromBody] ReceptionistCheckInRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.CheckInAsync(
            request ?? new ReceptionistCheckInRequest(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> WalkInAvailability(
        DateOnly checkOutDate,
        CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetWalkInAvailabilityAsync(
            new ReceptionistWalkInAvailabilityRequest { CheckOutDate = checkOutDate },
            cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> WalkInCheckIn(
        [FromBody] ReceptionistWalkInCheckInRequest request,
        CancellationToken cancellationToken)
    {
        request ??= new ReceptionistWalkInCheckInRequest();
        request.EmployeeId = User.FindFirstValue("EmployeeId");
        var result = await _receptionistCheckInService.WalkInCheckInAsync(request, cancellationToken);
        return Ok(result);
    }
}
