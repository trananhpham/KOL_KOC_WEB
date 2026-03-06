using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Services;
using KOL_KOC_TAAA.Models;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<IGroqService, GroqService>();

// Configure MoMo Options
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("Momo"));
builder.Services.AddHttpClient<IMomoService, MomoService>();

builder.Services.AddAuthentication("KolCookies")
    .AddCookie("KolCookies", options =>
    {
        options.Cookie.Name = "KolAuthCookie";
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<KolMarketplaceContext>(options =>
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
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<KolMarketplaceContext>();
        
        var adminRole = context.Roles.FirstOrDefault(r => r.Code == "Admin");
        if (adminRole == null)
        {
            adminRole = new Role { Code = "Admin", Name = "Quản trị viên" };
            context.Roles.Add(adminRole);
            context.SaveChanges();
        }

        var adminUser = context.Users.Include(u => u.Roles).FirstOrDefault(u => u.Email == "admin@kol.com");
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
            context.Users.Add(adminUser);
            context.SaveChanges();
        }
        else
        {
            if (!adminUser.Roles.Any(r => r.Code == "Admin"))
            {
                adminUser.Roles.Add(adminRole);
            }
            // Luôn đặt lại mật khẩu thành admin123 để đảm bảo đăng nhập thành công
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra khi tạo tài khoản Admin mặc định.");
    }
}

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
