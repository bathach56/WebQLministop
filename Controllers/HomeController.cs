using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using WebQLministop.Data;
using WebQLministop.Models;

namespace WebQLministop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var sanPhams = await _context.SanPhams
                .Include(p => p.DanhMuc)
                .OrderBy(p => p.Ten)
                .ToListAsync();

            var donHangs = await _context.DonHangs
                .Include(o => o.KhachHang)
                .Include(o => o.NhanVien)
                .Include(o => o.ChiTiet)
                    .ThenInclude(i => i.SanPham)
                .OrderByDescending(o => o.NgayDat)
                .ToListAsync();

            var khachHangs = await _context.KhachHangs
                .OrderBy(c => c.HoTen)
                .ToListAsync();

            var nhanViens = await _context.NhanViens
                .OrderBy(e => e.HoTen)
                .ToListAsync();

            var khuyenMais = await _context.KhuyenMais
                .OrderByDescending(p => p.NgayBatDau)
                .ToListAsync();

            ViewBag.SanPhams = sanPhams;
            ViewBag.DonHangs = donHangs;
            ViewBag.KhachHangs = khachHangs;
            ViewBag.NhanViens = nhanViens;
            ViewBag.KhuyenMais = khuyenMais;
            ViewBag.DanhMucs = await _context.DanhMucs.OrderBy(c => c.Ten).ToListAsync();
            ViewBag.NhaCungCaps = await _context.NhaCungCaps.OrderBy(s => s.Ten).ToListAsync();

            ViewBag.DoanhThuHomNay = donHangs
                .Where(o => o.NgayDat >= today && o.NgayDat < tomorrow)
                .Sum(o => o.TongTien);
            ViewBag.SoHoaDonHomNay = donHangs.Count(o => o.NgayDat >= today && o.NgayDat < tomorrow);
            ViewBag.SanPhamSapHet = sanPhams.Count(p => p.TonKho <= p.MucCanNhapLai);
            ViewBag.NhanVienKichHoat = nhanViens.Count(e => e.KichHoat);
            ViewBag.KhachHangKichHoat = khachHangs.Count(c => c.KichHoat);
            ViewBag.KhuyenMaiDangChay = khuyenMais.Count(p => p.KichHoat && p.NgayBatDau <= DateTime.UtcNow && p.NgayKetThuc >= DateTime.UtcNow);
            ViewBag.KhuyenMaiSapHetHan = khuyenMais.Count(p => p.KichHoat && p.NgayKetThuc >= DateTime.UtcNow && p.NgayKetThuc <= DateTime.UtcNow.AddDays(7));
            ViewBag.KhuyenMaiDaKetThuc = khuyenMais.Count(p => p.NgayKetThuc < DateTime.UtcNow);
            ViewBag.TopSanPhams = donHangs
                .SelectMany(o => o.ChiTiet)
                .Where(i => i.SanPham != null)
                .GroupBy(i => i.SanPham!.Ten)
                .Select(g => new { Ten = g.Key, SoLuong = g.Sum(i => i.SoLuong) })
                .OrderByDescending(i => i.SoLuong)
                .Take(5)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSanPham(SanPham sanPham)
        {
            if (await _context.SanPhams.AnyAsync(p => p.Ma == sanPham.Ma))
            {
                ModelState.AddModelError(nameof(SanPham.Ma), "Ma san pham da ton tai.");
            }

            var hasDanhMuc = await _context.DanhMucs.AnyAsync(c => c.Id == sanPham.DanhMucId);
            var hasNhaCungCap = await _context.NhaCungCaps.AnyAsync(s => s.Id == sanPham.NhaCungCapId);

            if (!hasDanhMuc)
            {
                ModelState.AddModelError(nameof(SanPham.DanhMucId), "Vui long chon danh muc hop le.");
            }

            if (!hasNhaCungCap)
            {
                ModelState.AddModelError(nameof(SanPham.NhaCungCapId), "Vui long chon nha cung cap hop le.");
            }

            if (!ModelState.IsValid)
            {
                TempData["SanPhamMessage"] = "Khong the luu san pham. Vui long kiem tra lai thong tin.";
                TempData["SanPhamMessageType"] = "danger";
                return RedirectToAction(nameof(Index), "Home", null, "products");
            }

            sanPham.KichHoat = true;
            _context.SanPhams.Add(sanPham);
            await _context.SaveChangesAsync();

            TempData["SanPhamMessage"] = "Da luu san pham moi.";
            TempData["SanPhamMessageType"] = "success";
            return RedirectToAction(nameof(Index), "Home", null, "products");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
