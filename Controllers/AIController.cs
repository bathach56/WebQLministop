using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using WebQLministop.Data;

namespace WebQLministop.Controllers;

public class AIController : Controller
{
    private readonly ApplicationDbContext _context;

    public AIController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Hoi([FromBody] YeuCauAI request)
    {
        var cauHoi = request.CauHoi?.Trim();
        if (string.IsNullOrWhiteSpace(cauHoi))
        {
            return Json(new { traLoi = "Vui long nhap cau hoi." });
        }

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Json(new
            {
                traLoi = "Chua cau hinh GEMINI_API_KEY. Hay them bien moi truong GEMINI_API_KEY roi chay lai project."
            });
        }

        var model = Environment.GetEnvironmentVariable("GEMINI_MODEL");
        if (string.IsNullOrWhiteSpace(model))
        {
            model = "gemini-2.0-flash";
        }

        var prompt = await TaoPrompt(cauHoi);
        var traLoi = await GoiGemini(apiKey, model, prompt);

        return Json(new { traLoi });
    }

    private async Task<string> TaoPrompt(string cauHoi)
    {
        var sanPhamsSapHet = await _context.SanPhams
            .Where(s => s.KichHoat && s.TonKho <= s.MucCanNhapLai)
            .OrderBy(s => s.TonKho)
            .Take(8)
            .Select(s => $"{s.Ma} - {s.Ten}: ton {s.TonKho}, muc can nhap lai {s.MucCanNhapLai}, gia ban {s.GiaBan:N0}")
            .ToListAsync();

        var donHangsGanDay = await _context.DonHangs
            .Include(d => d.NhanVien)
            .OrderByDescending(d => d.NgayDat)
            .Take(5)
            .Select(d => $"HD #{d.Id}: {d.TongTien:N0}, {d.TrangThai}, NV {d.NhanVien!.HoTen}")
            .ToListAsync();

        var tongSanPham = await _context.SanPhams.CountAsync(s => s.KichHoat);
        var tongKhachHang = await _context.KhachHangs.CountAsync(k => k.KichHoat);

        return $"""
        Ban la tro ly AI cho web quan ly cua hang MiniStop.
        Hay tra loi ngan gon bang tieng Viet, uu tien nghiep vu nhan vien cua hang.
        Khong tu tao du lieu neu khong co trong thong tin ben duoi.

        Du lieu he thong:
        - So san pham dang ban: {tongSanPham}
        - So khach hang dang hoat dong: {tongKhachHang}
        - San pham sap het hang: {string.Join("; ", sanPhamsSapHet.DefaultIfEmpty("khong co"))}
        - Don hang gan day: {string.Join("; ", donHangsGanDay.DefaultIfEmpty("khong co"))}

        Cau hoi cua nguoi dung: {cauHoi}
        """;
    }

    private static async Task<string> GoiGemini(string apiKey, string model, string prompt)
    {
        using var client = new HttpClient();

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = "Tra loi nhu tro ly nghiep vu MiniStop. Khong noi dai.\n\n" + prompt
                        }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 600
            }
        };

        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        using var response = await client.PostAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return $"Khong goi duoc Gemini. Ma loi: {(int)response.StatusCode}. Noi dung: {responseText}";
        }

        using var document = JsonDocument.Parse(responseText);
        if (document.RootElement.TryGetProperty("candidates", out var candidates))
        {
            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var candidateContent) ||
                    !candidateContent.TryGetProperty("parts", out var parts))
                {
                    continue;
                }

                var builder = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text))
                    {
                        builder.Append(text.GetString());
                    }
                }

                if (builder.Length > 0)
                {
                    return builder.ToString();
                }
            }
        }

        return "Gemini khong tra ve noi dung.";
    }
}

public class YeuCauAI
{
    public string? CauHoi { get; set; }
}
