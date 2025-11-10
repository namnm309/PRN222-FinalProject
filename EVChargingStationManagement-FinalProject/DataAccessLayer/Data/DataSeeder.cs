using System;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(EVDbContext context)
        {
            // Đảm bảo DB đã được tạo/migration áp dụng
            await context.Database.MigrateAsync();

            var nowUtc = DateTime.UtcNow;
            var minDateUtc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

            // Thêm từng tài khoản nếu chưa tồn tại (kiểm tra theo Username hoặc Email)

            if (!await context.Users.AnyAsync(u => u.Username == "admin" || u.Email == "admin@example.com"))
            {
                await context.Users.AddAsync(new Users
                {
                    Id = Guid.NewGuid(),
                    FullName = "System Administrator",
                    Username = "admin",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Phone = "",
                    Email = "admin@example.com",
                    DateOfBirth = minDateUtc,
                    Gender = "Unknown",
                    IsActive = true,
                    Role = UserRole.Admin,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                });
            }

            if (!await context.Users.AnyAsync(u => u.Username == "staff" || u.Email == "staff@example.com"))
            {
                await context.Users.AddAsync(new Users
                {
                    Id = Guid.NewGuid(),
                    FullName = "Charging Station Staff",
                    Username = "staff",
                    Password = BCrypt.Net.BCrypt.HashPassword("staff123"),
                    Phone = "",
                    Email = "staff@example.com",
                    DateOfBirth = minDateUtc,
                    Gender = "Unknown",
                    IsActive = true,
                    Role = UserRole.CSStaff,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                });
            }

            if (!await context.Users.AnyAsync(u => u.Username == "evdriver" || u.Email == "evdriver@example.com"))
            {
                await context.Users.AddAsync(new Users
                {
                    Id = Guid.NewGuid(),
                    FullName = "EV Driver",
                    Username = "evdriver",
                    Password = BCrypt.Net.BCrypt.HashPassword("driver123"),
                    Phone = "",
                    Email = "evdriver@example.com",
                    DateOfBirth = minDateUtc,
                    Gender = "Unknown",
                    IsActive = true,
                    Role = UserRole.EVDriver,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                });
            }

            await context.SaveChangesAsync();
        }
    }
}


