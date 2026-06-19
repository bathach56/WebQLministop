using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using WebQLministop.Data;
using WebQLministop.Models;
using KhachHangModel = WebQLministop.Models.KhachHang;

namespace WebQLministop.Areas.NhanVien.Controllers;

[Area("NhanVien")]
public class NhanVienController : Controller
{
    private static readonly string[] QuyenNhanVienHopLe =
    [
        "SanPham.Tao",
        "DanhMuc.QuanLy",
        "DonHang.QuanLy",
        "KhuyenMai.Tao",
        "HoaDon.Xem",
        "NhanVien.PhanQuyen",
        "BanHang.TaoDon",
        "BanHang.TraCuuSanPham"
    ];

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public NhanVienController(ApplicationDbContext context, IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _configuration = configuration;
        _userManager = userManager;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await DongBoQuyenNhanVienDangNhap();
        await next();
    }

    public async Task<IActionResult> Index()
    {
        if (!CoQuyenBatKy("BanHang.TaoDon", "BanHang.TraCuuSanPham", "HoaDon.Xem"))
        {
            return RedirectToAction("Index", "DangNhap", new { area = "KhachHang" });
        }

        ViewBag.SanPhams = await _context.SanPhams
            .Include(s => s.DanhMuc)
            .Where(s => s.KichHoat && (s.DanhMuc == null || s.DanhMuc.KichHoat))
            .OrderBy(s => s.Ten)
            .ToListAsync();

        ViewBag.KhachHangs = await _context.KhachHangs
            .Where(k => k.KichHoat && k.HoTen != "Khach le")
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
        if (!CoQuyen("BanHang.TraCuuSanPham"))
        {
            return Unauthorized();
        }

        var keyword = (tuKhoa ?? string.Empty).Trim();
        if (keyword.Length == 0)
        {
            return Json(Array.Empty<object>());
        }

        var sanPhams = await _context.SanPhams
            .Include(s => s.DanhMuc)
            .Where(s => s.KichHoat && (s.DanhMuc == null || s.DanhMuc.KichHoat))
            .OrderBy(s => s.Ten)
            .ToListAsync();

        var keywordKhongDau = BoDau(keyword);
        var ketQua = sanPhams
            .Where(s => BoDau(s.Ma).Contains(keywordKhongDau) || BoDau(s.Ten).Contains(keywordKhongDau))
            .Take(10)
            .Select(s => new
            {
                s.Id,
                s.Ma,
                s.Ten,
                s.GiaBan,
                s.TonKho,
                s.DonVi,
                HinhAnh = string.IsNullOrWhiteSpace(s.HinhAnh) ? "https://images.unsplash.com/photo-1601598851547-4302969d0614?auto=format&fit=crop&w=600&q=80" : s.HinhAnh,
                DanhMuc = HienThiDanhMuc(s.DanhMuc?.Ten)
            })
            .ToList();

        return Json(ketQua);
    }

    [HttpGet]
    public async Task<IActionResult> TimKhachHang(string? ma)
    {
        if (!CoQuyen("BanHang.TaoDon")) return Unauthorized();

        var search = (ma ?? "").Trim();
        if (string.IsNullOrEmpty(search)) return Json(new { thanhCong = false, thongBao = "Vui lòng nhập mã." });

        // Tách số từ mã "KH-0000" hoặc "0000"
        var parsedId = search.Replace("KH-", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (int.TryParse(parsedId, out int khId))
        {
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Id == khId && k.KichHoat);
            if (khachHang != null)
            {
                return Json(new { thanhCong = true, id = khachHang.Id, hoTen = khachHang.HoTen, diemThuong = khachHang.DiemThuong });
            }
        }

        return Json(new { thanhCong = false, thongBao = "Không tìm thấy khách hàng." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoDonTaiQuay([FromBody] TaoDonTaiQuayRequest request)
    {
        if (!CoQuyen("BanHang.TaoDon"))
        {
            return Unauthorized(new { thanhCong = false, thongBao = "Vui lòng đăng nhập bằng tài khoản nhân viên." });
        }

        if (request.SanPhams.Count == 0)
        {
            return BadRequest(new { thanhCong = false, thongBao = "Hóa đơn chưa có sản phẩm." });
        }

        var sanPhamIds = request.SanPhams.Select(i => i.SanPhamId).Distinct().ToList();
        var sanPhams = await _context.SanPhams
            .Include(s => s.DanhMuc)
            .Where(s => sanPhamIds.Contains(s.Id) && s.KichHoat)
            .Where(s => s.DanhMuc == null || s.DanhMuc.KichHoat)
            .ToDictionaryAsync(s => s.Id);

        var chiTietDonHang = new List<ChiTietDonHang>();
        foreach (var item in request.SanPhams)
        {
            if (item.SoLuong <= 0)
            {
                return BadRequest(new { thanhCong = false, thongBao = "Số lượng sản phẩm không hợp lệ." });
            }

            if (!sanPhams.TryGetValue(item.SanPhamId, out var sanPham))
            {
                return BadRequest(new { thanhCong = false, thongBao = "Có sản phẩm không còn hoạt động." });
            }

            if (sanPham.TonKho < item.SoLuong)
            {
                return BadRequest(new { thanhCong = false, thongBao = $"{sanPham.Ten} chỉ còn {sanPham.TonKho} {sanPham.DonVi}." });
            }

            sanPham.TonKho -= item.SoLuong;
            chiTietDonHang.Add(new ChiTietDonHang
            {
                SanPhamId = sanPham.Id,
                MaSanPham = sanPham.Ma,
                TenSanPham = sanPham.Ten,
                DonViSanPham = sanPham.DonVi,
                SoLuong = item.SoLuong,
                DonGia = sanPham.GiaBan,
                TienGiam = 0m
            });
        }

        var nhanVienId = await LayNhanVienLapDon(request.NhanVienId);
        if (nhanVienId == null)
        {
            return BadRequest(new { thanhCong = false, thongBao = "Không tìm thấy nhân viên lập hóa đơn." });
        }

        var khachHang = await LayKhachHangChoDonTaiQuay(request.KhachHangId);
        var tongTienGoc = chiTietDonHang.Sum(i => i.SoLuong * i.DonGia - i.TienGiam);
        
        // Xử lý điểm sử dụng
        var tienGiamTuDiem = 0m;
        if (request.DiemSuDung > 0 && khachHang.HoTen != "Khach le")
        {
            if (khachHang.DiemThuong < request.DiemSuDung)
            {
                return BadRequest(new { thanhCong = false, thongBao = "Không đủ điểm tích lũy." });
            }
            
            tienGiamTuDiem = request.DiemSuDung * 1m; // 1 điểm = 1 vnđ
            if (tienGiamTuDiem > tongTienGoc)
            {
                tienGiamTuDiem = tongTienGoc;
                request.DiemSuDung = (int)tongTienGoc;
            }
            
            khachHang.DiemThuong -= request.DiemSuDung;
        }

        var tongSauGiam = tongTienGoc - tienGiamTuDiem;
        var diemCong = khachHang.HoTen == "Khach le" ? 0 : TinhDiemCong(tongSauGiam);

        if (diemCong > 0)
        {
            khachHang.DiemThuong += diemCong;
        }

        var phuongThuc = string.Equals(request.PhuongThucThanhToan, "ChuyenKhoan", StringComparison.OrdinalIgnoreCase) 
            ? "ChuyenKhoan" 
            : "TienMat";
        var trangThai = phuongThuc == "ChuyenKhoan" ? "DangXuLy" : "DaThanhToan";

        var donHang = new DonHang
        {
            KhachHangId = khachHang.Id,
            NhanVienId = nhanVienId.Value,
            KenhBanHang = "TaiQuay",
            DiaChiGiaoHang = "Tại quầy",
            NgayDat = DateTime.UtcNow,
            TrangThai = trangThai,
            PhuongThucThanhToan = phuongThuc,
            TongTien = tongSauGiam,
            DiemThuongSuDung = request.DiemSuDung,
            DiemThuongCong = diemCong,
            ChiTiet = chiTietDonHang
        };

        _context.DonHangs.Add(donHang);
        await _context.SaveChangesAsync();

        var qrUrl = "";
        var noiDung = "";
        if (phuongThuc == "ChuyenKhoan")
        {
            var bank = _configuration["SePay:Bank"] ?? "BIDV";
            var accountNumber = _configuration["SePay:VirtualAccountNumber"] ?? _configuration["SePay:AccountNumber"] ?? "";
            var accountHolder = _configuration["SePay:AccountHolder"] ?? "";
            noiDung = $"DH{donHang.Id:000000}";
            qrUrl = "https://qr.sepay.vn/img"
                + $"?bank={Uri.EscapeDataString(bank)}"
                + $"&acc={Uri.EscapeDataString(accountNumber)}"
                + $"&amount={(long)donHang.TongTien}"
                + $"&des={Uri.EscapeDataString(noiDung)}"
                + $"&accountName={Uri.EscapeDataString(accountHolder)}"
                + "&template=compact";
        }

        return Json(new
        {
            thanhCong = true,
            maDonHang = donHang.Id,
            tongTien = donHang.TongTien,
            diemCong,
            qrUrl,
            noiDung,
            thongBao = diemCong > 0
                ? $"Đã tạo hóa đơn #{donHang.Id}. Khách được cộng {diemCong:N0} điểm tích lũy."
                : $"Đã tạo hóa đơn #{donHang.Id}."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KiemTraThanhToanSePay(int donHangId)
    {
        if (!LaNhanVienHoacQuanLy()) return Unauthorized(new { thanhCong = false, thongBao = "Không có quyền kiểm tra." });

        var donHang = await _context.DonHangs.Include(d => d.KhachHang).FirstOrDefaultAsync(d => d.Id == donHangId);
        if (donHang == null) return Json(new { thanhCong = false, thongBao = "Không tìm thấy hóa đơn." });
        if (donHang.TrangThai == "DaThanhToan") return Json(new { thanhCong = true, thongBao = "Đã thanh toán." });

        var apiToken = _configuration["SePay:ApiToken"];
        if (string.IsNullOrWhiteSpace(apiToken)) return Json(new { thanhCong = false, thongBao = "Chưa cấu hình SePAY API token." });

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
            var url = "https://my.sepay.vn/userapi/transactions/list?limit=50";
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return Json(new { thanhCong = false, thongBao = $"Lỗi gọi API SePAY: {(int)response.StatusCode}." });

            var json = await response.Content.ReadAsStringAsync();
            using var document = System.Text.Json.JsonDocument.Parse(json);
            var noiDung = $"DH{donHang.Id:000000}";
            
            if (CoGiaoDichKhop(document.RootElement, noiDung, donHang.TongTien))
            {
                donHang.TrangThai = "DaThanhToan";
                donHang.GhiChuThanhToan = $"{donHang.GhiChuThanhToan} Xác nhận bởi SePAY tại quầy.";
                if (donHang.KhachHang != null && donHang.DiemThuongCong > 0)
                {
                    donHang.KhachHang.DiemThuong += donHang.DiemThuongCong;
                }
                await _context.SaveChangesAsync();
                return Json(new { thanhCong = true, thongBao = "Thanh toán thành công. Đang in hóa đơn..." });
            }

            return Json(new { thanhCong = false, thongBao = "Chưa nhận được thanh toán. Vui lòng đợi..." });
        }
        catch
        {
            return Json(new { thanhCong = false, thongBao = "Lỗi kết nối đến SePAY." });
        }
    }

    private static bool CoGiaoDichKhop(System.Text.Json.JsonElement element, string maDonHang, decimal tongTien)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var content = "";
            var amount = 0m;
            var type = "";

            foreach (var property in element.EnumerateObject())
            {
                var name = property.Name.ToLowerInvariant();
                if (name is "content" or "description" or "transaction_content") content = property.Value.GetString() ?? "";
                else if (name is "transferamount" or "transfer_amount" or "amount_in" or "amount" or "money")
                {
                    if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Number && property.Value.TryGetDecimal(out var n)) amount = n;
                    else if (property.Value.ValueKind == System.Text.Json.JsonValueKind.String && decimal.TryParse(property.Value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var n2)) amount = n2;
                }
                else if (name is "transfertype" or "transfer_type" or "type") type = property.Value.GetString() ?? "";
            }

            if (!string.IsNullOrWhiteSpace(content) &&
                content.Contains(maDonHang, StringComparison.OrdinalIgnoreCase) &&
                amount >= tongTien &&
                (string.IsNullOrWhiteSpace(type) || type.Equals("in", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (CoGiaoDichKhop(property.Value, maDonHang, tongTien)) return true;
            }
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (CoGiaoDichKhop(item, maDonHang, tongTien)) return true;
            }
        }
        return false;
    }

    private async Task<int?> LayNhanVienLapDon(int? nhanVienIdDuocChon)
    {
        if (nhanVienIdDuocChon != null &&
            await _context.NhanViens.AnyAsync(n => n.Id == nhanVienIdDuocChon.Value && n.KichHoat))
        {
            return nhanVienIdDuocChon.Value;
        }

        var nhanVienIdSession = HttpContext.Session.GetInt32("NhanVienId");
        if (nhanVienIdSession != null &&
            await _context.NhanViens.AnyAsync(n => n.Id == nhanVienIdSession.Value && n.KichHoat))
        {
            return nhanVienIdSession.Value;
        }

        return await _context.NhanViens
            .Where(n => n.KichHoat)
            .OrderBy(n => n.Id)
            .Select(n => (int?)n.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<KhachHangModel> LayKhachHangChoDonTaiQuay(int? khachHangId)
    {
        if (khachHangId != null)
        {
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Id == khachHangId.Value && k.KichHoat);
            if (khachHang != null)
            {
                return khachHang;
            }
        }

        var khachLe = await _context.KhachHangs.FirstOrDefaultAsync(k =>
            k.HoTen == "Khach le" &&
            k.Email == null &&
            k.DienThoai == null);

        if (khachLe != null)
        {
            return khachLe;
        }

        khachLe = new KhachHangModel
        {
            HoTen = "Khach le",
            KichHoat = true
        };
        _context.KhachHangs.Add(khachLe);
        await _context.SaveChangesAsync();
        return khachLe;
    }

    private static int TinhDiemCong(decimal tongTienGoc)
    {
        return (int)Math.Floor(tongTienGoc / 100m);
    }

    private static string HienThiDanhMuc(string? ten)
    {
        return ten switch
        {
            "Do uong" => "Đồ uống",
            "Banh keo" => "Bánh kẹo",
            "Thuc pham" => "Thực phẩm",
            "Gia dung" => "Gia dụng",
            _ => string.IsNullOrWhiteSpace(ten) ? "Khác" : ten
        };
    }

    private bool LaNhanVienHoacQuanLy()
    {
        return CoQuyen("BanHang.TaoDon");
    }

    private bool CoQuyen(string quyen)
    {
        if (HttpContext.Session.GetString("VaiTro") == "QuanLy")
        {
            return true;
        }

        if (HttpContext.Session.GetString("VaiTro") != "NhanVien")
        {
            return false;
        }

        var quyens = HttpContext.Session.GetString("QuyenNhanVien");
        return !string.IsNullOrWhiteSpace(quyens) &&
            quyens.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(quyen, StringComparer.OrdinalIgnoreCase);
    }

    private bool CoQuyenBatKy(params string[] quyens)
    {
        return quyens.Any(CoQuyen);
    }

    private async Task DongBoQuyenNhanVienDangNhap()
    {
        var nhanVienId = HttpContext.Session.GetInt32("NhanVienId");
        if (nhanVienId == null)
        {
            return;
        }

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.NhanVienId == nhanVienId.Value && u.LoaiTaiKhoan != "KhachHang");

        if (user == null)
        {
            return;
        }

        var vaiTro = string.Equals(user.LoaiTaiKhoan, "QuanLy", StringComparison.OrdinalIgnoreCase)
            ? "QuanLy"
            : "NhanVien";

        var claims = await _userManager.GetClaimsAsync(user);
        var quyens = claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .Where(q => QuyenNhanVienHopLe.Contains(q, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        HttpContext.Session.SetString("VaiTro", vaiTro);
        HttpContext.Session.SetString("QuyenNhanVien", string.Join(",", quyens));
    }

    [HttpGet]
    public async Task<IActionResult> InHoaDon(int id)
    {
        if (!CoQuyen("HoaDon.Xem")) return Unauthorized();

        var donHang = await _context.DonHangs
            .Include(d => d.KhachHang)
            .Include(d => d.NhanVien)
            .Include(d => d.ChiTiet)
                .ThenInclude(c => c.SanPham)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (donHang == null)
        {
            return NotFound("Không tìm thấy hóa đơn.");
        }

        return View(donHang);
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

public class TaoDonTaiQuayRequest
{
    public int? KhachHangId { get; set; }
    public int? NhanVienId { get; set; }
    public string? PhuongThucThanhToan { get; set; }
    public int DiemSuDung { get; set; }
    public List<TaoDonTaiQuayItem> SanPhams { get; set; } = new();
}

public class TaoDonTaiQuayItem
{
    public int SanPhamId { get; set; }
    public int SoLuong { get; set; }
}
