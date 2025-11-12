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

            var driverUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "evdriver" || u.Email == "evdriver@example.com");
            if (driverUser == null)
            {
                driverUser = new Users
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
                };

                await context.Users.AddAsync(driverUser);
            }

            await context.SaveChangesAsync();

            // Seed sample charging station with spots and amenities
            if (!await context.ChargingStations.AnyAsync())
            {
                var stationId = Guid.NewGuid();
                var station = new ChargingStation
                {
                    Id = stationId,
                    Name = "EV City Center",
                    Address = "123 Electric Avenue",
                    City = "Ho Chi Minh City",
                    Province = "HCMC",
                    PostalCode = "700000",
                    Latitude = 10.776530m,
                    Longitude = 106.700981m,
                    Phone = "0123456789",
                    Email = "center@evstations.vn",
                    Status = StationStatus.Active,
                    Description = "Trạm sạc trung tâm với nhiều đầu sạc nhanh.",
                    OpeningTime = new TimeOnly(6, 0),
                    ClosingTime = new TimeOnly(22, 0),
                    Is24Hours = false,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                await context.ChargingStations.AddAsync(station);

                var spotFast = new ChargingSpot
                {
                    Id = Guid.NewGuid(),
                    SpotNumber = "A1",
                    ChargingStationId = stationId,
                    Status = SpotStatus.Available,
                    ConnectorType = "CCS",
                    PowerOutput = 120m,
                    PricePerKwh = 4500m,
                    Description = "Đầu sạc nhanh CCS",
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                var spotStandard = new ChargingSpot
                {
                    Id = Guid.NewGuid(),
                    SpotNumber = "A2",
                    ChargingStationId = stationId,
                    Status = SpotStatus.Available,
                    ConnectorType = "Type2",
                    PowerOutput = 22m,
                    PricePerKwh = 3200m,
                    Description = "Đầu sạc Type 2 AC",
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                await context.ChargingSpots.AddRangeAsync(spotFast, spotStandard);

                var amenities = new[]
                {
                    new StationAmenity
                    {
                        Id = Guid.NewGuid(),
                        ChargingStationId = stationId,
                        Name = "Wifi miễn phí",
                        Description = "Kết nối Internet tốc độ cao",
                        IsActive = true,
                        DisplayOrder = 1,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    },
                    new StationAmenity
                    {
                        Id = Guid.NewGuid(),
                        ChargingStationId = stationId,
                        Name = "Khu vực nghỉ ngơi",
                        Description = "Ghế ngồi, nước uống miễn phí",
                        IsActive = true,
                        DisplayOrder = 2,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc
                    }
                };

                await context.StationAmenities.AddRangeAsync(amenities);
            }

            await context.SaveChangesAsync();

            // Seed vehicle for driver
            if (driverUser != null && !await context.UserVehicles.AnyAsync(uv => uv.UserId == driverUser.Id))
            {
                var vehicle = new Vehicle
                {
                    Id = Guid.NewGuid(),
                    Make = "VinFast",
                    Model = "VF8",
                    ModelYear = 2024,
                    LicensePlate = "51A-123.45",
                    VehicleType = VehicleType.Car,
                    BatteryCapacityKwh = 82m,
                    MaxChargingPowerKw = 160m,
                    Color = "Trắng",
                    Notes = "Xe sử dụng thường xuyên",
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                var userVehicle = new UserVehicle
                {
                    Id = Guid.NewGuid(),
                    UserId = driverUser.Id,
                    VehicleId = vehicle.Id,
                    IsPrimary = true,
                    Nickname = "VinFast VF8",
                    ChargePortLocation = "Trước bên trái",
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                await context.Vehicles.AddAsync(vehicle);
                await context.UserVehicles.AddAsync(userVehicle);
                await context.SaveChangesAsync();

                // Create sample reservation & session
                var spot = await context.ChargingSpots.FirstAsync();

                var reservation = new Reservation
                {
                    Id = Guid.NewGuid(),
                    UserId = driverUser.Id,
                    ChargingSpotId = spot.Id,
                    VehicleId = vehicle.Id,
                    ScheduledStartTime = nowUtc.AddHours(1),
                    ScheduledEndTime = nowUtc.AddHours(2),
                    Status = ReservationStatus.Confirmed,
                    ConfirmationCode = $"RSV-{nowUtc:yyyyMMddHHmmss}",
                    EstimatedEnergyKwh = 40m,
                    EstimatedCost = 180000m,
                    Notes = "Đặt chỗ minh họa",
                    IsPrepaid = false,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                await context.Reservations.AddAsync(reservation);
                await context.SaveChangesAsync();

                var session = new ChargingSession
                {
                    Id = Guid.NewGuid(),
                    ChargingSpotId = spot.Id,
                    UserId = driverUser.Id,
                    ReservationId = reservation.Id,
                    VehicleId = vehicle.Id,
                    Status = ChargingSessionStatus.Scheduled,
                    SessionStartTime = reservation.ScheduledStartTime,
                    SessionEndTime = null,
                    EnergyRequestedKwh = 40m,
                    PricePerKwh = spot.PricePerKwh,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                await context.ChargingSessions.AddAsync(session);
                await context.SaveChangesAsync();

                await context.PaymentTransactions.AddAsync(new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = driverUser.Id,
                    ReservationId = reservation.Id,
                    ChargingSessionId = session.Id,
                    Amount = reservation.EstimatedCost ?? 0,
                    Currency = "VND",
                    Method = PaymentMethod.QrCode,
                    Status = PaymentStatus.Pending,
                    Description = "Thanh toán giữ chỗ minh họa",
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                });

                await context.Notifications.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = driverUser.Id,
                    Title = "Đặt chỗ thành công",
                    Message = "Bạn đã đặt chỗ thành công tại EV City Center vào giờ tới.",
                    Type = NotificationType.Reservation,
                    IsRead = false,
                    SentAt = nowUtc,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                });

                await context.SaveChangesAsync();
            }
        }
    }
}


