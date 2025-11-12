using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Data
{
    public class EVDbContext : DbContext
    {
        public EVDbContext(DbContextOptions<EVDbContext> op) : base(op) { }

        //Khai báo entities tại đây dùm
        public DbSet<Users> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ChargingStation> ChargingStations { get; set; }
        public DbSet<ChargingSpot> ChargingSpots { get; set; }
        public DbSet<StationMaintenance> StationMaintenances { get; set; }
        public DbSet<StationError> StationErrors { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<UserVehicle> UserVehicles { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ChargingSession> ChargingSessions { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<StationAmenity> StationAmenities { get; set; }
        public DbSet<SubscriptionPackage> SubscriptionPackages { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<ChargingSessionProgress> ChargingSessionProgresses { get; set; }
        public DbSet<StationReport> StationReports { get; set; }


        //Cấu hình chi tiết entities thì tại đây
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ChargingStation entity
            modelBuilder.Entity<ChargingStation>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.SerpApiPlaceId);
                entity.HasIndex(e => e.IsFromSerpApi);
                entity.Property(e => e.ExternalRating).HasPrecision(18, 2);
            });

            // Configure ChargingSpot entity
            modelBuilder.Entity<ChargingSpot>(entity =>
            {
                entity.HasIndex(e => e.ChargingStationId);
                entity.HasIndex(e => e.SpotNumber);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.QrCode);
                entity.HasIndex(e => e.IsOnline);
                
                entity.HasOne(e => e.ChargingStation)
                    .WithMany(s => s.ChargingSpots)
                    .HasForeignKey(e => e.ChargingStationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure StationMaintenance entity
            modelBuilder.Entity<StationMaintenance>(entity =>
            {
                entity.HasIndex(e => e.ChargingStationId);
                entity.HasIndex(e => e.ChargingSpotId);
                entity.HasIndex(e => e.ReportedByUserId);
                entity.HasIndex(e => e.AssignedToUserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ScheduledDate);
                
                entity.HasOne(e => e.ChargingStation)
                    .WithMany()
                    .HasForeignKey(e => e.ChargingStationId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.ChargingSpot)
                    .WithMany()
                    .HasForeignKey(e => e.ChargingSpotId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.ReportedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ReportedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.AssignedToUser)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure StationError entity
            modelBuilder.Entity<StationError>(entity =>
            {
                entity.HasIndex(e => e.ChargingStationId);
                entity.HasIndex(e => e.ChargingSpotId);
                entity.HasIndex(e => e.ReportedByUserId);
                entity.HasIndex(e => e.ResolvedByUserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ErrorCode);
                entity.HasIndex(e => e.ReportedAt);
                
                entity.HasOne(e => e.ChargingStation)
                    .WithMany()
                    .HasForeignKey(e => e.ChargingStationId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.ChargingSpot)
                    .WithMany()
                    .HasForeignKey(e => e.ChargingSpotId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.ReportedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ReportedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.ResolvedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ResolvedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasIndex(e => e.LicensePlate);
                entity.HasIndex(e => e.Vin);
                entity.Property(e => e.BatteryCapacityKwh).HasPrecision(18, 2);
                entity.Property(e => e.MaxChargingPowerKw).HasPrecision(18, 2);
            });

            modelBuilder.Entity<UserVehicle>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.VehicleId }).IsUnique();
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserVehicles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Vehicle)
                    .WithMany(v => v.UserVehicles)
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ChargingSpotId);
                entity.HasIndex(e => e.VehicleId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ConfirmationCode).IsUnique();
                entity.Property(e => e.EstimatedEnergyKwh).HasPrecision(18, 2);
                entity.Property(e => e.EstimatedCost).HasPrecision(18, 2);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Reservations)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ChargingSpot)
                    .WithMany(s => s.Reservations)
                    .HasForeignKey(e => e.ChargingSpotId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Vehicle)
                    .WithMany()
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ChargingSession>(entity =>
            {
                entity.HasIndex(e => e.ChargingSpotId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ReservationId);
                entity.HasIndex(e => e.VehicleId);
                entity.HasIndex(e => e.Status);
                entity.Property(e => e.EnergyDeliveredKwh).HasPrecision(18, 2);
                entity.Property(e => e.EnergyRequestedKwh).HasPrecision(18, 2);
                entity.Property(e => e.Cost).HasPrecision(18, 2);
                entity.Property(e => e.PricePerKwh).HasPrecision(18, 2);
                entity.Property(e => e.CurrentSocPercentage).HasPrecision(18, 2);
                entity.Property(e => e.InitialSocPercentage).HasPrecision(18, 2);
                entity.Property(e => e.TargetSocPercentage).HasPrecision(18, 2);
                entity.Property(e => e.CurrentPowerKw).HasPrecision(18, 2);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.ChargingSessions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ChargingSpot)
                    .WithMany(s => s.ChargingSessions)
                    .HasForeignKey(e => e.ChargingSpotId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.ChargingSessions)
                    .HasForeignKey(e => e.ReservationId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Vehicle)
                    .WithMany(v => v.ChargingSessions)
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ReservationId);
                entity.HasIndex(e => e.ChargingSessionId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Method);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.WalletBalanceBefore).HasPrecision(18, 2);
                entity.Property(e => e.WalletBalanceAfter).HasPrecision(18, 2);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.PaymentTransactions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.PaymentTransactions)
                    .HasForeignKey(e => e.ReservationId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ChargingSession)
                    .WithMany(cs => cs.PaymentTransactions)
                    .HasForeignKey(e => e.ChargingSessionId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.SubscriptionPackage)
                    .WithMany(p => p.PaymentTransactions)
                    .HasForeignKey(e => e.SubscriptionPackageId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.IsRead);
                entity.Property(e => e.Title).HasMaxLength(200);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StationAmenity>(entity =>
            {
                entity.HasIndex(e => e.ChargingStationId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => new { e.ChargingStationId, e.Name }).IsUnique();

                entity.HasOne(e => e.ChargingStation)
                    .WithMany(s => s.Amenities)
                    .HasForeignKey(e => e.ChargingStationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SubscriptionPackage entity
            modelBuilder.Entity<SubscriptionPackage>(entity =>
            {
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.Name);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.EnergyKwh).HasPrecision(18, 2);
            });

            // Configure UserSubscription entity
            modelBuilder.Entity<UserSubscription>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SubscriptionPackageId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => new { e.UserId, e.SubscriptionPackageId });
                entity.Property(e => e.RemainingEnergyKwh).HasPrecision(18, 2);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SubscriptionPackage)
                    .WithMany(p => p.UserSubscriptions)
                    .HasForeignKey(e => e.SubscriptionPackageId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ChargingSessionProgress entity
            modelBuilder.Entity<ChargingSessionProgress>(entity =>
            {
                entity.HasIndex(e => e.ChargingSessionId);
                entity.HasIndex(e => e.RecordedAt);
                entity.HasIndex(e => new { e.ChargingSessionId, e.RecordedAt });
                entity.Property(e => e.SocPercentage).HasPrecision(18, 2);
                entity.Property(e => e.PowerKw).HasPrecision(18, 2);
                entity.Property(e => e.EnergyDeliveredKwh).HasPrecision(18, 2);
                entity.Property(e => e.EstimatedTimeRemainingMinutes).HasPrecision(18, 2);

                entity.HasOne(e => e.ChargingSession)
                    .WithMany(cs => cs.ProgressHistory)
                    .HasForeignKey(e => e.ChargingSessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure StationReport entity
            modelBuilder.Entity<StationReport>(entity =>
            {
                entity.HasIndex(e => e.ChargingStationId);
                entity.HasIndex(e => e.ReportDate);
                entity.HasIndex(e => new { e.ChargingStationId, e.ReportDate }).IsUnique();
                entity.Property(e => e.TotalEnergyDeliveredKwh).HasPrecision(18, 2);
                entity.Property(e => e.TotalRevenue).HasPrecision(18, 2);
                entity.Property(e => e.AverageSessionDurationMinutes).HasPrecision(18, 2);

                entity.HasOne(e => e.ChargingStation)
                    .WithMany(s => s.StationReports)
                    .HasForeignKey(e => e.ChargingStationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
