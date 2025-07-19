using Final_Project.Models;

namespace Final_Project.Data;

using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<City> Cities { get; set; }
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<Room> Rooms { get; set; }

    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingItem> BookingItems { get; set; }

    public DbSet<HotelImage> HotelImages { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<BookingCart> BookingCarts { get; set; }
    public DbSet<BookingCartItem> BookingCartItems { get; set; }
    

    public DbSet<RecentlyViewedHotel> RecentlyViewedHotels { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BookingItem>()
            .HasOne(bi => bi.Booking)
            .WithMany(b => b.Items)
            .HasForeignKey(bi => bi.BookingId);

        modelBuilder.Entity<BookingItem>()
            .HasOne(bi => bi.Room)
            .WithMany(r => r.BookingItems)
            .HasForeignKey(bi => bi.RoomId);

        modelBuilder.Entity<RecentlyViewedHotel>()
            .HasOne(rv => rv.User)
            .WithMany(u => u.RecentlyViewedHotels)
            .HasForeignKey(rv => rv.UserId);

        modelBuilder.Entity<RecentlyViewedHotel>()
            .HasOne(rv => rv.Hotel)
            .WithMany()
            .HasForeignKey(rv => rv.HotelId);

        modelBuilder.Entity<Room>()
            .Property(r => r.Type)
            .HasConversion<string>();


        modelBuilder.Entity<Hotel>()
            .HasOne(h => h.MainImage)
            .WithMany()
            .HasForeignKey(h => h.MainImageId)
            .OnDelete(DeleteBehavior.Restrict);
        
        
        modelBuilder.Entity<Hotel>()
            .Property(h => h.Amenities)
            .HasConversion<string>();
    }
}