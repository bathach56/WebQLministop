using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebQLministop.Data;
using WebQLministop.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "MiniStopCookie";
})
.AddCookie("MiniStopCookie", options =>
{
    options.LoginPath = "/Auth/DangNhap";
    options.LogoutPath = "/Auth/DangXuat";
})
.AddOAuth("Google", options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "not-configured";
    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "not-configured";
    options.CallbackPath = "/auth/google-callback";
    options.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    options.TokenEndpoint = "https://oauth2.googleapis.com/token";
    options.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
    options.Scope.Add("email");
    options.Scope.Add("profile");
    options.SaveTokens = true;
    options.Events.OnCreatingTicket = async context =>
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
        using var response = await context.Backchannel.SendAsync(request);
        using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = user.RootElement;
        ThemClaim(context.Identity, ClaimTypes.NameIdentifier, root, "id");
        ThemClaim(context.Identity, ClaimTypes.Name, root, "name");
        ThemClaim(context.Identity, ClaimTypes.Email, root, "email");
    };
})
.AddOAuth("Facebook", options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("FACEBOOK_CLIENT_ID") ?? "not-configured";
    options.ClientSecret = Environment.GetEnvironmentVariable("FACEBOOK_CLIENT_SECRET") ?? "not-configured";
    options.CallbackPath = "/auth/facebook-callback";
    options.AuthorizationEndpoint = "https://www.facebook.com/v19.0/dialog/oauth";
    options.TokenEndpoint = "https://graph.facebook.com/v19.0/oauth/access_token";
    options.UserInformationEndpoint = "https://graph.facebook.com/me?fields=id,name,email";
    options.Scope.Add("email");
    options.SaveTokens = true;
    options.Events.OnCreatingTicket = async context =>
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
        using var response = await context.Backchannel.SendAsync(request);
        using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = user.RootElement;
        ThemClaim(context.Identity, ClaimTypes.NameIdentifier, root, "id");
        ThemClaim(context.Identity, ClaimTypes.Name, root, "name");
        ThemClaim(context.Identity, ClaimTypes.Email, root, "email");
    };
})
.AddOAuth("Twitter", options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("TWITTER_CLIENT_ID") ?? "not-configured";
    options.ClientSecret = Environment.GetEnvironmentVariable("TWITTER_CLIENT_SECRET") ?? "not-configured";
    options.CallbackPath = "/auth/twitter-callback";
    options.AuthorizationEndpoint = "https://twitter.com/i/oauth2/authorize";
    options.TokenEndpoint = "https://api.twitter.com/2/oauth2/token";
    options.UserInformationEndpoint = "https://api.twitter.com/2/users/me?user.fields=name,username";
    options.Scope.Add("users.read");
    options.SaveTokens = true;
    options.Events.OnCreatingTicket = async context =>
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
        using var response = await context.Backchannel.SendAsync(request);
        using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = user.RootElement.GetProperty("data");
        ThemClaim(context.Identity, ClaimTypes.NameIdentifier, data, "id");
        ThemClaim(context.Identity, ClaimTypes.Name, data, "name");
    };
})
.AddOAuth("GitHub", options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID") ?? "not-configured";
    options.ClientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET") ?? "not-configured";
    options.CallbackPath = "/auth/github-callback";
    options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
    options.TokenEndpoint = "https://github.com/login/oauth/access_token";
    options.UserInformationEndpoint = "https://api.github.com/user";
    options.Scope.Add("user:email");
    options.SaveTokens = true;
    options.Events.OnCreatingTicket = async context =>
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
        request.Headers.UserAgent.ParseAdd("WebQLministop");
        using var response = await context.Backchannel.SendAsync(request);
        using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = user.RootElement;
        ThemClaim(context.Identity, ClaimTypes.NameIdentifier, root, "id");
        ThemClaim(context.Identity, ClaimTypes.Name, root, "name");
        ThemClaim(context.Identity, ClaimTypes.Email, root, "email");
    };
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=.\\SQLEXPRESS;Database=WebQLministop;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    SeedData(db);
}

app.Run();

static void SeedData(ApplicationDbContext db)
{
    if (db.DanhMucs.Any()) return;

    var categories = new[]
    {
        new DanhMuc { Ten = "\u0110\u1ed3 u\u1ed1ng", MoTa = "N\u01b0\u1edbc ng\u1ecdt, n\u01b0\u1edbc su\u1ed1i, c\u00e0 ph\u00ea" },
        new DanhMuc { Ten = "B\u00e1nh k\u1eb9o", MoTa = "Snack, k\u1eb9o, b\u00e1nh" },
        new DanhMuc { Ten = "Th\u1ef1c ph\u1ea9m", MoTa = "M\u00ec g\u00f3i, s\u1eefa, \u0111\u1ed3 \u0103n nhanh" },
        new DanhMuc { Ten = "Gia d\u1ee5ng", MoTa = "V\u1ec7 sinh, h\u1ed9p \u0111\u1ef1ng" }
    };
    db.DanhMucs.AddRange(categories);

    var suppliers = new[]
    {
        new NhaCungCap { Ten = "C\u00f4ng ty ABC", TenLienHe = "Nguy\u1ec5n V\u0103n A", DienThoai = "0901111111", Email = "abc@ministop.vn", DiaChi = "Qu\u1eadn 1" },
        new NhaCungCap { Ten = "C\u00f4ng ty T\u00e2n Ph\u00e1t", TenLienHe = "Tr\u1ea7n Th\u1ecb B", DienThoai = "0902222222", Email = "tanphat@ministop.vn", DiaChi = "Qu\u1eadn 7" },
        new NhaCungCap { Ten = "C\u00f4ng ty Minh Anh", TenLienHe = "L\u00ea V\u0103n C", DienThoai = "0903333333", Email = "minhanh@ministop.vn", DiaChi = "B\u00ecnh Th\u1ea1nh" }
    };
    db.NhaCungCaps.AddRange(suppliers);
    db.SaveChanges();

    var products = new[]
    {
        new SanPham { Ma = "DR001", Ten = "Coca Cola 330ml", DanhMucId = categories[0].Id, NhaCungCapId = suppliers[0].Id, GiaVon = 7_000m, GiaBan = 10_000m, DonVi = "chai", TonKho = 120, MucCanNhapLai = 30 },
        new SanPham { Ma = "DR002", Ten = "Pepsi 355ml", DanhMucId = categories[0].Id, NhaCungCapId = suppliers[1].Id, GiaVon = 7_500m, GiaBan = 10_500m, DonVi = "chai", TonKho = 100, MucCanNhapLai = 25 },
        new SanPham { Ma = "SN001", Ten = "Snack Oishi", DanhMucId = categories[1].Id, NhaCungCapId = suppliers[2].Id, GiaVon = 5_000m, GiaBan = 7_500m, DonVi = "g\u00f3i", TonKho = 200, MucCanNhapLai = 40 },
        new SanPham { Ma = "FD001", Ten = "M\u00ec Omachi", DanhMucId = categories[2].Id, NhaCungCapId = suppliers[0].Id, GiaVon = 4_000m, GiaBan = 6_000m, DonVi = "g\u00f3i", TonKho = 150, MucCanNhapLai = 30 },
        new SanPham { Ma = "HM001", Ten = "Kh\u0103n gi\u1ea5y 4 l\u1edbp", DanhMucId = categories[3].Id, NhaCungCapId = suppliers[1].Id, GiaVon = 12_000m, GiaBan = 16_000m, DonVi = "cu\u1ed9n", TonKho = 80, MucCanNhapLai = 20 }
    };
    db.SanPhams.AddRange(products);

    var employees = new[]
    {
        new NhanVien { HoTen = "Nguy\u1ec5n Th\u1ecb Lan", ChucVu = "Qu\u1ea3n l\u00fd", DienThoai = "0910000001", Email = "lan@ministop.vn", Luong = 15000000m },
        new NhanVien { HoTen = "Tr\u1ea7n Minh Tu\u1ea5n", ChucVu = "Thu ng\u00e2n", DienThoai = "0910000002", Email = "tuan@ministop.vn", Luong = 9000000m },
        new NhanVien { HoTen = "Ph\u1ea1m H\u1ed3ng Nhung", ChucVu = "Pha ch\u1ebf", DienThoai = "0910000003", Email = "nhung@ministop.vn", Luong = 8500000m }
    };
    db.NhanViens.AddRange(employees);

    var customers = new[]
    {
        new KhachHang { HoTen = "L\u00ea V\u0103n D\u0169ng", DienThoai = "0930000001", Email = "dung@gmail.com", DiemThuong = 120 },
        new KhachHang { HoTen = "B\u00f9i Th\u1ecb H\u1ea1nh", DienThoai = "0930000002", Email = "hanh@gmail.com", DiemThuong = 85 },
        new KhachHang { HoTen = "\u0110\u1ed7 Minh Kh\u00f4i", DienThoai = "0930000003", Email = "khoi@gmail.com", DiemThuong = 60 }
    };
    db.KhachHangs.AddRange(customers);

    var promotions = new[]
    {
        new KhuyenMai { Ten = "Khuy\u1ebfn m\u00e3i cu\u1ed1i tu\u1ea7n", PhanTramGiam = 10m, NgayBatDau = DateTime.UtcNow.AddDays(-5), NgayKetThuc = DateTime.UtcNow.AddDays(10) },
        new KhuyenMai { Ten = "Mua 2 t\u1eb7ng 1", PhanTramGiam = 15m, NgayBatDau = DateTime.UtcNow, NgayKetThuc = DateTime.UtcNow.AddDays(20) }
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

static void ThemClaim(ClaimsIdentity? identity, string claimType, JsonElement element, string propertyName)
{
    if (identity == null ||
        !element.TryGetProperty(propertyName, out var property) ||
        property.ValueKind == JsonValueKind.Null)
    {
        return;
    }

    var value = property.ToString();
    if (!string.IsNullOrWhiteSpace(value))
    {
        identity.AddClaim(new Claim(claimType, value));
    }
}
