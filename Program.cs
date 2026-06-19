using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebQLministop.Data;
using WebQLministop.Models;

var builder = WebApplication.CreateBuilder(args);

var mvcBuilder = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=.\\SQLEXPRESS;Database=WebQLministop;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var authenticationBuilder = builder.Services.AddAuthentication();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

var app = builder.Build();


app.Use((context, next) =>
{
    context.Request.Scheme = "https";
    return next(context);
});


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
 
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapAreaControllerRoute(
    name: "quanly-root",
    areaName: "QuanLy",
    pattern: "QuanLy/{action=Index}/{id?}",
    defaults: new { controller = "QuanLy" });

app.MapAreaControllerRoute(
    name: "nhanvien-root",
    areaName: "NhanVien",
    pattern: "NhanVien/{action=Index}/{id?}",
    defaults: new { controller = "NhanVien" });

app.MapAreaControllerRoute(
    name: "khachhang-root",
    areaName: "KhachHang",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "LichSuDonHang",
    pattern: "LichSuDonHang",
    defaults: new { area = "KhachHang", controller = "TaiKhoan", action = "LichSuDonHang" });

app.MapControllerRoute(
    name: "TimKiem",
    pattern: "TimKiem",
    defaults: new { area = "KhachHang", controller = "Home", action = "TimKiem" });

app.MapControllerRoute(
    name: "DangXuat",
    pattern: "DangXuat",
    defaults: new { area = "KhachHang", controller = "DangNhap", action = "DangXuat" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    SeedData(db);
    EnsureSeedKhachHangPasswords(db);
    EnsureNhanVienPasswords(db);
    await EnsureIdentityAccounts(scope.ServiceProvider, db);
}

app.Run();

static void SeedData(ApplicationDbContext db)
{
    if (db.DanhMucs.Any()) return;

    var categories = new[]
    {
        new DanhMuc { Ten = "Do uong", MoTa = "Nuoc ngot, nuoc suoi, ca phe" },
        new DanhMuc { Ten = "Banh keo", MoTa = "Snack, keo, banh" },
        new DanhMuc { Ten = "Thuc pham", MoTa = "Mi goi, sua, do an nhanh" },
        new DanhMuc { Ten = "Gia dung", MoTa = "Ve sinh, hop dung" }
    };
    db.DanhMucs.AddRange(categories);

    var suppliers = new[]
    {
        new NhaCungCap { Ten = "Cong ty ABC", TenLienHe = "Nguyen Van A", DienThoai = "0901111111", Email = "abc@ministop.vn", DiaChi = "Quan 1" },
        new NhaCungCap { Ten = "Cong ty Tan Phat", TenLienHe = "Tran Thi B", DienThoai = "0902222222", Email = "tanphat@ministop.vn", DiaChi = "Quan 7" },
        new NhaCungCap { Ten = "Cong ty Minh Anh", TenLienHe = "Le Van C", DienThoai = "0903333333", Email = "minhanh@ministop.vn", DiaChi = "Binh Thanh" }
    };
    db.NhaCungCaps.AddRange(suppliers);
    db.SaveChanges();

    var products = new[]
    {
        new SanPham { Ma = "DR001", Ten = "Coca Cola 330ml", DanhMucId = categories[0].Id, NhaCungCapId = suppliers[0].Id, GiaVon = 7_000m, GiaBan = 10_000m, DonVi = "chai", TonKho = 120, MucCanNhapLai = 30, HinhAnh = "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?auto=format&fit=crop&w=600&q=80" },
        new SanPham { Ma = "DR002", Ten = "Pepsi 355ml", DanhMucId = categories[0].Id, NhaCungCapId = suppliers[1].Id, GiaVon = 7_500m, GiaBan = 10_500m, DonVi = "chai", TonKho = 100, MucCanNhapLai = 25, HinhAnh = "https://images.unsplash.com/photo-1599028903149-5919b5ce4c94?auto=format&fit=crop&w=600&q=80" },
        new SanPham { Ma = "SN001", Ten = "Snack Oishi", DanhMucId = categories[1].Id, NhaCungCapId = suppliers[2].Id, GiaVon = 5_000m, GiaBan = 7_500m, DonVi = "goi", TonKho = 200, MucCanNhapLai = 40, HinhAnh = "https://images.unsplash.com/photo-1580828369619-1425e4c632e9?auto=format&fit=crop&w=600&q=80" },
        new SanPham { Ma = "FD001", Ten = "Mi Omachi", DanhMucId = categories[2].Id, NhaCungCapId = suppliers[0].Id, GiaVon = 4_000m, GiaBan = 6_000m, DonVi = "goi", TonKho = 150, MucCanNhapLai = 30, HinhAnh = "https://images.unsplash.com/photo-1621939514646-b2861e053f3f?auto=format&fit=crop&w=600&q=80" },
        new SanPham { Ma = "HM001", Ten = "Khan giay 4 lop", DanhMucId = categories[3].Id, NhaCungCapId = suppliers[1].Id, GiaVon = 12_000m, GiaBan = 16_000m, DonVi = "cuon", TonKho = 80, MucCanNhapLai = 20, HinhAnh = "https://images.unsplash.com/photo-1583947581924-860bda6a26df?auto=format&fit=crop&w=600&q=80" }
    };
    db.SanPhams.AddRange(products);

    var employees = new[]
    {
        new NhanVien { HoTen = "Nguyen Thi Lan", ChucVu = "Quan ly", DienThoai = "0910000001", Email = "lan@ministop.vn", Luong = 15000000m },
        new NhanVien { HoTen = "Tran Minh Tuan", ChucVu = "Thu ngan", DienThoai = "0910000002", Email = "tuan@ministop.vn", Luong = 9000000m },
        new NhanVien { HoTen = "Pham Hong Nhung", ChucVu = "Pha che", DienThoai = "0910000003", Email = "nhung@ministop.vn", Luong = 8500000m }
    };
    db.NhanViens.AddRange(employees);

    var passwordHasher = new PasswordHasher<KhachHang>();
    var customers = new[]
    {
        new KhachHang { HoTen = "Le Van Dung", DienThoai = "0930000001", Email = "dung@gmail.com", DiemThuong = 120 },
        new KhachHang { HoTen = "Bui Thi Hanh", DienThoai = "0930000002", Email = "hanh@gmail.com", DiemThuong = 85 },
        new KhachHang { HoTen = "Do Minh Khoi", DienThoai = "0930000003", Email = "khoi@gmail.com", DiemThuong = 60 }
    };
    foreach (var customer in customers)
    {
        customer.MatKhauHash = passwordHasher.HashPassword(customer, "123456");
    }
    db.KhachHangs.AddRange(customers);

    var promotions = new[]
    {
        new KhuyenMai { Ten = "Khuyen mai cuoi tuan", Ma = "CUOITUAN", PhanTramGiam = 10m, NgayBatDau = DateTime.UtcNow.AddDays(-5), NgayKetThuc = DateTime.UtcNow.AddDays(10) },
        new KhuyenMai { Ten = "Mua 2 tang 1", Ma = "MUA2TANG1", PhanTramGiam = 15m, NgayBatDau = DateTime.UtcNow, NgayKetThuc = DateTime.UtcNow.AddDays(20) }
    };
    db.KhuyenMais.AddRange(promotions);
    db.SaveChanges();

    var order = new DonHang
    {
        KhachHangId = customers[0].Id,
        NhanVienId = employees[1].Id,
        NgayDat = DateTime.UtcNow.AddDays(-1),
        TrangThai = "DaThanhToan",
        PhuongThucThanhToan = "TienMat",
        TongTien = 0m,
        ChiTiet = new List<ChiTietDonHang>
        {
            new ChiTietDonHang { SanPhamId = products[0].Id, SoLuong = 2, DonGia = products[0].GiaBan, TienGiam = 0m },
            new ChiTietDonHang { SanPhamId = products[2].Id, SoLuong = 1, DonGia = products[2].GiaBan, TienGiam = 0m }
        }
    };
    db.DonHangs.Add(order);
    db.SaveChanges();

    order.TongTien = order.ChiTiet.Sum(i => i.SoLuong * i.DonGia - i.TienGiam);
    db.DonHangs.Update(order);
    db.SaveChanges();
}

static void EnsureNhanVienPasswords(ApplicationDbContext db)
{
    var nhanViens = db.NhanViens
        .Where(n => n.KichHoat && string.IsNullOrWhiteSpace(n.MatKhauHash))
        .ToList();

    if (nhanViens.Count == 0) return;

    var passwordHasher = new PasswordHasher<NhanVien>();
    foreach (var nhanVien in nhanViens)
    {
        nhanVien.MatKhauHash = passwordHasher.HashPassword(nhanVien, "123456");
    }

    db.SaveChanges();
}

static void EnsureSeedKhachHangPasswords(ApplicationDbContext db)
{
    var seedEmails = new[] { "dung@gmail.com", "hanh@gmail.com", "khoi@gmail.com" };
    var khachHangs = db.KhachHangs
        .Where(k => k.KichHoat &&
            k.Email != null &&
            seedEmails.Contains(k.Email) &&
            string.IsNullOrWhiteSpace(k.MatKhauHash))
        .ToList();

    if (khachHangs.Count == 0) return;

    var passwordHasher = new PasswordHasher<KhachHang>();
    foreach (var khachHang in khachHangs)
    {
        khachHang.MatKhauHash = passwordHasher.HashPassword(khachHang, "123456");
    }

    db.SaveChanges();
}

static async Task EnsureIdentityAccounts(IServiceProvider services, ApplicationDbContext db)
{
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var roleName in new[] { "KhachHang", "NhanVien", "QuanLy" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var khachHangs = db.KhachHangs.Where(k => k.KichHoat).ToList();
    foreach (var khachHang in khachHangs)
    {
        var user = await TimIdentityUser(userManager, "KhachHang", khachHang.Id, khachHang.Email, khachHang.DienThoai);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = $"khachhang-{khachHang.Id}",
                HoTen = khachHang.HoTen,
                Email = khachHang.Email,
                PhoneNumber = khachHang.DienThoai,
                LoaiTaiKhoan = "KhachHang",
                KhachHangId = khachHang.Id,
                PasswordHash = khachHang.MatKhauHash
            };

            await userManager.CreateAsync(user);
        }
        else
        {
            user.HoTen = khachHang.HoTen;
            user.Email = khachHang.Email;
            user.PhoneNumber = khachHang.DienThoai;
            user.LoaiTaiKhoan = "KhachHang";
            user.KhachHangId = khachHang.Id;
            await userManager.UpdateAsync(user);
        }

        await DamBaoVaiTro(userManager, user, "KhachHang");
    }

    var nhanViens = db.NhanViens.Where(n => n.KichHoat).ToList();
    foreach (var nhanVien in nhanViens)
    {
        var vaiTro = LaQuanLy(nhanVien.ChucVu) ? "QuanLy" : "NhanVien";
        var user = await TimIdentityUser(userManager, vaiTro, nhanVien.Id, nhanVien.Email, nhanVien.DienThoai);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = $"nhanvien-{nhanVien.Id}",
                HoTen = nhanVien.HoTen,
                Email = nhanVien.Email,
                PhoneNumber = nhanVien.DienThoai,
                LoaiTaiKhoan = vaiTro,
                NhanVienId = nhanVien.Id,
                PasswordHash = nhanVien.MatKhauHash
            };

            await userManager.CreateAsync(user);
        }
        else
        {
            user.HoTen = nhanVien.HoTen;
            user.Email = nhanVien.Email;
            user.PhoneNumber = nhanVien.DienThoai;
            user.LoaiTaiKhoan = vaiTro;
            user.NhanVienId = nhanVien.Id;
            await userManager.UpdateAsync(user);
        }

        await DamBaoVaiTro(userManager, user, vaiTro);
    }
}

static async Task<ApplicationUser?> TimIdentityUser(
    UserManager<ApplicationUser> userManager,
    string loaiTaiKhoan,
    int idNguoiDung,
    string? email,
    string? dienThoai)
{
    ApplicationUser? user = loaiTaiKhoan == "KhachHang"
        ? await userManager.Users.FirstOrDefaultAsync(u => u.KhachHangId == idNguoiDung)
        : await userManager.Users.FirstOrDefaultAsync(u => u.NhanVienId == idNguoiDung);

    if (user != null) return user;

    if (!string.IsNullOrWhiteSpace(email))
    {
        user = await userManager.Users.FirstOrDefaultAsync(u =>
            u.Email == email &&
            ((loaiTaiKhoan == "KhachHang" && u.LoaiTaiKhoan == "KhachHang") ||
             (loaiTaiKhoan != "KhachHang" && u.LoaiTaiKhoan != "KhachHang")));
    }

    if (user != null || string.IsNullOrWhiteSpace(dienThoai)) return user;

    return await userManager.Users.FirstOrDefaultAsync(u =>
        u.PhoneNumber == dienThoai &&
        ((loaiTaiKhoan == "KhachHang" && u.LoaiTaiKhoan == "KhachHang") ||
         (loaiTaiKhoan != "KhachHang" && u.LoaiTaiKhoan != "KhachHang")));
}

static async Task DamBaoVaiTro(UserManager<ApplicationUser> userManager, ApplicationUser user, string vaiTro)
{
    var roles = await userManager.GetRolesAsync(user);
    foreach (var role in roles.Where(r => r != vaiTro))
    {
        await userManager.RemoveFromRoleAsync(user, role);
    }

    if (!await userManager.IsInRoleAsync(user, vaiTro))
    {
        await userManager.AddToRoleAsync(user, vaiTro);
    }
}

static bool LaQuanLy(string? chucVu)
{
    var text = (chucVu ?? string.Empty).Trim().ToLowerInvariant();
    return text.Contains("quan") || text.Contains("quản");
}
