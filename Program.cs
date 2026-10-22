using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using nhdphuong_1787_api.Models;
using nhdphuong_1787_api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database Context (Tự động hỗ trợ SQLite độc lập hoặc PostgreSQL)
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=childguard.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connStr.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) || connStr.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connStr);
    }
    else
    {
        options.UseNpgsql(connStr);
    }
});

// 2. Cấu hình ASP.NET Core Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Đăng ký Services vào DI Container
builder.Services.AddHttpClient();
builder.Services.AddScoped<ISmsGatewayService, SmsGatewayService>();
builder.Services.AddScoped<IEmailSenderService, EmailSenderService>();

// 4. Thêm Controllers & Bỏ qua vòng lặp tham chiếu JSON (ReferenceHandler.IgnoreCycles)
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// 5. Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JWTKey");
var secret = jwtSettings["Secret"] ?? "AReallyLongSecretKeyForYourJWTTokenSigningMustBeAtLeast32BytesLong!";
var key = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["ValidIssuer"] ?? "NHDPhuong_API",
        ValidAudience = jwtSettings["ValidAudience"] ?? "NHDPhuong_FlutterApp",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// 6. Cấu hình Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 7. Cấu hình CORS để Flutter Mobile & Emulator kết nối thông suốt
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// 8. Tự động khởi tạo CSDL & Seed Data cho cả 3 Roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        await db.Database.EnsureCreatedAsync();

        // Seed Roles
        var roles = new[] { "Admin", "Staff", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Admin Account
        var admin = await userManager.FindByNameAsync("admin1787");
        if (admin == null)
        {
            admin = new User
            {
                UserName = "admin1787",
                Email = "admin1787@childguard.local",
                FullName = "Quản trị viên Hệ thống (Admin)",
                Role = "Admin",
                PhoneNumber = "0901787001",
                SecurityStamp = Guid.NewGuid().ToString()
            };
            await userManager.CreateAsync(admin, "Password123@");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Seed Staff Account
        var staff = await userManager.FindByNameAsync("staff1787");
        if (staff == null)
        {
            staff = new User
            {
                UserName = "staff1787",
                Email = "staff1787@childguard.local",
                FullName = "Kỹ thuật viên Kiểm duyệt (Staff)",
                Role = "Staff",
                PhoneNumber = "0901787002",
                SecurityStamp = Guid.NewGuid().ToString()
            };
            await userManager.CreateAsync(staff, "Password123@");
            await userManager.AddToRoleAsync(staff, "Staff");
        }

        // Seed Customer (Parent) Account
        var parent = await userManager.FindByNameAsync("parent1787");
        if (parent == null)
        {
            parent = new User
            {
                UserName = "parent1787",
                Email = "parent1787@childguard.local",
                FullName = "Phụ huynh Duy Phương (Parent)",
                Role = "Customer",
                PhoneNumber = "0901787187",
                SecurityStamp = Guid.NewGuid().ToString()
            };
            await userManager.CreateAsync(parent, "Password123@");
            await userManager.AddToRoleAsync(parent, "Customer");

            // Seed 2 Hồ sơ trẻ em mẫu
            var child1 = new ChildProfile
            {
                ParentId = parent.Id,
                FullName = "Bé Bo",
                Age = 8,
                AvatarUrl = "boy",
                DeviceName = "Samsung Galaxy Tab A9",
                DeviceModel = "SM-X110 (Android Emulator)",
                PairingCode = "178701",
                ChildUsername = "bebo1787",
                ChildPassword = "Password123@",
                BatteryLevel = 82,
                IsOnline = true,
                IsLocked = false,
                LastKnownLat = 10.762622,
                LastKnownLng = 106.660172,
                UpdatedAt = DateTime.UtcNow
            };

            var child2 = new ChildProfile
            {
                ParentId = parent.Id,
                FullName = "Bé Na",
                Age = 12,
                AvatarUrl = "girl",
                DeviceName = "Redmi Note 13",
                DeviceModel = "23129RAA4G",
                PairingCode = "178702",
                ChildUsername = "bena1787",
                ChildPassword = "Password123@",
                BatteryLevel = 95,
                IsOnline = true,
                IsLocked = false,
                LastKnownLat = 10.776889,
                LastKnownLng = 106.700806,
                UpdatedAt = DateTime.UtcNow
            };

            db.ChildProfiles.AddRange(child1, child2);
            await db.SaveChangesAsync();

            // Seed AppRules cho Bé Bo
            db.AppRules.AddRange(
                new AppRule { ChildProfileId = child1.Id, AppName = "TikTok", Category = "SocialMedia", Status = "Blocked", DailyLimitMinutes = 0, UsedMinutesToday = 0 },
                new AppRule { ChildProfileId = child1.Id, AppName = "YouTube", Category = "Entertainment", Status = "Limited", DailyLimitMinutes = 45, UsedMinutesToday = 20 },
                new AppRule { ChildProfileId = child1.Id, AppName = "Duolingo", Category = "Education", Status = "Allowed", DailyLimitMinutes = 120, UsedMinutesToday = 30 },
                new AppRule { ChildProfileId = child1.Id, AppName = "Roblox", Category = "Game", Status = "Limited", DailyLimitMinutes = 30, UsedMinutesToday = 15 },
                // AppRules cho Bé Na
                new AppRule { ChildProfileId = child2.Id, AppName = "Facebook", Category = "SocialMedia", Status = "Blocked", DailyLimitMinutes = 0, UsedMinutesToday = 0 },
                new AppRule { ChildProfileId = child2.Id, AppName = "YouTube", Category = "Entertainment", Status = "Limited", DailyLimitMinutes = 60, UsedMinutesToday = 40 }
            );

            // Seed Schedules
            db.ScreenTimeSchedules.AddRange(
                new ScreenTimeSchedule
                {
                    ChildProfileId = child1.Id,
                    ScheduleTitle = "Khung giờ học bài tối",
                    DayOfWeek = "Thứ 2 - Thứ 6",
                    AllowedStartTime = new TimeSpan(19, 0, 0),
                    AllowedEndTime = new TimeSpan(21, 0, 0),
                    MaxDailyHours = 2,
                    IsActive = true
                },
                new ScreenTimeSchedule
                {
                    ChildProfileId = child2.Id,
                    ScheduleTitle = "Giải trí cuối tuần",
                    DayOfWeek = "Thứ 7 - Chủ Nhật",
                    AllowedStartTime = new TimeSpan(14, 0, 0),
                    AllowedEndTime = new TimeSpan(17, 0, 0),
                    MaxDailyHours = 3,
                    IsActive = true
                }
            );

            // Seed AlertLogs mẫu
            db.AlertLogs.AddRange(
                new AlertLog
                {
                    ChildProfileId = child1.Id,
                    AlertType = "EYE_DISTANCE",
                    Value = "18 cm",
                    Severity = "Warning",
                    Message = "Bé Bo để máy quá gần mắt (<20cm) lúc 20:15",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-25)
                },
                new AlertLog
                {
                    ChildProfileId = child1.Id,
                    AlertType = "NOISE_DANGER",
                    Value = "86 dB",
                    Severity = "Danger",
                    Message = "Phát hiện tiếng ồn lớn 86 dB xung quanh bé Bo",
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                }
            );

            await db.SaveChangesAsync();
        }

        // Seed Thư viện mẫu cho Staff nếu rỗng
        if (!await db.StaffCatalogItems.AnyAsync())
        {
            db.StaffCatalogItems.AddRange(
                new StaffCatalogItem { AppName = "TikTok", Category = "SocialMedia", RiskLevel = "High", Description = "Mạng xã hội video ngắn dễ gây nghiện và khó kiểm soát nội dung độc hại" },
                new StaffCatalogItem { AppName = "Roblox", Category = "Game", RiskLevel = "Medium", Description = "Game nhiều người chơi trực tuyến, cần cảnh giác lừa đảo nạp thẻ Robux" },
                new StaffCatalogItem { AppName = "Web Cờ Bạc / Game Bài", Category = "Gambling", RiskLevel = "High", Description = "Các trang cá cược trực tuyến, cấm trẻ vị thành niên truy cập" },
                new StaffCatalogItem { AppName = "Duolingo", Category = "Education", RiskLevel = "Low", Description = "Ứng dụng học ngoại ngữ an toàn, nên khuyến khích trẻ sử dụng" }
            );
            await db.SaveChangesAsync();
        }

        logger.LogInformation(">>> [DATABASE SEED] Khởi tạo CSDL & Seed Data thành công rực rỡ!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo hoặc Seed CSDL.");
    }
}

// 9. Pipeline xử lý HTTP Request
app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Child Phone Guard API v1");
    c.RoutePrefix = "swagger";
});

// Cho phép truy cập cả trang chủ '/' lẫn '/swagger'
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
