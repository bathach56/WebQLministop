using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using WebQLministop.Data;

namespace WebQLministop.Controllers;

public class NhanVienController : Controller
{
    private readonly ApplicationDbContext _context;

    public NhanVienController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.SanPhams = await _context.SanPhams
            .Include(s => s.DanhMuc)
            .Where(s => s.KichHoat)
            .OrderBy(s => s.Ten)
            .ToListAsync();

        ViewBag.KhachHangs = await _context.KhachHangs
            .Where(k => k.KichHoat)
            .OrderBy(k => k.HoTen)
            .ToListAsync();

        ViewBag.NhanViens = await _context.NhanViens
            .Where(n => n.KichHoat)
            .OrderBy(n => n.HoTen)
            .ToListAsync();

        ViewBag.DonHangs = await _context.DonHangs
            .Include(d => d.KhachHang)
            .Include(d => d.NhanVien)
            .OrderByDescending(d => d.NgayDat)
            .Take(10)
            .ToListAsync();

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> TimSanPham(string? tuKhoa)
    {
        var keyword = (tuKhoa ?? string.Empty).Trim();

        if (keyword.Length == 0)
        {
            return Json(Array.Empty<object>());
        }

        var sanPhams = await _context.SanPhams
            .Include(s => s.DanhMuc)
            .Where(s => s.KichHoat)
            .OrderBy(s => s.Ten)
            .ToListAsync();

        var keywordKhongDau = BoDau(keyword);
        var ketQua = sanPhams
            .Where(s =>
                BoDau(s.Ma).Contains(keywordKhongDau) ||
                BoDau(s.Ten).Contains(keywordKhongDau))
            .Take(10)
            .Select(s => new
            {
                s.Id,
                s.Ma,
                s.Ten,
                s.GiaBan,
                s.TonKho,
                s.DonVi,
                DanhMuc = s.DanhMuc != null ? s.DanhMuc.Ten : "Khac"
            })
            .ToList();

        return Json(ketQua);
    }

    private static string BoDau(string value)
    {
        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd');
    }
}
