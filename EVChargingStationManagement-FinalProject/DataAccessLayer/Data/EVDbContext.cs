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
		public DbSet<Booking> Bookings { get; set; }
		public DbSet<ChargingSession> ChargingSessions { get; set; }
		public DbSet<Transaction> Transactions { get; set; }
		public DbSet<Review> Reviews { get; set; }
		public DbSet<BookingPayment> BookingPayments { get; set; }


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
            });

            // Configure ChargingSpot entity
            modelBuilder.Entity<ChargingSpot>(entity =>
            {
                entity.HasIndex(e => e.ChargingStationId);
                entity.HasIndex(e => e.SpotNumber);
                entity.HasIndex(e => e.Status);
                
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

			// Vehicle
			modelBuilder.Entity<Vehicle>(entity =>
			{
				entity.HasIndex(e => e.UserId);
				entity.HasIndex(e => e.LicensePlate).IsUnique();
				entity.Property(e => e.LicensePlate).IsRequired();
				entity.HasOne(e => e.User)
					.WithMany()
					.HasForeignKey(e => e.UserId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			// Booking
			modelBuilder.Entity<Booking>(entity =>
			{
				entity.HasIndex(e => e.UserId);
				entity.HasIndex(e => e.VehicleId);
				entity.HasIndex(e => e.ChargingStationId);
				entity.HasIndex(e => e.ChargingSpotId);
				entity.HasIndex(e => e.StartTime);
				entity.HasIndex(e => e.EndTime);

				entity.HasOne(e => e.User)
					.WithMany()
					.HasForeignKey(e => e.UserId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(e => e.Vehicle)
					.WithMany(v => v.Bookings!)
					.HasForeignKey(e => e.VehicleId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(e => e.ChargingStation)
					.WithMany()
					.HasForeignKey(e => e.ChargingStationId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(e => e.ChargingSpot)
					.WithMany()
					.HasForeignKey(e => e.ChargingSpotId)
					.OnDelete(DeleteBehavior.Restrict);
			});

			// ChargingSession
			modelBuilder.Entity<ChargingSession>(entity =>
			{
				entity.HasIndex(e => e.BookingId).IsUnique();
				entity.HasOne(e => e.Booking)
					.WithOne(b => b.ChargingSession!)
					.HasForeignKey<ChargingSession>(e => e.BookingId)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(e => e.ChargingStation)
					.WithMany()
					.HasForeignKey(e => e.ChargingStationId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(e => e.ChargingSpot)
					.WithMany()
					.HasForeignKey(e => e.ChargingSpotId)
					.OnDelete(DeleteBehavior.Restrict);
			});

			// Transaction
			modelBuilder.Entity<Transaction>(entity =>
			{
				entity.HasIndex(e => e.ChargingSessionId).IsUnique();
				entity.HasOne(e => e.ChargingSession)
					.WithOne(s => s.Transaction!)
					.HasForeignKey<Transaction>(e => e.ChargingSessionId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			// Review
			modelBuilder.Entity<Review>(entity =>
			{
				entity.HasIndex(e => e.UserId);
				entity.HasIndex(e => e.ChargingStationId);
				entity.HasOne(e => e.User)
					.WithMany()
					.HasForeignKey(e => e.UserId)
					.OnDelete(DeleteBehavior.Restrict);
				entity.HasOne(e => e.ChargingStation)
					.WithMany()
					.HasForeignKey(e => e.ChargingStationId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			// BookingPayment
			modelBuilder.Entity<BookingPayment>(entity =>
			{
				entity.HasIndex(e => e.BookingId);
				entity.HasIndex(e => e.VnpTxnRef);
				entity.HasIndex(e => e.Status);
				entity.HasOne(e => e.Booking)
					.WithMany()
					.HasForeignKey(e => e.BookingId)
					.OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
