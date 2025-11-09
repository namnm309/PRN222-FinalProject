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
        }
    }
}
