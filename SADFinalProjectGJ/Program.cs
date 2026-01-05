using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Configuration;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Configure Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// 1. Register Email Service
builder.Services.AddTransient<IEmailService, EmailService>();

// 2. Register Background Services (Automation)
builder.Services.AddHostedService<InvoiceReminderService>();

// 3. Configure Stripe Payment Gateway
// Ideally, secrets should be loaded from Azure Key Vault or User Secrets in production
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// 4. Register Audit Service
builder.Services.AddScoped<IAuditService, AuditService>();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
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