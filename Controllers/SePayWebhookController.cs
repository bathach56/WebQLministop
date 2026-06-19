using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using WebQLministop.Data;

namespace WebQLministop.Controllers;

[ApiController]
[Route("api/sepay")]
public class SePayWebhookController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SePayWebhookController> _logger;

    public SePayWebhookController(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<SePayWebhookController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        if (!KiemTraBaoMat(body))
        {
            _logger.LogWarning("SePAY webhook bi tu choi do xac thuc khong hop le.");
            return Unauthorized(new { success = false, message = "Unauthorized" });
        }

        SePayWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SePayWebhookPayload>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "SePAY webhook payload khong phai JSON hop le. Body: {Body}", body);
            return BadRequest(new { success = false, message = "Invalid JSON" });
        }

        if (payload == null)
        {
            _logger.LogWarning("SePAY webhook khong doc duoc payload JSON.");
            return BadRequest(new { success = false, message = "Invalid payload" });
        }

        var maDonHang = LayMaDonHang(payload.Content, payload.Description, payload.Code);
        if (maDonHang == null || payload.TransferAmount <= 0)
        {
            _logger.LogInformation(
                "SePAY webhook bo qua vi khong tim thay ma don hang hoac so tien khong hop le. Content: {Content}, Description: {Description}, Code: {Code}, Amount: {Amount}",
                payload.Content,
                payload.Description,
                payload.Code,
                payload.TransferAmount);
            return Ok(new { success = true });
        }

        var donHang = await _context.DonHangs
            .Include(d => d.KhachHang)
            .Include(d => d.ChiTiet)
            .ThenInclude(c => c.SanPham)
            .FirstOrDefaultAsync(d =>
                d.Id == maDonHang.Value &&
                d.PhuongThucThanhToan == "ChuyenKhoan" &&
                d.TrangThai == "DangXuLy");

        if (donHang == null || payload.TransferAmount < donHang.TongTien)
        {
            _logger.LogInformation(
                "SePAY webhook khong khop don hang. DonHangId: {DonHangId}, Amount: {Amount}",
                maDonHang,
                payload.TransferAmount);
            return Ok(new { success = true });
        }

        if (!LaGiaoDichTienVao(payload.TransferType) &&
            !string.IsNullOrWhiteSpace(payload.TransferType))
        {
            _logger.LogInformation("SePAY webhook bo qua giao dich tien ra. DonHangId: {DonHangId}", maDonHang);
            return Ok(new { success = true });
        }

        if (donHang.NgayDat.AddMinutes(5) < DateTime.UtcNow)
        {
            donHang.TrangThai = "DaHuy";
            donHang.GhiChuThanhToan = $"{donHang.GhiChuThanhToan} SePAY báo giao dịch sau khi đơn đã quá hạn 5 phút nên đơn không được xác nhận.";
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
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        donHang.TrangThai = "DaThanhToan";
        donHang.GhiChuThanhToan = $"{donHang.GhiChuThanhToan} SePAY đã xác nhận nhận tiền: {payload.TransferAmount:N0}đ.";
        if (donHang.KhachHang != null && donHang.DiemThuongCong > 0)
        {
            donHang.KhachHang.DiemThuong += donHang.DiemThuongCong;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("SePAY webhook da xac nhan thanh toan don hang #{DonHangId}.", donHang.Id);
        return Ok(new { success = true });
    }

    private bool KiemTraBaoMat(string body)
    {
        var secret = _configuration["SePay:WebhookApiKey"];
        if (string.IsNullOrWhiteSpace(secret)) return true;

        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = authorization.Substring("Apikey ".Length).Trim();
            return string.Equals(apiKey, secret, StringComparison.Ordinal);
        }

        var xApiKey = Request.Headers["X-API-KEY"].ToString();
        if (!string.IsNullOrWhiteSpace(xApiKey))
        {
            return string.Equals(xApiKey, secret, StringComparison.Ordinal);
        }

        var signature = Request.Headers["X-SePay-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature)) return false;

        if (signature.StartsWith("sha256="))
            signature = signature.Substring(7);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expectedSignature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    private static int? LayMaDonHang(params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;

            var match = Regex.Match(value, @"(?:DH|HD)[\s-]*0*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
            {
                return id;
            }

            match = Regex.Match(value, @"#\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out id))
            {
                return id;
            }
        }

        return null;
    }

    private static bool LaGiaoDichTienVao(string? transferType)
    {
        if (string.IsNullOrWhiteSpace(transferType)) return true;

        var text = transferType.Trim().ToLowerInvariant();
        return text is "in" or "income" or "credit" or "deposit" or "receive";
    }
}

public class SePayWebhookPayload
{
    [JsonPropertyName("transferType")]
    public string? TransferType { get; set; }

    [JsonPropertyName("transferAmount")]
    public decimal TransferAmount { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}
