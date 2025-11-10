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

            // Seed Charging Stations if none exist
            if (!await context.ChargingStations.AnyAsync())
            {
                var stations = new List<ChargingStation>
                {
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "VinFast Charging Station",
                        Address = "9 Đình Tiên Hoàng, Da Kao, Quận 1",
                        City = "Thành phố Hồ Chí Minh",
                        Province = "Hồ Chí Minh",
                        PostalCode = "700000",
                        Latitude = 10.7879m,
                        Longitude = 106.7029m,
                        Phone = "1900545591",
                        Email = "support@vinfastauto.com",
                        Status = StationStatus.Active,
                        Description = "Trạm sạc VinFast tại trung tâm Quận 1",
                        Is24Hours = true,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    },
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "EV ONE Charging Station",
                        Address = "33 Lê Duẩn, Bến Nghé, Quận 1",
                        City = "Thành phố Hồ Chí Minh",
                        Province = "Hồ Chí Minh",
                        PostalCode = "700000",
                        Latitude = 10.7769m,
                        Longitude = 106.7009m,
                        Phone = "0283521234",
                        Email = "info@evone.vn",
                        Status = StationStatus.Active,
                        Description = "Trạm sạc EV ONE gần công viên Lê Văn Tám",
                        OpeningTime = new TimeOnly(6, 0),
                        ClosingTime = new TimeOnly(22, 0),
                        Is24Hours = false,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    },
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Electric Vehicle Charging Station",
                        Address = "110 Đường Số 14, Khu đô thị Him Lam, Quận 7",
                        City = "Thành phố Hồ Chí Minh",
                        Province = "Hồ Chí Minh",
                        PostalCode = "700000",
                        Latitude = 10.7343m,
                        Longitude = 106.7212m,
                        Phone = "0287654321",
                        Email = "contact@evcharging.vn",
                        Status = StationStatus.Active,
                        Description = "Trạm sạc tại khu đô thị Him Lam Quận 7",
                        Is24Hours = true,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    },
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Charge+ Charging Station",
                        Address = "68 Nguyễn Thị Diệu, Phường 6, Quận 3",
                        City = "Thành phố Hồ Chí Minh",
                        Province = "Hồ Chí Minh",
                        PostalCode = "700000",
                        Latitude = 10.7756m,
                        Longitude = 106.6878m,
                        Phone = "0289876543",
                        Email = "support@chargeplus.vn",
                        Status = StationStatus.Active,
                        Description = "Trạm sạc Charge+ Quận 3",
                        OpeningTime = new TimeOnly(7, 0),
                        ClosingTime = new TimeOnly(23, 0),
                        Is24Hours = false,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    },
                    new ChargingStation
                    {
                        Id = Guid.NewGuid(),
                        Name = "EV Power Charging Station",
                        Address = "332 Đường Nguyễn Văn Linh, Bình Thuận, Quận 7",
                        City = "Thành phố Hồ Chí Minh",
                        Province = "Hồ Chí Minh",
                        PostalCode = "72914",
                        Latitude = 10.7412m,
                        Longitude = 106.7156m,
                        Phone = "0281234567",
                        Email = "info@evpower.vn",
                        Status = StationStatus.Active,
                        Description = "Trạm sạc nhanh EV Power",
                        Is24Hours = true,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    }
                };

                await context.ChargingStations.AddRangeAsync(stations);
                await context.SaveChangesAsync();

                // Seed Charging Spots for each station
                foreach (var station in stations)
                {
                    var spots = new List<ChargingSpot>
                    {
                        new ChargingSpot
                        {
                            Id = Guid.NewGuid(),
                            SpotNumber = "A01",
                            ChargingStationId = station.Id,
                            Status = SpotStatus.Available,
                            ConnectorType = "Type 2",
                            PowerOutput = 50,
                            PricePerKwh = 3500,
                            Description = "Fast charging spot",
                            CreatedAt = nowUtc,
                            UpdatedAt = nowUtc
                        },
                        new ChargingSpot
                        {
                            Id = Guid.NewGuid(),
                            SpotNumber = "A02",
                            ChargingStationId = station.Id,
                            Status = SpotStatus.Available,
                            ConnectorType = "CCS",
                            PowerOutput = 100,
                            PricePerKwh = 4000,
                            Description = "DC Fast charging",
                            CreatedAt = nowUtc,
                            UpdatedAt = nowUtc
                        },
                        new ChargingSpot
                        {
                            Id = Guid.NewGuid(),
                            SpotNumber = "A03",
                            ChargingStationId = station.Id,
                            Status = SpotStatus.Available,
                            ConnectorType = "CHAdeMO",
                            PowerOutput = 50,
                            PricePerKwh = 3500,
                            Description = "CHAdeMO charging",
                            CreatedAt = nowUtc,
                            UpdatedAt = nowUtc
                        },
                        new ChargingSpot
                        {
                            Id = Guid.NewGuid(),
                            SpotNumber = "A04",
                            ChargingStationId = station.Id,
                            Status = SpotStatus.Available,
                            ConnectorType = "Type 2",
                            PowerOutput = 22,
                            PricePerKwh = 3000,
                            Description = "AC charging",
                            CreatedAt = nowUtc,
                            UpdatedAt = nowUtc
                        }
                    };

                    await context.ChargingSpots.AddRangeAsync(spots);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}


