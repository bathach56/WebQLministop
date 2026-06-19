using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebQLministop.Data;
using ClosedXML.Excel;
using System.IO;
using WebQLministop.Models;
using NhanVienModel = WebQLministop.Models.NhanVien;

namespace WebQLministop.Areas.QuanLy.Controllers;

[Area("QuanLy")]
public class QuanLyController : Controller
{
    private static readonly IReadOnlyDictionary<string, string> DanhSachQuyen = new Dictionary<string, string>
    {
        ["SanPham.Tao"] = "Tao/sua san pham",
        ["DanhMuc.QuanLy"] = "Quan ly danh muc",
        ["DonHang.QuanLy"] = "Quan ly don hang",
        ["KhuyenMai.Tao"] = "Tao/bat tat voucher",
        ["HoaDon.Xem"] = "Xem/in hoa don",
        ["NhanVien.PhanQuyen"] = "Phan quyen nhan vien",
        ["BanHang.TaoDon"] = "Tao hoa don tai quay",
        ["BanHang.TraCuuSanPham"] = "Tra cuu san pham tai quay"
    };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public QuanLyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        if (!CoQuyenBatKy())
        {
            return RedirectToAction("Index", "DangNhap", new { area = "KhachHang" });
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var sanPhams = await _context.SanPhams
            .Include(p => p.DanhMuc)
            .OrderBy(p => p.Ten)
            .ToListAsync();

        var donHangs = await _context.DonHangs
            .Include(d => d.KhachHang)
            .Include(d => d.NhanVien)
            .Include(d => d.ChiTiet)
            .ThenInclude(c => c.SanPham)
            .OrderByDescending(d => d.NgayDat)
            .ToListAsync();

        var khachHangs = await _context.KhachHangs.Where(k => k.HoTen != "Khach le").OrderBy(k => k.HoTen).ToListAsync();
        var nhanViens = await _context.NhanViens.OrderBy(n => n.HoTen).ToListAsync();
        var khuyenMais = await _context.KhuyenMais.OrderByDescending(k => k.NgayBatDau).ToListAsync();
        var nhanVienQuyens = new Dictionary<int, HashSet<string>>();
        foreach (var nhanVien in nhanViens)
        {
            var user = await TimTaiKhoanNhanVien(nhanVien);
            var claims = (user == null
                ? QuyenMacDinh(nhanVien.ChucVu)
                : (await _userManager.GetClaimsAsync(user))
                    .Where(c => c.Type == "Permission")
                    .Select(c => c.Value)
                    .Where(LaQuyenHopLe))
                .ToList();
            if (claims.Count == 0)
            {
                claims = QuyenMacDinh(nhanVien.ChucVu).ToList();
            }
            nhanVienQuyens[nhanVien.Id] = claims.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        ViewBag.SanPhams = sanPhams;
        ViewBag.DonHangs = donHangs;
        ViewBag.KhachHangs = khachHangs;
        ViewBag.NhanViens = nhanViens;
        ViewBag.KhuyenMais = khuyenMais;
        ViewBag.DanhSachQuyen = DanhSachQuyen;
        ViewBag.NhanVienQuyens = nhanVienQuyens;
        ViewBag.QuyenHienTai = LayQuyenHienTai();
        ViewBag.DanhMucs = await _context.DanhMucs.OrderBy(d => d.Ten).ToListAsync();
        ViewBag.NhaCungCaps = await _context.NhaCungCaps.OrderBy(n => n.Ten).ToListAsync();

        ViewBag.DoanhThuHomNay = donHangs.Where(d => d.NgayDat >= today && d.NgayDat < tomorrow).Sum(d => d.TongTien);
        ViewBag.SoHoaDonHomNay = donHangs.Count(d => d.NgayDat >= today && d.NgayDat < tomorrow);
        ViewBag.SanPhamSapHet = sanPhams.Count(p => p.TonKho <= p.MucCanNhapLai);
        ViewBag.NhanVienKichHoat = nhanViens.Count(n => n.KichHoat);
        ViewBag.KhachHangKichHoat = khachHangs.Count(k => k.KichHoat);
        ViewBag.KhuyenMaiDangChay = khuyenMais.Count(k => k.KichHoat && k.NgayBatDau <= DateTime.UtcNow && k.NgayKetThuc >= DateTime.UtcNow);
        ViewBag.KhuyenMaiSapHetHan = khuyenMais.Count(k => k.KichHoat && k.NgayKetThuc >= DateTime.UtcNow && k.NgayKetThuc <= DateTime.UtcNow.AddDays(7));
        ViewBag.KhuyenMaiDaKetThuc = khuyenMais.Count(k => k.NgayKetThuc < DateTime.UtcNow);
        ViewBag.TopSanPhams = donHangs
            .SelectMany(d => d.ChiTiet)
            .GroupBy(c => !string.IsNullOrWhiteSpace(c.TenSanPham) ? c.TenSanPham! : c.SanPham?.Ten ?? "San pham khong xac dinh")
            .Select(g => new { Ten = g.Key, SoLuong = g.Sum(c => c.SoLuong) })
            .OrderByDescending(x => x.SoLuong)
            .Take(5)
            .ToList();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSanPham(SanPham sanPham)
    {
        if (!CoQuyen("SanPham.Tao")) return KhongCoQuyen();

        if (await _context.SanPhams.AnyAsync(p => p.Ma == sanPham.Ma))
        {
            ModelState.AddModelError(nameof(SanPham.Ma), "Mã sản phẩm đã tồn tại.");
        }

        if (!await _context.DanhMucs.AnyAsync(d => d.Id == sanPham.DanhMucId && d.KichHoat))
        {
            ModelState.AddModelError(nameof(SanPham.DanhMucId), "Vui lòng chọn danh mục hợp lệ.");
        }

        if (!await _context.NhaCungCaps.AnyAsync(n => n.Id == sanPham.NhaCungCapId))
        {
            ModelState.AddModelError(nameof(SanPham.NhaCungCapId), "Vui lòng chọn nhà cung cấp hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            ThongBao("Không thể lưu sản phẩm. Vui lòng kiểm tra lại thông tin.", "danger");
            return RedirectToAction(nameof(Index), null, null, "products");
        }

        _context.SanPhams.Add(sanPham);
        await _context.SaveChangesAsync();

        ThongBao("Đã lưu sản phẩm mới.", "success");
        return RedirectToAction(nameof(Index), null, null, "products");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSanPham(int id)
    {
        if (!CoQuyen("SanPham.Tao")) return KhongCoQuyen();

        var sanPham = await _context.SanPhams.FindAsync(id);
        if (sanPham == null)
        {
            ThongBao("Không tìm thấy sản phẩm cần cập nhật.", "danger");
            return RedirectToAction(nameof(Index), null, null, "products");
        }

        sanPham.KichHoat = !sanPham.KichHoat;
        await _context.SaveChangesAsync();
        ThongBao(sanPham.KichHoat ? "Đã bật bán sản phẩm." : "Đã ngừng bán sản phẩm.", "success");
        return RedirectToAction(nameof(Index), null, null, "products");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSanPham(SanPham sanPham)
    {
        if (!CoQuyen("SanPham.Tao")) return KhongCoQuyen();

        var sanPhamHienTai = await _context.SanPhams.FindAsync(sanPham.Id);
        if (sanPhamHienTai == null)
        {
            ThongBao("Không tìm thấy sản phẩm cần sửa.", "danger");
            return RedirectToAction(nameof(Index), null, null, "products");
        }

        if (await _context.SanPhams.AnyAsync(p => p.Id != sanPham.Id && p.Ma == sanPham.Ma))
        {
            ModelState.AddModelError(nameof(SanPham.Ma), "Mã sản phẩm đã tồn tại.");
        }

        if (!await _context.DanhMucs.AnyAsync(d => d.Id == sanPham.DanhMucId && d.KichHoat) ||
            !await _context.NhaCungCaps.AnyAsync(n => n.Id == sanPham.NhaCungCapId))
        {
            ModelState.AddModelError(string.Empty, "Danh mục hoặc nhà cung cấp không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            ThongBao("Không thể cập nhật sản phẩm. Vui lòng kiểm tra lại thông tin.", "danger");
            return RedirectToAction(nameof(Index), null, null, "products");
        }

        sanPhamHienTai.Ma = sanPham.Ma;
        sanPhamHienTai.Ten = sanPham.Ten;
        sanPhamHienTai.MoTa = sanPham.MoTa;
        sanPhamHienTai.HinhAnh = sanPham.HinhAnh;
        sanPhamHienTai.DanhMucId = sanPham.DanhMucId;
        sanPhamHienTai.NhaCungCapId = sanPham.NhaCungCapId;
        sanPhamHienTai.GiaVon = sanPham.GiaVon;
        sanPhamHienTai.GiaBan = sanPham.GiaBan;
        sanPhamHienTai.DonVi = sanPham.DonVi;
        sanPhamHienTai.TonKho = sanPham.TonKho;
        sanPhamHienTai.MucCanNhapLai = sanPham.MucCanNhapLai;
        sanPhamHienTai.KichHoat = sanPham.KichHoat;

        await _context.SaveChangesAsync();
        ThongBao("Đã cập nhật sản phẩm.", "success");
        return RedirectToAction(nameof(Index), null, null, "products");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDanhMuc(DanhMuc danhMuc)
    {
        if (!CoQuyen("DanhMuc.QuanLy")) return KhongCoQuyen();

        danhMuc.Ten = danhMuc.Ten?.Trim() ?? string.Empty;
        danhMuc.MoTa = danhMuc.MoTa?.Trim();

        if (string.IsNullOrWhiteSpace(danhMuc.Ten))
        {
            ModelState.AddModelError(nameof(DanhMuc.Ten), "Vui lòng nhập tên danh mục.");
        }

        if (await _context.DanhMucs.AnyAsync(d => d.Ten == danhMuc.Ten))
        {
            ModelState.AddModelError(nameof(DanhMuc.Ten), "Danh mục này đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            ThongBao("Không thể thêm danh mục. Vui lòng kiểm tra lại thông tin.", "danger");
            return RedirectToAction(nameof(Index), null, null, "categories");
        }

        danhMuc.KichHoat = true;
        _context.DanhMucs.Add(danhMuc);
        await _context.SaveChangesAsync();
        ThongBao("Đã thêm danh mục mới.", "success");
        return RedirectToAction(nameof(Index), null, null, "categories");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDanhMuc(DanhMuc danhMuc)
    {
        if (!CoQuyen("DanhMuc.QuanLy")) return KhongCoQuyen();

        var danhMucHienTai = await _context.DanhMucs.FindAsync(danhMuc.Id);
        if (danhMucHienTai == null)
        {
            ThongBao("Không tìm thấy danh mục cần sửa.", "danger");
            return RedirectToAction(nameof(Index), null, null, "categories");
        }

        danhMuc.Ten = danhMuc.Ten?.Trim() ?? string.Empty;
        danhMuc.MoTa = danhMuc.MoTa?.Trim();

        if (string.IsNullOrWhiteSpace(danhMuc.Ten))
        {
            ModelState.AddModelError(nameof(DanhMuc.Ten), "Vui lòng nhập tên danh mục.");
        }

        if (await _context.DanhMucs.AnyAsync(d => d.Id != danhMuc.Id && d.Ten == danhMuc.Ten))
        {
            ModelState.AddModelError(nameof(DanhMuc.Ten), "Danh mục này đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            ThongBao("Không thể cập nhật danh mục. Vui lòng kiểm tra lại thông tin.", "danger");
            return RedirectToAction(nameof(Index), null, null, "categories");
        }

        danhMucHienTai.Ten = danhMuc.Ten;
        danhMucHienTai.MoTa = danhMuc.MoTa;
        await _context.SaveChangesAsync();

        ThongBao("Đã cập nhật danh mục.", "success");
        return RedirectToAction(nameof(Index), null, null, "categories");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDanhMuc(int id)
    {
        if (!CoQuyen("DanhMuc.QuanLy")) return KhongCoQuyen();

        var danhMuc = await _context.DanhMucs.FindAsync(id);
        if (danhMuc == null)
        {
            ThongBao("Không tìm thấy danh mục cần cập nhật.", "danger");
            return RedirectToAction(nameof(Index), null, null, "categories");
        }

        danhMuc.KichHoat = !danhMuc.KichHoat;
        await _context.SaveChangesAsync();

        ThongBao(
            danhMuc.KichHoat
                ? "Đã khôi phục danh mục."
                : "Đã ẩn danh mục. Sản phẩm thuộc danh mục này sẽ không hiển thị ở trang khách và POS.",
            "success");
        return RedirectToAction(nameof(Index), null, null, "categories");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatTrangThaiDonHang(int id, string trangThai)
    {
        if (!CoQuyen("DonHang.QuanLy")) return KhongCoQuyen();

        var trangThaiHopLe = new[] { "ChoXuLy", "DangXuLy", "DaThanhToan", "ThanhCong", "DaHuy" };
        if (!trangThaiHopLe.Contains(trangThai))
        {
            ThongBao("Trạng thái đơn hàng không hợp lệ.", "danger");
            return RedirectToAction(nameof(Index), null, null, "orders");
        }

        var donHang = await _context.DonHangs.FindAsync(id);
        if (donHang == null)
        {
            ThongBao("Không tìm thấy đơn hàng cần cập nhật.", "danger");
            return RedirectToAction(nameof(Index), null, null, "orders");
        }

        donHang.TrangThai = trangThai;
        await _context.SaveChangesAsync();
        ThongBao($"Đã cập nhật trạng thái đơn hàng HD-{donHang.Id:0000}.", "success");
        return RedirectToAction(nameof(Index), null, null, "orders");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNhanVien(NhanVienModel nhanVien, string? matKhau)
    {
        if (!CoQuyen("NhanVien.PhanQuyen")) return KhongCoQuyen();

        if (!string.IsNullOrWhiteSpace(nhanVien.Email) && await _context.NhanViens.AnyAsync(n => n.Email == nhanVien.Email))
        {
            ModelState.AddModelError(nameof(NhanVienModel.Email), "Email nhân viên đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            ThongBao("Không thể thêm nhân viên. Vui lòng kiểm tra lại thông tin.", "danger");
            return RedirectToAction(nameof(Index), null, null, "staff");
        }

        nhanVien.KichHoat = true;
        nhanVien.NgayVaoLam = DateTime.UtcNow;
        _context.NhanViens.Add(nhanVien);
        await _context.SaveChangesAsync();
        var daTaoTaiKhoan = await TaoHoacCapNhatTaiKhoanNhanVien(nhanVien, string.IsNullOrWhiteSpace(matKhau) ? "123456" : matKhau);
        if (!daTaoTaiKhoan)
        {
            ThongBao("Đã thêm hồ sơ nhân viên nhưng chưa tạo được tài khoản đăng nhập. Vui lòng kiểm tra email/số điện thoại.", "danger");
            return RedirectToAction(nameof(Index), null, null, "staff");
        }

        ThongBao("Đã thêm nhân viên mới. Nếu không nhập mật khẩu thì mật khẩu mặc định là 123456.", "success");
        return RedirectToAction(nameof(Index), null, null, "staff");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleNhanVien(int id)
    {
        if (!CoQuyen("NhanVien.PhanQuyen")) return KhongCoQuyen();

        var nhanVien = await _context.NhanViens.FindAsync(id);
        if (nhanVien == null)
        {
            ThongBao("Không tìm thấy nhân viên cần cập nhật.", "danger");
            return RedirectToAction(nameof(Index), null, null, "staff");
        }

        nhanVien.KichHoat = !nhanVien.KichHoat;
        await _context.SaveChangesAsync();
        ThongBao(nhanVien.KichHoat ? "Đã kích hoạt nhân viên." : "Đã ngừng tài khoản nhân viên.", "success");
        return RedirectToAction(nameof(Index), null, null, "staff");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatChucVuNhanVien(int id, string chucVu)
    {
        if (!CoQuyen("NhanVien.PhanQuyen")) return KhongCoQuyen();

        chucVu = chucVu?.Trim() ?? string.Empty;
        var chucVuHopLe = new[] { "Quan ly", "Nhan vien", "Thu ngan", "Pha che", "Kho" };
        if (!chucVuHopLe.Contains(chucVu))
        {
            ThongBao("Chức vụ nhân viên không hợp lệ.", "danger");
            return RedirectToAction(nameof(Index), null, null, "staff");
        }

        var nhanVien = await _context.NhanViens.FindAsync(id);
        if (nhanVien == null)
        {
            ThongBao("Không tìm thấy nhân viên cần phân quyền.", "danger");
            return RedirectToAction(nameof(Index), null, null, "staff");
        }

        nhanVien.ChucVu = chucVu;
        await _context.SaveChangesAsync();

        var daCapNhat = await TaoHoacCapNhatTaiKhoanNhanVien(nhanVien, null);
        ThongBao(daCapNhat ? "Đã cập nhật chức vụ và role đăng nhập cho nhân viên." : "Đã lưu chức vụ nhưng chưa cập nhật được role đăng nhập.", daCapNhat ? "success" : "danger");
        return RedirectToAction(nameof(Index), null, null, "staff");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNhanVien(NhanVienModel nhanVien, string? matKhau)
    {
        if (!CoQuyen("NhanVien.PhanQuyen")) return KhongCoQuyen();

        var nhanVienHienTai = await _context.NhanViens.FindAsync(nhanVien.Id);
        if (nhanVienHienTai == null)
        {
            ThongBao("Không tìm thấy nhân viên cần sửa.", "danger");
            return RedirectToAction(nameof(Index), null, null, "staff");
        }

        if (!string.IsNullOrWhiteSpace(nhanVien.Email) &&
            await _context.NhanViens.AnyAsync(n => n.Id != nhanVien.Id && n.Email == nhanVien.Email))
        {
            ModelState.AddModelError(nameof(NhanVienModel.Email), "Email nhân viên đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            ThongBao("Không thể cập nhật nhân viên. Vui lòng kiểm tra lại thông tin.", "danger");
            return RedirectToAction(nameof(Index), null, null, "staff");
        }

        nhanVienHienTai.HoTen = nhanVien.HoTen;
        nhanVienHienTai.ChucVu = nhanVien.ChucVu;
        nhanVienHienTai.DienThoai = nhanVien.DienThoai;
        nhanVienHienTai.Email = nhanVien.Email;
        nhanVienHienTai.Luong = nhanVien.Luong;
        nhanVienHienTai.KichHoat = nhanVien.KichHoat;

        await _context.SaveChangesAsync();
        await TaoHoacCapNhatTaiKhoanNhanVien(nhanVienHienTai, matKhau);
        ThongBao("Đã cập nhật nhân viên.", "success");
        return RedirectToAction(nameof(Index), null, null, "staff");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNhaCungCap(NhaCungCap nhaCungCap)
    {
        if (!CoQuyen("SanPham.Tao")) return KhongCoQuyen();

        if (!ModelState.IsValid)
        {
            ThongBao("Không thể thêm nhà cung cấp. Vui lòng kiểm tra lại thông tin.", "danger");
            return RedirectToAction(nameof(Index), null, null, "suppliers");
        }

        _context.NhaCungCaps.Add(nhaCungCap);
        await _context.SaveChangesAsync();
        ThongBao("Đã thêm nhà cung cấp mới.", "success");
        return RedirectToAction(nameof(Index), null, null, "suppliers");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNhaCungCap(NhaCungCap nhaCungCap)
    {
        if (!CoQuyen("SanPham.Tao")) return KhongCoQuyen();

        var nhaCungCapHienTai = await _context.NhaCungCaps.FindAsync(nhaCungCap.Id);
        if (nhaCungCapHienTai == null)
        {
            ThongBao("Không tìm thấy nhà cung cấp cần sửa.", "danger");
            return RedirectToAction(nameof(Index), null, null, "suppliers");
        }

        if (!ModelState.IsValid)
        {
            ThongBao("Không thể cập nhật nhà cung cấp. Vui lòng kiểm tra lại thông tin.", "danger");
            return RedirectToAction(nameof(Index), null, null, "suppliers");
        }

        nhaCungCapHienTai.Ten = nhaCungCap.Ten;
        nhaCungCapHienTai.TenLienHe = nhaCungCap.TenLienHe;
        nhaCungCapHienTai.DienThoai = nhaCungCap.DienThoai;
        nhaCungCapHienTai.Email = nhaCungCap.Email;
        nhaCungCapHienTai.DiaChi = nhaCungCap.DiaChi;

        await _context.SaveChangesAsync();
        ThongBao("Đã cập nhật nhà cung cấp.", "success");
        return RedirectToAction(nameof(Index), null, null, "suppliers");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateKhuyenMai(KhuyenMai khuyenMai)
    {
        if (!CoQuyen("KhuyenMai.Tao")) return KhongCoQuyen();

        if (!string.IsNullOrWhiteSpace(khuyenMai.Ma) && await _context.KhuyenMais.AnyAsync(k => k.Ma == khuyenMai.Ma))
        {
            ModelState.AddModelError(nameof(KhuyenMai.Ma), "Mã khuyến mãi đã tồn tại.");
        }

        if (khuyenMai.NgayKetThuc < khuyenMai.NgayBatDau)
        {
            ModelState.AddModelError(nameof(KhuyenMai.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");
        }

        if (!ModelState.IsValid)
        {
            ThongBao("Không thể tạo khuyến mãi. Vui lòng kiểm tra lại thông tin.", "danger");
            return RedirectToAction(nameof(Index), null, null, "promotions");
        }

        khuyenMai.KichHoat = true;
        _context.KhuyenMais.Add(khuyenMai);
        await _context.SaveChangesAsync();
        ThongBao("Đã tạo khuyến mãi mới.", "success");
        return RedirectToAction(nameof(Index), null, null, "promotions");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleKhuyenMai(int id)
    {
        if (!CoQuyen("KhuyenMai.Tao")) return KhongCoQuyen();

        var khuyenMai = await _context.KhuyenMais.FindAsync(id);
        if (khuyenMai == null)
        {
            ThongBao("Không tìm thấy khuyến mãi cần cập nhật.", "danger");
            return RedirectToAction(nameof(Index), null, null, "promotions");
        }

        khuyenMai.KichHoat = !khuyenMai.KichHoat;
        await _context.SaveChangesAsync();
        ThongBao(khuyenMai.KichHoat ? "Đã bật khuyến mãi." : "Đã tắt khuyến mãi.", "success");
        return RedirectToAction(nameof(Index), null, null, "promotions");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateKhuyenMai(KhuyenMai khuyenMai)
    {
        if (!CoQuyen("KhuyenMai.Tao")) return KhongCoQuyen();

        var khuyenMaiHienTai = await _context.KhuyenMais.FindAsync(khuyenMai.Id);
        if (khuyenMaiHienTai == null)
        {
            ThongBao("Không tìm thấy khuyến mãi cần sửa.", "danger");
            return RedirectToAction(nameof(Index), null, null, "promotions");
        }

        if (!string.IsNullOrWhiteSpace(khuyenMai.Ma) &&
            await _context.KhuyenMais.AnyAsync(k => k.Id != khuyenMai.Id && k.Ma == khuyenMai.Ma))
        {
            ModelState.AddModelError(nameof(KhuyenMai.Ma), "Mã khuyến mãi đã tồn tại.");
        }

        if (khuyenMai.NgayKetThuc < khuyenMai.NgayBatDau)
        {
            ModelState.AddModelError(nameof(KhuyenMai.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");
        }

        if (!ModelState.IsValid)
        {
            ThongBao("Không thể cập nhật khuyến mãi. Vui lòng kiểm tra lại thông tin.", "danger");
            return RedirectToAction(nameof(Index), null, null, "promotions");
        }

        khuyenMaiHienTai.Ten = khuyenMai.Ten;
        khuyenMaiHienTai.Ma = khuyenMai.Ma;
        khuyenMaiHienTai.PhanTramGiam = khuyenMai.PhanTramGiam;
        khuyenMaiHienTai.NgayBatDau = khuyenMai.NgayBatDau;
        khuyenMaiHienTai.NgayKetThuc = khuyenMai.NgayKetThuc;
        khuyenMaiHienTai.KichHoat = khuyenMai.KichHoat;

        await _context.SaveChangesAsync();
        ThongBao("Đã cập nhật khuyến mãi.", "success");
        return RedirectToAction(nameof(Index), null, null, "promotions");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatQuyenNhanVien(int id, string[] quyens)
    {
        if (!CoQuyen("NhanVien.PhanQuyen")) return KhongCoQuyen();

        var nhanVien = await _context.NhanViens.FindAsync(id);
        if (nhanVien == null)
        {
            ThongBao("Khong tim thay nhan vien can phan quyen.", "danger");
            return RedirectToAction(nameof(Index), null, null, "staff");
        }

        var user = await TimTaiKhoanNhanVien(nhanVien);
        if (user == null)
        {
            await TaoHoacCapNhatTaiKhoanNhanVien(nhanVien, null);
            user = await TimTaiKhoanNhanVien(nhanVien);
        }

        if (user == null)
        {
            ThongBao("Chua tao duoc tai khoan dang nhap cho nhan vien nay.", "danger");
            return RedirectToAction(nameof(Index), null, null, "staff");
        }

        var quyensHopLe = quyens
            .Where(LaQuyenHopLe)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (LaQuanLy(nhanVien.ChucVu))
        {
            quyensHopLe = DanhSachQuyen.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        await CapNhatPermissionClaims(user, quyensHopLe);
        ThongBao("Da cap nhat quyen cho nhan vien.", "success");
        return RedirectToAction(nameof(Index), null, null, "staff");
    }

    private async Task<bool> TaoHoacCapNhatTaiKhoanNhanVien(NhanVienModel nhanVien, string? matKhau)
    {
        var vaiTro = LaQuanLy(nhanVien.ChucVu) ? "QuanLy" : "NhanVien";
        await DamBaoRoleClaims(vaiTro);
        var user = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.NhanVienId == nhanVien.Id ||
            (!string.IsNullOrWhiteSpace(nhanVien.Email) && u.Email == nhanVien.Email && u.LoaiTaiKhoan != "KhachHang") ||
            (!string.IsNullOrWhiteSpace(nhanVien.DienThoai) && u.PhoneNumber == nhanVien.DienThoai && u.LoaiTaiKhoan != "KhachHang"));

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = $"nhanvien-{nhanVien.Id}",
                HoTen = nhanVien.HoTen,
                Email = nhanVien.Email,
                PhoneNumber = nhanVien.DienThoai,
                LoaiTaiKhoan = vaiTro,
                NhanVienId = nhanVien.Id
            };

            var taoTaiKhoan = await _userManager.CreateAsync(user, string.IsNullOrWhiteSpace(matKhau) ? "123456" : matKhau);
            if (!taoTaiKhoan.Succeeded)
            {
                return false;
            }
        }
        else
        {
            user.HoTen = nhanVien.HoTen;
            user.Email = nhanVien.Email;
            user.PhoneNumber = nhanVien.DienThoai;
            user.LoaiTaiKhoan = vaiTro;
            user.NhanVienId = nhanVien.Id;
            var capNhat = await _userManager.UpdateAsync(user);
            if (!capNhat.Succeeded)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(matKhau))
            {
                if (await _userManager.HasPasswordAsync(user))
                {
                    var xoaMatKhau = await _userManager.RemovePasswordAsync(user);
                    if (!xoaMatKhau.Succeeded)
                    {
                        return false;
                    }
                }

                var datMatKhau = await _userManager.AddPasswordAsync(user, matKhau);
                if (!datMatKhau.Succeeded)
                {
                    return false;
                }
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles.Where(r => r != vaiTro))
        {
            await _userManager.RemoveFromRoleAsync(user, role);
        }

        if (!await _userManager.IsInRoleAsync(user, vaiTro))
        {
            await _userManager.AddToRoleAsync(user, vaiTro);
        }

        var claims = await _userManager.GetClaimsAsync(user);
        foreach (var claim in claims.Where(c => c.Type == "ChucVu").ToList())
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        var claimValue = vaiTro == "QuanLy" ? "QuanLy" : ChuanHoaClaimNhanVien(nhanVien.ChucVu);
        await _userManager.AddClaimAsync(user, new Claim("ChucVu", claimValue));
        var permissionClaims = claims.Where(c => c.Type == "Permission").Select(c => c.Value).Where(LaQuyenHopLe).ToArray();
        if (vaiTro == "QuanLy" || permissionClaims.Length == 0)
        {
            await CapNhatPermissionClaims(user, QuyenMacDinh(nhanVien.ChucVu));
        }

        nhanVien.MatKhauHash = user.PasswordHash;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task DamBaoRoleClaims(string vaiTro)
    {
        var role = await _roleManager.FindByNameAsync(vaiTro);
        if (role == null) return;

        var canCoClaims = vaiTro == "QuanLy"
            ? DanhSachQuyen.Keys
            : new[] { "BanHang.TaoDon", "BanHang.TraCuuSanPham", "HoaDon.Xem" };

        var claims = await _roleManager.GetClaimsAsync(role);
        foreach (var claimValue in canCoClaims)
        {
            if (!claims.Any(c => c.Type == "Permission" && c.Value == claimValue))
            {
                await _roleManager.AddClaimAsync(role, new Claim("Permission", claimValue));
            }
        }
    }

    private static string ChuanHoaClaimNhanVien(string? chucVu)
    {
        var text = (chucVu ?? string.Empty).Trim().ToLowerInvariant();
        if (text.Contains("thu")) return "ThuNgan";
        if (text.Contains("pha")) return "PhaChe";
        if (text.Contains("kho")) return "Kho";
        return "NhanVien";
    }

    private async Task<ApplicationUser?> TimTaiKhoanNhanVien(NhanVienModel nhanVien)
    {
        return await _userManager.Users.FirstOrDefaultAsync(u =>
            u.LoaiTaiKhoan != "KhachHang" &&
            (u.NhanVienId == nhanVien.Id ||
             (!string.IsNullOrWhiteSpace(nhanVien.Email) && u.Email == nhanVien.Email) ||
             (!string.IsNullOrWhiteSpace(nhanVien.DienThoai) && u.PhoneNumber == nhanVien.DienThoai)));
    }

    private async Task CapNhatPermissionClaims(ApplicationUser user, IEnumerable<string> quyens)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        foreach (var claim in claims.Where(c => c.Type == "Permission").ToList())
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        foreach (var quyen in quyens.Where(LaQuyenHopLe).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await _userManager.AddClaimAsync(user, new Claim("Permission", quyen));
        }
    }

    private static HashSet<string> QuyenMacDinh(string? chucVu)
    {
        if (LaQuanLy(chucVu))
        {
            return DanhSachQuyen.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return new[] { "BanHang.TaoDon", "BanHang.TraCuuSanPham", "HoaDon.Xem" }
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool LaQuyenHopLe(string quyen)
    {
        return DanhSachQuyen.ContainsKey(quyen);
    }

    private static bool LaQuanLy(string? chucVu)
    {
        var text = (chucVu ?? string.Empty).Trim().ToLowerInvariant();
        return text.Contains("quan") || text.Contains("quản");
    }

    private void ThongBao(string noiDung, string loai)
    {
        TempData["QuanLyMessage"] = noiDung;
        TempData["QuanLyMessageType"] = loai;
        TempData["SanPhamMessage"] = noiDung;
        TempData["SanPhamMessageType"] = loai;
    }

    private bool LaQuanLy()
    {
        return HttpContext.Session.GetString("VaiTro") == "QuanLy";
    }

    private bool CoQuyen(string quyen)
    {
        if (LaQuanLy())
        {
            return true;
        }

        var quyens = HttpContext.Session.GetString("QuyenNhanVien");
        return !string.IsNullOrWhiteSpace(quyens) &&
            quyens.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(quyen, StringComparer.OrdinalIgnoreCase);
    }

    private bool CoQuyenBatKy()
    {
        return LaQuanLy() || LayQuyenHienTai().Any();
    }

    private HashSet<string> LayQuyenHienTai()
    {
        if (LaQuanLy())
        {
            return DanhSachQuyen.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var quyens = HttpContext.Session.GetString("QuyenNhanVien");
        if (string.IsNullOrWhiteSpace(quyens))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return quyens.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(LaQuyenHopLe)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private IActionResult KhongCoQuyen()
    {
        ThongBao("Ban khong co quyen thuc hien thao tac nay.", "danger");
        return RedirectToAction(nameof(Index));
    }
}
