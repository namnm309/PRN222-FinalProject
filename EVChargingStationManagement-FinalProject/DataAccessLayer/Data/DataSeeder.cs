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

            // Seed Charging Stations nếu chưa có
            if (!await context.ChargingStations.AnyAsync())
            {
                var stations = new[]
                {
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Trạm sạc VinFast Times City",
                        Address = "458 Minh Khai, Hai Bà Trưng, Hà Nội",
                        City = "Hà Nội",
                        Province = "Hà Nội",
                        Latitude = 21.0085m,
                        Longitude = 105.8542m,
                        Status = StationStatus.Active,
                        Is24Hours = true,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    },
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Trạm sạc Vincom Mega Mall",
                        Address = "Royal City, Thanh Xuân, Hà Nội",
                        City = "Hà Nội",
                        Province = "Hà Nội",
                        Latitude = 21.0055m,
                        Longitude = 105.8435m,
                        Status = StationStatus.Active,
                        Is24Hours = false,
                        OpeningTime = TimeOnly.Parse("08:00"),
                        ClosingTime = TimeOnly.Parse("22:00"),
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    },
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Trạm sạc Aeon Mall Long Biên",
                        Address = "27 Cổ Linh, Long Biên, Hà Nội",
                        City = "Hà Nội",
                        Province = "Hà Nội",
                        Latitude = 21.0175m,
                        Longitude = 105.9185m,
                        Status = StationStatus.Active,
                        Is24Hours = false,
                        OpeningTime = TimeOnly.Parse("09:00"),
                        ClosingTime = TimeOnly.Parse("22:00"),
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    },
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Trạm sạc Lotte Mall",
                        Address = "Lieu Giai, Ba Đình, Hà Nội",
                        City = "Hà Nội",
                        Province = "Hà Nội",
                        Latitude = 21.0375m,
                        Longitude = 105.8145m,
                        Status = StationStatus.Active,
                        Is24Hours = true,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    },
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Trạm sạc Vincom Center",
                        Address = "Bà Triệu, Hoàn Kiếm, Hà Nội",
                        City = "Hà Nội",
                        Province = "Hà Nội",
                        Latitude = 21.0245m,
                        Longitude = 105.8525m,
                        Status = StationStatus.Active,
                        Is24Hours = false,
                        OpeningTime = TimeOnly.Parse("08:00"),
                        ClosingTime = TimeOnly.Parse("23:00"),
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    },
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Trạm sạc Big C Thăng Long",
                        Address = "Đường Thăng Long, Nam Từ Liêm, Hà Nội",
                        City = "Hà Nội",
                        Province = "Hà Nội",
                        Latitude = 21.0505m,
                        Longitude = 105.7545m,
                        Status = StationStatus.Active,
                        Is24Hours = true,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    }
                };

                await context.ChargingStations.AddRangeAsync(stations);
                await context.SaveChangesAsync();

                // Seed Charging Spots cho mỗi trạm
                foreach (var station in stations)
                {
                    var spots = new List<ChargingSpot>();
                    var spotCount = station.Name.Contains("Times City") ? 8 :
                                   station.Name.Contains("Mega Mall") ? 6 :
                                   station.Name.Contains("Aeon") ? 4 :
                                   station.Name.Contains("Lotte") ? 10 :
                                   station.Name.Contains("Vincom Center") ? 4 : 6;

                    for (int i = 1; i <= spotCount; i++)
                    {
                        var isAvailable = i <= (spotCount * 0.5); // 50% available
                        spots.Add(new ChargingSpot
                        {
                            Id = Guid.NewGuid(),
                            SpotNumber = $"SP{i:D2}",
                            ChargingStationId = station.Id,
                            Status = isAvailable ? SpotStatus.Available : SpotStatus.Occupied,
                            ConnectorType = i % 2 == 0 ? "CCS" : "Type 2",
                            PowerOutput = i % 2 == 0 ? 50 : 22,
                            PricePerKwh = 3500 + (i * 100),
                            CreatedAt = nowUtc,
                            UpdatedAt = nowUtc
                        });
                    }

                    await context.ChargingSpots.AddRangeAsync(spots);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}


