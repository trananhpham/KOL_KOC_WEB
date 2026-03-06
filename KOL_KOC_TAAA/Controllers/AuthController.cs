using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KOL_KOC_TAAA.Controllers;

public class AuthController : Controller
{
    private readonly KolMarketplaceContext _context;

    public AuthController(KolMarketplaceContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        if (user.Status != "active")
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị vô hiệu hóa.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName ?? user.Email)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }

        var identity = new ClaimsIdentity(claims, "KolCookies");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("KolCookies", principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTime.UtcNow.AddHours(8)
        });

        // Set simple session data
        HttpContext.Session.SetString("UserName", user.FullName ?? user.Email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            return View(model);
        }

        // Assign default Role based on selection (Customer or KOL)
        string defaultRoleName = model.Role == "KOL" ? "KOL" : "Customer";
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == defaultRoleName);
        if (role == null)
        {
            // Seed the role if it doesn't exist just in case
            role = new Role { Name = defaultRoleName, Code = defaultRoleName.ToUpper() };
            _context.Roles.Add(role);
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = model.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Status = "active",
            Roles = new List<Role> { role }
        };

        _context.Users.Add(newUser);
        
        // Also create KolProfile if role is KOL
        if (defaultRoleName == "KOL")
        {
            _context.KolProfiles.Add(new KolProfile { UserId = newUser.Id, InfluencerType = "Nano", Bio = "" });
        }
        else
        {
            _context.UserWallets.Add(new UserWallet { UserId = newUser.Id, Balance = 0, LockedBalance = 0, Currency = "VND" });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("KolCookies");
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
