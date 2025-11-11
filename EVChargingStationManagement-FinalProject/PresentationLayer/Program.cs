using BusinessLayer.Services;
using DataAccessLayer.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Diagnostics;
using PresentationLayer.Hubs;
using PresentationLayer.Services;

namespace PresentationLayer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            //ConnectionString
            builder.Services.AddDbContext<EVDbContext>(options =>
               options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Configure JWT Settings
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            // Configure Google OAuth Settings
            var googleOAuth = builder.Configuration.GetSection("GoogleOAuth");
            var googleClientId = googleOAuth["ClientId"];
            var googleClientSecret = googleOAuth["ClientSecret"];

            // Add Authentication
            builder.Services.AddAuthentication(options =>
            {
                // Default scheme cho Razor Pages
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = googleClientId!;
                options.ClientSecret = googleClientSecret!;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = true;
                
                // Request thêm scopes để lấy thông tin profile đầy đủ
                // Lưu ý: Google không cung cấp DateOfBirth vì đây là thông tin nhạy cảm
                options.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
                options.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();

            // Register Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IChargingStationService, ChargingStationService>();
            builder.Services.AddScoped<IChargingSpotService, ChargingSpotService>();
            builder.Services.AddScoped<IStationMaintenanceService, StationMaintenanceService>();
            builder.Services.AddScoped<IStationErrorService, StationErrorService>();
            builder.Services.AddScoped<IVehicleService, VehicleService>();
            builder.Services.AddScoped<IReservationService, ReservationService>();
            builder.Services.AddScoped<IChargingSessionService, ChargingSessionService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IStationAmenityService, StationAmenityService>();
            builder.Services.AddScoped<IRealtimeNotifier, StationHubNotifier>();
            builder.Services.AddScoped<IStationDataMergeService, StationDataMergeService>();
            builder.Services.AddScoped<IQrCodeService, QrCodeService>();
            builder.Services.AddScoped<IChargingProgressService>(sp =>
            {
                var context = sp.GetRequiredService<EVDbContext>();
                var notifier = sp.GetService<IRealtimeNotifier>();
                return notifier != null 
                    ? new ChargingProgressService(context, notifier)
                    : new ChargingProgressService(context);
            });
            builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
            builder.Services.AddScoped<IReportingService, ReportingService>();
            builder.Services.AddScoped<IVnPayService, VnPayService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddHostedService<RefreshTokenCleanupService>();

            // Add Controllers with JSON options
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });
            builder.Services.AddSignalR();
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<EVDbContext>("database");
            // HttpClient for external APIs (SerpApi)
            builder.Services.AddHttpClient();

            // Add Swagger for API testing
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "EV Charging Station API", Version = "v1" });
                
                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            // Seed database with default users
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EVDbContext>();
                DataSeeder.SeedAsync(db).GetAwaiter().GetResult();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            else
            {
                // Enable Swagger in Development
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EV Charging Station API v1");
                });
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/health"))
                {
                    await next();
                    return;
                }

                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RequestTiming");
                var sw = Stopwatch.StartNew();
                try
                {
                    await next();
                }
                finally
                {
                    sw.Stop();
                    logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Elapsed} ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        sw.ElapsedMilliseconds);
                }
            });

            // Authentication must come before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();
            app.MapHub<StationHub>("/hubs/station");
            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
