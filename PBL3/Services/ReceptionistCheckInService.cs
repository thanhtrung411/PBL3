using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;
using PBL3.Services.Receptionist;

namespace PBL3.Services;

public class ReceptionistCheckInService : IReceptionistCheckInService
{
    private static readonly string[] MaintenanceStatuses =
    {
        DomainValues.PhongTrangThai.BaoTri,
        DomainValues.PhongTrangThai.NgungSuDung,
        "Bao tri",
        "Ngung su dung"
    };

    private static readonly string[] BusyStatuses =
    {
        DomainValues.PhongTrangThai.DangSuDung,
        "Dang su dung",
        "Có khách",
        "Co khach"
    };

    private readonly ApplicationDbContext _context;

    public ReceptionistCheckInService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReceptionistBookingLookupResult> LookupBookingAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        var bookingCode = ExtractBookingCode(input);
        if (string.IsNullOrWhiteSpace(bookingCode))
        {
            return FailLookup("Không tìm thấy mã đặt phòng trong dữ liệu QR hoặc ô nhập.");
        }

        var booking = await LoadBookingAsync(bookingCode, tracking: false, cancellationToken);
        if (booking == null)
        {
            return FailLookup("Không tìm thấy đặt phòng.");
        }

        var dto = BuildBookingDto(booking);
        return new ReceptionistBookingLookupResult
        {
            Success = true,
            Message = dto.CanCheckIn ? "Đã tìm thấy đặt phòng hợp lệ." : dto.CheckInMessage,
            Booking = dto
        };
    }

    public async Task<ReceptionistRoomSelectionResult> GetRoomSelectionAsync(
        string bookingCode,
        CancellationToken cancellationToken = default)
    {
        var booking = await LoadBookingAsync(bookingCode, tracking: false, cancellationToken);
        if (booking == null)
        {
            return FailRoomSelection("Không tìm thấy đặt phòng.");
        }

        var bookingDto = BuildBookingDto(booking);
        if (!bookingDto.CanCheckIn)
        {
            return FailRoomSelection(bookingDto.CheckInMessage, booking.MaDatPhong);
        }

        var requiredTypeIds = bookingDto.Requirements
            .Select(x => x.RoomTypeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var blockedRoomIds = await GetBlockedRoomIdsAsync(booking, cancellationToken);
        var rooms = await _context.Phongs
            .AsNoTracking()
            .Include(x => x.MaLoaiPhongNavigation)
            .Where(x => requiredTypeIds.Contains(x.MaLoaiPhong))
            .OrderBy(x => x.Tang)
            .ThenBy(x => x.SoPhong)
            .ToListAsync(cancellationToken);

        var groups = bookingDto.Requirements
            .Select(requirement => new ReceptionistRoomGroupDto
            {
                RoomTypeId = requirement.RoomTypeId,
                RoomTypeName = requirement.RoomTypeName,
                RequiredRooms = requirement.RequiredRooms,
                Rooms = rooms
                    .Where(room => string.Equals(room.MaLoaiPhong, requirement.RoomTypeId, StringComparison.OrdinalIgnoreCase))
                    .Select(room => BuildRoomDto(room, blockedRoomIds.Contains(room.MaPhong)))
                    .ToList()
            })
            .ToList();

        return new ReceptionistRoomSelectionResult
        {
            Success = true,
            Message = "Đã tải sơ đồ phòng.",
            BookingCode = booking.MaDatPhong,
            Requirements = bookingDto.Requirements,
            Groups = groups
        };
    }

    public async Task<ReceptionistCheckInResult> CheckInAsync(
        ReceptionistCheckInRequest request,
        CancellationToken cancellationToken = default)
    {
        var bookingCode = ExtractBookingCode(request.BookingCode);
        if (string.IsNullOrWhiteSpace(bookingCode))
        {
            return FailCheckIn("Thiếu mã đặt phòng.");
        }

        var roomIds = request.RoomIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var booking = await LoadBookingAsync(bookingCode, tracking: true, cancellationToken);
        if (booking == null)
        {
            return FailCheckIn("Không tìm thấy đặt phòng.", bookingCode);
        }

        var bookingDto = BuildBookingDto(booking);
        if (!bookingDto.CanCheckIn)
        {
            return FailCheckIn(bookingDto.CheckInMessage, booking.MaDatPhong);
        }

        var roomLines = GetActiveRoomLines(booking).ToList();
        if (roomIds.Count != roomLines.Count)
        {
            return FailCheckIn($"Cần chọn đúng {roomLines.Count} phòng để check-in.", booking.MaDatPhong);
        }

        var selectedRooms = await _context.Phongs
            .Include(x => x.MaLoaiPhongNavigation)
            .Where(x => roomIds.Contains(x.MaPhong))
            .ToListAsync(cancellationToken);

        if (selectedRooms.Count != roomIds.Count)
        {
            return FailCheckIn("Có phòng không tồn tại hoặc đã bị xóa.", booking.MaDatPhong);
        }

        var requiredCounts = bookingDto.Requirements.ToDictionary(
            x => x.RoomTypeId,
            x => x.RequiredRooms,
            StringComparer.OrdinalIgnoreCase);

        var selectedCounts = selectedRooms
            .GroupBy(x => x.MaLoaiPhong, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (var requirement in requiredCounts)
        {
            selectedCounts.TryGetValue(requirement.Key, out var selectedCount);
            if (selectedCount != requirement.Value)
            {
                return FailCheckIn(
                    $"Loại phòng {requirement.Key} cần chọn {requirement.Value} phòng.",
                    booking.MaDatPhong);
            }
        }

        var blockedRoomIds = await GetBlockedRoomIdsAsync(booking, cancellationToken);
        foreach (var room in selectedRooms)
        {
            if (blockedRoomIds.Contains(room.MaPhong) ||
                IsMaintenanceStatus(room.TrangThai) ||
                IsBusyStatus(room.TrangThai))
            {
                return FailCheckIn($"Phòng {room.SoPhong} không còn trống để nhận khách.", booking.MaDatPhong);
            }
        }

        foreach (var typeGroup in roomLines.GroupBy(x => x.MaLoaiPhong!, StringComparer.OrdinalIgnoreCase))
        {
            var roomsForType = selectedRooms
                .Where(x => string.Equals(x.MaLoaiPhong, typeGroup.Key, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.SoPhong)
                .ToList();

            var lineIndex = 0;
            foreach (var line in typeGroup.OrderBy(x => x.MaCthd))
            {
                line.MaPhong = roomsForType[lineIndex].MaPhong;
                lineIndex++;
            }
        }

        booking.TrangThai = DomainValues.DatPhongTrangThai.DaNhanPhong;
        foreach (var room in selectedRooms)
        {
            room.TrangThai = DomainValues.PhongTrangThai.DangSuDung;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ReceptionistCheckInResult
        {
            Success = true,
            Message = "Check-in thành công.",
            BookingCode = booking.MaDatPhong,
            AssignedRooms = selectedRooms
                .OrderBy(x => x.SoPhong)
                .Select(x => new ReceptionistAssignedRoomDto
                {
                    RoomId = x.MaPhong,
                    RoomNumber = x.SoPhong,
                    RoomTypeId = x.MaLoaiPhong,
                    RoomTypeName = x.MaLoaiPhongNavigation.TenLoaiPhong
                })
                .ToList()
        };
    }

    private async Task<DatPhong?> LoadBookingAsync(
        string bookingCode,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var query = _context.DatPhongs
            .Include(x => x.MaKhNavigation)
            .Include(x => x.HoaDon)
            .ThenInclude(x => x!.ChiTietHoaDons)
            .ThenInclude(x => x.MaLoaiPhongNavigation)
            .AsQueryable();

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            x => x.MaDatPhong == bookingCode.Trim(),
            cancellationToken);
    }

    private async Task<HashSet<string>> GetBlockedRoomIdsAsync(
        DatPhong booking,
        CancellationToken cancellationToken)
    {
        var blockedRoomIds = await _context.ChiTietHoaDons
            .AsNoTracking()
            .Include(x => x.MaHoaDonNavigation)
            .ThenInclude(x => x.MaDatPhongNavigation)
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.MaPhong != null &&
                        x.MaHoaDonNavigation.MaDatPhong != booking.MaDatPhong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.DaHuy &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.TraPhong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.NgayNhanPhong < booking.NgayTraPhong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.NgayTraPhong > booking.NgayNhanPhong)
            .Select(x => x.MaPhong!)
            .Distinct()
            .ToListAsync(cancellationToken);

        return blockedRoomIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static ReceptionistBookingDto BuildBookingDto(DatPhong booking)
    {
        var invoice = booking.HoaDon;
        var requirements = GetActiveRoomLines(booking)
            .Where(x => !string.IsNullOrWhiteSpace(x.MaLoaiPhong))
            .GroupBy(x => new
            {
                RoomTypeId = x.MaLoaiPhong!,
                RoomTypeName = x.MaLoaiPhongNavigation?.TenLoaiPhong ?? x.MaLoaiPhong!
            })
            .Select(group => new ReceptionistRoomTypeRequirementDto
            {
                RoomTypeId = group.Key.RoomTypeId,
                RoomTypeName = group.Key.RoomTypeName,
                RequiredRooms = group.Count(),
                Guests = group.Sum(x => x.SoNguoi)
            })
            .OrderBy(x => x.RoomTypeName)
            .ToList();

        var canCheckIn = true;
        var checkInMessage = "Có thể check-in.";
        if (booking.TrangThai == DomainValues.DatPhongTrangThai.DaHuy)
        {
            canCheckIn = false;
            checkInMessage = "Đặt phòng đã bị hủy.";
        }
        else if (booking.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong)
        {
            canCheckIn = false;
            checkInMessage = "Đặt phòng này đã check-in rồi.";
        }
        else if (invoice == null)
        {
            canCheckIn = false;
            checkInMessage = "Đặt phòng chưa có hóa đơn.";
        }
        else if (invoice.TrangThai != DomainValues.HoaDonTrangThai.DaThanhToan ||
                 invoice.SoTienDaThanhToan < invoice.TongThanhToan)
        {
            canCheckIn = false;
            checkInMessage = "Hóa đơn chưa được thanh toán đủ.";
        }
        else if (requirements.Count == 0)
        {
            canCheckIn = false;
            checkInMessage = "Đặt phòng chưa có dòng phòng hợp lệ.";
        }

        return new ReceptionistBookingDto
        {
            BookingCode = booking.MaDatPhong,
            CustomerName = booking.TenKhSnapshot,
            PhoneNumber = booking.SdtSnapshot ?? booking.MaKhNavigation.SoDienThoai,
            Email = booking.MaKhNavigation.Email,
            IdentityNumber = booking.CccdSnapshot ?? booking.MaKhNavigation.Cccd,
            CheckInDate = booking.NgayNhanPhong.ToString("dd/MM/yyyy"),
            CheckOutDate = booking.NgayTraPhong.ToString("dd/MM/yyyy"),
            Nights = Math.Max(booking.NgayTraPhong.DayNumber - booking.NgayNhanPhong.DayNumber, 1),
            BookingStatus = booking.TrangThai,
            InvoiceStatus = invoice?.TrangThai ?? "NO_INVOICE",
            TotalAmount = invoice?.TongThanhToan ?? 0,
            PaidAmount = invoice?.SoTienDaThanhToan ?? 0,
            CanCheckIn = canCheckIn,
            CheckInMessage = checkInMessage,
            Requirements = requirements
        };
    }

    private static IEnumerable<ChiTietHoaDon> GetActiveRoomLines(DatPhong booking)
    {
        return booking.HoaDon?.ChiTietHoaDons
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc) ??
               Enumerable.Empty<ChiTietHoaDon>();
    }

    private static ReceptionistRoomDto BuildRoomDto(Phong room, bool hasOverlappingAssignment)
    {
        var isMaintenance = IsMaintenanceStatus(room.TrangThai);
        var isBusy = IsBusyStatus(room.TrangThai) || hasOverlappingAssignment;
        var status = isMaintenance ? "maintenance" : isBusy ? "occupied" : "available";

        return new ReceptionistRoomDto
        {
            RoomId = room.MaPhong,
            RoomNumber = room.SoPhong,
            RoomTypeId = room.MaLoaiPhong,
            RoomTypeName = room.MaLoaiPhongNavigation.TenLoaiPhong,
            Floor = room.Tang,
            Status = status,
            StatusLabel = status switch
            {
                "maintenance" => "Bảo trì",
                "occupied" => "Đã có khách",
                _ => "Trống"
            },
            IsSelectable = status == "available"
        };
    }

    private static bool IsMaintenanceStatus(string? status)
    {
        return MatchesStatus(status, MaintenanceStatuses);
    }

    private static bool IsBusyStatus(string? status)
    {
        return MatchesStatus(status, BusyStatuses);
    }

    private static bool MatchesStatus(string? status, IEnumerable<string> candidates)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return candidates.Any(candidate =>
            string.Equals(status.Trim(), candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractBookingCode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();
        if (trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (document.RootElement.TryGetProperty("booking", out var bookingProperty))
                {
                    return ExtractBookingCode(bookingProperty.GetString());
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        var upper = trimmed.ToUpperInvariant();
        var start = upper.IndexOf("DP", StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        var end = start;
        while (end < upper.Length && char.IsLetterOrDigit(upper[end]) && end - start < 10)
        {
            end++;
        }

        return end > start ? upper[start..end] : null;
    }

    private static ReceptionistBookingLookupResult FailLookup(string message)
    {
        return new ReceptionistBookingLookupResult { Success = false, Message = message };
    }

    private static ReceptionistRoomSelectionResult FailRoomSelection(string message, string bookingCode = "")
    {
        return new ReceptionistRoomSelectionResult
        {
            Success = false,
            Message = message,
            BookingCode = bookingCode
        };
    }

    private static ReceptionistCheckInResult FailCheckIn(string message, string bookingCode = "")
    {
        return new ReceptionistCheckInResult
        {
            Success = false,
            Message = message,
            BookingCode = bookingCode
        };
    }
}
