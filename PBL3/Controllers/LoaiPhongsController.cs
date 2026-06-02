using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;

namespace PBL3.Controllers
{
    public class LoaiPhongsController : Controller
    {
        private static readonly HashSet<string> AllowedRoomImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public LoaiPhongsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: LoaiPhongs
        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayWeekday = GetVietnamWeekday(DateTime.Today);
            var roomTypes = await _context.LoaiPhongs
                .AsNoTracking()
                .Include(x => x.Phongs)
                .OrderBy(x => x.MaLoaiPhong)
                .ToListAsync();

            var allPriceRows = await _context.BangGiaPhongs
                .AsNoTracking()
                .Where(x => x.TrangThai == "Hoạt động" &&
                            x.GiaApDung > 0)
                .ToListAsync();
            var holidayPriceRows = await _context.BangGiaPhongs
                .AsNoTracking()
                .Where(x => x.LoaiGia == "NGAYLE" &&
                            x.ThuApDung == null &&
                            x.GiaApDung > 0)
                .OrderByDescending(x => x.TuNgay)
                .ThenByDescending(x => x.DenNgay)
                .ToListAsync();

            var priceRows = allPriceRows
                .Where(x => (x.LoaiGia == "MACDINH" && x.ThuApDung == null) ||
                            (x.TuNgay <= today &&
                             x.DenNgay >= today &&
                             ((x.LoaiGia == "THEOTHU" && x.ThuApDung == todayWeekday) ||
                              (x.LoaiGia == "NGAYLE" && x.ThuApDung == null))))
                .OrderByDescending(x => x.UuTien)
                .ThenByDescending(x => x.TuNgay)
                .ToList();
            var prices = priceRows
                .GroupBy(x => x.MaLoaiPhong, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    StringComparer.OrdinalIgnoreCase);
            var defaultPrices = allPriceRows
                .Where(x => x.LoaiGia == "MACDINH" && x.ThuApDung == null)
                .GroupBy(x => x.MaLoaiPhong, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => SelectDefaultPrice(x, today),
                    StringComparer.OrdinalIgnoreCase);
            var weekdayPrices = allPriceRows
                .Where(x => x.LoaiGia == "THEOTHU" &&
                            x.ThuApDung is >= 2 and <= 8 &&
                            x.TuNgay <= today &&
                            x.DenNgay >= today)
                .GroupBy(x => new { x.MaLoaiPhong, x.ThuApDung })
                .ToDictionary(
                    x => (x.Key.MaLoaiPhong, x.Key.ThuApDung!.Value),
                    x => x.OrderByDescending(price => price.TuNgay).First());
            var holidayPrices = holidayPriceRows
                .Where(x => x.TrangThai == "Hoạt động" &&
                            x.DenNgay >= today)
                .OrderBy(x => x.TuNgay)
                .ThenBy(x => x.DenNgay)
                .GroupBy(x => x.MaLoaiPhong, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList(),
                    StringComparer.OrdinalIgnoreCase);

            var imageRows = await _context.LinkAnhs
                .AsNoTracking()
                .Where(x => x.TrangThai == DomainValues.LinkAnhTrangThai.HoatDong &&
                            x.UrlAnh != "")
                .OrderByDescending(x => x.LaAnhDaiDien)
                .ThenBy(x => x.ThuTu)
                .ToListAsync();
            var images = imageRows
                .GroupBy(x => x.DoiTuong, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.First().UrlAnh,
                    StringComparer.OrdinalIgnoreCase);

            var viewModel = new AdminRoomTypeIndexViewModel
            {
                RoomTypes = roomTypes
                    .Select(roomType =>
                    {
                        prices.TryGetValue(roomType.MaLoaiPhong, out var price);
                        defaultPrices.TryGetValue(roomType.MaLoaiPhong, out var defaultPrice);
                        holidayPrices.TryGetValue(roomType.MaLoaiPhong, out var roomHolidayPrices);
                        var roomHolidayHistory = holidayPriceRows
                            .Where(x => string.Equals(x.MaLoaiPhong, roomType.MaLoaiPhong, StringComparison.OrdinalIgnoreCase) &&
                                        !(x.TrangThai == "Hoạt động" && x.DenNgay >= today))
                            .ToList();
                        images.TryGetValue(roomType.MaLoaiPhong, out var imageUrl);
                        var rooms = roomType.Phongs
                            .OrderBy(x => x.Tang)
                            .ThenBy(x => x.SoPhong)
                            .ToList();

                        return new AdminRoomTypeCardViewModel
                        {
                            RoomTypeId = roomType.MaLoaiPhong,
                            RoomTypeName = roomType.TenLoaiPhong,
                            MaxGuests = roomType.SoNguoiToiDa,
                            Description = string.IsNullOrWhiteSpace(roomType.MoTa)
                                ? "Chưa có mô tả cho loại phòng này."
                                : roomType.MoTa,
                            ImageUrl = string.IsNullOrWhiteSpace(imageUrl)
                                ? "/images/booking_hero.jpg"
                                : imageUrl,
                            CurrentPrice = price?.GiaApDung > 0 ? price.GiaApDung : null,
                            CurrentPriceSource = GetPriceSourceLabel(price),
                            PriceOverview = BuildPriceOverview(
                                roomType.MaLoaiPhong,
                                defaultPrice?.GiaApDung,
                                weekdayPrices,
                                roomHolidayPrices ?? new List<BangGiaPhong>(),
                                roomHolidayHistory,
                                today),
                            TotalRooms = rooms.Count,
                            AvailableRooms = rooms.Count(x => IsAvailableStatus(x.TrangThai)),
                            OccupiedRooms = rooms.Count(x => IsOccupiedStatus(x.TrangThai)),
                            MaintenanceRooms = rooms.Count(x => IsMaintenanceStatus(x.TrangThai)),
                            Rooms = rooms
                                .Select(room => new AdminRoomSummaryViewModel
                                {
                                    RoomId = room.MaPhong,
                                    RoomNumber = room.SoPhong,
                                    Floor = room.Tang,
                                    Status = room.TrangThai
                                })
                                .ToList()
                        };
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        // GET: LoaiPhongs/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _context.LoaiPhongs
                .FirstOrDefaultAsync(m => m.MaLoaiPhong == id);
            if (loaiPhong == null)
            {
                return NotFound();
            }

            return View(loaiPhong);
        }

        // GET: LoaiPhongs/Create
        public IActionResult Create()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePriceRules([FromBody] AdminRoomTypePriceUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoomTypeId) || request.DefaultPrice <= 0)
            {
                return BadRequest(new { message = "Dữ liệu giá không hợp lệ." });
            }

            var roomTypeId = request.RoomTypeId.Trim();
            var roomTypeExists = await _context.LoaiPhongs.AnyAsync(x => x.MaLoaiPhong == roomTypeId);
            if (!roomTypeExists)
            {
                return NotFound(new { message = "Không tìm thấy loại phòng." });
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var defaultPrice = await _context.BangGiaPhongs
                .Where(x => x.MaLoaiPhong == roomTypeId &&
                            x.LoaiGia == "MACDINH" &&
                            x.ThuApDung == null &&
                            x.TrangThai == "Hoạt động")
                .OrderByDescending(x => x.TuNgay <= today && x.DenNgay >= today)
                .ThenByDescending(x => x.TuNgay)
                .FirstOrDefaultAsync();
            var previousDefaultPrice = defaultPrice?.GiaApDung;

            if (defaultPrice == null)
            {
                defaultPrice = new BangGiaPhong
                {
                    MaBangGia = await GenerateBangGiaIdAsync(),
                    MaLoaiPhong = roomTypeId,
                    LoaiGia = "MACDINH",
                    UuTien = 1,
                    ThuApDung = null,
                    TrangThai = "Hoạt động"
                };
                _context.BangGiaPhongs.Add(defaultPrice);
            }

            defaultPrice.TuNgay = new DateOnly(1900, 1, 1);
            defaultPrice.DenNgay = new DateOnly(9999, 12, 31);
            defaultPrice.GiaApDung = RoundPrice(request.DefaultPrice);
            defaultPrice.GhiChu = "Giá mặc định áp dụng hằng ngày";

            var weekdayRequests = request.WeekdayMultipliers
                .Where(x => x.DayValue is >= 2 and <= 8)
                .GroupBy(x => x.DayValue)
                .ToDictionary(x => x.Key, x => x.Last().Multiplier);
            var currentWeekdayPrices = await _context.BangGiaPhongs
                .Where(x => x.MaLoaiPhong == roomTypeId &&
                            x.LoaiGia == "THEOTHU" &&
                            x.ThuApDung != null)
                .ToListAsync();

            foreach (var day in Enumerable.Range(2, 7).Select(x => (byte)x))
            {
                var multiplier = weekdayRequests.TryGetValue(day, out var requestedMultiplier)
                    ? requestedMultiplier
                    : 1;
                if (multiplier <= 0)
                {
                    return BadRequest(new { message = "Hệ số theo thứ phải lớn hơn 0." });
                }

                var weekdayPrice = currentWeekdayPrices
                    .Where(x => x.ThuApDung == day)
                    .OrderByDescending(x => x.TrangThai == "Hoạt động")
                    .ThenByDescending(x => x.TuNgay)
                    .FirstOrDefault();

                if (Math.Abs(multiplier - 1) < 0.0001m)
                {
                    foreach (var row in currentWeekdayPrices.Where(x => x.ThuApDung == day))
                    {
                        row.TrangThai = "Ngừng áp dụng";
                    }

                    continue;
                }

                if (weekdayPrice == null)
                {
                    weekdayPrice = new BangGiaPhong
                    {
                        MaBangGia = await GenerateBangGiaIdAsync(),
                        MaLoaiPhong = roomTypeId,
                        LoaiGia = "THEOTHU",
                        UuTien = 2,
                        ThuApDung = day
                    };
                    _context.BangGiaPhongs.Add(weekdayPrice);
                }

                weekdayPrice.TuNgay = new DateOnly(1900, 1, 1);
                weekdayPrice.DenNgay = new DateOnly(9999, 12, 31);
                weekdayPrice.GiaApDung = RoundPrice(request.DefaultPrice * multiplier);
                weekdayPrice.TrangThai = "Hoạt động";
                weekdayPrice.GhiChu = $"Hệ số {multiplier:0.####} so với giá mặc định";
            }

            var futureHolidayPrices = await _context.BangGiaPhongs
                .Where(x => x.MaLoaiPhong == roomTypeId &&
                            x.LoaiGia == "NGAYLE" &&
                            x.ThuApDung == null &&
                            x.TrangThai == "Hoạt động" &&
                            x.DenNgay >= today)
                .ToListAsync();
            foreach (var holidayPrice in futureHolidayPrices)
            {
                var multiplier = ResolveStoredMultiplier(holidayPrice, previousDefaultPrice);
                holidayPrice.GiaApDung = RoundPrice(request.DefaultPrice * multiplier);
                if (string.IsNullOrWhiteSpace(holidayPrice.GhiChu))
                {
                    holidayPrice.GhiChu = $"Hệ số {multiplier:0.####} so với giá mặc định";
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật bảng giá." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHolidayPrice([FromBody] AdminRoomTypeHolidayPriceCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoomTypeId) ||
                request.Multiplier <= 0 ||
                request.DaysBefore < 0 ||
                request.DaysAfter < 0)
            {
                return BadRequest(new { message = "Dữ liệu giá ngày lễ không hợp lệ." });
            }

            var roomTypeId = request.RoomTypeId.Trim();
            var defaultPrice = await GetDefaultPriceValueAsync(roomTypeId);
            if (!defaultPrice.HasValue)
            {
                return BadRequest(new { message = "Cần thiết lập giá mặc định trước khi thêm giá ngày lễ." });
            }

            var holidayPrice = new BangGiaPhong
            {
                MaBangGia = await GenerateBangGiaIdAsync(),
                MaLoaiPhong = roomTypeId,
                TuNgay = request.HolidayDate.AddDays(-request.DaysBefore),
                DenNgay = request.HolidayDate.AddDays(request.DaysAfter),
                ThuApDung = null,
                GiaApDung = RoundPrice(defaultPrice.Value * request.Multiplier),
                LoaiGia = "NGAYLE",
                UuTien = 3,
                TrangThai = "Hoạt động",
                GhiChu = $"Ngày lễ {request.HolidayDate:dd/MM/yyyy}; trước {request.DaysBefore} ngày; sau {request.DaysAfter} ngày; hệ số {request.Multiplier:0.####}"
            };

            _context.BangGiaPhongs.Add(holidayPrice);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã thêm giá ngày lễ." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHolidayPrice([FromBody] AdminRoomTypeHolidayPriceDeleteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoomTypeId) || string.IsNullOrWhiteSpace(request.PriceId))
            {
                return BadRequest(new { message = "Dữ liệu ngày lễ không hợp lệ." });
            }

            var roomTypeId = request.RoomTypeId.Trim();
            var priceId = request.PriceId.Trim();
            var holidayPrice = await _context.BangGiaPhongs
                .FirstOrDefaultAsync(x => x.MaBangGia == priceId &&
                                          x.MaLoaiPhong == roomTypeId &&
                                          x.LoaiGia == "NGAYLE" &&
                                          x.TrangThai == "Hoạt động");
            if (holidayPrice == null)
            {
                return NotFound(new { message = "Không tìm thấy giá ngày lễ." });
            }

            holidayPrice.TrangThai = "Ngừng áp dụng";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa giá ngày lễ." });
        }

        // POST: LoaiPhongs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminRoomTypeCreateViewModel model)
        {
            model.RoomTypeId = await GenerateRoomTypeIdAsync();
            NormalizeCreateModel(model);
            ValidateCreateModel(model);

            if (!ModelState.IsValid)
            {
                EnsureWeekdayMultiplierRows(model);
                TempData["CreateRoomTypeError"] = "Vui lòng kiểm tra lại thông tin loại phòng.";
                return RedirectToAction(nameof(Index));
            }

            var loaiPhong = new LoaiPhong
            {
                MaLoaiPhong = model.RoomTypeId,
                TenLoaiPhong = model.RoomTypeName,
                SoNguoiToiDa = model.MaxGuests,
                MoTa = model.Description
            };
            _context.LoaiPhongs.Add(loaiPhong);

            _context.BangGiaPhongs.Add(new BangGiaPhong
            {
                MaBangGia = await GenerateBangGiaIdAsync(),
                MaLoaiPhong = model.RoomTypeId,
                TuNgay = new DateOnly(1900, 1, 1),
                DenNgay = new DateOnly(9999, 12, 31),
                ThuApDung = null,
                GiaApDung = RoundPrice(model.DefaultPrice),
                LoaiGia = "MACDINH",
                UuTien = 1,
                TrangThai = "Hoạt động",
                GhiChu = "Giá mặc định áp dụng hằng ngày"
            });

            foreach (var weekday in model.WeekdayMultipliers.Where(x => x.DayValue is >= 2 and <= 8))
            {
                if (Math.Abs(weekday.Multiplier - 1) < 0.0001m)
                {
                    continue;
                }

                _context.BangGiaPhongs.Add(new BangGiaPhong
                {
                    MaBangGia = await GenerateBangGiaIdAsync(),
                    MaLoaiPhong = model.RoomTypeId,
                    TuNgay = new DateOnly(1900, 1, 1),
                    DenNgay = new DateOnly(9999, 12, 31),
                    ThuApDung = weekday.DayValue,
                    GiaApDung = RoundPrice(model.DefaultPrice * weekday.Multiplier),
                    LoaiGia = "THEOTHU",
                    UuTien = 2,
                    TrangThai = "Hoạt động",
                    GhiChu = $"Hệ số {weekday.Multiplier:0.####} so với giá mặc định"
                });
            }

            var imageLinks = await SaveRoomTypeImagesAsync(model);
            foreach (var imageLink in imageLinks)
            {
                _context.LinkAnhs.Add(imageLink);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: LoaiPhongs/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _context.LoaiPhongs.FindAsync(id);
            if (loaiPhong == null)
            {
                return NotFound();
            }
            return View(loaiPhong);
        }

        // POST: LoaiPhongs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaLoaiPhong,TenLoaiPhong,SoNguoiToiDa,MoTa")] LoaiPhong loaiPhong)
        {
            if (id != loaiPhong.MaLoaiPhong)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loaiPhong);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoaiPhongExists(loaiPhong.MaLoaiPhong))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(loaiPhong);
        }

        // GET: LoaiPhongs/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _context.LoaiPhongs
                .FirstOrDefaultAsync(m => m.MaLoaiPhong == id);
            if (loaiPhong == null)
            {
                return NotFound();
            }

            return View(loaiPhong);
        }

        // POST: LoaiPhongs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var loaiPhong = await _context.LoaiPhongs.FindAsync(id);
            if (loaiPhong != null)
            {
                _context.LoaiPhongs.Remove(loaiPhong);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LoaiPhongExists(string id)
        {
            return _context.LoaiPhongs.Any(e => e.MaLoaiPhong == id);
        }

        private AdminRoomTypeCreateViewModel CreateEmptyRoomTypeModel()
        {
            var model = new AdminRoomTypeCreateViewModel();
            EnsureWeekdayMultiplierRows(model);
            return model;
        }

        private static void EnsureWeekdayMultiplierRows(AdminRoomTypeCreateViewModel model)
        {
            var existingRows = model.WeekdayMultipliers
                .Where(x => x.DayValue is >= 2 and <= 8)
                .GroupBy(x => x.DayValue)
                .ToDictionary(x => x.Key, x => x.Last().Multiplier);
            model.WeekdayMultipliers = Enumerable.Range(2, 7)
                .Select(day => new AdminRoomTypeWeekdayMultiplierRequest
                {
                    DayValue = (byte)day,
                    Multiplier = existingRows.TryGetValue((byte)day, out var multiplier) && multiplier > 0
                        ? multiplier
                        : 1
                })
                .ToList();
        }

        private static void NormalizeCreateModel(AdminRoomTypeCreateViewModel model)
        {
            model.RoomTypeId = model.RoomTypeId.Trim().ToUpperInvariant();
            model.RoomTypeName = model.RoomTypeName.Trim();
            model.Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();
            EnsureWeekdayMultiplierRows(model);
        }

        private void ValidateCreateModel(AdminRoomTypeCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.RoomTypeId))
            {
                ModelState.AddModelError(nameof(model.RoomTypeId), "Vui lòng nhập mã loại phòng.");
            }
            else if (model.RoomTypeId.Length > 10)
            {
                ModelState.AddModelError(nameof(model.RoomTypeId), "Mã loại phòng tối đa 10 ký tự.");
            }
            else if (_context.LoaiPhongs.Any(x => x.MaLoaiPhong == model.RoomTypeId))
            {
                ModelState.AddModelError(nameof(model.RoomTypeId), "Mã loại phòng đã tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(model.RoomTypeName))
            {
                ModelState.AddModelError(nameof(model.RoomTypeName), "Vui lòng nhập tên loại phòng.");
            }
            else if (model.RoomTypeName.Length > 50)
            {
                ModelState.AddModelError(nameof(model.RoomTypeName), "Tên loại phòng tối đa 50 ký tự.");
            }
            else if (_context.LoaiPhongs.Any(x => x.TenLoaiPhong == model.RoomTypeName))
            {
                ModelState.AddModelError(nameof(model.RoomTypeName), "Tên loại phòng đã tồn tại.");
            }

            if (model.MaxGuests <= 0)
            {
                ModelState.AddModelError(nameof(model.MaxGuests), "Sức chứa phải lớn hơn 0.");
            }

            if (model.DefaultPrice <= 0)
            {
                ModelState.AddModelError(nameof(model.DefaultPrice), "Giá mặc định phải lớn hơn 0.");
            }

            foreach (var weekday in model.WeekdayMultipliers)
            {
                if (weekday.Multiplier <= 0)
                {
                    ModelState.AddModelError(nameof(model.WeekdayMultipliers), "Hệ số theo thứ phải lớn hơn 0.");
                    break;
                }
            }

            if (model.Images.Count > 6)
            {
                ModelState.AddModelError(nameof(model.Images), "Chỉ được chọn tối đa 6 ảnh.");
            }

            foreach (var image in model.Images.Where(x => x.Length > 0))
            {
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedRoomImageExtensions.Contains(extension) ||
                    !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(model.Images), "Ảnh phải là file JPG, PNG hoặc WEBP.");
                    break;
                }
            }
        }

        private static BangGiaPhong SelectDefaultPrice(IEnumerable<BangGiaPhong> prices, DateOnly today)
        {
            return prices
                .OrderByDescending(price => price.TuNgay <= today && price.DenNgay >= today)
                .ThenByDescending(price => price.TuNgay <= today)
                .ThenByDescending(price => price.TuNgay)
                .First();
        }

        private static AdminRoomTypePriceOverviewViewModel BuildPriceOverview(
            string roomTypeId,
            decimal? defaultPrice,
            Dictionary<(string MaLoaiPhong, byte ThuApDung), BangGiaPhong> weekdayPrices,
            List<BangGiaPhong> holidayPrices,
            List<BangGiaPhong> holidayPriceHistory,
            DateOnly today)
        {
            var overview = new AdminRoomTypePriceOverviewViewModel
            {
                DefaultPrice = defaultPrice
            };

            foreach (var day in Enumerable.Range(2, 7).Select(x => (byte)x))
            {
                weekdayPrices.TryGetValue((roomTypeId, day), out var weekdayPrice);
                var price = weekdayPrice?.GiaApDung ?? defaultPrice;

                overview.WeekdayPrices.Add(new AdminRoomTypeWeekdayPriceViewModel
                {
                    DayValue = day,
                    DayLabel = GetWeekdayLabel(day),
                    Price = price,
                    Multiplier = CalculateMultiplier(defaultPrice, weekdayPrice?.GiaApDung),
                    IsChanged = weekdayPrice != null &&
                                (!defaultPrice.HasValue || weekdayPrice.GiaApDung != defaultPrice.Value)
                });
            }

            overview.HolidayPrices = holidayPrices
                .Select(price => new AdminRoomTypeHolidayPriceViewModel
                {
                    PriceId = price.MaBangGia,
                    StartDate = price.TuNgay,
                    EndDate = price.DenNgay,
                    Price = price.GiaApDung,
                    Multiplier = CalculateMultiplier(defaultPrice, price.GiaApDung),
                    Note = price.GhiChu ?? string.Empty,
                    StatusLabel = "Đang áp dụng",
                    IsActiveFuture = true
                })
                .ToList();

            overview.HolidayPriceHistory = holidayPriceHistory
                .OrderByDescending(price => price.TuNgay)
                .ThenByDescending(price => price.DenNgay)
                .Select(price => new AdminRoomTypeHolidayPriceViewModel
                {
                    PriceId = price.MaBangGia,
                    StartDate = price.TuNgay,
                    EndDate = price.DenNgay,
                    Price = price.GiaApDung,
                    Multiplier = CalculateMultiplier(defaultPrice, price.GiaApDung),
                    Note = price.GhiChu ?? string.Empty,
                    StatusLabel = price.TrangThai == "Hoạt động" && price.DenNgay < today
                        ? "Đã qua"
                        : "Đã xóa",
                    IsActiveFuture = false
                })
                .ToList();

            return overview;
        }

        private async Task<decimal?> GetDefaultPriceValueAsync(string roomTypeId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var prices = await _context.BangGiaPhongs
                .AsNoTracking()
                .Where(x => x.MaLoaiPhong == roomTypeId &&
                            x.LoaiGia == "MACDINH" &&
                            x.ThuApDung == null &&
                            x.TrangThai == "Hoạt động" &&
                            x.GiaApDung > 0)
                .ToListAsync();

            return prices.Count == 0 ? null : SelectDefaultPrice(prices, today).GiaApDung;
        }

        private async Task<string> GenerateBangGiaIdAsync()
        {
            var ids = await _context.BangGiaPhongs
                .AsNoTracking()
                .Select(x => x.MaBangGia)
                .ToListAsync();
            ids.AddRange(_context.ChangeTracker
                .Entries<BangGiaPhong>()
                .Select(x => x.Entity.MaBangGia)
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            var nextNumber = ids
                .Select(id => Regex.Match(id.Trim(), @"^BG(\d+)$"))
                .Where(match => match.Success)
                .Select(match => int.Parse(match.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"BG{nextNumber:0000}";
        }

        private async Task<string> GenerateRoomTypeIdAsync()
        {
            var ids = await _context.LoaiPhongs
                .AsNoTracking()
                .Select(x => x.MaLoaiPhong)
                .ToListAsync();
            ids.AddRange(_context.ChangeTracker
                .Entries<LoaiPhong>()
                .Select(x => x.Entity.MaLoaiPhong)
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            var nextNumber = ids
                .Select(id => Regex.Match(id.Trim(), @"^LP(\d+)$"))
                .Where(match => match.Success)
                .Select(match => int.Parse(match.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"LP{nextNumber:00}";
        }

        private async Task<int> GetNextLinkAnhNumberAsync()
        {
            var ids = await _context.LinkAnhs
                .AsNoTracking()
                .Select(x => x.MaAnh)
                .ToListAsync();
            ids.AddRange(_context.ChangeTracker
                .Entries<LinkAnh>()
                .Select(x => x.Entity.MaAnh)
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            var nextNumber = ids
                .Select(id => Regex.Match(id.Trim(), @"^AN(\d+)$"))
                .Where(match => match.Success)
                .Select(match => int.Parse(match.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max() + 1;

            return nextNumber;
        }

        private async Task<List<LinkAnh>> SaveRoomTypeImagesAsync(AdminRoomTypeCreateViewModel model)
        {
            var files = model.Images
                .Where(x => x.Length > 0)
                .Take(6)
                .ToList();
            var result = new List<LinkAnh>();
            if (files.Count == 0)
            {
                return result;
            }

            var relativeDirectory = $"/images/link-anh/{model.RoomTypeId}";
            var physicalDirectory = Path.Combine(
                _environment.WebRootPath,
                "images",
                "link-anh",
                model.RoomTypeId);
            Directory.CreateDirectory(physicalDirectory);
            var nextImageNumber = await GetNextLinkAnhNumberAsync();

            for (var index = 0; index < files.Count; index++)
            {
                var file = files[index];
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{index + 1:00}-{Guid.NewGuid():N}{extension}";
                var physicalPath = Path.Combine(physicalDirectory, fileName);

                await using (var stream = System.IO.File.Create(physicalPath))
                {
                    await file.CopyToAsync(stream);
                }

                result.Add(new LinkAnh
                {
                    MaAnh = $"AN{nextImageNumber + index:000000}",
                    DoiTuong = model.RoomTypeId,
                    UrlAnh = $"{relativeDirectory}/{fileName}",
                    ThongTin = $"Ảnh loại phòng {model.RoomTypeName}",
                    ThuTu = index + 1,
                    LaAnhDaiDien = index == 0,
                    TrangThai = DomainValues.LinkAnhTrangThai.HoatDong
                });
            }

            return result;
        }

        private static decimal CalculateMultiplier(decimal? defaultPrice, decimal? price)
        {
            if (!defaultPrice.HasValue || defaultPrice.Value <= 0 || !price.HasValue)
            {
                return 1;
            }

            return Math.Round(price.Value / defaultPrice.Value, 4, MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveStoredMultiplier(BangGiaPhong price, decimal? previousDefaultPrice)
        {
            var multiplierFromNote = TryReadMultiplier(price.GhiChu);
            if (multiplierFromNote.HasValue)
            {
                return multiplierFromNote.Value;
            }

            return CalculateMultiplier(previousDefaultPrice, price.GiaApDung);
        }

        private static decimal? TryReadMultiplier(string? note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return null;
            }

            var match = Regex.Match(note, @"hệ\s*số\s*([0-9]+(?:[\.,][0-9]+)?)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            var value = match.Groups[1].Value.Replace(',', '.');
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var multiplier) &&
                   multiplier > 0
                ? multiplier
                : null;
        }

        private static decimal RoundPrice(decimal price)
        {
            return Math.Round(price, 0, MidpointRounding.AwayFromZero);
        }

        private static byte GetVietnamWeekday(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Sunday
                ? (byte)8
                : (byte)((int)date.DayOfWeek + 1);
        }

        private static string GetPriceSourceLabel(BangGiaPhong? bangGiaPhong)
        {
            if (bangGiaPhong == null)
            {
                return "Chưa có bảng giá phù hợp hôm nay";
            }

            return bangGiaPhong.LoaiGia switch
            {
                "NGAYLE" => "Giá ngày lễ - ưu tiên 3",
                "THEOTHU" => $"Giá theo thứ - ưu tiên 2 ({GetWeekdayLabel(bangGiaPhong.ThuApDung)})",
                "MACDINH" => "Giá mặc định - ưu tiên 1",
                _ => $"Bảng giá ưu tiên {bangGiaPhong.UuTien}"
            };
        }

        private static string GetWeekdayLabel(byte? thuApDung)
        {
            return thuApDung switch
            {
                2 => "Thứ 2",
                3 => "Thứ 3",
                4 => "Thứ 4",
                5 => "Thứ 5",
                6 => "Thứ 6",
                7 => "Thứ 7",
                8 => "Chủ nhật",
                _ => "không xác định"
            };
        }

        private static bool IsAvailableStatus(string? status)
        {
            return MatchesStatus(status, DomainValues.PhongTrangThai.Trong, "Trong");
        }

        private static bool IsOccupiedStatus(string? status)
        {
            return MatchesStatus(status, DomainValues.PhongTrangThai.DangSuDung, "Dang su dung", "Co khach");
        }

        private static bool IsMaintenanceStatus(string? status)
        {
            return MatchesStatus(
                status,
                DomainValues.PhongTrangThai.BaoTri,
                DomainValues.PhongTrangThai.NgungSuDung,
                "Bao tri",
                "Ngung su dung");
        }

        private static bool MatchesStatus(string? status, params string[] candidates)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return candidates.Any(candidate =>
                string.Equals(status.Trim(), candidate, StringComparison.OrdinalIgnoreCase));
        }
    }
}
