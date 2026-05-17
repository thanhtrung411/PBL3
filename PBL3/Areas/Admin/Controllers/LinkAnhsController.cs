using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LinkAnhsController : Controller
    {
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;
        private const string UploadFolder = "images/link-anh";

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif"
        };

        private readonly ILinkAnhService _linkAnhService;
        private readonly IWebHostEnvironment _environment;

        public LinkAnhsController(ILinkAnhService linkAnhService, IWebHostEnvironment environment)
        {
            _linkAnhService = linkAnhService;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _linkAnhService.GetAllAsync());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var linkAnh = await _linkAnhService.GetByIdAsync(id);
            if (linkAnh == null) return NotFound();

            return View(linkAnh);
        }

        public IActionResult Create()
        {
            return View(new LinkAnh
            {
                ThuTu = 1,
                TrangThai = DomainValues.LinkAnhTrangThai.HoatDong
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("MaAnh,DoiTuong,UrlAnh,ThongTin,ThuTu,LaAnhDaiDien,TrangThai")] LinkAnh linkAnh,
            IFormFile? tepAnh)
        {
            if (tepAnh is { Length: > 0 })
            {
                ModelState.Remove(nameof(LinkAnh.UrlAnh));
            }

            if (!ModelState.IsValid) return View(linkAnh);

            if (await _linkAnhService.KiemTraTrungMaAsync(linkAnh.MaAnh))
            {
                ModelState.AddModelError("MaAnh", "Mã ảnh đã tồn tại.");
                return View(linkAnh);
            }

            if (tepAnh is { Length: > 0 })
            {
                var uploadedUrl = await TrySaveUploadedImageAsync(tepAnh, linkAnh.MaAnh);
                if (uploadedUrl == null)
                {
                    return View(linkAnh);
                }

                linkAnh.UrlAnh = uploadedUrl;
            }

            var createResult = await _linkAnhService.CreateAsync(linkAnh);
            if (createResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể tạo link ảnh. Vui lòng kiểm tra dữ liệu.");
            return View(linkAnh);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var linkAnh = await _linkAnhService.GetByIdAsync(id);
            if (linkAnh == null) return NotFound();

            return View(linkAnh);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            [Bind("MaAnh,DoiTuong,UrlAnh,ThongTin,ThuTu,LaAnhDaiDien,TrangThai")] LinkAnh linkAnh,
            IFormFile? tepAnh)
        {
            if (id != linkAnh.MaAnh) return NotFound();

            if (tepAnh is { Length: > 0 })
            {
                ModelState.Remove(nameof(LinkAnh.UrlAnh));
            }

            if (!ModelState.IsValid) return View(linkAnh);

            if (tepAnh is { Length: > 0 })
            {
                var uploadedUrl = await TrySaveUploadedImageAsync(tepAnh, linkAnh.MaAnh);
                if (uploadedUrl == null)
                {
                    return View(linkAnh);
                }

                linkAnh.UrlAnh = uploadedUrl;
            }

            var updateResult = await _linkAnhService.UpdateAsync(linkAnh);
            if (updateResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể cập nhật link ảnh. Vui lòng kiểm tra dữ liệu.");
            return View(linkAnh);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var linkAnh = await _linkAnhService.GetByIdAsync(id);
            if (linkAnh == null) return NotFound();

            return View(linkAnh);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var deleteResult = await _linkAnhService.DeleteAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa link ảnh vì dữ liệu không còn tồn tại.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> TrySaveUploadedImageAsync(IFormFile tepAnh, string maAnh)
        {
            if (tepAnh.Length > MaxImageSizeBytes)
            {
                ModelState.AddModelError("UrlAnh", "Ảnh upload không được vượt quá 5MB.");
                return null;
            }

            var extension = Path.GetExtension(tepAnh.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("UrlAnh", "Chỉ hỗ trợ ảnh .jpg, .jpeg, .png, .webp hoặc .gif.");
                return null;
            }

            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                ModelState.AddModelError("UrlAnh", "Không tìm thấy thư mục wwwroot để lưu ảnh.");
                return null;
            }

            var uploadDirectory = Path.Combine(webRootPath, UploadFolder);
            Directory.CreateDirectory(uploadDirectory);

            var safeCode = string.Concat(maAnh.Trim().Where(char.IsLetterOrDigit));
            if (string.IsNullOrWhiteSpace(safeCode))
            {
                safeCode = "anh";
            }

            var fileName = $"{safeCode.ToLowerInvariant()}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension.ToLowerInvariant()}";
            var physicalPath = Path.Combine(uploadDirectory, fileName);

            await using var stream = new FileStream(physicalPath, FileMode.CreateNew);
            await tepAnh.CopyToAsync(stream);

            return $"/{UploadFolder.Replace('\\', '/')}/{fileName}";
        }
    }
}
