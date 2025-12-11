using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using Stripe;
using SADFinalProjectGJ.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// 1. 注册邮件服务 (你之前加的)
builder.Services.AddTransient<IEmailService, EmailService>();

// 2. 注册后台自动化服务
builder.Services.AddHostedService<InvoiceReminderService>();

// ... ?????? ...

// ???????? User Secrets ??? Stripe
// ?????? "Stripe:SecretKey" ???? secrets.json ??????????
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();
// ... 前面有很多 builder.Services.Add... 代码 ...

// ==================== ✅ 开始插入这段代码 ====================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // 这里定义你需要的所有角色
        var roles = new[] { "Admin", "FinanceStaff", "Client" };

        foreach (var role in roles)
        {
            // 如果角色不存在，就创建一个
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "在创建角色时发生了错误");
    }
}
// ==================== 结束插入 ====================

// ... 后面是 app.UseHttpsRedirection(); 等等 ...

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAuthentication();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
