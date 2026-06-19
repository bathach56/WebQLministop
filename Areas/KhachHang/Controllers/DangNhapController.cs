using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebQLministop.Data;
using WebQLministop.Models;
using KhachHangModel = WebQLministop.Models.KhachHang;
using NhanVienModel = WebQLministop.Models.NhanVien;

namespace WebQLministop.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    public class DangNhapController : Controller
    {
        private const string SessionNhanVienLoginToken = "NhanVienLoginToken";
        private static readonly ConcurrentDictionary<string, PhienDangNhapNhanVien> PhienNhanVienDangNhap = new();
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public DangNhapController(
            ApplicationDbContext context,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string taiKhoan, string matKhau)
        {
            taiKhoan = taiKhoan?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(taiKhoan) || string.IsNullOrWhiteSpace(matKhau))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập tài khoản và mật khẩu.");
                return View();
            }

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n =>
                n.KichHoat &&
                (n.Email == taiKhoan || n.DienThoai == taiKhoan));

            if (nhanVien != null)
            {
                var userNhanVien = await TimTaiKhoanNhanVien(nhanVien, taiKhoan);
                if (userNhanVien == null)
                {
                    userNhanVien = await TaoTaiKhoanNhanVien(nhanVien, matKhau);
                }

                var ketQuaNhanVien = await _signInManager.CheckPasswordSignInAsync(userNhanVien, matKhau, false);
                if (!ketQuaNhanVien.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
                    return View();
                }

                if (TaiKhoanNhanVienDangDuocSuDung(userNhanVien))
                {
                    ModelState.AddModelError(string.Empty, "Tai khoan nhan vien/quan ly nay dang duoc su dung o noi khac. Vui long dang xuat phien cu truoc khi dang nhap.");
                    return View();
                }

                await _signInManager.SignInAsync(userNhanVien, false);
                await LuuSessionNhanVien(nhanVien, userNhanVien);
                return ChuanHoaVaiTro(userNhanVien.LoaiTaiKhoan) == "QuanLy"
                    ? RedirectToAction("Index", "QuanLy", new { area = "QuanLy" })
                    : RedirectToAction("Index", "NhanVien", new { area = "NhanVien" });
            }

            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k =>
                k.KichHoat &&
                (k.Email == taiKhoan || k.DienThoai == taiKhoan));

            if (khachHang == null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
                return View();
            }

            var userKhachHang = await TimTaiKhoanKhachHang(khachHang, taiKhoan);
            if (userKhachHang == null)
            {
                userKhachHang = await TaoTaiKhoanKhachHang(khachHang, matKhau);
            }

            var ketQua = await _signInManager.CheckPasswordSignInAsync(userKhachHang, matKhau, false);
            if (!ketQua.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
                return View();
            }

            await _signInManager.SignInAsync(userKhachHang, false);
            LuuSessionKhachHang(khachHang);
            return RedirectToAction("Index", "Home", new { area = "KhachHang" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangNhapGoogle()
        {
            if (string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) ||
                string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]))
            {
                ModelState.AddModelError(string.Empty, "Đăng nhập Google chưa được cấu hình trên máy này.");
                return View(nameof(Index));
            }

            var redirectUrl = Url.Action(nameof(DangNhapGoogleCallback), "DangNhap", new { area = "KhachHang" });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        [HttpGet]
        public async Task<IActionResult> DangNhapGoogleCallback()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ModelState.AddModelError(string.Empty, "Không thể đăng nhập bằng Google. Vui lòng thử lại.");
                return View(nameof(Index));
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email)?.Trim();
            var hoTen = info.Principal.FindFirstValue(ClaimTypes.Name)?.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                await _signInManager.SignOutAsync();
                ModelState.AddModelError(string.Empty, "Tài khoản Google này không cung cấp email.");
                return View(nameof(Index));
            }

            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.KichHoat && k.Email == email);

            if (khachHang == null)
            {
                khachHang = new KhachHangModel
                {
                    HoTen = string.IsNullOrWhiteSpace(hoTen) ? email : hoTen,
                    Email = email,
                    KichHoat = true
                };

                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();
            }

            if (user == null)
            {
                user = await TimTaiKhoanKhachHang(khachHang, email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = $"khachhang-{khachHang.Id}",
                        HoTen = khachHang.HoTen,
                        Email = khachHang.Email,
                        PhoneNumber = khachHang.DienThoai,
                        LoaiTaiKhoan = "KhachHang",
                        KhachHangId = khachHang.Id
                    };

                    var taoTaiKhoan = await _userManager.CreateAsync(user);
                    if (!taoTaiKhoan.Succeeded)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản từ Google.");
                        return View(nameof(Index));
                    }

                    await _userManager.AddToRoleAsync(user, "KhachHang");
                }

                var logins = await _userManager.GetLoginsAsync(user);
                if (!logins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
                {
                    await _userManager.AddLoginAsync(user, info);
                }
            }

            user.HoTen = khachHang.HoTen;
            user.Email = khachHang.Email;
            user.PhoneNumber = khachHang.DienThoai;
            user.KhachHangId = khachHang.Id;
            user.LoaiTaiKhoan = "KhachHang";
            await _userManager.UpdateAsync(user);
            await _userManager.AddToRoleAsync(user, "KhachHang");

            await _signInManager.SignInAsync(user, false);
            LuuSessionKhachHang(khachHang);

            return RedirectToAction("Index", "Home", new { area = "KhachHang" });
        }

        [HttpGet]
        public IActionResult QuenMatKhau()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuenMatKhau(string email)
        {
            email = email?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(nameof(email), "Vui lòng nhập email đã đăng ký.");
                return View();
            }

            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.KichHoat && k.Email == email);
            if (khachHang == null)
            {
                TempData["ThongBao"] = "Nếu email đã được đăng ký, hệ thống sẽ gửi đường link đặt lại mật khẩu.";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            khachHang.MaDatLaiMatKhau = Guid.NewGuid().ToString("N");
            khachHang.HanDatLaiMatKhau = DateTime.UtcNow.AddMinutes(30);
            await _context.SaveChangesAsync();

            var resetLink = Url.Action(nameof(DatLaiMatKhau), "DangNhap", new { area = "KhachHang", token = khachHang.MaDatLaiMatKhau }, Request.Scheme) ?? string.Empty;
            var daGuiEmail = await GuiEmailDatLaiMatKhau(khachHang.Email!, resetLink);

            TempData["ThongBao"] = daGuiEmail
                ? "Đường link đặt lại mật khẩu đã được gửi tới email của bạn."
                : $"SMTP chưa bật nên chưa gửi email thật. Link test: {resetLink}";

            return RedirectToAction(nameof(QuenMatKhau));
        }

        [HttpGet]
        public async Task<IActionResult> DatLaiMatKhau(string token)
        {
            var khachHang = await TimKhachHangTheoToken(token);
            if (khachHang == null)
            {
                TempData["ThongBao"] = "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatLaiMatKhau(string token, string matKhau, string xacNhanMatKhau)
        {
            var khachHang = await TimKhachHangTheoToken(token);
            if (khachHang == null)
            {
                TempData["ThongBao"] = "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            if (string.IsNullOrWhiteSpace(matKhau) || matKhau.Length < 6)
            {
                ModelState.AddModelError(nameof(matKhau), "Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            if (matKhau != xacNhanMatKhau)
            {
                ModelState.AddModelError(nameof(xacNhanMatKhau), "Mật khẩu xác nhận không khớp.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Token = token;
                return View();
            }

            var user = await TimTaiKhoanKhachHang(khachHang, khachHang.Email ?? khachHang.DienThoai ?? string.Empty);
            user ??= await TaoTaiKhoanKhachHang(khachHang, matKhau);

            if (await _userManager.HasPasswordAsync(user))
            {
                await _userManager.RemovePasswordAsync(user);
            }

            var ketQua = await _userManager.AddPasswordAsync(user, matKhau);
            if (!ketQua.Succeeded)
            {
                foreach (var loi in ketQua.Errors)
                {
                    ModelState.AddModelError(string.Empty, loi.Description);
                }

                ViewBag.Token = token;
                return View();
            }

            khachHang.MatKhauHash = user.PasswordHash;
            khachHang.MaDatLaiMatKhau = null;
            khachHang.HanDatLaiMatKhau = null;
            await _context.SaveChangesAsync();

            TempData["ThongBaoThanhCong"] = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangXuat()
        {
            GiaiPhongPhienNhanVien();
            await _signInManager.SignOutAsync();
            XoaSessionDangNhap();
            return RedirectToAction("Index", "Home", new { area = "KhachHang" });
        }

        private async Task<ApplicationUser?> TimTaiKhoanKhachHang(KhachHangModel khachHang, string taiKhoan)
        {
            return await _userManager.Users.FirstOrDefaultAsync(u =>
                u.LoaiTaiKhoan == "KhachHang" &&
                (u.KhachHangId == khachHang.Id ||
                 (!string.IsNullOrWhiteSpace(khachHang.Email) && u.Email == khachHang.Email) ||
                 (!string.IsNullOrWhiteSpace(khachHang.DienThoai) && u.PhoneNumber == khachHang.DienThoai) ||
                 u.Email == taiKhoan ||
                 u.PhoneNumber == taiKhoan));
        }

        private async Task<ApplicationUser?> TimTaiKhoanNhanVien(NhanVienModel nhanVien, string taiKhoan)
        {
            return await _userManager.Users.FirstOrDefaultAsync(u =>
                u.LoaiTaiKhoan != "KhachHang" &&
                (u.NhanVienId == nhanVien.Id ||
                 (!string.IsNullOrWhiteSpace(nhanVien.Email) && u.Email == nhanVien.Email) ||
                 (!string.IsNullOrWhiteSpace(nhanVien.DienThoai) && u.PhoneNumber == nhanVien.DienThoai) ||
                 u.Email == taiKhoan ||
                 u.PhoneNumber == taiKhoan));
        }

        private async Task<ApplicationUser> TaoTaiKhoanKhachHang(KhachHangModel khachHang, string matKhau)
        {
            var user = new ApplicationUser
            {
                UserName = $"khachhang-{khachHang.Id}",
                HoTen = khachHang.HoTen,
                Email = khachHang.Email,
                PhoneNumber = khachHang.DienThoai,
                LoaiTaiKhoan = "KhachHang",
                KhachHangId = khachHang.Id
            };

            await _userManager.CreateAsync(user, matKhau);
            await _userManager.AddToRoleAsync(user, "KhachHang");
            khachHang.MatKhauHash = user.PasswordHash;
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task<ApplicationUser> TaoTaiKhoanNhanVien(NhanVienModel nhanVien, string matKhau)
        {
            var vaiTro = LaQuanLy(nhanVien) ? "QuanLy" : "NhanVien";
            var user = new ApplicationUser
            {
                UserName = $"nhanvien-{nhanVien.Id}",
                HoTen = nhanVien.HoTen,
                Email = nhanVien.Email,
                PhoneNumber = nhanVien.DienThoai,
                LoaiTaiKhoan = vaiTro,
                NhanVienId = nhanVien.Id
            };

            await _userManager.CreateAsync(user, matKhau);
            await _userManager.AddToRoleAsync(user, vaiTro);
            foreach (var quyen in QuyenMacDinh(vaiTro))
            {
                await _userManager.AddClaimAsync(user, new Claim("Permission", quyen));
            }
            nhanVien.MatKhauHash = user.PasswordHash;
            await _context.SaveChangesAsync();
            return user;
        }

        private void LuuSessionKhachHang(KhachHangModel khachHang)
        {
            XoaSessionDangNhap();

            HttpContext.Session.SetInt32("KhachHangId", khachHang.Id);
            HttpContext.Session.SetString("KhachHangHoTen", khachHang.HoTen);
            HttpContext.Session.SetInt32("KhachHangDiemThuong", khachHang.DiemThuong);

            if (string.IsNullOrWhiteSpace(khachHang.AnhDaiDien))
            {
                HttpContext.Session.Remove("KhachHangAnhDaiDien");
            }
            else
            {
                HttpContext.Session.SetString("KhachHangAnhDaiDien", khachHang.AnhDaiDien);
            }
        }

        private async Task LuuSessionNhanVien(NhanVienModel nhanVien, ApplicationUser user)
        {
            XoaSessionDangNhap();

            var loginToken = Guid.NewGuid().ToString("N");
            PhienNhanVienDangNhap[user.Id] = new PhienDangNhapNhanVien(loginToken, DateTime.UtcNow.AddMinutes(60));

            HttpContext.Session.SetInt32("NhanVienId", nhanVien.Id);
            HttpContext.Session.SetString("NhanVienHoTen", nhanVien.HoTen);
            HttpContext.Session.SetString("VaiTro", ChuanHoaVaiTro(user.LoaiTaiKhoan));
            HttpContext.Session.SetString(SessionNhanVienLoginToken, loginToken);

            var quyen = (await _userManager.GetClaimsAsync(user))
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .Where(LaQuyenNhanVienHopLe)
                .Distinct()
                .ToList();

            if (quyen.Count == 0)
            {
                quyen = QuyenMacDinh(ChuanHoaVaiTro(user.LoaiTaiKhoan)).ToList();
                foreach (var item in quyen)
                {
                    await _userManager.AddClaimAsync(user, new Claim("Permission", item));
                }
            }

            HttpContext.Session.SetString("QuyenNhanVien", string.Join(",", quyen));
        }

        private void XoaSessionDangNhap()
        {
            HttpContext.Session.Remove("KhachHangId");
            HttpContext.Session.Remove("KhachHangHoTen");
            HttpContext.Session.Remove("KhachHangAnhDaiDien");
            HttpContext.Session.Remove("KhachHangDiemThuong");
            HttpContext.Session.Remove("NhanVienId");
            HttpContext.Session.Remove("NhanVienHoTen");
            HttpContext.Session.Remove("VaiTro");
            HttpContext.Session.Remove("QuyenNhanVien");
            HttpContext.Session.Remove(SessionNhanVienLoginToken);
        }

        private bool TaiKhoanNhanVienDangDuocSuDung(ApplicationUser user)
        {
            if (!PhienNhanVienDangNhap.TryGetValue(user.Id, out var phien))
            {
                return false;
            }

            if (phien.HetHan <= DateTime.UtcNow)
            {
                PhienNhanVienDangNhap.TryRemove(user.Id, out _);
                return false;
            }

            var tokenHienTai = HttpContext.Session.GetString(SessionNhanVienLoginToken);
            return string.IsNullOrWhiteSpace(tokenHienTai) || tokenHienTai != phien.Token;
        }

        private void GiaiPhongPhienNhanVien()
        {
            var token = HttpContext.Session.GetString(SessionNhanVienLoginToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            if (PhienNhanVienDangNhap.TryGetValue(userId, out var phien) && phien.Token == token)
            {
                PhienNhanVienDangNhap.TryRemove(userId, out _);
            }
        }

        private static string ChuanHoaVaiTro(string? vaiTro)
        {
            return string.Equals(vaiTro, "QuanLy", StringComparison.OrdinalIgnoreCase) ? "QuanLy" : "NhanVien";
        }

        private static IEnumerable<string> QuyenMacDinh(string vaiTro)
        {
            return vaiTro == "QuanLy" ? QuyenNhanVienMacDinh : new[] { "BanHang.TaoDon", "BanHang.TraCuuSanPham", "HoaDon.Xem" };
        }

        private static bool LaQuyenNhanVienHopLe(string quyen)
        {
            return QuyenNhanVienMacDinh.Contains(quyen);
        }

        private static readonly string[] QuyenNhanVienMacDinh =
        {
            "SanPham.Tao",
            "DanhMuc.QuanLy",
            "DonHang.QuanLy",
            "KhuyenMai.Tao",
            "HoaDon.Xem",
            "NhanVien.PhanQuyen",
            "BanHang.TaoDon",
            "BanHang.TraCuuSanPham"
        };

        private static bool LaQuanLy(NhanVienModel nhanVien)
        {
            var chucVu = (nhanVien.ChucVu ?? string.Empty).Trim().ToLowerInvariant();
            return chucVu.Contains("quan") || chucVu.Contains("quản");
        }

        private sealed record PhienDangNhapNhanVien(string Token, DateTime HetHan);

        private async Task<KhachHangModel?> TimKhachHangTheoToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            return await _context.KhachHangs.FirstOrDefaultAsync(k =>
                k.KichHoat &&
                k.MaDatLaiMatKhau == token &&
                k.HanDatLaiMatKhau != null &&
                k.HanDatLaiMatKhau > DateTime.UtcNow);
        }

        private async Task<bool> GuiEmailDatLaiMatKhau(string email, string resetLink)
        {
            if (!_configuration.GetValue<bool>("Smtp:Enabled"))
            {
                return false;
            }

            var host = _configuration["Smtp:Host"];
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"];
            var fromName = _configuration["Smtp:FromName"] ?? "MiniStop";
            var port = _configuration.GetValue<int>("Smtp:Port");
            var enableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl");

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
            {
                return false;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "Đặt lại mật khẩu MiniStop",
                Body = $"Bạn bấm vào link này để đặt lại mật khẩu trong 30 phút: {resetLink}",
                IsBodyHtml = false
            };
            message.To.Add(email);

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                smtp.Credentials = new NetworkCredential(username, password);
            }

            await smtp.SendMailAsync(message);
            return true;
        }
    }
}
