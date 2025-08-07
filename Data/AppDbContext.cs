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

        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<BookingCartItem>()
            .Property(bci => bci.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Hotel)
            .WithMany(h => h.Reviews)
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookingCart>()
            .HasOne(bc => bc.User)
            .WithMany()
            .HasForeignKey(bc => bc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecentlyViewedHotel>()
            .HasOne(rv => rv.User)
            .WithMany(u => u.RecentlyViewedHotels)
            .HasForeignKey(rv => rv.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecentlyViewedHotel>()
            .HasOne(rv => rv.Hotel)
            .WithMany()
            .HasForeignKey(rv => rv.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Hotel ownership - RESTRICT to prevent cascade conflicts
        modelBuilder.Entity<Hotel>()
            .HasOne(h => h.Owner)
            .WithMany()
            .HasForeignKey(h => h.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hotel relationships
        modelBuilder.Entity<Hotel>()
            .HasOne(h => h.City)
            .WithMany(c => c.Hotels)
            .HasForeignKey(h => h.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Hotel>()
            .HasOne(h => h.MainImage)
            .WithMany()
            .HasForeignKey(h => h.MainImageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HotelImage>()
            .HasOne(hi => hi.Hotel)
            .WithMany(h => h.Images)
            .HasForeignKey(hi => hi.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Room relationships
        modelBuilder.Entity<Room>()
            .HasOne(r => r.Hotel)
            .WithMany(h => h.Rooms)
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade); 

        // Booking relationships
        modelBuilder.Entity<BookingItem>()
            .HasOne(bi => bi.Booking)
            .WithMany(b => b.Items)
            .HasForeignKey(bi => bi.BookingId)
            .OnDelete(DeleteBehavior.Cascade); // Delete booking items when booking is deleted

        modelBuilder.Entity<BookingItem>()
            .HasOne(bi => bi.Room)
            .WithMany(r => r.BookingItems)
            .HasForeignKey(bi => bi.RoomId)
            .OnDelete(DeleteBehavior.Restrict); 

        // Booking cart relationships
        modelBuilder.Entity<BookingCartItem>()
            .HasOne(bci => bci.BookingCart)
            .WithMany(bc => bc.Items)
            .HasForeignKey(bci => bci.BookingCartId)
            .OnDelete(DeleteBehavior.Cascade); 

        modelBuilder.Entity<BookingCartItem>()
            .HasOne(bci => bci.Room)
            .WithMany()
            .HasForeignKey(bci => bci.RoomId)
            .OnDelete(DeleteBehavior.Restrict); 

        // Property conversions
        modelBuilder.Entity<Room>()
            .Property(r => r.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Hotel>()
            .Property(h => h.Amenities)
            .HasConversion<string>();

        modelBuilder.Entity<Booking>()
            .Property(b => b.Status)
            .HasConversion<string>();
    }
}