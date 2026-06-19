using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebQLministop.Data;
using WebQLministop.Models;

namespace WebQLministop.Controllers.Api;

[ApiController]
[Route("api/san-pham")]
public class SanPhamApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SanPhamApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDanhSach(string? keyword, int? danhMucId, string? sapXep, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.SanPhams
            .AsNoTracking()
            .Include(p => p.DanhMuc)
            .Where(p => p.KichHoat && (p.DanhMuc == null || p.DanhMuc.KichHoat));

        if (danhMucId.HasValue)
        {
            query = query.Where(p => p.DanhMucId == danhMucId.Value);
        }

        var sanPhams = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var tuKhoaChuan = ChuanHoaTimKiem(keyword);
            sanPhams = sanPhams
                .Where(p => SanPhamKhopTuKhoa(p, tuKhoaChuan))
                .ToList();
        }

        sanPhams = sapXep switch
        {
            "gia-tang" => sanPhams.OrderBy(p => p.GiaBan).ToList(),
            "gia-giam" => sanPhams.OrderByDescending(p => p.GiaBan).ToList(),
            _ => sanPhams.OrderByDescending(p => p.Id).ToList()
        };

        var totalItems = sanPhams.Count;
        var items = sanPhams
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(TaoSanPhamResponse)
            .ToList();

        return Ok(new
        {
            page,
            pageSize,
            totalItems,
            totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            items
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetChiTiet(int id)
    {
        var sanPham = await _context.SanPhams
            .AsNoTracking()
            .Include(p => p.DanhMuc)
            .FirstOrDefaultAsync(p => p.Id == id && p.KichHoat && (p.DanhMuc == null || p.DanhMuc.KichHoat));

        if (sanPham == null)
        {
            return NotFound(new { message = "Khong tim thay san pham." });
        }

        return Ok(TaoSanPhamResponse(sanPham));
    }

    private static object TaoSanPhamResponse(SanPham sanPham)
    {
        return new
        {
            sanPham.Id,
            sanPham.Ma,
            sanPham.Ten,
            sanPham.MoTa,
            sanPham.HinhAnh,
            sanPham.GiaBan,
            sanPham.DonVi,
            sanPham.TonKho,
            sanPham.DanhMucId,
            DanhMuc = sanPham.DanhMuc == null
                ? null
                : new
                {
                    sanPham.DanhMuc.Id,
                    sanPham.DanhMuc.Ten,
                    sanPham.DanhMuc.MoTa
                }
        };
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
                builder.Append(ch == '\u0111' ? 'd' : ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
