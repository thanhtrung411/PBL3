using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;
using PBL3.Services.Receptionist;

namespace PBL3.Services;

public class ReceptionistCheckInService : IReceptionistCheckInService
{
    private const string WalkInVnPayMarker = "WALKIN_VNPAY_PENDING";

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
    private readonly IBookingEmailService _bookingEmailService;
    private readonly IInvoicePromotionService _invoicePromotionService;

    public ReceptionistCheckInService(
        ApplicationDbContext context,
        IBookingEmailService bookingEmailService,
        IInvoicePromotionService invoicePromotionService)
    {
        _context = context;
        _bookingEmailService = bookingEmailService;
        _invoicePromotionService = invoicePromotionService;
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

    public async Task<ReceptionistTodayArrivalsResult> GetTodayArrivalsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var bookingData = await _context.DatPhongs
            .AsNoTracking()
            .Where(x => x.NgayNhanPhong == today &&
                        x.TrangThai != DomainValues.DatPhongTrangThai.DaHuy &&
                        x.TrangThai != DomainValues.DatPhongTrangThai.TraPhong &&
                        x.TrangThai != DomainValues.DatPhongTrangThai.QuaHanNhanPhong)
            .OrderBy(x => x.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong)
            .ThenBy(x => x.TenKhSnapshot)
            .ThenBy(x => x.MaDatPhong)
            .Select(x => new
            {
                BookingCode = x.MaDatPhong,
                CustomerName = x.TenKhSnapshot,
                SdtSnapshot = x.SdtSnapshot,
                CccdSnapshot = x.CccdSnapshot,
                CustomerPhone = x.MaKhNavigation.SoDienThoai,
                CustomerEmail = x.MaKhNavigation.Email,
                CustomerCccd = x.MaKhNavigation.Cccd,
                BookingDate = x.NgayDat,
                CheckInDate = x.NgayNhanPhong,
                CheckOutDate = x.NgayTraPhong,
                BookingStatus = x.TrangThai,
                InvoiceStatus = x.HoaDon != null ? x.HoaDon.TrangThai : "NO_INVOICE",
                TotalAmount = x.HoaDon != null ? x.HoaDon.TongThanhToan : 0,
                PaidAmount = x.HoaDon != null ? x.HoaDon.SoTienDaThanhToan : 0,
                HasInvoice = x.HoaDon != null,
                RoomLines = x.HoaDon != null 
                    ? x.HoaDon.ChiTietHoaDons
                        .Where(ct => ct.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong && 
                                     ct.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                        .Select(ct => new
                        {
                            RoomTypeId = ct.MaLoaiPhong ?? string.Empty,
                            RoomTypeName = ct.MaLoaiPhongNavigation != null ? ct.MaLoaiPhongNavigation.TenLoaiPhong : (ct.MaLoaiPhong ?? "Phòng"),
                            Guests = ct.SoNguoi,
                            RoomId = ct.MaPhongNavigation != null ? ct.MaPhongNavigation.MaPhong : null,
                            RoomNumber = ct.MaPhongNavigation != null ? ct.MaPhongNavigation.SoPhong : null
                        })
                        .ToList()
                    : null
            })
            .ToListAsync(cancellationToken);

        var bookingDtos = bookingData.Select(data => 
        {
            var canCheckIn = true;
            var checkInMessage = "Có thể check-in.";
            
            if (data.BookingStatus == DomainValues.DatPhongTrangThai.DaHuy)
            {
                canCheckIn = false;
                checkInMessage = "Đặt phòng đã bị hủy.";
            }
            else if (data.BookingStatus == DomainValues.DatPhongTrangThai.QuaHanNhanPhong)
            {
                canCheckIn = false;
                checkInMessage = "Đặt phòng đã quá hạn nhận phòng.";
            }
            else if (data.BookingStatus == DomainValues.DatPhongTrangThai.DaNhanPhong)
            {
                canCheckIn = false;
                checkInMessage = "Đặt phòng này đã check-in rồi.";
            }
            else if (!data.HasInvoice)
            {
                canCheckIn = false;
                checkInMessage = "Đặt phòng chưa có hóa đơn.";
            }
            else if (data.InvoiceStatus != DomainValues.HoaDonTrangThai.DaThanhToan ||
                     data.PaidAmount < data.TotalAmount)
            {
                canCheckIn = false;
                checkInMessage = "Hóa đơn chưa được thanh toán đủ.";
            }

            var requirements = new List<ReceptionistRoomTypeRequirementDto>();
            var assignedRooms = new List<ReceptionistAssignedRoomDto>();

            if (data.RoomLines != null)
            {
                requirements = data.RoomLines
                    .Where(x => !string.IsNullOrWhiteSpace(x.RoomTypeId))
                    .GroupBy(x => new { x.RoomTypeId, x.RoomTypeName })
                    .Select(group => new ReceptionistRoomTypeRequirementDto
                    {
                        RoomTypeId = group.Key.RoomTypeId,
                        RoomTypeName = group.Key.RoomTypeName,
                        RequiredRooms = group.Count(),
                        Guests = group.Sum(x => x.Guests)
                    })
                    .OrderBy(x => x.RoomTypeName)
                    .ToList();

                assignedRooms = data.RoomLines
                    .Where(x => x.RoomId != null)
                    .OrderBy(x => x.RoomNumber)
                    .Select(x => new ReceptionistAssignedRoomDto
                    {
                        RoomId = x.RoomId,
                        RoomNumber = x.RoomNumber,
                        RoomTypeId = x.RoomTypeId,
                        RoomTypeName = x.RoomTypeName
                    })
                    .ToList();
            }

            if (canCheckIn && requirements.Count == 0)
            {
                canCheckIn = false;
                checkInMessage = "Đặt phòng chưa có dòng phòng hợp lệ.";
            }

            return new ReceptionistBookingDto
            {
                BookingCode = data.BookingCode,
                CustomerName = data.CustomerName,
                PhoneNumber = data.SdtSnapshot ?? data.CustomerPhone,
                Email = data.CustomerEmail,
                IdentityNumber = data.CccdSnapshot ?? data.CustomerCccd,
                BookingDate = data.BookingDate.ToString("dd/MM/yyyy"),
                CheckInDate = data.CheckInDate.ToString("dd/MM/yyyy"),
                CheckOutDate = data.CheckOutDate.ToString("dd/MM/yyyy"),
                Nights = Math.Max(data.CheckOutDate.DayNumber - data.CheckInDate.DayNumber, 1),
                BookingStatus = data.BookingStatus,
                InvoiceStatus = data.InvoiceStatus,
                TotalAmount = data.TotalAmount,
                PaidAmount = data.PaidAmount,
                CanCheckIn = canCheckIn,
                CheckInMessage = checkInMessage,
                Requirements = requirements,
                AssignedRooms = assignedRooms
            };
        }).ToList();

        return new ReceptionistTodayArrivalsResult
        {
            Success = true,
            Message = bookingDtos.Count == 0
                ? "Không có khách dự kiến đến hôm nay."
                : $"Có {bookingDtos.Count} khách dự kiến đến hôm nay.",
            DateLabel = today.ToString("dd/MM/yyyy"),
            Bookings = bookingDtos
        };
    }

    public async Task<ReceptionistRoomMapResult> GetRoomMapAsync(
        CancellationToken cancellationToken = default)
    {
        var roomsData = await _context.Phongs
            .AsNoTracking()
            .OrderBy(x => x.Tang)
            .ThenBy(x => x.SoPhong)
            .Select(x => new
            {
                RoomId = x.MaPhong,
                RoomNumber = x.SoPhong,
                RoomTypeId = x.MaLoaiPhong,
                RoomTypeName = x.MaLoaiPhongNavigation.TenLoaiPhong,
                Floor = x.Tang,
                Status = x.TrangThai,
                Capacity = x.MaLoaiPhongNavigation.SoNguoiToiDa
            })
            .ToListAsync(cancellationToken);

        var activeAssignments = await _context.ChiTietHoaDons
            .AsNoTracking()
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc &&
                        x.MaPhong != null &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong)
            .Select(x => new
            {
                RoomId = x.MaPhong!,
                BookingCode = x.MaHoaDonNavigation.MaDatPhong,
                GuestName = x.MaHoaDonNavigation.MaDatPhongNavigation.TenKhSnapshot,
                CheckOutDate = x.MaHoaDonNavigation.MaDatPhongNavigation.NgayTraPhong
            })
            .ToListAsync(cancellationToken);

        var assignmentByRoom = activeAssignments
            .GroupBy(x => x.RoomId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var roomDtos = roomsData
            .Select(room =>
            {
                assignmentByRoom.TryGetValue(room.RoomId, out var assignment);
                var hasActiveAssignment = assignment != null;
                var isMaintenance = IsMaintenanceStatus(room.Status);
                var isBusy = IsBusyStatus(room.Status) || hasActiveAssignment;
                var mappedStatus = isMaintenance ? "maintenance" : isBusy ? "occupied" : "available";

                return new ReceptionistRoomDto
                {
                    RoomId = room.RoomId,
                    RoomNumber = room.RoomNumber,
                    RoomTypeId = room.RoomTypeId,
                    RoomTypeName = room.RoomTypeName,
                    Floor = room.Floor,
                    Status = mappedStatus,
                    StatusLabel = mappedStatus switch
                    {
                        "maintenance" => "Bảo trì",
                        "occupied" => "Đang sử dụng",
                        _ => "Trống"
                    },
                    IsSelectable = mappedStatus == "available",
                    Capacity = room.Capacity,
                    BookingCode = assignment?.BookingCode,
                    CurrentGuestName = assignment?.GuestName,
                    CheckOutDate = assignment?.CheckOutDate.ToString("dd/MM/yyyy")
                };
            })
            .ToList();

        var statusCounts = new[] { "available", "occupied", "maintenance" }
            .Select(status => new ReceptionistRoomMapStatusCountDto
            {
                Status = status,
                StatusLabel = status switch
                {
                    "available" => "Trống",
                    "occupied" => "Đang sử dụng",
                    _ => "Bảo trì"
                },
                Count = roomDtos.Count(x => x.Status == status)
            })
            .ToList();

        return new ReceptionistRoomMapResult
        {
            Success = true,
            Message = $"Đã tải {roomDtos.Count} phòng.",
            StatusCounts = statusCounts,
            Floors = roomDtos
                .GroupBy(x => x.Floor)
                .OrderBy(x => x.Key)
                .Select(group => new ReceptionistFloorRoomMapDto
                {
                    Floor = group.Key,
                    Rooms = group.ToList()
                })
                .ToList()
        };
    }

    public async Task<RoomMaintenanceResult> SetRoomMaintenanceAsync(
        RoomMaintenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var roomId = NormalizeOptional(request.RoomId);
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return FailRoomMaintenance("Thiếu mã phòng.");
        }

        var room = await _context.Phongs
            .FirstOrDefaultAsync(x => x.MaPhong == roomId, cancellationToken);
        if (room == null)
        {
            return FailRoomMaintenance("Không tìm thấy phòng.");
        }

        var hasActiveAssignment = await _context.ChiTietHoaDons
            .AsNoTracking()
            .AnyAsync(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                           x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc &&
                           x.MaPhong == room.MaPhong &&
                           x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong,
                cancellationToken);

        if (hasActiveAssignment || IsBusyStatus(room.TrangThai))
        {
            return FailRoomMaintenance("Chỉ có thể đổi bảo trì khi phòng đang trống.");
        }

        room.TrangThai = request.Maintenance
            ? DomainValues.PhongTrangThai.BaoTri
            : DomainValues.PhongTrangThai.Trong;
        await _context.SaveChangesAsync(cancellationToken);

        return new RoomMaintenanceResult
        {
            Success = true,
            Message = request.Maintenance
                ? "Đã chuyển phòng sang bảo trì."
                : "Đã hủy bảo trì, phòng đã trống.",
            RoomId = room.MaPhong,
            Status = request.Maintenance ? "maintenance" : "available",
            StatusLabel = request.Maintenance ? "Bảo trì" : "Trống"
        };
    }

    public async Task<ReceptionistServiceUsageResult> GetServiceUsageAsync(
        CancellationToken cancellationToken = default)
    {
        var activeBookingsData = await _context.DatPhongs
            .AsNoTracking()
            .Where(x => x.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong &&
                        x.HoaDon != null)
            .OrderBy(x => x.TenKhSnapshot)
            .ThenBy(x => x.MaDatPhong)
            .Select(x => new
            {
                BookingCode = x.MaDatPhong,
                CustomerName = x.TenKhSnapshot,
                SdtSnapshot = x.SdtSnapshot,
                CustomerPhone = x.MaKhNavigation.SoDienThoai,
                CustomerEmail = x.MaKhNavigation.Email,
                CheckInDate = x.NgayNhanPhong,
                CheckOutDate = x.NgayTraPhong,
                InvoiceStatus = x.HoaDon!.TrangThai,
                RoomTotal = x.HoaDon.TongTienPhong,
                ServiceTotal = x.HoaDon.TongTienDichVu,
                DiscountAmount = x.HoaDon.TienGiamGiaPhong,
                PromotionCode = x.HoaDon.MaGiamGiaPhongNavigation != null ? x.HoaDon.MaGiamGiaPhongNavigation.CodeGiamGia : x.HoaDon.MaGiamGiaPhong,
                PromotionName = x.HoaDon.MaGiamGiaPhongNavigation != null ? x.HoaDon.MaGiamGiaPhongNavigation.TenMaGiamGia : null,
                PaidAmount = x.HoaDon.SoTienDaThanhToan,
                GrandTotal = x.HoaDon.TongThanhToan,
                RoomLines = x.HoaDon.ChiTietHoaDons
                    .Where(ct => ct.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                                 ct.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                    .Select(ct => new
                    {
                        LineId = ct.MaCthd,
                        RoomTypeId = ct.MaLoaiPhong ?? string.Empty,
                        RoomTypeName = ct.MaLoaiPhongNavigation != null ? ct.MaLoaiPhongNavigation.TenLoaiPhong : (ct.MaLoaiPhong ?? ct.NoiDung),
                        RoomNumber = ct.MaPhongNavigation != null ? ct.MaPhongNavigation.SoPhong : null,
                        Guests = ct.SoNguoi,
                        Quantity = ct.SoLuong,
                        UnitPrice = ct.DonGia,
                        Total = ct.ThanhTien
                    })
                    .ToList(),
                ServiceLines = x.HoaDon.ChiTietHoaDons
                    .Where(ct => ct.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.DichVu &&
                                 ct.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                    .Select(ct => new
                    {
                        LineId = ct.MaCthd,
                        ServiceId = ct.MaDv ?? string.Empty,
                        ServiceName = ct.MaDvNavigation != null ? ct.MaDvNavigation.TenDv : ct.NoiDung,
                        Unit = ct.MaDvNavigation != null ? ct.MaDvNavigation.DonViTinh : string.Empty,
                        Quantity = ct.SoLuong,
                        UnitPrice = ct.DonGia,
                        Total = ct.ThanhTien,
                        AppliedDate = ct.NgayApDung,
                        Note = ct.GhiChu
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var services = await _context.DichVus
            .AsNoTracking()
            .OrderBy(x => x.LoaiDichVu)
            .ThenBy(x => x.TenDv)
            .Select(x => new ReceptionistServiceOptionDto
            {
                ServiceId = x.MaDv,
                ServiceName = x.TenDv,
                Unit = x.DonViTinh,
                Category = x.LoaiDichVu,
                UnitPrice = x.DonGia
            })
            .ToListAsync(cancellationToken);

        var stays = activeBookingsData.Select(data => new ReceptionistActiveStayDto
        {
            BookingCode = data.BookingCode,
            CustomerName = data.CustomerName,
            PhoneNumber = data.SdtSnapshot ?? data.CustomerPhone,
            Email = data.CustomerEmail,
            CheckInDate = data.CheckInDate.ToString("dd/MM/yyyy"),
            CheckOutDate = data.CheckOutDate.ToString("dd/MM/yyyy"),
            Nights = Math.Max(data.CheckOutDate.DayNumber - data.CheckInDate.DayNumber, 1),
            InvoiceStatus = data.InvoiceStatus,
            RoomTotal = data.RoomTotal,
            ServiceTotal = data.ServiceTotal,
            DiscountAmount = data.DiscountAmount,
            PromotionCode = data.PromotionCode?.Trim(),
            PromotionName = data.PromotionName?.Trim(),
            PaidAmount = data.PaidAmount,
            GrandTotal = data.GrandTotal,
            RemainingAmount = Math.Max(data.GrandTotal - data.PaidAmount, 0),
            RoomNumbers = data.RoomLines.Where(x => x.RoomNumber != null).Select(x => x.RoomNumber!).OrderBy(x => x).ToList(),
            RoomLines = data.RoomLines.Select(x => new ReceptionistRoomChargeLineDto
            {
                LineId = x.LineId,
                RoomTypeId = x.RoomTypeId,
                RoomTypeName = x.RoomTypeName,
                RoomNumber = x.RoomNumber,
                Guests = x.Guests,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                Total = x.Total
            }).OrderBy(x => x.LineId).ToList(),
            ServiceLines = data.ServiceLines.Select(x => new ReceptionistServiceLineDto
            {
                LineId = x.LineId,
                ServiceId = x.ServiceId,
                ServiceName = x.ServiceName,
                Unit = x.Unit,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                Total = x.Total,
                AppliedDate = x.AppliedDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                Note = x.Note
            }).OrderByDescending(x => string.IsNullOrWhiteSpace(x.AppliedDate) ? "0000" : x.AppliedDate).ThenByDescending(x => x.LineId).ToList()
        }).ToList();

        return new ReceptionistServiceUsageResult
        {
            Success = true,
            Message = stays.Count == 0
                ? "Hiện chưa có khách đang lưu trú để ghi nhận dịch vụ."
                : $"Đã tải {stays.Count} khách đang lưu trú.",
            ActiveStays = stays,
            Services = services
        };
    }

    public async Task<ReceptionistAddServiceResult> AddServiceUsageAsync(
        ReceptionistAddServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var bookingCode = ExtractBookingCode(request.BookingCode);
        if (string.IsNullOrWhiteSpace(bookingCode))
        {
            return FailAddService("Thiếu mã đặt phòng.");
        }

        if (string.IsNullOrWhiteSpace(request.ServiceId))
        {
            return FailAddService("Chọn dịch vụ trước khi lưu.", bookingCode);
        }

        if (request.Quantity <= 0)
        {
            return FailAddService("Số lượng dịch vụ phải lớn hơn 0.", bookingCode);
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(ExecuteAddServiceTransactionAsync);

        async Task<ReceptionistAddServiceResult> ExecuteAddServiceTransactionAsync()
        {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var booking = await _context.DatPhongs
            .Include(x => x.HoaDon)
            .ThenInclude(x => x!.ChiTietHoaDons)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.MaDatPhong == bookingCode, cancellationToken);

        if (booking == null)
        {
            return FailAddService("Không tìm thấy đặt phòng.", bookingCode);
        }

        if (booking.TrangThai != DomainValues.DatPhongTrangThai.DaNhanPhong)
        {
            return FailAddService("Chỉ ghi nhận dịch vụ cho khách đã check-in.", booking.MaDatPhong);
        }

        if (booking.HoaDon == null)
        {
            return FailAddService("Đặt phòng chưa có hóa đơn.", booking.MaDatPhong);
        }

        var serviceId = request.ServiceId.Trim();
        var service = await _context.DichVus
            .FirstOrDefaultAsync(x => x.MaDv == serviceId, cancellationToken);
        if (service == null)
        {
            return FailAddService("Không tìm thấy dịch vụ.", booking.MaDatPhong);
        }

        var lineCode = await CodeGenerator.GenerateFromSequenceAsync(
            _context,
            "dbo.Seq_ChiTietHoaDon",
            "CT",
            8);
        if (lineCode == null)
        {
            return FailAddService("Không thể tạo mã chi tiết hóa đơn mới.", booking.MaDatPhong);
        }

        var quantity = request.Quantity;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        var line = new ChiTietHoaDon
        {
            MaCthd = lineCode,
            MaHoaDon = booking.HoaDon.MaHoaDon,
            LoaiMuc = DomainValues.ChiTietHoaDonLoaiMuc.DichVu,
            MaDv = service.MaDv,
            NoiDung = $"Dịch vụ {service.TenDv}",
            NgayApDung = today,
            SoNguoi = 0,
            SoLuong = quantity,
            DonGia = service.DonGia,
            ThanhTien = service.DonGia * quantity,
            TrangThai = DomainValues.ChiTietHoaDonTrangThai.HieuLuc,
            GhiChu = note
        };

        booking.HoaDon.ChiTietHoaDons.Add(line);
        booking.HoaDon.TongTienDichVu = booking.HoaDon.ChiTietHoaDons
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.DichVu &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
            .Sum(x => x.ThanhTien);
        await _invoicePromotionService.ApplyBestPromotionAsync(booking.HoaDon, cancellationToken, serviceOnly: true);

        booking.HoaDon.TrangThai = booking.HoaDon.SoTienDaThanhToan >= booking.HoaDon.TongThanhToan
            ? DomainValues.HoaDonTrangThai.DaThanhToan
            : DomainValues.HoaDonTrangThai.ThanhToanMotPhan;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ReceptionistAddServiceResult
        {
            Success = true,
            Message = "Đã ghi nhận dịch vụ vào hóa đơn.",
            BookingCode = booking.MaDatPhong,
            ServiceTotal = booking.HoaDon.TongTienDichVu,
            GrandTotal = booking.HoaDon.TongThanhToan,
            AddedLine = BuildServiceLineDto(line, service)
        };
        }
    }

    public async Task<ReceptionistCheckoutListResult> GetCheckoutListAsync(
        CancellationToken cancellationToken = default)
    {
        var activeBookingsData = await _context.DatPhongs
            .AsNoTracking()
            .Where(x => x.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong &&
                        x.HoaDon != null)
            .OrderBy(x => x.NgayTraPhong)
            .ThenBy(x => x.TenKhSnapshot)
            .ThenBy(x => x.MaDatPhong)
            .Select(x => new
            {
                BookingCode = x.MaDatPhong,
                CustomerName = x.TenKhSnapshot,
                SdtSnapshot = x.SdtSnapshot,
                CustomerPhone = x.MaKhNavigation.SoDienThoai,
                CustomerEmail = x.MaKhNavigation.Email,
                CheckInDate = x.NgayNhanPhong,
                CheckOutDate = x.NgayTraPhong,
                InvoiceStatus = x.HoaDon!.TrangThai,
                RoomTotal = x.HoaDon.TongTienPhong,
                ServiceTotal = x.HoaDon.TongTienDichVu,
                DiscountAmount = x.HoaDon.TienGiamGiaPhong,
                PromotionCode = x.HoaDon.MaGiamGiaPhongNavigation != null ? x.HoaDon.MaGiamGiaPhongNavigation.CodeGiamGia : x.HoaDon.MaGiamGiaPhong,
                PromotionName = x.HoaDon.MaGiamGiaPhongNavigation != null ? x.HoaDon.MaGiamGiaPhongNavigation.TenMaGiamGia : null,
                PaidAmount = x.HoaDon.SoTienDaThanhToan,
                GrandTotal = x.HoaDon.TongThanhToan,
                RoomLines = x.HoaDon.ChiTietHoaDons
                    .Where(ct => ct.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                                 ct.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                    .Select(ct => new
                    {
                        LineId = ct.MaCthd,
                        RoomTypeId = ct.MaLoaiPhong ?? string.Empty,
                        RoomTypeName = ct.MaLoaiPhongNavigation != null ? ct.MaLoaiPhongNavigation.TenLoaiPhong : (ct.MaLoaiPhong ?? ct.NoiDung),
                        RoomNumber = ct.MaPhongNavigation != null ? ct.MaPhongNavigation.SoPhong : null,
                        Guests = ct.SoNguoi,
                        Quantity = ct.SoLuong,
                        UnitPrice = ct.DonGia,
                        Total = ct.ThanhTien
                    })
                    .ToList(),
                ServiceLines = x.HoaDon.ChiTietHoaDons
                    .Where(ct => ct.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.DichVu &&
                                 ct.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                    .Select(ct => new
                    {
                        LineId = ct.MaCthd,
                        ServiceId = ct.MaDv ?? string.Empty,
                        ServiceName = ct.MaDvNavigation != null ? ct.MaDvNavigation.TenDv : ct.NoiDung,
                        Unit = ct.MaDvNavigation != null ? ct.MaDvNavigation.DonViTinh : string.Empty,
                        Quantity = ct.SoLuong,
                        UnitPrice = ct.DonGia,
                        Total = ct.ThanhTien,
                        AppliedDate = ct.NgayApDung,
                        Note = ct.GhiChu
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var stays = activeBookingsData.Select(data => new ReceptionistActiveStayDto
        {
            BookingCode = data.BookingCode,
            CustomerName = data.CustomerName,
            PhoneNumber = data.SdtSnapshot ?? data.CustomerPhone,
            Email = data.CustomerEmail,
            CheckInDate = data.CheckInDate.ToString("dd/MM/yyyy"),
            CheckOutDate = data.CheckOutDate.ToString("dd/MM/yyyy"),
            Nights = Math.Max(data.CheckOutDate.DayNumber - data.CheckInDate.DayNumber, 1),
            InvoiceStatus = data.InvoiceStatus,
            RoomTotal = data.RoomTotal,
            ServiceTotal = data.ServiceTotal,
            DiscountAmount = data.DiscountAmount,
            PromotionCode = data.PromotionCode?.Trim(),
            PromotionName = data.PromotionName?.Trim(),
            PaidAmount = data.PaidAmount,
            GrandTotal = data.GrandTotal,
            RemainingAmount = Math.Max(data.GrandTotal - data.PaidAmount, 0),
            RoomNumbers = data.RoomLines.Where(x => x.RoomNumber != null).Select(x => x.RoomNumber!).OrderBy(x => x).ToList(),
            RoomLines = data.RoomLines.Select(x => new ReceptionistRoomChargeLineDto
            {
                LineId = x.LineId,
                RoomTypeId = x.RoomTypeId,
                RoomTypeName = x.RoomTypeName,
                RoomNumber = x.RoomNumber,
                Guests = x.Guests,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                Total = x.Total
            }).OrderBy(x => x.LineId).ToList(),
            ServiceLines = data.ServiceLines.Select(x => new ReceptionistServiceLineDto
            {
                LineId = x.LineId,
                ServiceId = x.ServiceId,
                ServiceName = x.ServiceName,
                Unit = x.Unit,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                Total = x.Total,
                AppliedDate = x.AppliedDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                Note = x.Note
            }).OrderByDescending(x => string.IsNullOrWhiteSpace(x.AppliedDate) ? "0000" : x.AppliedDate).ThenByDescending(x => x.LineId).ToList()
        }).ToList();

        return new ReceptionistCheckoutListResult
        {
            Success = true,
            Message = stays.Count == 0
                ? "Hiện chưa có khách đang lưu trú để check-out."
                : $"Đã tải {stays.Count} khách đang lưu trú.",
            ActiveStays = stays
        };
    }

    public async Task<ReceptionistCheckoutResult> CheckoutAsync(
        ReceptionistCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var bookingCode = ExtractBookingCode(request.BookingCode);
        if (string.IsNullOrWhiteSpace(bookingCode))
        {
            return FailCheckout("Thiếu mã đặt phòng.");
        }

        if (request.PaymentAmount < 0)
        {
            return FailCheckout("Số tiền thu thêm không hợp lệ.", bookingCode);
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(ExecuteCheckoutTransactionAsync);

        async Task<ReceptionistCheckoutResult> ExecuteCheckoutTransactionAsync()
        {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var booking = await _context.DatPhongs
            .Include(x => x.MaKhNavigation)
            .Include(x => x.HoaDon)
            .ThenInclude(x => x!.ChiTietHoaDons)
            .ThenInclude(x => x.MaPhongNavigation)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.MaDatPhong == bookingCode, cancellationToken);

        if (booking == null)
        {
            return FailCheckout("Không tìm thấy đặt phòng.", bookingCode);
        }

        if (booking.TrangThai != DomainValues.DatPhongTrangThai.DaNhanPhong)
        {
            return FailCheckout("Chỉ có thể check-out khách đang lưu trú.", booking.MaDatPhong);
        }

        if (booking.HoaDon == null)
        {
            return FailCheckout("Đặt phòng chưa có hóa đơn.", booking.MaDatPhong);
        }

        var invoice = booking.HoaDon;
        invoice.TongThanhToan = Math.Max(
            invoice.TongTienPhong + invoice.TongTienDichVu - invoice.TienGiamGiaPhong,
            0);

        var remaining = Math.Max(invoice.TongThanhToan - invoice.SoTienDaThanhToan, 0);
        if (request.PaymentAmount + 0.01m < remaining)
        {
            return FailCheckout(
                $"Cần thu đủ số tiền còn lại {remaining:N0}đ trước khi check-out.",
                booking.MaDatPhong,
                invoice.SoTienDaThanhToan,
                invoice.TongThanhToan,
                remaining);
        }

        var paymentToApply = Math.Min(request.PaymentAmount, remaining);
        if (paymentToApply > 0)
        {
            invoice.SoTienDaThanhToan += paymentToApply;
            invoice.NgayThanhToanCuoi = DateTime.UtcNow;
            invoice.PhuongThucThanhToan = string.IsNullOrWhiteSpace(request.PaymentMethod)
                ? DomainValues.PhuongThucThanhToan.TienMat
                : request.PaymentMethod.Trim();
        }

        invoice.TrangThai = DomainValues.HoaDonTrangThai.DaThanhToan;
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            invoice.GhiChu = string.IsNullOrWhiteSpace(invoice.GhiChu)
                ? request.Note.Trim()
                : $"{invoice.GhiChu}\n{request.Note.Trim()}";
        }

        var receiptEmail = NormalizeOptional(request.ReceiptEmail);
        if (request.SendReceiptEmail && string.IsNullOrWhiteSpace(receiptEmail))
        {
            return FailCheckout(
                "Nhập email khách trước khi gửi hóa đơn.",
                booking.MaDatPhong,
                invoice.SoTienDaThanhToan,
                invoice.TongThanhToan,
                Math.Max(invoice.TongThanhToan - invoice.SoTienDaThanhToan, 0));
        }

        if (!string.IsNullOrWhiteSpace(receiptEmail) &&
            !string.Equals(booking.MaKhNavigation.Email, receiptEmail, StringComparison.OrdinalIgnoreCase))
        {
            booking.MaKhNavigation.Email = receiptEmail;
        }

        var roomLines = invoice.ChiTietHoaDons
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc &&
                        x.MaPhongNavigation != null)
            .ToList();

        var releasedRooms = roomLines
            .Select(x => x.MaPhongNavigation!.SoPhong)
            .OrderBy(x => x)
            .ToList();

        foreach (var room in roomLines.Select(x => x.MaPhongNavigation!).DistinctBy(x => x.MaPhong))
        {
            room.TrangThai = DomainValues.PhongTrangThai.Trong;
        }

        booking.TrangThai = DomainValues.DatPhongTrangThai.TraPhong;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var emailSent = request.SendReceiptEmail &&
            await _bookingEmailService.SendCheckoutReceiptEmailAsync(
                booking.MaDatPhong,
                receiptEmail,
                cancellationToken);
        var emailMessage = request.SendReceiptEmail
            ? emailSent
                ? "Đã gửi hóa đơn check-out qua email."
                : "Không gửi được email hóa đơn. Kiểm tra email khách hoặc cấu hình SMTP."
            : "Không gửi email hóa đơn theo lựa chọn của lễ tân.";

        return new ReceptionistCheckoutResult
        {
            Success = true,
            Message = $"Check-out thành công. {emailMessage}",
            BookingCode = booking.MaDatPhong,
            PaidAmount = invoice.SoTienDaThanhToan,
            GrandTotal = invoice.TongThanhToan,
            RemainingAmount = Math.Max(invoice.TongThanhToan - invoice.SoTienDaThanhToan, 0),
            EmailSent = emailSent,
            EmailMessage = emailMessage,
            ReleasedRooms = releasedRooms
        };
        }
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
            .Where(x => requiredTypeIds.Contains(x.MaLoaiPhong))
            .OrderBy(x => x.Tang)
            .ThenBy(x => x.SoPhong)
            .Select(x => new
            {
                x.MaPhong,
                x.SoPhong,
                x.MaLoaiPhong,
                RoomTypeName = x.MaLoaiPhongNavigation.TenLoaiPhong,
                x.Tang,
                x.TrangThai,
                Capacity = x.MaLoaiPhongNavigation.SoNguoiToiDa
            })
            .ToListAsync(cancellationToken);
        var groups = bookingDto.Requirements
            .Select(requirement => new ReceptionistRoomGroupDto
            {
                RoomTypeId = requirement.RoomTypeId,
                RoomTypeName = requirement.RoomTypeName,
                RequiredRooms = requirement.RequiredRooms,
                Rooms = rooms
                    .Where(room => string.Equals(room.MaLoaiPhong, requirement.RoomTypeId, StringComparison.OrdinalIgnoreCase))
                    .Select(room =>
                    {
                        var hasOverlap = blockedRoomIds.Contains(room.MaPhong);
                        var isMaintenance = IsMaintenanceStatus(room.TrangThai);
                        var isBusy = IsBusyStatus(room.TrangThai) || hasOverlap;
                        var status = isMaintenance ? "maintenance" : isBusy ? "occupied" : "available";
                        return new ReceptionistRoomDto
                        {
                            RoomId = room.MaPhong,
                            RoomNumber = room.SoPhong,
                            RoomTypeId = room.MaLoaiPhong,
                            RoomTypeName = room.RoomTypeName,
                            Floor = room.Tang,
                            Status = status,
                            StatusLabel = status switch
                            {
                                "maintenance" => "Bảo trì",
                                "occupied" => "Đang sử dụng",
                                _ => "Trống"
                            },
                            IsSelectable = status == "available",
                            Capacity = room.Capacity
                        };
                    })
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

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(ExecuteCheckInTransactionAsync);

        async Task<ReceptionistCheckInResult> ExecuteCheckInTransactionAsync()
        {
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
    }

    public async Task<ReceptionistWalkInAvailabilityResult> GetWalkInAvailabilityAsync(
        ReceptionistWalkInAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var checkInDate = request.CheckInDate == default ? today : request.CheckInDate;
        if (checkInDate < today)
        {
            return new ReceptionistWalkInAvailabilityResult
            {
                Success = false,
                Message = "Ngày nhận phòng không được trước hôm nay."
            };
        }

        if (request.CheckOutDate <= checkInDate)
        {
            return new ReceptionistWalkInAvailabilityResult
            {
                Success = false,
                Message = "Ngày trả phòng phải sau hôm nay."
            };
        }

        var nights = Math.Max(request.CheckOutDate.DayNumber - checkInDate.DayNumber, 1);
        var rooms = await _context.Phongs
            .AsNoTracking()
            .OrderBy(x => x.Tang)
            .ThenBy(x => x.SoPhong)
            .Select(x => new
            {
                x.MaPhong,
                x.SoPhong,
                x.MaLoaiPhong,
                RoomTypeName = x.MaLoaiPhongNavigation.TenLoaiPhong,
                x.Tang,
                x.TrangThai,
                Capacity = x.MaLoaiPhongNavigation.SoNguoiToiDa
            })
            .ToListAsync(cancellationToken);
        var blockedRoomIds = await GetBlockedRoomIdsForRangeAsync(
            checkInDate,
            request.CheckOutDate,
            cancellationToken);

        var selectableRooms = rooms
            .Where(room => !blockedRoomIds.Contains(room.MaPhong) &&
                           !IsMaintenanceStatus(room.TrangThai) &&
                           (checkInDate > today || !IsBusyStatus(room.TrangThai)))
            .ToList();
        var unassignedDemand = await GetUnassignedDemandByRoomTypeAsync(
            checkInDate,
            request.CheckOutDate,
            excludedBookingCode: null,
            cancellationToken);
        var roomTypePrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var roomTypeId in rooms.Select(x => x.MaLoaiPhong).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            roomTypePrices[roomTypeId] = await GetCurrentRoomTypePriceAsync(roomTypeId, checkInDate, cancellationToken);
        }

        var maxSelectableRoomCount = selectableRooms
            .GroupBy(x => x.MaLoaiPhong, StringComparer.OrdinalIgnoreCase)
            .Sum(group =>
            {
                unassignedDemand.TryGetValue(group.Key, out var reservedCount);
                return Math.Max(group.Count() - reservedCount, 0);
            });
        var reservedRoomCount = Math.Max(selectableRooms.Count - maxSelectableRoomCount, 0);

        return new ReceptionistWalkInAvailabilityResult
        {
            Success = true,
            Message = selectableRooms.Count == 0 || maxSelectableRoomCount == 0
                ? "Hiện không có phòng trống phù hợp."
                : reservedRoomCount > 0
                    ? $"Có {maxSelectableRoomCount} phòng có thể chọn. {reservedRoomCount} suất phòng đang giữ cho đặt trước."
                    : $"Có {maxSelectableRoomCount} phòng trống có thể nhận khách.",
            CheckInDate = checkInDate.ToString("dd/MM/yyyy"),
            CheckOutDate = request.CheckOutDate.ToString("dd/MM/yyyy"),
            Nights = nights,
            Groups = rooms
                .GroupBy(x => new { x.MaLoaiPhong, x.RoomTypeName })
                .OrderBy(x => x.Min(room => room.Capacity))
                .ThenBy(x => roomTypePrices.TryGetValue(x.Key.MaLoaiPhong, out var price) ? price : 0)
                .ThenBy(x => x.Key.RoomTypeName)
                .Select(group =>
                {
                    var selectableCount = group.Count(room => selectableRooms.Any(selectable =>
                        string.Equals(selectable.MaPhong, room.MaPhong, StringComparison.OrdinalIgnoreCase)));
                    var reservedForType = unassignedDemand.TryGetValue(group.Key.MaLoaiPhong, out var reservedDemand)
                        ? Math.Min(reservedDemand, selectableCount)
                        : 0;

                    return new ReceptionistRoomGroupDto
                    {
                        RoomTypeId = group.Key.MaLoaiPhong,
                        RoomTypeName = group.Key.RoomTypeName,
                        RequiredRooms = 0,
                        MaxSelectableRooms = Math.Max(selectableCount - reservedForType, 0),
                        ReservedForBookingCount = reservedForType,
                        PricePerNight = roomTypePrices.TryGetValue(group.Key.MaLoaiPhong, out var groupPrice) ? groupPrice : 0,
                        Capacity = group.First().Capacity,
                        Rooms = group.OrderBy(room => room.Tang).ThenBy(room => room.SoPhong).Select(room =>
                        {
                            var hasOverlappingAssignment = blockedRoomIds.Contains(room.MaPhong) ||
                                (checkInDate <= today && IsBusyStatus(room.TrangThai));
                            var isMaintenance = IsMaintenanceStatus(room.TrangThai);
                            var isBusy = IsBusyStatus(room.TrangThai) || hasOverlappingAssignment;
                            var status = isMaintenance ? "maintenance" : isBusy ? "occupied" : "available";
                            return new ReceptionistRoomDto
                            {
                                RoomId = room.MaPhong,
                                RoomNumber = room.SoPhong,
                                RoomTypeId = room.MaLoaiPhong,
                                RoomTypeName = room.RoomTypeName,
                                Floor = room.Tang,
                                Status = status,
                                StatusLabel = status switch
                                {
                                    "maintenance" => "Bảo trì",
                                    "occupied" => "Đang sử dụng",
                                    _ => "Trống"
                                },
                                IsSelectable = status == "available",
                                Capacity = room.Capacity,
                                PricePerNight = roomTypePrices.TryGetValue(room.MaLoaiPhong, out var price) ? price : 0
                            };
                        }).ToList()
                    };
                })
                .ToList()
        };
    }

    public async Task<ReceptionistWalkInPromotionPreviewResult> PreviewWalkInPromotionAsync(
        ReceptionistWalkInPromotionPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var checkInDate = request.CheckInDate == default ? today : request.CheckInDate;
        if (checkInDate < today)
        {
            return FailWalkInPromotionPreview("Ngày nhận phòng không được trước hôm nay.");
        }

        if (request.CheckOutDate <= checkInDate)
        {
            return FailWalkInPromotionPreview("Ngày trả phòng phải sau ngày nhận phòng.");
        }

        var roomIds = request.RoomIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (roomIds.Count == 0)
        {
            return FailWalkInPromotionPreview("Chọn ít nhất một phòng trống.");
        }

        var selectedRooms = await _context.Phongs
            .AsNoTracking()
            .Where(x => roomIds.Contains(x.MaPhong))
            .Select(x => new
            {
                x.MaPhong,
                x.MaLoaiPhong,
                x.SoPhong,
                x.Tang,
                x.TrangThai,
                RoomTypeName = x.MaLoaiPhongNavigation.TenLoaiPhong,
                Capacity = x.MaLoaiPhongNavigation.SoNguoiToiDa
            })
            .ToListAsync(cancellationToken);
        if (selectedRooms.Count != roomIds.Count)
        {
            return FailWalkInPromotionPreview("Có phòng không tồn tại hoặc đã bị xóa.");
        }

        var nights = Math.Max(request.CheckOutDate.DayNumber - checkInDate.DayNumber, 1);
        var roomPrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var roomTypeId in selectedRooms.Select(x => x.MaLoaiPhong).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var price = await GetCurrentRoomTypePriceAsync(roomTypeId, checkInDate, cancellationToken);
            if (price <= 0)
            {
                return FailWalkInPromotionPreview($"Loại phòng {roomTypeId} chưa có giá áp dụng.");
            }

            roomPrices[roomTypeId] = price;
        }

        var roomTotal = selectedRooms.Sum(room => roomPrices[room.MaLoaiPhong] * nights);
        var previewInvoice = new HoaDon
        {
            MaHoaDon = "PREVIEW",
            MaDatPhong = "PREVIEW",
            TongTienPhong = roomTotal,
            TongTienDichVu = 0,
            TienDatCoc = 0,
            TienGiamGiaPhong = 0,
            TongThanhToan = roomTotal,
            SoTienDaThanhToan = 0,
            TrangThai = DomainValues.HoaDonTrangThai.ChuaThanhToan
        };
        var promotion = await _invoicePromotionService.ApplyBestPromotionAsync(previewInvoice, cancellationToken);

        return new ReceptionistWalkInPromotionPreviewResult
        {
            Success = true,
            RoomTotal = roomTotal,
            DiscountAmount = previewInvoice.TienGiamGiaPhong,
            GrandTotal = previewInvoice.TongThanhToan,
            PromotionCode = promotion?.CodeGiamGia.Trim(),
            PromotionName = promotion?.TenMaGiamGia.Trim()
        };
    }

    public async Task<ReceptionistWalkInCheckInResult> WalkInCheckInAsync(
        ReceptionistWalkInCheckInRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var checkInDate = request.CheckInDate == default ? today : request.CheckInDate;
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return FailWalkIn("Nhập họ tên khách trước khi check-in.");
        }

        if (checkInDate < today)
        {
            return FailWalkIn("Ngày nhận phòng không được trước hôm nay.");
        }

        if (request.CheckOutDate <= checkInDate)
        {
            return FailWalkIn("Ngày trả phòng phải sau ngày nhận phòng.");
        }

        if (request.GuestCount <= 0)
        {
            return FailWalkIn("Số khách phải lớn hơn 0.");
        }

        var roomIds = request.RoomIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (roomIds.Count == 0)
        {
            return FailWalkIn("Chọn ít nhất một phòng trống.");
        }

        var employeeId = request.EmployeeId?.Trim();
        if (string.IsNullOrWhiteSpace(employeeId) ||
            !await _context.NhanViens.AnyAsync(x => x.MaNv == employeeId, cancellationToken))
        {
            return FailWalkIn("Không xác định được nhân viên lễ tân.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(ExecuteWalkInTransactionAsync);

        async Task<ReceptionistWalkInCheckInResult> ExecuteWalkInTransactionAsync()
        {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var selectedRooms = await _context.Phongs
            .Include(x => x.MaLoaiPhongNavigation)
            .Where(x => roomIds.Contains(x.MaPhong))
            .ToListAsync(cancellationToken);
        if (selectedRooms.Count != roomIds.Count)
        {
            return FailWalkIn("Có phòng không tồn tại hoặc đã bị xóa.");
        }

        var blockedRoomIds = await GetBlockedRoomIdsForRangeAsync(
            checkInDate,
            request.CheckOutDate,
            cancellationToken);
        foreach (var room in selectedRooms)
        {
            if (blockedRoomIds.Contains(room.MaPhong) ||
                IsMaintenanceStatus(room.TrangThai) ||
                (checkInDate == today && IsBusyStatus(room.TrangThai)))
            {
                return FailWalkIn($"Phòng {room.SoPhong} không còn trống để nhận khách.");
            }
        }

        var selectedRoomTypeIds = selectedRooms
            .Select(x => x.MaLoaiPhong)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var selectableRoomsForReservation = await _context.Phongs
            .AsNoTracking()
            .Where(x => selectedRoomTypeIds.Contains(x.MaLoaiPhong))
            .ToListAsync(cancellationToken);
        var reservationCandidates = selectableRoomsForReservation
            .Where(room => !blockedRoomIds.Contains(room.MaPhong) &&
                           !IsMaintenanceStatus(room.TrangThai) &&
                           (checkInDate > today || !IsBusyStatus(room.TrangThai)))
            .ToList();
        var unassignedDemand = await GetUnassignedDemandByRoomTypeAsync(
            checkInDate,
            request.CheckOutDate,
            excludedBookingCode: null,
            cancellationToken);
        var selectedCountByType = selectedRooms
            .GroupBy(x => x.MaLoaiPhong, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        var candidateCountByType = reservationCandidates
            .GroupBy(x => x.MaLoaiPhong, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        foreach (var selectedType in selectedCountByType)
        {
            candidateCountByType.TryGetValue(selectedType.Key, out var candidateCount);
            unassignedDemand.TryGetValue(selectedType.Key, out var reservedCount);
            var maxSelectable = Math.Max(candidateCount - reservedCount, 0);
            if (selectedType.Value > maxSelectable)
            {
                var typeName = selectedRooms.First(x => x.MaLoaiPhong == selectedType.Key).MaLoaiPhongNavigation.TenLoaiPhong;
                return FailWalkIn($"{typeName} chỉ còn chọn được tối đa {maxSelectable} phòng vì đang giữ chỗ cho đặt trước.");
            }
        }

        var nights = Math.Max(request.CheckOutDate.DayNumber - checkInDate.DayNumber, 1);
        var capacity = selectedRooms.Sum(x => x.MaLoaiPhongNavigation.SoNguoiToiDa);
        if (capacity < request.GuestCount)
        {
            return FailWalkIn($"Các phòng đã chọn chỉ chứa tối đa {capacity} khách.");
        }

        var total = 0m;
        var roomPrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var roomTypeId in selectedRooms.Select(x => x.MaLoaiPhong).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var price = await GetCurrentRoomTypePriceAsync(roomTypeId, checkInDate, cancellationToken);
            if (price <= 0)
            {
                return FailWalkIn($"Loại phòng {roomTypeId} chưa có giá áp dụng.");
            }

            roomPrices[roomTypeId] = price;
        }

        total = selectedRooms.Sum(room => roomPrices[room.MaLoaiPhong] * nights);
        var isVnPayPayment = IsVnPayPayment(request.PaymentMethod);
        var previewInvoice = new HoaDon
        {
            MaHoaDon = "PREVIEW",
            MaDatPhong = "PREVIEW",
            TongTienPhong = total,
            TongTienDichVu = 0,
            TienDatCoc = 0,
            TienGiamGiaPhong = 0,
            TongThanhToan = total,
            SoTienDaThanhToan = 0,
            TrangThai = DomainValues.HoaDonTrangThai.ChuaThanhToan
        };
        await _invoicePromotionService.ApplyBestPromotionAsync(previewInvoice, cancellationToken);
        var payableTotal = previewInvoice.TongThanhToan;
        if (!isVnPayPayment && request.PaymentAmount + 0.01m < payableTotal)
        {
            total = payableTotal;
            return FailWalkIn($"Khách vãng lai cần thanh toán đủ {total:N0}đ trước khi check-in.");
        }

        var customer = await UpsertWalkInCustomerAsync(request, cancellationToken);
        if (customer == null)
        {
            return FailWalkIn("Không thể tạo hoặc cập nhật thông tin khách hàng.");
        }

        var bookingCode = await CodeGenerator.GenerateFromSequenceAsync(_context, "dbo.Seq_DatPhong", "DP", 8);
        var invoiceCode = await CodeGenerator.GenerateFromSequenceAsync(_context, "dbo.Seq_HoaDon", "H", 9);
        if (bookingCode == null || invoiceCode == null)
        {
            return FailWalkIn("Không thể tạo mã đặt phòng hoặc hóa đơn.");
        }

        var isImmediateCheckIn = checkInDate == today;
        var booking = new DatPhong
        {
            MaDatPhong = bookingCode,
            MaKh = customer.MaKh,
            MaNv = employeeId,
            TenKhSnapshot = request.CustomerName.Trim(),
            CccdSnapshot = NormalizeOptional(request.IdentityNumber),
            SdtSnapshot = NormalizeOptional(request.PhoneNumber),
            NgayDat = DateTime.UtcNow,
            NgayNhanPhong = checkInDate,
            NgayTraPhong = request.CheckOutDate,
            TrangThai = isVnPayPayment
                ? DomainValues.DatPhongTrangThai.GiuCho
                : isImmediateCheckIn
                    ? DomainValues.DatPhongTrangThai.DaNhanPhong
                    : DomainValues.DatPhongTrangThai.DaDatCoc,
            GhiChu = string.IsNullOrWhiteSpace(request.Note)
                ? isVnPayPayment
                    ? "Khách vãng lai - chờ thanh toán VNPay"
                    : "Khách vãng lai - thu đủ tiền trước check-in"
                : $"Khách vãng lai - {request.Note.Trim()}"
        };

        var invoice = new HoaDon
        {
            MaHoaDon = invoiceCode,
            MaDatPhong = bookingCode,
            TongTienPhong = total,
            TongTienDichVu = 0,
            TienDatCoc = 0,
            MaGiamGiaPhong = previewInvoice.MaGiamGiaPhong,
            TienGiamGiaPhong = previewInvoice.TienGiamGiaPhong,
            TongThanhToan = payableTotal,
            SoTienDaThanhToan = 0,
            NgayThanhToanCuoi = isVnPayPayment ? null : DateTime.UtcNow,
            PhuongThucThanhToan = isVnPayPayment
                ? DomainValues.PhuongThucThanhToan.Qr
                : DomainValues.PhuongThucThanhToan.TienMat,
            TrangThai = isVnPayPayment
                ? DomainValues.HoaDonTrangThai.ChuaThanhToan
                : DomainValues.HoaDonTrangThai.DaThanhToan,
            GhiChu = isVnPayPayment
                ? $"{WalkInVnPayMarker}; Chờ thanh toán VNPay khi check-in vãng lai"
                : "Thanh toán đủ khi check-in vãng lai"
        };

        await _invoicePromotionService.ApplyBestPromotionAsync(invoice, cancellationToken);
        if (!isVnPayPayment)
        {
            invoice.SoTienDaThanhToan = invoice.TongThanhToan;
        }

        _context.DatPhongs.Add(booking);
        _context.HoaDons.Add(invoice);

        var remainingGuests = request.GuestCount;
        foreach (var room in selectedRooms.OrderBy(x => x.SoPhong))
        {
            var assignedGuests = Math.Min(
                Math.Max(remainingGuests, 1),
                room.MaLoaiPhongNavigation.SoNguoiToiDa);
            remainingGuests = Math.Max(remainingGuests - assignedGuests, 0);
            var lineCode = await CodeGenerator.GenerateFromSequenceAsync(
                _context,
                "dbo.Seq_ChiTietHoaDon",
                "CT",
                8);
            if (lineCode == null)
            {
                return FailWalkIn("Không thể tạo chi tiết hóa đơn.");
            }

            var unitPrice = roomPrices[room.MaLoaiPhong];
            _context.ChiTietHoaDons.Add(new ChiTietHoaDon
            {
                MaCthd = lineCode,
                MaHoaDon = invoiceCode,
                LoaiMuc = DomainValues.ChiTietHoaDonLoaiMuc.Phong,
                MaPhong = room.MaPhong,
                MaLoaiPhong = room.MaLoaiPhong,
                NoiDung = $"Check-in vãng lai phòng {room.SoPhong} - {room.MaLoaiPhongNavigation.TenLoaiPhong}",
                NgayApDung = checkInDate,
                SoNguoi = assignedGuests,
                SoLuong = nights,
                DonGia = unitPrice,
                ThanhTien = unitPrice * nights,
                TrangThai = DomainValues.ChiTietHoaDonTrangThai.HieuLuc
            });

            if (isImmediateCheckIn)
            {
                room.TrangThai = DomainValues.PhongTrangThai.DangSuDung;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ReceptionistWalkInCheckInResult
        {
            Success = true,
            Message = isVnPayPayment
                ? "Đã giữ phòng, chuyển sang VNPay để thanh toán."
                : "Check-in khách vãng lai thành công.",
            BookingCode = bookingCode,
            InvoiceCode = invoiceCode,
            GrandTotal = invoice.TongThanhToan,
            RequiresOnlinePayment = isVnPayPayment,
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
    }

    public async Task CancelWalkInPendingPaymentAsync(
        string bookingCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookingCode))
        {
            return;
        }

        var booking = await _context.DatPhongs
            .Include(x => x.HoaDon)
            .ThenInclude(x => x!.ChiTietHoaDons)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.MaDatPhong == bookingCode.Trim(), cancellationToken);
        if (booking?.HoaDon == null ||
            !IsWalkInVnPayInvoice(booking.HoaDon) ||
            booking.HoaDon.TrangThai == DomainValues.HoaDonTrangThai.DaThanhToan)
        {
            return;
        }

        booking.TrangThai = DomainValues.DatPhongTrangThai.DaHuy;
        booking.HoaDon.TrangThai = DomainValues.HoaDonTrangThai.DaHuy;
        booking.HoaDon.GhiChu = AppendNote(booking.HoaDon.GhiChu, "Hủy vì không tạo được thanh toán VNPay.");

        var roomIds = booking.HoaDon.ChiTietHoaDons
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        !string.IsNullOrWhiteSpace(x.MaPhong))
            .Select(x => x.MaPhong!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (roomIds.Count > 0)
        {
            var rooms = await _context.Phongs
                .Where(x => roomIds.Contains(x.MaPhong))
                .ToListAsync(cancellationToken);
            foreach (var room in rooms)
            {
                room.TrangThai = DomainValues.PhongTrangThai.Trong;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
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
            .Include(x => x.HoaDon)
            .ThenInclude(x => x!.ChiTietHoaDons)
            .ThenInclude(x => x.MaPhongNavigation)
            .AsSplitQuery()
            .AsQueryable();

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            x => x.MaDatPhong == bookingCode.Trim(),
            cancellationToken);
    }

    private async Task<HashSet<string>> GetBlockedRoomIdsForRangeAsync(
        DateOnly checkInDate,
        DateOnly checkOutDate,
        CancellationToken cancellationToken)
    {
        var blockedRoomIds = await _context.ChiTietHoaDons
            .AsNoTracking()
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.MaPhong != null &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.DaHuy &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.TraPhong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.QuaHanNhanPhong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.NgayNhanPhong < checkOutDate &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.NgayTraPhong > checkInDate)
            .Select(x => x.MaPhong!)
            .Distinct()
            .ToListAsync(cancellationToken);

        return blockedRoomIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, int>> GetUnassignedDemandByRoomTypeAsync(
        DateOnly checkInDate,
        DateOnly checkOutDate,
        string? excludedBookingCode,
        CancellationToken cancellationToken)
    {
        var bookings = await _context.DatPhongs
            .AsNoTracking()
            .Where(x => x.TrangThai != DomainValues.DatPhongTrangThai.DaHuy &&
                        x.TrangThai != DomainValues.DatPhongTrangThai.TraPhong &&
                        x.TrangThai != DomainValues.DatPhongTrangThai.QuaHanNhanPhong &&
                        x.NgayNhanPhong < checkOutDate &&
                        x.NgayTraPhong > checkInDate &&
                        (excludedBookingCode == null || x.MaDatPhong != excludedBookingCode))
            .Select(x => new
            {
                x.MaDatPhong,
                x.NgayNhanPhong,
                x.NgayTraPhong,
                RoomLines = x.HoaDon != null
                    ? x.HoaDon.ChiTietHoaDons
                        .Where(ct => ct.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                                     ct.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                        .Select(ct => new
                        {
                            ct.MaPhong,
                            ct.MaLoaiPhong
                        })
                        .ToList()
                    : null
            })
            .ToListAsync(cancellationToken);

        var demand = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var date = checkInDate; date < checkOutDate; date = date.AddDays(1))
        {
            var demandForDate = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var booking in bookings.Where(x => x.NgayNhanPhong <= date && x.NgayTraPhong > date))
            {
                if (booking.RoomLines == null) continue;
                foreach (var line in booking.RoomLines
                             .Where(x => string.IsNullOrWhiteSpace(x.MaPhong) &&
                                         !string.IsNullOrWhiteSpace(x.MaLoaiPhong)))
                {
                    var roomTypeId = line.MaLoaiPhong!;
                    demandForDate.TryGetValue(roomTypeId, out var count);
                    demandForDate[roomTypeId] = count + 1;
                }
            }

            foreach (var item in demandForDate)
            {
                demand.TryGetValue(item.Key, out var currentMax);
                demand[item.Key] = Math.Max(currentMax, item.Value);
            }
        }

        return demand;
    }

    private static HashSet<string> GetReservedRoomIdsForUnassignedDemand(
        IEnumerable<Phong> candidateRooms,
        IReadOnlyDictionary<string, int> demandByRoomType)
    {
        var reservedRoomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidatesByType = candidateRooms
            .GroupBy(x => x.MaLoaiPhong, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(room => room.Tang).ThenBy(room => room.SoPhong).ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var demand in demandByRoomType)
        {
            if (demand.Value <= 0 ||
                !candidatesByType.TryGetValue(demand.Key, out var roomsForType))
            {
                continue;
            }

            foreach (var room in roomsForType.Take(demand.Value))
            {
                reservedRoomIds.Add(room.MaPhong);
            }
        }

        return reservedRoomIds;
    }

    private async Task<decimal> GetCurrentRoomTypePriceAsync(
        string roomTypeId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var weekday = GetVietnamWeekday(date);
        var prices = await _context.BangGiaPhongs
            .AsNoTracking()
            .Where(x => x.MaLoaiPhong == roomTypeId &&
                        x.TrangThai == "Hoạt động" &&
                        x.GiaApDung > 0 &&
                        ((x.LoaiGia == "MACDINH" && x.ThuApDung == null) ||
                         (x.TuNgay <= date &&
                          x.DenNgay >= date &&
                          ((x.LoaiGia == "THEOTHU" && x.ThuApDung == weekday) ||
                           (x.LoaiGia == "NGAYLE" && x.ThuApDung == null)))))
            .OrderByDescending(x => x.UuTien)
            .ThenByDescending(x => x.TuNgay)
            .ToListAsync(cancellationToken);

        return prices.FirstOrDefault()?.GiaApDung ?? 0;
    }

    private async Task<KhachHang?> UpsertWalkInCustomerAsync(
        ReceptionistWalkInCheckInRequest request,
        CancellationToken cancellationToken)
    {
        var identityNumber = NormalizeOptional(request.IdentityNumber);
        var phoneNumber = NormalizeOptional(request.PhoneNumber);
        KhachHang? customer = null;

        if (!string.IsNullOrWhiteSpace(identityNumber))
        {
            customer = await _context.KhachHangs
                .FirstOrDefaultAsync(x => x.Cccd == identityNumber, cancellationToken);
        }

        if (customer == null && !string.IsNullOrWhiteSpace(phoneNumber))
        {
            customer = await _context.KhachHangs
                .FirstOrDefaultAsync(x => x.SoDienThoai == phoneNumber, cancellationToken);
        }

        if (customer == null)
        {
            var customerCode = await CodeGenerator.GenerateFromSequenceAsync(
                _context,
                "dbo.Seq_KhachHang",
                "KH",
                8);
            if (customerCode == null)
            {
                return null;
            }

            customer = new KhachHang
            {
                MaKh = customerCode
            };
            _context.KhachHangs.Add(customer);
        }

        customer.HoTen = request.CustomerName.Trim();
        customer.Cccd = identityNumber;
        customer.SoDienThoai = phoneNumber;
        customer.Email = NormalizeOptional(request.Email);
        customer.GioiTinh = NormalizeOptional(request.Gender);
        customer.QuocTich = NormalizeOptional(request.Nationality);
        customer.DiaChi = NormalizeOptional(request.Address);

        return customer;
    }

    private static byte GetVietnamWeekday(DateOnly date)
    {
        return date.DayOfWeek == DayOfWeek.Sunday
            ? (byte)8
            : (byte)((int)date.DayOfWeek + 1);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsVnPayPayment(string? paymentMethod)
    {
        return string.Equals(paymentMethod?.Trim(), "VNPAY", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWalkInVnPayInvoice(HoaDon invoice)
    {
        return invoice.GhiChu?.Contains(WalkInVnPayMarker, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string AppendNote(string? existing, string note)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return note;
        }

        return $"{existing.Trim()} | {note}";
    }

    private async Task<HashSet<string>> GetBlockedRoomIdsAsync(
        DatPhong booking,
        CancellationToken cancellationToken)
    {
        var blockedRoomIds = await _context.ChiTietHoaDons
            .AsNoTracking()
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.MaPhong != null &&
                        x.MaHoaDonNavigation.MaDatPhong != booking.MaDatPhong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.DaHuy &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.TraPhong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.QuaHanNhanPhong &&
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
        var assignedRooms = GetActiveRoomLines(booking)
            .Where(x => x.MaPhongNavigation != null)
            .OrderBy(x => x.MaPhongNavigation!.SoPhong)
            .Select(x => new ReceptionistAssignedRoomDto
            {
                RoomId = x.MaPhongNavigation!.MaPhong,
                RoomNumber = x.MaPhongNavigation.SoPhong,
                RoomTypeId = x.MaLoaiPhong ?? string.Empty,
                RoomTypeName = x.MaLoaiPhongNavigation?.TenLoaiPhong ?? x.MaLoaiPhong ?? "Phòng"
            })
            .ToList();

        var canCheckIn = true;
        var checkInMessage = "Có thể check-in.";
        if (booking.TrangThai == DomainValues.DatPhongTrangThai.DaHuy)
        {
            canCheckIn = false;
            checkInMessage = "Đặt phòng đã bị hủy.";
        }
        else if (booking.TrangThai == DomainValues.DatPhongTrangThai.QuaHanNhanPhong)
        {
            canCheckIn = false;
            checkInMessage = "Đặt phòng đã quá hạn nhận phòng.";
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
            BookingDate = booking.NgayDat.ToString("dd/MM/yyyy"),
            CheckInDate = booking.NgayNhanPhong.ToString("dd/MM/yyyy"),
            CheckOutDate = booking.NgayTraPhong.ToString("dd/MM/yyyy"),
            Nights = Math.Max(booking.NgayTraPhong.DayNumber - booking.NgayNhanPhong.DayNumber, 1),
            BookingStatus = booking.TrangThai,
            InvoiceStatus = invoice?.TrangThai ?? "NO_INVOICE",
            TotalAmount = invoice?.TongThanhToan ?? 0,
            PaidAmount = invoice?.SoTienDaThanhToan ?? 0,
            CanCheckIn = canCheckIn,
            CheckInMessage = checkInMessage,
            Requirements = requirements,
            AssignedRooms = assignedRooms
        };
    }

    private static IEnumerable<ChiTietHoaDon> GetActiveRoomLines(DatPhong booking)
    {
        return booking.HoaDon?.ChiTietHoaDons
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc) ??
               Enumerable.Empty<ChiTietHoaDon>();
    }

    private static ReceptionistActiveStayDto BuildActiveStayDto(DatPhong booking)
    {
        var invoice = booking.HoaDon!;
        var roomLines = invoice.ChiTietHoaDons
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
            .OrderBy(x => x.MaCthd)
            .Select(BuildRoomChargeLineDto)
            .ToList();

        var serviceLines = invoice.ChiTietHoaDons
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.DichVu &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
            .OrderByDescending(x => x.NgayApDung)
            .ThenByDescending(x => x.MaCthd)
            .Select(x => BuildServiceLineDto(x, x.MaDvNavigation))
            .ToList();

        var rooms = invoice.ChiTietHoaDons
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc &&
                        x.MaPhongNavigation != null)
            .Select(x => x.MaPhongNavigation!.SoPhong)
            .OrderBy(x => x)
            .ToList();

        return new ReceptionistActiveStayDto
        {
            BookingCode = booking.MaDatPhong,
            CustomerName = booking.TenKhSnapshot,
            PhoneNumber = booking.SdtSnapshot ?? booking.MaKhNavigation.SoDienThoai,
            Email = booking.MaKhNavigation.Email,
            CheckInDate = booking.NgayNhanPhong.ToString("dd/MM/yyyy"),
            CheckOutDate = booking.NgayTraPhong.ToString("dd/MM/yyyy"),
            Nights = Math.Max(booking.NgayTraPhong.DayNumber - booking.NgayNhanPhong.DayNumber, 1),
            InvoiceStatus = invoice.TrangThai,
            RoomTotal = invoice.TongTienPhong,
            ServiceTotal = invoice.TongTienDichVu,
            DiscountAmount = invoice.TienGiamGiaPhong,
            PromotionCode = invoice.MaGiamGiaPhongNavigation?.CodeGiamGia.Trim() ?? invoice.MaGiamGiaPhong?.Trim(),
            PromotionName = invoice.MaGiamGiaPhongNavigation?.TenMaGiamGia.Trim(),
            PaidAmount = invoice.SoTienDaThanhToan,
            GrandTotal = invoice.TongThanhToan,
            RemainingAmount = Math.Max(invoice.TongThanhToan - invoice.SoTienDaThanhToan, 0),
            RoomNumbers = rooms,
            RoomLines = roomLines,
            ServiceLines = serviceLines
        };
    }

    private static ReceptionistRoomChargeLineDto BuildRoomChargeLineDto(ChiTietHoaDon line)
    {
        return new ReceptionistRoomChargeLineDto
        {
            LineId = line.MaCthd,
            RoomTypeId = line.MaLoaiPhong ?? string.Empty,
            RoomTypeName = line.MaLoaiPhongNavigation?.TenLoaiPhong ?? line.MaLoaiPhong ?? line.NoiDung,
            RoomNumber = line.MaPhongNavigation?.SoPhong,
            Guests = line.SoNguoi,
            Quantity = line.SoLuong,
            UnitPrice = line.DonGia,
            Total = line.ThanhTien
        };
    }

    private static ReceptionistServiceLineDto BuildServiceLineDto(ChiTietHoaDon line, DichVu? service)
    {
        return new ReceptionistServiceLineDto
        {
            LineId = line.MaCthd,
            ServiceId = line.MaDv ?? string.Empty,
            ServiceName = service?.TenDv ?? line.NoiDung,
            Unit = service?.DonViTinh ?? string.Empty,
            Quantity = line.SoLuong,
            UnitPrice = line.DonGia,
            Total = line.ThanhTien,
            AppliedDate = line.NgayApDung?.ToString("dd/MM/yyyy") ?? string.Empty,
            Note = line.GhiChu
        };
    }

    private static ReceptionistRoomDto BuildRoomMapDto(
        Phong room,
        bool hasActiveAssignment,
        string? bookingCode,
        string? guestName,
        DateOnly? checkOutDate)
    {
        var isMaintenance = IsMaintenanceStatus(room.TrangThai);
        var isBusy = IsBusyStatus(room.TrangThai) || hasActiveAssignment;
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
                "occupied" => "Đang sử dụng",
                _ => "Trống"
            },
            IsSelectable = status == "available",
            Capacity = room.MaLoaiPhongNavigation.SoNguoiToiDa,
            BookingCode = bookingCode,
            CurrentGuestName = guestName,
            CheckOutDate = checkOutDate?.ToString("dd/MM/yyyy")
        };
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
                "occupied" => "Đang sử dụng",
                _ => "Trống"
            },
            IsSelectable = status == "available",
            Capacity = room.MaLoaiPhongNavigation.SoNguoiToiDa
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

    private static ReceptionistWalkInCheckInResult FailWalkIn(string message)
    {
        return new ReceptionistWalkInCheckInResult
        {
            Success = false,
            Message = message
        };
    }

    private static ReceptionistWalkInPromotionPreviewResult FailWalkInPromotionPreview(string message)
    {
        return new ReceptionistWalkInPromotionPreviewResult
        {
            Success = false,
            Message = message
        };
    }

    private static ReceptionistAddServiceResult FailAddService(string message, string bookingCode = "")
    {
        return new ReceptionistAddServiceResult
        {
            Success = false,
            Message = message,
            BookingCode = bookingCode
        };
    }

    private static ReceptionistCheckoutResult FailCheckout(
        string message,
        string bookingCode = "",
        decimal paidAmount = 0,
        decimal grandTotal = 0,
        decimal remainingAmount = 0)
    {
        return new ReceptionistCheckoutResult
        {
            Success = false,
            Message = message,
            BookingCode = bookingCode,
            PaidAmount = paidAmount,
            GrandTotal = grandTotal,
            RemainingAmount = remainingAmount
        };
    }

    private static RoomMaintenanceResult FailRoomMaintenance(string message)
    {
        return new RoomMaintenanceResult
        {
            Success = false,
            Message = message
        };
    }
}
