using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace WebQLministop.Controllers;

public class AuthController : Controller
{
    private static readonly string[] Providers = ["Google", "Facebook", "Twitter", "GitHub"];

    public IActionResult DangNhap(string? provider)
    {
        ViewBag.Providers = Providers;

        if (string.IsNullOrWhiteSpace(provider))
        {
            return View();
        }

        if (!Providers.Contains(provider))
        {
            ViewBag.Loi = "Nha cung cap OAuth khong hop le. Vui long chon lai.";
            return View();
        }

        if (!DaCauHinh(provider))
        {
            ViewBag.Loi = $"Chua cau hinh {provider}. Hay them {provider.ToUpperInvariant()}_CLIENT_ID va {provider.ToUpperInvariant()}_CLIENT_SECRET, sau do chon lai.";
            return View();
        }

        var redirectUrl = Url.Action(nameof(DangNhapThanhCong), "Auth");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    public IActionResult DangNhapThanhCong()
    {
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> DangXuat()
    {
        await HttpContext.SignOutAsync("MiniStopCookie");
        return RedirectToAction("Index", "Home");
    }

    private static bool DaCauHinh(string provider)
    {
        var prefix = provider.ToUpperInvariant();
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable($"{prefix}_CLIENT_ID")) &&
               !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable($"{prefix}_CLIENT_SECRET"));
    }
}
