using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using SmartMaintenance.Dispatcher.Data;
using SmartMaintenance.Dispatcher.Hubs;
using SmartMaintenance.Dispatcher.Services;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// 1. تسجيل اتصال قاعدة البيانات SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. تسجيل خدمات محرك التعيين والتحليلات
builder.Services.AddScoped<ITechnicianAssignmentService, TechnicianAssignmentService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// 3. تسجيل سيرفر SignalR للإشعارات اللحظية
builder.Services.AddSignalR();

// 4. تسجيل الـ Controllers و CORS
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

var app = builder.Build();

// تشغيل الـ Seeder تلقائياً لإنشاء الجداول وحفظ البيانات
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.SeedAsync(context);
}

app.UseCors("AllowAll");

// تفعيل ملفات واجهة الويب التفاعلية (HTML/CSS/JS)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// ربط SignalR Hub والـ Controllers
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapControllers();

app.Run();
