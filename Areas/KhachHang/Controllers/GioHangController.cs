using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using WebQLministop.Data;
using WebQLministop.Models;

namespace WebQLministop.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    public class GioHangController : Controller
    {
        private const string SessionKhuyenMaiId = "KhuyenMaiId";
        private const string SessionDiemThuongSuDung = "DiemThuongSuDung";
        private const decimal GiaTriMoiDiem = 1m;
        private const decimal SoTienMoiDiemThuong = 100m;
        private const int ThoiGianChoThanhToanPhut = 5;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GioHangController> _logger;

        public GioHangController(ApplicationDbContext context, IConfiguration configuration, ILogger<GioHangController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var khachHangId = HttpContext.Session.GetInt32("KhachHangId");
            if (khachHangId == null)
            {
                TempData["ThongBaoDangNhap"] = "Vui lòng đăng nhập để xem giỏ hàng.";
                return RedirectToAction("Index", "DangNhap");
            }

            var gioHang = await LayGioHang(khachHangId.Value);
            var khuyenMai = await LayKhuyenMaiDangApDung();
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Id == khachHangId.Value && k.KichHoat);
            var tongSauKhuyenMai = TinhTongSauKhuyenMai(gioHang, khuyenMai);
            await HuyDonSePayQuaHan(khachHangId.Value);
            var donHangSePay = await LayDonSePayDangCho(khachHangId.Value);

            ViewBag.KhuyenMai = khuyenMai;
            ViewBag.DiemThuong = khachHang?.DiemThuong ?? 0;
            ViewBag.DiemThuongSuDung = LayDiemThuongSuDungHopLe(khachHang?.DiemThuong ?? 0, tongSauKhuyenMai);
            ViewBag.GiaTriMoiDiem = GiaTriMoiDiem;
            ViewBag.DonHangSePay = donHangSePay;
            ViewBag.SePayQrUrl = donHangSePay == null ? null : TaoSePayQrUrl(donHangSePay);
            ViewBag.SePayNoiDung = donHangSePay == null ? null : TaoNoiDungChuyenKhoan(donHangSePay.Id);
            ViewBag.SePayHetHanUtc = donHangSePay?.NgayDat.AddMinutes(ThoiGianChoThanhToanPhut);
            return View(gioHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Them(int sanPhamId)
        {
            var khachHangId = HttpContext.Session.GetInt32("KhachHangId");
            if (khachHangId == null)
            {
                return Json(new { thanhCong = false, canDangNhap = true, thongBao = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng." });
            }

            var sanPham = await _context.SanPhams
                .Include(p => p.DanhMuc)
                .FirstOrDefaultAsync(p => p.Id == sanPhamId && p.KichHoat && (p.DanhMuc == null || p.DanhMuc.KichHoat));
            if (sanPham == null)
            {
                return Json(new { thanhCong = false, thongBao = "Sản phẩm không tồn tại hoặc đã ngừng bán." });
            }

            if (sanPham.TonKho <= 0)
            {
                return Json(new { thanhCong = false, thongBao = "Sản phẩm đã hết hàng." });
            }

            var gioHang = await LayHoacTaoGioHang(khachHangId.Value);
            var chiTiet = await _context.ChiTietGioHangs.FirstOrDefaultAsync(c => c.GioHangId == gioHang.Id && c.SanPhamId == sanPhamId);

            if (chiTiet == null)
            {
                _context.ChiTietGioHangs.Add(new ChiTietGioHang
                {
                    GioHangId = gioHang.Id,
                    SanPhamId = sanPhamId,
                    SoLuong = 1
                });
            }
            else
            {
                if (chiTiet.SoLuong >= sanPham.TonKho)
                {
                    return Json(new { thanhCong = false, thongBao = "Số lượng trong giỏ đã bằng tồn kho hiện có." });
                }

                chiTiet.SoLuong += 1;
            }

            gioHang.NgayCapNhat = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { thanhCong = true, thongBao = "Đã thêm sản phẩm vào giỏ hàng.", soLuong = await DemSoLuong(khachHangId.Value) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhat(int chiTietId, int soLuong)
        {
            var chiTiet = await LayChiTietCuaKhachHang(chiTietId);
            if (chiTiet == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (soLuong <= 0)
            {
                _context.ChiTietGioHangs.Remove(chiTiet);
            }
            else
            {
                chiTiet.SoLuong = Math.Min(soLuong, chiTiet.SanPham?.TonKho ?? soLuong);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Xoa(int chiTietId)
        {
            var chiTiet = await LayChiTietCuaKhachHang(chiTietId);
            if (chiTiet != null)
            {
                _context.ChiTietGioHangs.Remove(chiTiet);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaTatCa()
        {
            var khachHangId = HttpContext.Session.GetInt32("KhachHangId");
            if (khachHangId != null)
            {
                var gioHang = await LayGioHang(khachHangId.Value);
                _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTiet);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApDungKhuyenMai(string? maKhuyenMai)
        {
            var khachHangId = HttpContext.Session.GetInt32("KhachHangId");
            if (khachHangId == null)
            {
                TempData["ThongBaoDangNhap"] = "Vui lòng đăng nhập để áp dụng mã giảm giá.";
                return RedirectToAction("Index", "DangNhap");
            }

            maKhuyenMai = maKhuyenMai?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(maKhuyenMai))
            {
                TempData["ThongBaoGioHang"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToAction(nameof(Index));
            }

            var hienTai = DateTime.UtcNow;
            var khuyenMai = await _context.KhuyenMais.FirstOrDefaultAsync(k =>
                k.KichHoat &&
                k.Ma == maKhuyenMai &&
                k.NgayBatDau <= hienTai &&
                k.NgayKetThuc >= hienTai);

            if (khuyenMai == null)
            {
                HttpContext.Session.Remove(SessionKhuyenMaiId);
                TempData["ThongBaoGioHang"] = "Mã giảm giá không tồn tại, đã hết hạn hoặc chưa được kích hoạt.";
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.SetInt32(SessionKhuyenMaiId, khuyenMai.Id);
            TempData["ThongBaoGioHang"] = $"Đã áp dụng mã {khuyenMai.Ma}, giảm {khuyenMai.PhanTramGiam:N0}%.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BoKhuyenMai()
        {
            HttpContext.Session.Remove(SessionKhuyenMaiId);
            TempData["ThongBaoGioHang"] = "Đã bỏ mã giảm giá khỏi giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApDungDiemThuong(int diemSuDung)
        {
            var khachHangId = HttpContext.Session.GetInt32("KhachHangId");
            if (khachHangId == null)
            {
                TempData["ThongBaoDangNhap"] = "Vui lòng đăng nhập để sử dụng điểm tích lũy.";
                return RedirectToAction("Index", "DangNhap");
            }

            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Id == khachHangId.Value && k.KichHoat);
            if (khachHang == null)
            {
                TempData["ThongBaoDangNhap"] = "Vui lòng đăng nhập lại để sử dụng điểm tích lũy.";
                return RedirectToAction("Index", "DangNhap");
            }

            var gioHang = await LayGioHang(khachHangId.Value);
            if (!gioHang.ChiTiet.Any())
            {
                HttpContext.Session.Remove(SessionDiemThuongSuDung);
                TempData["ThongBaoGioHang"] = "Giỏ hàng đang trống, chưa thể dùng điểm tích lũy.";
                return RedirectToAction(nameof(Index));
            }

            var tongSauKhuyenMai = TinhTongSauKhuyenMai(gioHang, await LayKhuyenMaiDangApDung());
            var diemToiDa = Math.Min(khachHang.DiemThuong, (int)Math.Floor(tongSauKhuyenMai / GiaTriMoiDiem));
            if (diemToiDa <= 0)
            {
                HttpContext.Session.Remove(SessionDiemThuongSuDung);
                TempData["ThongBaoGioHang"] = "Bạn chưa có điểm tích lũy khả dụng cho đơn hàng này.";
                return RedirectToAction(nameof(Index));
            }

            var diemHopLe = Math.Clamp(diemSuDung, 1, diemToiDa);
            HttpContext.Session.SetInt32(SessionDiemThuongSuDung, diemHopLe);
            TempData["ThongBaoGioHang"] = $"Đã áp dụng {diemHopLe:N0} điểm tích lũy, giảm {(diemHopLe * GiaTriMoiDiem):N0}đ.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BoDiemThuong()
        {
            HttpContext.Session.Remove(SessionDiemThuongSuDung);
            TempData["ThongBaoGioHang"] = "Đã bỏ điểm tích lũy khỏi giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhan(string? diaChiGiaoHang, string? phuongThucThanhToan)
        {
            var khachHangId = HttpContext.Session.GetInt32("KhachHangId");
            if (khachHangId == null)
            {
                TempData["ThongBaoDangNhap"] = "Vui lòng đăng nhập để tạo đơn hàng.";
                return RedirectToAction("Index", "DangNhap");
            }

            diaChiGiaoHang = diaChiGiaoHang?.Trim();
            phuongThucThanhToan = ChuanHoaPhuongThucThanhToan(phuongThucThanhToan);
            if (string.IsNullOrWhiteSpace(diaChiGiaoHang))
            {
                TempData["ThongBaoGioHang"] = "Vui lòng nhập địa chỉ giao hàng trước khi tạo đơn.";
                return RedirectToAction(nameof(Index));
            }

            var gioHang = await _context.GioHangs
                .Include(g => g.ChiTiet)
                .ThenInclude(c => c.SanPham)
                .ThenInclude(p => p!.DanhMuc)
                .FirstOrDefaultAsync(g => g.KhachHangId == khachHangId.Value);

            if (gioHang == null || !gioHang.ChiTiet.Any())
            {
                TempData["ThongBaoGioHang"] = "Giỏ hàng đang trống, chưa thể tạo đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            await HuyDonSePayQuaHan(khachHangId.Value);
            var donHangDangCho = await LayDonSePayDangCho(khachHangId.Value);
            if (donHangDangCho != null)
            {
                TempData["ThongBaoGioHang"] = "Bạn đang có đơn hàng chờ quét mã thanh toán QR SePAY. Vui lòng hoàn tất hoặc hủy đơn cũ trước khi tạo đơn mới.";
                return RedirectToAction(nameof(Index));
            }

            // Bỏ đi đoạn gán nhân viên mặc định vì giờ Đơn Web sẽ dùng NhanVienId = null
            foreach (var item in gioHang.ChiTiet)
            {
                if (item.SanPham == null || !item.SanPham.KichHoat || item.SanPham.DanhMuc?.KichHoat == false)
                {
                    TempData["ThongBaoGioHang"] = "Giỏ hàng có sản phẩm không còn bán. Vui lòng kiểm tra lại.";
                    return RedirectToAction(nameof(Index));
                }

                if (item.SoLuong > item.SanPham.TonKho)
                {
                    TempData["ThongBaoGioHang"] = $"Sản phẩm {item.SanPham.Ten} chỉ còn {item.SanPham.TonKho}. Vui lòng chỉnh số lượng.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var khuyenMai = await LayKhuyenMaiDangApDung();
            if (HttpContext.Session.GetInt32(SessionKhuyenMaiId) != null && khuyenMai == null)
            {
                HttpContext.Session.Remove(SessionKhuyenMaiId);
                TempData["ThongBaoGioHang"] = "Mã giảm giá đã hết hạn hoặc không còn hoạt động. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Index));
            }

            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Id == khachHangId.Value && k.KichHoat);
            if (khachHang == null)
            {
                TempData["ThongBaoDangNhap"] = "Vui lòng đăng nhập lại để tạo đơn hàng.";
                return RedirectToAction("Index", "DangNhap");
            }

            var phanTramGiam = khuyenMai?.PhanTramGiam ?? 0m;
            var tongTienGoc = gioHang.ChiTiet.Sum(item => item.SoLuong * (item.SanPham?.GiaBan ?? 0m));
            var tongSauKhuyenMai = TinhTongSauKhuyenMai(gioHang, khuyenMai);
            var diemSuDung = LayDiemThuongSuDungHopLe(khachHang.DiemThuong, tongSauKhuyenMai);
            var tienGiamTuDiem = diemSuDung * GiaTriMoiDiem;
            var donHang = new DonHang
            {
                KhachHangId = khachHangId.Value,
                NhanVienId = null,
                KenhBanHang = "Web",
                NgayDat = DateTime.UtcNow,
                TrangThai = phuongThucThanhToan == "ChuyenKhoan" ? "DangXuLy" : "DaThanhToan",
                PhuongThucThanhToan = phuongThucThanhToan,
                DiaChiGiaoHang = diaChiGiaoHang,
                GhiChuThanhToan = TaoGhiChuThanhToan(phuongThucThanhToan),
                TongTien = 0m,
                DiemThuongSuDung = diemSuDung,
                TienGiamDiem = tienGiamTuDiem,
                MaKhuyenMaiDaDung = khuyenMai?.Ma,
                ChiTiet = gioHang.ChiTiet.Select(item => new ChiTietDonHang
                {
                    SanPhamId = item.SanPhamId,
                    MaSanPham = item.SanPham?.Ma,
                    TenSanPham = item.SanPham?.Ten,
                    DonViSanPham = item.SanPham?.DonVi,
                    SoLuong = item.SoLuong,
                    DonGia = item.SanPham?.GiaBan ?? 0m,
                    TienGiam = Math.Round(item.SoLuong * (item.SanPham?.GiaBan ?? 0m) * phanTramGiam / 100m, 0)
                }).ToList()
            };

            ApDungTienGiamTuDiem(donHang.ChiTiet, tienGiamTuDiem);
            donHang.TongTien = donHang.ChiTiet.Sum(item => item.SoLuong * item.DonGia - item.TienGiam);
            donHang.TienGiamKhuyenMai = donHang.ChiTiet.Sum(item => Math.Round(item.SoLuong * item.DonGia * phanTramGiam / 100m, 0));
            var diemCong = (int)Math.Floor(tongTienGoc / SoTienMoiDiemThuong);
            donHang.DiemThuongCong = diemCong;
            khachHang.DiemThuong -= diemSuDung;
            if (phuongThucThanhToan != "ChuyenKhoan")
            {
                khachHang.DiemThuong += diemCong;
            }

            foreach (var item in gioHang.ChiTiet)
            {
                if (item.SanPham != null)
                {
                    item.SanPham.TonKho -= item.SoLuong;
                }
            }

            _context.DonHangs.Add(donHang);
            _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTiet);
            await _context.SaveChangesAsync();
            if (phuongThucThanhToan == "ChuyenKhoan")
            {
                donHang.GhiChuThanhToan = $"Khách chọn chuyển khoản QR SePAY. Nội dung chuyển khoản: {TaoNoiDungChuyenKhoan(donHang.Id)}. Đơn tự hủy sau {ThoiGianChoThanhToanPhut} phút nếu chưa thanh toán.";
                await _context.SaveChangesAsync();
            }
            HttpContext.Session.Remove(SessionKhuyenMaiId);
            HttpContext.Session.Remove(SessionDiemThuongSuDung);
            HttpContext.Session.SetInt32("KhachHangDiemThuong", khachHang.DiemThuong);

            TempData["ThongBaoGioHang"] = $"Đã tạo đơn hàng #{donHang.Id} thành công. Tổng tiền: {donHang.TongTien:N0}đ. Bạn được cộng {diemCong:N0} điểm tích lũy.";
            var thongBaoThanhToan = phuongThucThanhToan == "ChuyenKhoan"
                ? "Vui lòng quét mã QR SePAY và chờ nhân viên xác nhận tin nhắn giao dịch."
                : "Đơn sẽ được thu tiền mặt khi hàng giao đến.";
            TempData["ThongBaoGioHang"] = $"Đã tạo đơn hàng #{donHang.Id}. Tổng tiền: {donHang.TongTien:N0}đ. {thongBaoThanhToan} Bạn được cộng {diemCong:N0} điểm tích lũy.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KiemTraThanhToan(int donHangId)
        {
            var khachHangId = HttpContext.Session.GetInt32("KhachHangId");
            if (khachHangId == null)
            {
                return Json(new { thanhCong = false, trangThai = "CanDangNhap", thongBao = "Vui lòng đăng nhập để kiểm tra thanh toán." });
            }

            await HuyDonSePayQuaHan(khachHangId.Value);
            var donHang = await _context.DonHangs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTiet)
                .ThenInclude(c => c.SanPham)
                .FirstOrDefaultAsync(d => d.Id == donHangId && d.KhachHangId == khachHangId.Value);

            if (donHang == null)
            {
                return Json(new { thanhCong = false, trangThai = "KhongTimThay", thongBao = "Không tìm thấy đơn hàng cần kiểm tra." });
            }

            if (donHang.TrangThai == "DaThanhToan")
            {
                return Json(new { thanhCong = true, trangThai = donHang.TrangThai, thongBao = "Đơn hàng đã được xác nhận thanh toán." });
            }

            if (donHang.TrangThai == "DaHuy")
            {
                return Json(new { thanhCong = false, trangThai = donHang.TrangThai, thongBao = "Đơn hàng đã quá hạn thanh toán và bị hủy." });
            }

            var ketQuaSePay = await KiemTraGiaoDichSePay(donHang);
            if (ketQuaSePay.DaNhanTien)
            {
                await XacNhanDonHangDaThanhToan(donHang, "SePAY xác nhận giao dịch khi khách bấm kiểm tra thanh toán.");
                return Json(new { thanhCong = true, trangThai = donHang.TrangThai, thongBao = "Đã nhận được thanh toán. Đơn hàng đã chuyển sang trạng thái đã thanh toán." });
            }

            if (!string.IsNullOrWhiteSpace(ketQuaSePay.Loi))
            {
                return Json(new { thanhCong = false, trangThai = donHang.TrangThai, thongBao = ketQuaSePay.Loi });
            }

            var hetHanUtc = donHang.NgayDat.AddMinutes(ThoiGianChoThanhToanPhut);
            var conLaiGiay = Math.Max(0, (int)Math.Ceiling((hetHanUtc - DateTime.UtcNow).TotalSeconds));
            return Json(new { thanhCong = true, trangThai = donHang.TrangThai, conLaiGiay, thongBao = "Chưa nhận được thanh toán từ SePAY. Vui lòng thử lại sau vài giây." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDonSePay(int donHangId)
        {
            var khachHangId = HttpContext.Session.GetInt32("KhachHangId");
            if (khachHangId == null)
            {
                TempData["ThongBaoDangNhap"] = "Vui lòng đăng nhập để hủy đơn hàng.";
                return RedirectToAction("Index", "DangNhap");
            }

            var donHang = await _context.DonHangs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTiet)
                .ThenInclude(c => c.SanPham)
                .FirstOrDefaultAsync(d => d.Id == donHangId && d.KhachHangId == khachHangId.Value);

            if (donHang != null && donHang.TrangThai == "DangXuLy" && donHang.PhuongThucThanhToan == "ChuyenKhoan")
            {
                HuyDonHangChoThanhToan(donHang);
                donHang.GhiChuThanhToan = donHang.GhiChuThanhToan?.Replace($"Đơn đã tự hủy vì quá {ThoiGianChoThanhToanPhut} phút chưa nhận được thanh toán SePAY.", "Khách hàng chủ động hủy đơn trong lúc chờ quét mã SePAY.");
                await _context.SaveChangesAsync();
                
                if (donHang.KhachHang != null)
                {
                    HttpContext.Session.SetInt32("KhachHangDiemThuong", donHang.KhachHang.DiemThuong);
                }
                
                TempData["ThongBaoGioHang"] = $"Đã hủy đơn hàng #{donHang.Id} thành công. Bạn có thể tạo đơn hàng mới.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> SoLuong()
        {
            var khachHangId = HttpContext.Session.GetInt32("KhachHangId");
            var soLuong = khachHangId == null ? 0 : await DemSoLuong(khachHangId.Value);

            return Json(new { soLuong });
        }

        private async Task<GioHang> LayGioHang(int khachHangId)
        {
            return await _context.GioHangs
                .Include(g => g.ChiTiet)
                .ThenInclude(c => c.SanPham)
                .FirstOrDefaultAsync(g => g.KhachHangId == khachHangId)
                ?? new GioHang { KhachHangId = khachHangId };
        }

        private async Task<GioHang> LayHoacTaoGioHang(int khachHangId)
        {
            var gioHang = await _context.GioHangs.FirstOrDefaultAsync(g => g.KhachHangId == khachHangId);
            if (gioHang != null)
            {
                return gioHang;
            }

            gioHang = new GioHang { KhachHangId = khachHangId, NgayCapNhat = DateTime.UtcNow };
            _context.GioHangs.Add(gioHang);
            await _context.SaveChangesAsync();
            return gioHang;
        }

        private async Task<ChiTietGioHang?> LayChiTietCuaKhachHang(int chiTietId)
        {
            var khachHangId = HttpContext.Session.GetInt32("KhachHangId");
            if (khachHangId == null) return null;

            return await _context.ChiTietGioHangs
                .Include(c => c.GioHang)
                .Include(c => c.SanPham)
                .FirstOrDefaultAsync(c => c.Id == chiTietId && c.GioHang != null && c.GioHang.KhachHangId == khachHangId);
        }

        private async Task<int> DemSoLuong(int khachHangId)
        {
            return await _context.ChiTietGioHangs
                .Where(c => c.GioHang != null && c.GioHang.KhachHangId == khachHangId)
                .SumAsync(c => c.SoLuong);
        }

        private async Task<KhuyenMai?> LayKhuyenMaiDangApDung()
        {
            var khuyenMaiId = HttpContext.Session.GetInt32(SessionKhuyenMaiId);
            if (khuyenMaiId == null) return null;

            var hienTai = DateTime.UtcNow;
            return await _context.KhuyenMais.FirstOrDefaultAsync(k =>
                k.Id == khuyenMaiId.Value &&
                k.KichHoat &&
                k.NgayBatDau <= hienTai &&
                k.NgayKetThuc >= hienTai);
        }

        private int LayDiemThuongSuDungHopLe(int diemThuong, decimal tongSauKhuyenMai)
        {
            var diemTrongSession = HttpContext.Session.GetInt32(SessionDiemThuongSuDung) ?? 0;
            var diemToiDaTheoDon = Math.Max(0, (int)Math.Floor(tongSauKhuyenMai / GiaTriMoiDiem));
            var diemToiDa = Math.Min(diemThuong, diemToiDaTheoDon);
            var diemHopLe = Math.Clamp(diemTrongSession, 0, diemToiDa);

            if (diemHopLe != diemTrongSession)
            {
                if (diemHopLe <= 0)
                {
                    HttpContext.Session.Remove(SessionDiemThuongSuDung);
                }
                else
                {
                    HttpContext.Session.SetInt32(SessionDiemThuongSuDung, diemHopLe);
                }
            }

            return diemHopLe;
        }

        private static decimal TinhTongSauKhuyenMai(GioHang gioHang, KhuyenMai? khuyenMai)
        {
            var tongTien = gioHang.ChiTiet.Sum(c => c.SoLuong * (c.SanPham?.GiaBan ?? 0m));
            var tienGiam = khuyenMai == null ? 0m : Math.Round(tongTien * khuyenMai.PhanTramGiam / 100m, 0);
            return Math.Max(0m, tongTien - tienGiam);
        }

        private static string ChuanHoaPhuongThucThanhToan(string? phuongThuc)
        {
            return phuongThuc == "ChuyenKhoan" ? "ChuyenKhoan" : "TienMat";
        }

        private static string TaoGhiChuThanhToan(string phuongThuc)
        {
            return phuongThuc == "ChuyenKhoan"
                ? "Khách chọn chuyển khoản QR SePAY. Nhân viên chỉ cập nhật Đã thanh toán sau khi nhận tin nhắn xác nhận giao dịch từ SePAY."
                : "Khách chọn tiền mặt. Khi hàng giao đến, nhân viên thu tiền mặt và cập nhật trạng thái hoàn tất.";
        }

        private async Task<DonHang?> LayDonSePayDangCho(int khachHangId)
        {
            return await _context.DonHangs
                .Where(d =>
                    d.KhachHangId == khachHangId &&
                    d.PhuongThucThanhToan == "ChuyenKhoan" &&
                    d.TrangThai == "DangXuLy")
                .OrderByDescending(d => d.NgayDat)
                .FirstOrDefaultAsync();
        }

        private async Task HuyDonSePayQuaHan(int khachHangId)
        {
            var hanThanhToan = DateTime.UtcNow.AddMinutes(-ThoiGianChoThanhToanPhut);
            var donQuaHan = await _context.DonHangs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTiet)
                .ThenInclude(c => c.SanPham)
                .Where(d =>
                    d.KhachHangId == khachHangId &&
                    d.PhuongThucThanhToan == "ChuyenKhoan" &&
                    d.TrangThai == "DangXuLy" &&
                    d.NgayDat <= hanThanhToan)
                .ToListAsync();

            foreach (var donHang in donQuaHan)
            {
                HuyDonHangChoThanhToan(donHang);
            }

            if (donQuaHan.Count > 0)
            {
                await _context.SaveChangesAsync();
                HttpContext.Session.SetInt32("KhachHangDiemThuong", donQuaHan.Last().KhachHang?.DiemThuong ?? 0);
            }
        }

        private void HuyDonHangChoThanhToan(DonHang donHang)
        {
            if (donHang.TrangThai != "DangXuLy" || donHang.PhuongThucThanhToan != "ChuyenKhoan") return;

            donHang.TrangThai = "DaHuy";
            donHang.GhiChuThanhToan = $"{donHang.GhiChuThanhToan} Đơn đã tự hủy vì quá {ThoiGianChoThanhToanPhut} phút chưa nhận được thanh toán SePAY.";

            foreach (var item in donHang.ChiTiet)
            {
                if (item.SanPham != null)
                {
                    item.SanPham.TonKho += item.SoLuong;
                }
            }

            if (donHang.KhachHang != null && donHang.DiemThuongSuDung > 0)
            {
                donHang.KhachHang.DiemThuong += donHang.DiemThuongSuDung;
            }
        }

        private async Task XacNhanDonHangDaThanhToan(DonHang donHang, string ghiChu)
        {
            if (donHang.TrangThai == "DaThanhToan") return;

            donHang.TrangThai = "DaThanhToan";
            donHang.GhiChuThanhToan = $"{donHang.GhiChuThanhToan} {ghiChu}";
            if (donHang.KhachHang != null && donHang.DiemThuongCong > 0)
            {
                donHang.KhachHang.DiemThuong += donHang.DiemThuongCong;
                HttpContext.Session.SetInt32("KhachHangDiemThuong", donHang.KhachHang.DiemThuong);
            }

            await _context.SaveChangesAsync();
        }

        private string TaoSePayQrUrl(DonHang donHang)
        {
            var bank = _configuration["SePay:Bank"] ?? "BIDV";
            var accountNumber = _configuration["SePay:VirtualAccountNumber"]
                ?? _configuration["SePay:AccountNumber"]
                ?? "";
            var accountHolder = _configuration["SePay:AccountHolder"] ?? "";
            var noiDung = TaoNoiDungChuyenKhoan(donHang.Id);
            return "https://qr.sepay.vn/img"
                + $"?bank={Uri.EscapeDataString(bank)}"
                + $"&acc={Uri.EscapeDataString(accountNumber)}"
                + $"&amount={(long)donHang.TongTien}"
                + $"&des={Uri.EscapeDataString(noiDung)}"
                + $"&accountName={Uri.EscapeDataString(accountHolder)}"
                + "&template=compact";
        }

        private static string TaoNoiDungChuyenKhoan(int donHangId)
        {
            return $"DH{donHangId:000000}";
        }

        private async Task<(bool DaNhanTien, string? Loi)> KiemTraGiaoDichSePay(DonHang donHang)
        {
            var apiToken = _configuration["SePay:ApiToken"];
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                return (false, "Chưa cấu hình SePAY API token nên không thể kiểm tra giao dịch.");
            }

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
                var gioVietNam = LayMuiGioVietNam();
                var tuNgay = TimeZoneInfo.ConvertTimeFromUtc(donHang.NgayDat.AddMinutes(-10), gioVietNam);
                var denNgay = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddMinutes(10), gioVietNam);
                var url = "https://my.sepay.vn/userapi/transactions/list"
                    + "?limit=100"
                    + $"&transaction_date_min={Uri.EscapeDataString(tuNgay.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}"
                    + $"&transaction_date_max={Uri.EscapeDataString(denNgay.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}";
                var response = await httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SePAY API tra ve loi HTTP {StatusCode}: {Body}", response.StatusCode, json);
                    return (false, $"SePAY API trả về lỗi {(int)response.StatusCode}. Vui lòng kiểm tra API token hoặc cấu hình SePAY.");
                }

                using var document = JsonDocument.Parse(json);
                return (CoGiaoDichKhop(document.RootElement, TaoNoiDungChuyenKhoan(donHang.Id), donHang.TongTien), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Khong the kiem tra giao dich SePAY cho don hang #{DonHangId}.", donHang.Id);
                return (false, "Không gọi được SePAY API. Vui lòng kiểm tra kết nối, API token và cấu hình server.");
            }
        }

        private static TimeZoneInfo LayMuiGioVietNam()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                }
                catch (TimeZoneNotFoundException)
                {
                    return TimeZoneInfo.Utc;
                }
            }
        }

        private static bool CoGiaoDichKhop(JsonElement element, string maDonHang, decimal tongTien)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var content = "";
                var amount = 0m;
                var transferType = "";

                foreach (var property in element.EnumerateObject())
                {
                    var name = property.Name.ToLowerInvariant();
                    if (name is "content" or "description" or "transaction_content")
                    {
                        content = property.Value.GetString() ?? "";
                    }
                    else if (name is "transferamount" or "transfer_amount" or "amount_in" or "amount" or "money")
                    {
                        amount = DocSoTien(property.Value);
                    }
                    else if (name is "transfertype" or "transfer_type" or "type")
                    {
                        transferType = property.Value.GetString() ?? "";
                    }
                }

                if (!string.IsNullOrWhiteSpace(content) &&
                    content.Contains(maDonHang, StringComparison.OrdinalIgnoreCase) &&
                    amount >= tongTien &&
                    LaGiaoDichTienVao(transferType))
                {
                    return true;
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (CoGiaoDichKhop(property.Value, maDonHang, tongTien)) return true;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (CoGiaoDichKhop(item, maDonHang, tongTien)) return true;
                }
            }

            return false;
        }

        private static decimal DocSoTien(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var textNumber)) return textNumber;
            return 0m;
        }

        private static bool LaGiaoDichTienVao(string? transferType)
        {
            if (string.IsNullOrWhiteSpace(transferType)) return true;

            var text = transferType.Trim().ToLowerInvariant();
            return text is "in" or "income" or "credit" or "deposit" or "receive";
        }

        private static void ApDungTienGiamTuDiem(List<ChiTietDonHang> chiTiet, decimal tienGiamTuDiem)
        {
            if (tienGiamTuDiem <= 0 || chiTiet.Count == 0) return;

            var tongConLai = chiTiet.Sum(item => item.SoLuong * item.DonGia - item.TienGiam);
            if (tongConLai <= 0) return;

            var tienCanGiam = Math.Min(tienGiamTuDiem, tongConLai);
            var daPhanBo = 0m;

            for (var i = 0; i < chiTiet.Count; i++)
            {
                var item = chiTiet[i];
                var thanhTienSauKhuyenMai = item.SoLuong * item.DonGia - item.TienGiam;
                var tienGiamThem = i == chiTiet.Count - 1
                    ? tienCanGiam - daPhanBo
                    : Math.Round(tienCanGiam * thanhTienSauKhuyenMai / tongConLai, 0);

                tienGiamThem = Math.Clamp(tienGiamThem, 0m, thanhTienSauKhuyenMai);
                item.TienGiam += tienGiamThem;
                daPhanBo += tienGiamThem;
            }
        }
    }
}
