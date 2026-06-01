using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;
using System.Data.Common;

namespace PBL3.Services;

public class PublicBookingService : IPublicBookingService
{
    private const string OnlineEmployeeId = "NV_ONLINE";

    private readonly ApplicationDbContext _context;
    private readonly IDatPhongService _datPhongService;
    private readonly IKhachHangService _khachHangService;
    private readonly IBangGiaPhongService _bangGiaPhongService;
    private readonly IChiTietHoaDonService _chiTietHoaDonService;
    private readonly IHoaDonService _hoaDonService;
    private readonly IExpiredBookingCleanupService _expiredBookingCleanupService;
    private readonly VnPayOptions _vnPayOptions;

    public PublicBookingService(
        ApplicationDbContext context,
        IDatPhongService datPhongService,
        IKhachHangService khachHangService,
        IBangGiaPhongService bangGiaPhongService,
        IChiTietHoaDonService chiTietHoaDonService,
        IHoaDonService hoaDonService,
        IExpiredBookingCleanupService expiredBookingCleanupService,
        IOptions<VnPayOptions> vnPayOptions)
    {
        _context = context;
        _datPhongService = datPhongService;
        _khachHangService = khachHangService;
        _bangGiaPhongService = bangGiaPhongService;
        _chiTietHoaDonService = chiTietHoaDonService;
        _hoaDonService = hoaDonService;
        _expiredBookingCleanupService = expiredBookingCleanupService;
        _vnPayOptions = vnPayOptions.Value;
    }

    public async Task<RoomSearchViewModel> SearchRoomsAsync(DateTime? checkIn, DateTime? checkOut, int? guests, string? roomTypeId, int? rooms = null)
    {
        var (normalizedCheckIn, normalizedCheckOut) = NormalizeDates(checkIn, checkOut);
        var normalizedGuests = Math.Clamp(guests ?? 2, 1, 80);
        var normalizedRooms = Math.Clamp(rooms ?? 1, 1, 20);
        var selectedRoomType = string.IsNullOrWhiteSpace(roomTypeId) ? null : NormalizeCode(roomTypeId);
        var model = new RoomSearchViewModel
        {
            CheckIn = normalizedCheckIn,
            CheckOut = normalizedCheckOut,
            Guests = normalizedGuests,
            NumberOfRooms = normalizedRooms,
            RoomTypeId = selectedRoomType
        };

        try
        {
            await _expiredBookingCleanupService.CancelExpiredOnlinePaymentsAsync();

            var allRoomTypes = await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(x => x.MaLoaiPhong)
                .ToListAsync();

            model.RoomTypeOptions = allRoomTypes
                .Select(x => new RoomTypeOptionViewModel
                {
                    RoomTypeId = NormalizeCode(x.MaLoaiPhong),
                    RoomName = x.TenLoaiPhong
                })
                .ToList();

            var roomTypes = allRoomTypes;
            if (!string.IsNullOrWhiteSpace(selectedRoomType))
            {
                roomTypes = roomTypes
                    .Where(x => NormalizeCode(x.MaLoaiPhong) == selectedRoomType)
                    .ToList();
            }

            var results = new List<PublicRoomOptionViewModel>();
            var from = DateOnly.FromDateTime(normalizedCheckIn);
            var to = DateOnly.FromDateTime(normalizedCheckOut);

            foreach (var roomType in roomTypes)
            {
                var normalizedRoomTypeId = NormalizeCode(roomType.MaLoaiPhong);
                var price = await _bangGiaPhongService.LayGiaPhongHienTaiAsync(normalizedRoomTypeId);
                if (price <= 0)
                {
                    price = GetFallbackPrice(roomType.TenLoaiPhong);
                }

                var availableRooms = await CountAvailableRoomsAsync(normalizedRoomTypeId, from, to);
                if (availableRooms <= 0)
                {
                    continue;
                }

                results.Add(new PublicRoomOptionViewModel
                {
                    RoomTypeId = normalizedRoomTypeId,
                    RoomName = roomType.TenLoaiPhong,
                    Description = string.IsNullOrWhiteSpace(roomType.MoTa)
                        ? "Phòng chất lượng cao với đầy đủ tiện nghi cơ bản cho kỳ nghỉ thoải mái."
                        : roomType.MoTa,
                    MaxGuests = roomType.SoNguoiToiDa,
                    PricePerNight = price,
                    ImageUrl = await GetRoomImageAsync(normalizedRoomTypeId, roomType.TenLoaiPhong),
                    AvailableRooms = availableRooms
                });
            }

            var recommended = FindCheapestCombo(results, normalizedGuests, normalizedRooms);
            ApplyRecommendedCombo(model, results, recommended, normalizedGuests);
            model.Results = results;
            if (results.Any() && !model.RecommendedRooms.Any())
            {
                model.ErrorMessage = "Các phòng còn trống chưa đủ sức chứa cho số khách trong số phòng bạn chọn.";
            }
            return model;
        }
        catch (Exception ex) when (IsDatabaseException(ex))
        {
            model.ErrorMessage = "Không thể kết nối cơ sở dữ liệu để tải danh sách phòng. Vui lòng thử lại sau.";
            return model;
        }
    }

    public async Task<CheckoutViewModel?> BuildCheckoutAsync(string? roomTypeId, DateTime? checkIn, DateTime? checkOut, int? guests, int? rooms = null, string? roomSelection = null, bool capacityWarningConfirmed = false)
    {
        var normalizedSelection = NormalizeSelection(roomSelection);
        if (string.IsNullOrWhiteSpace(normalizedSelection) && !string.IsNullOrWhiteSpace(roomTypeId))
        {
            normalizedSelection = EncodeSelection(new Dictionary<string, int>
            {
                [NormalizeCode(roomTypeId)] = Math.Clamp(rooms ?? 1, 1, 20)
            });
        }

        if (string.IsNullOrWhiteSpace(normalizedSelection))
        {
            return null;
        }

        var selection = ParseSelection(normalizedSelection);
        var requestedRooms = selection.Values.Sum();
        var search = await SearchRoomsAsync(checkIn, checkOut, guests, null, requestedRooms);
        var roomLines = BuildCheckoutLines(search.Results, selection, search.Guests);
        if (roomLines.Count == 0 || roomLines.Sum(x => x.Rooms) != requestedRooms)
        {
            return null;
        }

        if (!capacityWarningConfirmed && roomLines.Sum(x => x.MaxGuestsPerRoom * x.Rooms) < search.Guests)
        {
            return null;
        }

        var vnPayAvailable = IsVnPayConfigured();
        var firstRoom = roomLines.First();

        return new CheckoutViewModel
        {
            RoomId = firstRoom.RoomTypeId,
            RoomName = roomLines.Count == 1 ? firstRoom.RoomName : "Combo phòng Venus Hotel",
            ImageUrl = firstRoom.ImageUrl,
            PricePerNight = firstRoom.PricePerNight,
            RoomSelection = normalizedSelection,
            RoomLines = roomLines,
            CapacityWarningConfirmed = capacityWarningConfirmed,
            CheckIn = search.CheckIn,
            CheckOut = search.CheckOut,
            Guests = search.Guests,
            NumberOfRooms = requestedRooms,
            PaymentMethod = PaymentMethods.VnPay,
            VnPayAvailable = vnPayAvailable,
            PaymentUnavailableMessage = vnPayAvailable ? null : "VNPay chua duoc cau hinh. Vui long thu lai sau.",
            TotalAmount = roomLines.Sum(x => x.PricePerNight * x.Rooms * Math.Max((search.CheckOut - search.CheckIn).Days, 1))
        };
    }

    private static Dictionary<string, int> FindCheapestCombo(List<PublicRoomOptionViewModel> rooms, int guests, int maxRooms)
    {
        Dictionary<string, int>? best = null;
        decimal bestCost = decimal.MaxValue;
        var bestRoomCount = int.MaxValue;
        var orderedRooms = rooms
            .Where(x => x.AvailableRooms > 0 && x.MaxGuests > 0)
            .OrderBy(x => x.PricePerNight)
            .ThenByDescending(x => x.MaxGuests)
            .ToList();

        void Search(int index, int roomCount, int capacity, decimal cost, Dictionary<string, int> current)
        {
            if (capacity >= guests && roomCount > 0)
            {
                if (cost < bestCost || (cost == bestCost && roomCount < bestRoomCount))
                {
                    bestCost = cost;
                    bestRoomCount = roomCount;
                    best = new Dictionary<string, int>(current);
                }
            }

            if (index >= orderedRooms.Count || roomCount >= maxRooms || cost >= bestCost)
            {
                return;
            }

            var room = orderedRooms[index];
            var maxSelectable = Math.Min(room.AvailableRooms, maxRooms - roomCount);
            for (var count = 0; count <= maxSelectable; count++)
            {
                if (count > 0)
                {
                    current[room.RoomTypeId] = count;
                }
                else
                {
                    current.Remove(room.RoomTypeId);
                }

                Search(
                    index + 1,
                    roomCount + count,
                    capacity + room.MaxGuests * count,
                    cost + room.PricePerNight * count,
                    current);
            }

            current.Remove(room.RoomTypeId);
        }

        Search(0, 0, 0, 0, new Dictionary<string, int>());
        return best ?? new Dictionary<string, int>();
    }

    private static void ApplyRecommendedCombo(
        RoomSearchViewModel model,
        List<PublicRoomOptionViewModel> results,
        Dictionary<string, int> combo,
        int guests)
    {
        if (combo.Count == 0)
        {
            model.RoomSelection = null;
            return;
        }

        model.RoomSelection = EncodeSelection(combo);
        var remainingGuests = guests;
        foreach (var result in results)
        {
            if (!combo.TryGetValue(result.RoomTypeId, out var selectedRooms))
            {
                continue;
            }

            result.RecommendedRooms = selectedRooms;
            var assignedGuests = Math.Min(remainingGuests, result.MaxGuests * selectedRooms);
            remainingGuests -= assignedGuests;
            model.RecommendedRooms.Add(new BookingRoomSelectionViewModel
            {
                RoomTypeId = result.RoomTypeId,
                RoomName = result.RoomName,
                ImageUrl = result.ImageUrl,
                Rooms = selectedRooms,
                Guests = assignedGuests,
                MaxGuestsPerRoom = result.MaxGuests,
                AvailableRooms = result.AvailableRooms,
                PricePerNight = result.PricePerNight
            });
        }
    }

    private static List<CheckoutRoomLineViewModel> BuildCheckoutLines(
        List<PublicRoomOptionViewModel> results,
        Dictionary<string, int> selection,
        int guests)
    {
        var lines = new List<CheckoutRoomLineViewModel>();
        var remainingGuests = guests;
        foreach (var item in selection)
        {
            var room = results.FirstOrDefault(x => x.RoomTypeId == item.Key);
            if (room == null || item.Value <= 0 || room.AvailableRooms < item.Value)
            {
                continue;
            }

            var assignedGuests = Math.Min(remainingGuests, room.MaxGuests * item.Value);
            remainingGuests -= assignedGuests;
            lines.Add(new CheckoutRoomLineViewModel
            {
                RoomTypeId = room.RoomTypeId,
                RoomName = room.RoomName,
                ImageUrl = room.ImageUrl,
                Rooms = item.Value,
                Guests = assignedGuests,
                MaxGuestsPerRoom = room.MaxGuests,
                PricePerNight = room.PricePerNight
            });
        }

        return lines;
    }

    public async Task<PublicBookingResult> ConfirmBookingAsync(CheckoutViewModel model)
    {
        await _expiredBookingCleanupService.CancelExpiredOnlinePaymentsAsync();
        model.PaymentMethod = PaymentMethods.VnPay;
        if (!IsVnPayConfigured())
        {
            return Fail("VNPay chua duoc cau hinh. Vui long thu lai sau.");
        }

        var (checkIn, checkOut) = NormalizeDates(model.CheckIn, model.CheckOut);
        var checkInDate = DateOnly.FromDateTime(checkIn);
        var checkOutDate = DateOnly.FromDateTime(checkOut);
        var guests = Math.Clamp(model.Guests <= 0 ? 2 : model.Guests, 1, 80);
        var normalizedSelection = NormalizeSelection(model.RoomSelection);
        if (string.IsNullOrWhiteSpace(normalizedSelection) && !string.IsNullOrWhiteSpace(model.RoomId))
        {
            normalizedSelection = EncodeSelection(new Dictionary<string, int>
            {
                [NormalizeCode(model.RoomId)] = Math.Clamp(model.NumberOfRooms <= 0 ? 1 : model.NumberOfRooms, 1, 20)
            });
        }

        var selection = ParseSelection(normalizedSelection);
        var requestedRooms = selection.Values.Sum();
        var roomLines = new List<CheckoutRoomLineViewModel>();
        var roomTypeId = selection.Keys.FirstOrDefault() ?? string.Empty;
        if (selection.Count == 0 || requestedRooms <= 0)
        {
            return Fail("Vui lòng chọn loại phòng trước khi xác nhận đặt phòng.");
        }

        var roomType = await _context.LoaiPhongs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaLoaiPhong == roomTypeId);
        if (roomType == null)
        {
            return Fail("Loại phòng không tồn tại. Vui lòng chọn lại phòng.");
        }

        if (false && guests > roomType.SoNguoiToiDa * requestedRooms)
        {
            return Fail("Số khách vượt quá sức chứa của loại phòng đã chọn.");
        }

        var price = await _bangGiaPhongService.LayGiaPhongHienTaiAsync(roomTypeId);
        if (price <= 0)
        {
            price = GetFallbackPrice(roomType.TenLoaiPhong);
        }

        var availableRooms = await CountAvailableRoomsAsync(roomTypeId, checkInDate, checkOutDate);
        if (false && availableRooms < requestedRooms)
        {
            return Fail("Loại phòng này vừa hết chỗ trong khoảng ngày bạn chọn. Vui lòng chọn ngày hoặc phòng khác.");
        }

        var freshSearch = await SearchRoomsAsync(checkIn, checkOut, guests, null, requestedRooms);
        roomLines = BuildCheckoutLines(freshSearch.Results, selection, guests);
        if (roomLines.Count == 0 || roomLines.Sum(x => x.Rooms) != requestedRooms)
        {
            return Fail("Một số loại phòng bạn chọn không còn đủ số lượng. Vui lòng chọn lại.");
        }

        if (!model.CapacityWarningConfirmed && roomLines.Sum(x => x.MaxGuestsPerRoom * x.Rooms) < guests)
        {
            return Fail("Combo phòng bạn chọn chưa đủ sức chứa cho số khách.");
        }

        var nights = Math.Max(checkOutDate.DayNumber - checkInDate.DayNumber, 1);
        var roomTotal = roomLines.Sum(x => x.PricePerNight * x.Rooms * nights);

        if (model.PaymentMethod == PaymentMethods.VnPay)
        {
            var pendingBookingCode = await FindReusablePendingOnlineBookingAsync(
                model,
                checkInDate,
                checkOutDate,
                roomLines,
                roomTotal);

            if (!string.IsNullOrWhiteSpace(pendingBookingCode))
            {
                return new PublicBookingResult { Success = true, BookingCode = pendingBookingCode };
            }
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            if (!await EnsureOnlineEmployeeAsync())
            {
                return Fail("Không thể chuẩn bị nhân viên xử lý đặt phòng online. Vui lòng thử lại.");
            }

            var customer = await UpsertCustomerAsync(model);
            if (customer == null)
            {
                return Fail("Không thể tạo hoặc cập nhật thông tin khách hàng. Vui lòng thử lại.");
            }

            var bookingCode = await CodeGenerator.GenerateFromSequenceAsync(_context, "dbo.Seq_DatPhong", "DP", 8);
            if (bookingCode == null)
            {
                return Fail("Không thể tạo mã đặt phòng. Vui lòng thử lại.");
            }

            var booking = new DatPhong
            {
                MaDatPhong = bookingCode,
                MaKh = customer.MaKh,
                MaNv = OnlineEmployeeId,
                TenKhSnapshot = model.CustomerName.Trim(),
                CccdSnapshot = model.Cccd.Trim(),
                SdtSnapshot = model.PhoneNumber.Trim(),
                NgayDat = DateTime.UtcNow,
                NgayNhanPhong = checkInDate,
                NgayTraPhong = checkOutDate,
                TrangThai = DomainValues.DatPhongTrangThai.GiuCho,
                GhiChu = model.Note
            };

            if (!await _datPhongService.CreateAsync(booking))
            {
                return Fail("Lỗi hệ thống khi tạo phiếu đặt phòng. Vui lòng thử lại.");
            }

            var invoice = await _context.HoaDons.FirstOrDefaultAsync(x => x.MaDatPhong == bookingCode);
            if (invoice == null)
            {
                return Fail("Không tìm thấy hóa đơn vừa tạo. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.");
            }

            invoice.TongTienPhong = roomTotal;
            invoice.TongTienDichVu = 0;
            invoice.TienDatCoc = 0;
            invoice.TienGiamGiaPhong = 0;
            invoice.TongThanhToan = roomTotal;
            invoice.SoTienDaThanhToan = 0;
            invoice.PhuongThucThanhToan = DomainValues.PhuongThucThanhToan.Qr;
            invoice.TrangThai = DomainValues.HoaDonTrangThai.ChuaThanhToan;

            if (!await _hoaDonService.UpdateAsync(invoice))
            {
                return Fail("Không thể cập nhật hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.");
            }

            foreach (var line in roomLines)
            {
                var remainingLineGuests = line.Guests;
                for (var roomIndex = 1; roomIndex <= line.Rooms; roomIndex++)
                {
                    var lineDetailCode = await CodeGenerator.GenerateFromSequenceAsync(_context, "dbo.Seq_ChiTietHoaDon", "CT", 8);
                    if (lineDetailCode == null)
                    {
                        return Fail("Không thể tạo mã chi tiết hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.");
                    }

                    var assignedGuests = Math.Min(remainingLineGuests, line.MaxGuestsPerRoom);
                    remainingLineGuests -= assignedGuests;
                    var lineDetail = new ChiTietHoaDon
                    {
                        MaCthd = lineDetailCode,
                        MaHoaDon = invoice.MaHoaDon,
                        LoaiMuc = DomainValues.ChiTietHoaDonLoaiMuc.Phong,
                        MaLoaiPhong = line.RoomTypeId,
                        NoiDung = $"Thuê phòng loại {line.RoomTypeId} - {line.RoomName}",
                        NgayApDung = checkInDate,
                        SoNguoi = Math.Max(assignedGuests, 1),
                        SoLuong = nights,
                        DonGia = line.PricePerNight,
                        ThanhTien = line.PricePerNight * nights,
                        TrangThai = DomainValues.ChiTietHoaDonTrangThai.HieuLuc
                    };

                    if (!await _chiTietHoaDonService.CreateAsync(lineDetail))
                    {
                        return Fail("Không thể tạo chi tiết hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.");
                    }
                }
            }

            if (DateTime.Now.Ticks < 0)
            {
            var detailCode = await CodeGenerator.GenerateFromSequenceAsync(_context, "dbo.Seq_ChiTietHoaDon", "CT", 8);
            if (detailCode == null)
            {
                return Fail("Không thể tạo mã chi tiết hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.");
            }

            var detail = new ChiTietHoaDon
            {
                MaCthd = detailCode,
                MaHoaDon = invoice.MaHoaDon,
                LoaiMuc = DomainValues.ChiTietHoaDonLoaiMuc.Phong,
                NoiDung = requestedRooms == 1
                    ? $"Thuê phòng loại {roomTypeId} - {roomType.TenLoaiPhong}"
                    : $"Thuê phòng loại {roomTypeId} - {roomType.TenLoaiPhong} | SoPhong={requestedRooms}",
                NgayApDung = checkInDate,
                SoNguoi = guests,
                SoLuong = nights,
                DonGia = price,
                ThanhTien = roomTotal,
                TrangThai = DomainValues.ChiTietHoaDonTrangThai.HieuLuc
            };

            if (!await _chiTietHoaDonService.CreateAsync(detail))
            {
                return Fail("Không thể tạo chi tiết hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.");
            }

            }

            await transaction.CommitAsync();
            return new PublicBookingResult { Success = true, BookingCode = bookingCode };
        });
    }

    public async Task<BookingLookupResultViewModel?> LookupAsync(string bookingCode, string phoneNumber)
    {
        var normalizedCode = bookingCode.Trim();
        var normalizedPhone = phoneNumber.Trim();

        var booking = await _context.DatPhongs
            .AsNoTracking()
            .Include(x => x.HoaDon)
            .ThenInclude(x => x!.ChiTietHoaDons)
            .FirstOrDefaultAsync(x => x.MaDatPhong == normalizedCode && x.SdtSnapshot == normalizedPhone);

        if (booking == null)
        {
            return null;
        }

        var roomDetail = booking.HoaDon?.ChiTietHoaDons
            .FirstOrDefault(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong);

        return new BookingLookupResultViewModel
        {
            BookingCode = booking.MaDatPhong,
            CustomerName = booking.TenKhSnapshot,
            PhoneNumber = booking.SdtSnapshot ?? "",
            CheckIn = booking.NgayNhanPhong,
            CheckOut = booking.NgayTraPhong,
            Status = booking.TrangThai,
            RoomSummary = roomDetail?.NoiDung ?? "Thông tin phòng đang được cập nhật",
            TotalAmount = booking.HoaDon?.TongThanhToan ?? 0
        };
    }

    private async Task<string?> FindReusablePendingOnlineBookingAsync(
        CheckoutViewModel model,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        List<CheckoutRoomLineViewModel> roomLines,
        decimal roomTotal)
    {
        var cccd = model.Cccd.Trim();
        var phoneNumber = model.PhoneNumber.Trim();
        var cutoff = DateTime.UtcNow.AddMinutes(-Math.Clamp(_vnPayOptions.ExpireMinutes, 1, 1440));
        var expectedSelection = roomLines
            .GroupBy(x => NormalizeCode(x.RoomTypeId), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Sum(line => line.Rooms), StringComparer.OrdinalIgnoreCase);

        var candidates = await _context.DatPhongs
            .AsNoTracking()
            .Include(x => x.HoaDon)
            .ThenInclude(x => x!.ChiTietHoaDons)
            .Where(x => x.TrangThai == DomainValues.DatPhongTrangThai.GiuCho &&
                        x.NgayDat >= cutoff &&
                        x.NgayNhanPhong == checkInDate &&
                        x.NgayTraPhong == checkOutDate &&
                        x.CccdSnapshot == cccd &&
                        x.SdtSnapshot == phoneNumber &&
                        x.HoaDon != null &&
                        x.HoaDon.TrangThai == DomainValues.HoaDonTrangThai.ChuaThanhToan &&
                        x.HoaDon.PhuongThucThanhToan == DomainValues.PhuongThucThanhToan.Qr &&
                        x.HoaDon.TongThanhToan == roomTotal)
            .OrderByDescending(x => x.NgayDat)
            .ToListAsync();

        foreach (var candidate in candidates)
        {
            var candidateSelection = candidate.HoaDon!.ChiTietHoaDons
                .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                            x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc &&
                            !string.IsNullOrWhiteSpace(x.MaLoaiPhong))
                .GroupBy(x => NormalizeCode(x.MaLoaiPhong), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

            if (SelectionsMatch(expectedSelection, candidateSelection))
            {
                return candidate.MaDatPhong.Trim();
            }
        }

        return null;
    }

    private async Task<KhachHang?> UpsertCustomerAsync(CheckoutViewModel model)
    {
        var cccd = model.Cccd.Trim();
        var customer = await _khachHangService.GetByCccdAsync(cccd);

        if (customer == null)
        {
            var customerCode = await CodeGenerator.GenerateFromSequenceAsync(_context, "dbo.Seq_KhachHang", "KH", 8);
            if (customerCode == null)
            {
                return null;
            }

            customer = new KhachHang
            {
                MaKh = customerCode,
                Cccd = cccd,
                HoTen = model.CustomerName.Trim(),
                SoDienThoai = model.PhoneNumber.Trim(),
                Email = model.Email.Trim()
            };

            return await _khachHangService.CreateAsync(customer) ? customer : null;
        }

        customer.HoTen = model.CustomerName.Trim();
        customer.SoDienThoai = model.PhoneNumber.Trim();
        customer.Email = model.Email.Trim();
        return await _khachHangService.UpdateAsync(customer) ? customer : null;
    }

    private async Task<int> CountAvailableRoomsAsync(string roomTypeId, DateOnly checkIn, DateOnly checkOut)
    {
        var normalizedRoomTypeId = NormalizeCode(roomTypeId);
        var totalRooms = await _context.Phongs
            .AsNoTracking()
            .Where(x => x.MaLoaiPhong == normalizedRoomTypeId &&
                        x.TrangThai != "Bảo trì" &&
                        x.TrangThai != "Ngưng sử dụng")
            .CountAsync();

        if (totalRooms <= 0)
        {
            return 0;
        }

        var overlappingBookings = await _context.ChiTietHoaDons
            .AsNoTracking()
            .Include(x => x.MaPhongNavigation)
            .Include(x => x.MaHoaDonNavigation)
            .ThenInclude(x => x.MaDatPhongNavigation)
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.DaHuy &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.TraPhong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.QuaHanNhanPhong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.NgayNhanPhong < checkOut &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.NgayTraPhong > checkIn &&
                        ((x.MaLoaiPhong == normalizedRoomTypeId) ||
                         (x.MaPhong != null && x.MaPhongNavigation!.MaLoaiPhong == normalizedRoomTypeId) ||
                         (x.MaPhong == null && x.NoiDung.Contains(normalizedRoomTypeId))))
            .Select(x => x.NoiDung)
            .ToListAsync();

        return Math.Max(totalRooms - overlappingBookings.Sum(GetReservedRoomCount), 0);
    }

    private static int GetReservedRoomCount(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 1;
        }

        const string marker = "SoPhong=";
        var markerIndex = content.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return 1;
        }

        var start = markerIndex + marker.Length;
        var end = start;
        while (end < content.Length && char.IsDigit(content[end]))
        {
            end++;
        }

        return int.TryParse(content[start..end], out var rooms) && rooms > 0 ? rooms : 1;
    }

    private async Task<bool> EnsureOnlineEmployeeAsync()
    {
        if (await _context.NhanViens.AnyAsync(nv => nv.MaNv == OnlineEmployeeId))
        {
            return true;
        }

        _context.NhanViens.Add(new NhanVien
        {
            MaNv = OnlineEmployeeId,
            HoTen = "Đặt phòng online",
            SoDienThoai = "0000000000",
            Email = "online@pbl3.local",
            ChucVu = "Hệ thống",
            TrangThai = DomainValues.NhanVienTrangThai.DangLam
        });

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            return await _context.NhanViens.AnyAsync(nv => nv.MaNv == OnlineEmployeeId);
        }
    }

    private static (DateTime CheckIn, DateTime CheckOut) NormalizeDates(DateTime? checkIn, DateTime? checkOut)
    {
        var normalizedCheckIn = (checkIn ?? DateTime.Today).Date;
        var normalizedCheckOut = (checkOut ?? normalizedCheckIn.AddDays(1)).Date;
        if (normalizedCheckOut <= normalizedCheckIn)
        {
            normalizedCheckOut = normalizedCheckIn.AddDays(1);
        }

        return (normalizedCheckIn, normalizedCheckOut);
    }

    private static PublicBookingResult Fail(string message)
    {
        return new PublicBookingResult { Success = false, ErrorMessage = message };
    }

    private static bool IsDatabaseException(Exception ex)
    {
        return ex is DbException ||
               ex is TimeoutException ||
               ex is InvalidOperationException { InnerException: DbException } ||
               ex.InnerException is DbException;
    }

    private bool IsVnPayConfigured()
    {
        return _vnPayOptions.Enabled &&
               !string.IsNullOrWhiteSpace(_vnPayOptions.PaymentUrl) &&
               !string.IsNullOrWhiteSpace(_vnPayOptions.TmnCode) &&
               !string.IsNullOrWhiteSpace(_vnPayOptions.HashSecret);
    }

    private static string GetRoomImage(string roomName)
    {
        return "/images/booking_hero.jpg";
    }

    private async Task<string> GetRoomImageAsync(string roomTypeId, string roomName)
    {
        var fallback = GetRoomImage(roomName);

        try
        {
            var linkedImage = await _context.LinkAnhs
                .AsNoTracking()
                .Where(x => x.DoiTuong == roomTypeId &&
                            x.TrangThai == DomainValues.LinkAnhTrangThai.HoatDong &&
                            x.UrlAnh != "")
                .OrderByDescending(x => x.LaAnhDaiDien)
                .ThenBy(x => x.ThuTu)
                .Select(x => x.UrlAnh)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(linkedImage) ? fallback : linkedImage;
        }
        catch (Exception ex) when (IsDatabaseException(ex))
        {
            return fallback;
        }
    }

    private static string NormalizeCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeSelection(string? value)
    {
        var selection = ParseSelection(value);
        return selection.Count == 0 ? null : EncodeSelection(selection);
    }

    private static Dictionary<string, int> ParseSelection(string? value)
    {
        var selection = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value))
        {
            return selection;
        }

        foreach (var item in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = item.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            var roomTypeId = NormalizeCode(parts[0]);
            if (string.IsNullOrWhiteSpace(roomTypeId) || !int.TryParse(parts[1], out var rooms))
            {
                continue;
            }

            rooms = Math.Clamp(rooms, 1, 20);
            selection[roomTypeId] = selection.TryGetValue(roomTypeId, out var existing)
                ? Math.Clamp(existing + rooms, 1, 20)
                : rooms;
        }

        return selection;
    }

    private static string EncodeSelection(Dictionary<string, int> selection)
    {
        return string.Join(",", selection
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0)
            .OrderBy(x => x.Key)
            .Select(x => $"{NormalizeCode(x.Key)}:{Math.Clamp(x.Value, 1, 20)}"));
    }

    private static bool SelectionsMatch(
        Dictionary<string, int> expected,
        Dictionary<string, int> actual)
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }

        foreach (var item in expected)
        {
            if (!actual.TryGetValue(item.Key, out var value) || value != item.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static decimal GetFallbackPrice(string roomName)
    {
        if (roomName.Contains("President", StringComparison.OrdinalIgnoreCase))
        {
            return 2_500_000m;
        }

        if (roomName.Contains("Suite", StringComparison.OrdinalIgnoreCase))
        {
            return 1_200_000m;
        }

        if (roomName.Contains("Deluxe", StringComparison.OrdinalIgnoreCase))
        {
            return 800_000m;
        }

        return 500_000m;
    }
}
