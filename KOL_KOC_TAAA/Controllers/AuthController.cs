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
            claims.Add(new Claim(ClaimTypes.Role, role.Code));
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

        // Assign default Role code (must match DB unique constraint UQ__Roles__A25C5AA7A32F6946)
        string defaultCode = model.Role == "KOL" ? "KOL" : "CUSTOMER";
        
        // Find existing role by Code (case-insensitive) - ToUpper() is safer for some providers
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Code.ToUpper() == defaultCode.ToUpper());

        if (role == null)
        {
            // Seed the role ONLY if it truly doesn't exist
            role = new Role { 
                Code = defaultCode, 
                Name = (defaultCode == "KOL" ? "KOL" : "Khách hàng") 
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync(); // Commit role first to be absolutely sure
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = model.Email,
            FullName = model.Email.Split('@')[0], // Set default FullName as prefix of email
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Status = "active",
            Roles = new List<Role> { role }
        };

        _context.Users.Add(newUser);
        
        // Also create KolProfile if role is KOL
        if (defaultCode == "KOL")
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

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> SeedAdmin()
    {
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "Admin");
        if (adminRole == null)
        {
            adminRole = new Role { Code = "Admin", Name = "Quản trị viên" };
            _context.Roles.Add(adminRole);
            await _context.SaveChangesAsync();
        }

        var adminUser = await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == "admin@kol.com");
        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@kol.com",
                FullName = "Hệ thống Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Status = "active",
                Roles = new List<Role> { adminRole }
            };
            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();
        }
        else 
        {
            if (!adminUser.Roles.Any(r => r.Code == "Admin"))
            {
                adminUser.Roles.Add(adminRole);
            }
            // Reset lại mật khẩu phòng trường hợp tài khoản đã tạo từ trước với mật khẩu khác
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            await _context.SaveChangesAsync();
        }

        return Content("Đã thiết lập tài khoản Admin thành công! Username: admin@kol.com | Password: admin123");
    }
}
