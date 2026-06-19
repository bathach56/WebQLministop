using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using WebQLministop.Data;
using WebQLministop.Models;

namespace WebQLministop.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sanPhams = await _context.SanPhams
                .Include(p => p.DanhMuc)
                .Where(p => p.KichHoat && (p.DanhMuc == null || p.DanhMuc.KichHoat))
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.DanhMucs = await _context.DanhMucs
                .Where(d => d.KichHoat)
                .OrderBy(d => d.Ten)
                .ToListAsync();
            ViewBag.SanPhamsMoi = sanPhams.Take(5).ToList();
            ViewBag.SanPhamsBanChay = sanPhams
                .OrderByDescending(p => p.TonKho)
                .Take(5)
                .ToList();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TimKiem(string? keyword, string? danhMuc, string? sapXep)
        {
            var sanPhams = await _context.SanPhams
                .Include(p => p.DanhMuc)
                .Where(p => p.KichHoat && (p.DanhMuc == null || p.DanhMuc.KichHoat))
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var tuKhoaChuan = ChuanHoaTimKiem(keyword);
                sanPhams = sanPhams
                    .Where(p => SanPhamKhopTuKhoa(p, tuKhoaChuan))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(danhMuc))
            {
                sanPhams = sanPhams
                    .Where(p => p.DanhMuc != null && p.DanhMuc.Ten == danhMuc)
                    .ToList();
            }

            sanPhams = sapXep switch
            {
                "gia-tang" => sanPhams.OrderBy(p => p.GiaBan).ToList(),
                "gia-giam" => sanPhams.OrderByDescending(p => p.GiaBan).ToList(),
                _ => sanPhams.OrderByDescending(p => p.Id).ToList()
            };

            ViewData["Keyword"] = keyword;
            ViewData["DanhMuc"] = danhMuc;
            ViewData["SapXep"] = sapXep;
            ViewBag.DanhMucs = await _context.DanhMucs
                .Where(d => d.KichHoat)
                .OrderBy(d => d.Ten)
                .ToListAsync();
            ViewBag.SanPhams = sanPhams.Take(40).ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GoiYTimKiem(string? tuKhoa)
        {
            tuKhoa = tuKhoa?.Trim() ?? string.Empty;
            if (tuKhoa.Length == 0)
            {
                return Json(Array.Empty<object>());
            }

            var tuKhoaChuan = ChuanHoaTimKiem(tuKhoa);
            var sanPhams = await _context.SanPhams
                .Include(p => p.DanhMuc)
                .Where(p => p.KichHoat && (p.DanhMuc == null || p.DanhMuc.KichHoat))
                .ToListAsync();

            var ketQua = sanPhams
                .Where(p => SanPhamKhopTuKhoa(p, tuKhoaChuan))
                .OrderByDescending(p => DiemGoiY(p, tuKhoaChuan))
                .ThenBy(p => p.Ten)
                .Take(10)
                .Select(p => new
                {
                    p.Id,
                    p.Ma,
                    p.Ten,
                    p.GiaBan,
                    DanhMuc = p.DanhMuc != null ? p.DanhMuc.Ten : null,
                    p.HinhAnh
                })
                .ToList();

            return Json(ketQua);
        }

        private static bool SanPhamKhopTuKhoa(SanPham sanPham, string tuKhoaChuan)
        {
            if (string.IsNullOrWhiteSpace(tuKhoaChuan))
            {
                return true;
            }

            return ChuanHoaTimKiem(sanPham.Ten).Contains(tuKhoaChuan)
                || ChuanHoaTimKiem(sanPham.Ma).Contains(tuKhoaChuan)
                || ChuanHoaTimKiem(sanPham.MoTa).Contains(tuKhoaChuan)
                || ChuanHoaTimKiem(sanPham.DanhMuc?.Ten).Contains(tuKhoaChuan);
        }

        private static int DiemGoiY(SanPham sanPham, string tuKhoaChuan)
        {
            var ma = ChuanHoaTimKiem(sanPham.Ma);
            var ten = ChuanHoaTimKiem(sanPham.Ten);
            var danhMuc = ChuanHoaTimKiem(sanPham.DanhMuc?.Ten);

            if (ma == tuKhoaChuan || ten == tuKhoaChuan) return 100;
            if (ma.StartsWith(tuKhoaChuan) || ten.StartsWith(tuKhoaChuan)) return 80;
            if (danhMuc.StartsWith(tuKhoaChuan)) return 60;
            if (ma.Contains(tuKhoaChuan) || ten.Contains(tuKhoaChuan)) return 40;
            return 10;
        }

        private static string ChuanHoaTimKiem(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch == 'đ' ? 'd' : ch);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
