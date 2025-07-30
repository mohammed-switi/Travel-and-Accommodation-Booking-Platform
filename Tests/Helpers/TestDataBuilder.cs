using Final_Project.Data;
using Final_Project.Enums;
using Final_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Tests.ServicesTests.Helpers;

public class TestDataBuilder
{
    private readonly AppDbContext _context;

    public TestDataBuilder(AppDbContext context)
    {
        _context = context;
    }

    #region User Builder

    public UserBuilder CreateUser(int id = 1)
    {
        return new UserBuilder(id);
    }

    public class UserBuilder
    {
        private readonly User _user;

        public UserBuilder(int id)
        {
            _user = new User
            {
                Id = id,
                FullName = "testuser",
                Email = "example@example.com",
                PasswordHash = "password123"
            };
        }

        public UserBuilder WithName(string fullName)
        {
            _user.FullName = fullName;
            return this;
        }

        public UserBuilder WithEmail(string email)
        {
            _user.Email = email;
            return this;
        }

        public User Build() => _user;
    }

    #endregion

    #region City Builder

    public CityBuilder CreateCity(int id = 1)
    {
        return new CityBuilder(id);
    }

    public class CityBuilder
    {
        private readonly City _city;

        public CityBuilder(int id)
        {
            _city = new City
            {
                Id = id,
                Name = "Test City",
                Country = "Test Country"
            };
        }

        public CityBuilder WithName(string name)
        {
            _city.Name = name;
            return this;
        }

        public CityBuilder WithCountry(string country)
        {
            _city.Country = country;
            return this;
        }

        public City Build() => _city;
    }

    #endregion

    #region Hotel Builder

    public HotelBuilder CreateHotel(int id = 1)
    {
        return new HotelBuilder(this, id);
    }

    public class HotelBuilder
    {
        private readonly TestDataBuilder _parent;
        private readonly Hotel _hotel;

        public HotelBuilder(TestDataBuilder parent, int id)
        {
            _parent = parent;
            _hotel = new Hotel
            {
                Id = id,
                Description = "A test hotel for room service",
                CityId = 1,
                Location = "Test Location",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OwnerId = 1,
                Name = "Test Hotel",
                IsActive = true
            };
        }

        public HotelBuilder WithName(string name)
        {
            _hotel.Name = name;
            return this;
        }

        public HotelBuilder WithDescription(string description)
        {
            _hotel.Description = description;
            return this;
        }

        public HotelBuilder WithCity(int cityId)
        {
            _hotel.CityId = cityId;
            return this;
        }

        public HotelBuilder WithOwner(int ownerId)
        {
            _hotel.OwnerId = ownerId;
            return this;
        }

        public HotelBuilder WithActiveStatus(bool isActive)
        {
            _hotel.IsActive = isActive;
            return this;
        }

        public HotelBuilder WithLocation(string location)
        {
            _hotel.Location = location;
            return this;
        }

        public Hotel Build()
        {
            // Ensure related entities exist
            if (_hotel.City == null)
            {
                _hotel.City = _parent.CreateCity(_hotel.CityId).Build();
            }
            
            if (_hotel.Owner == null)
            {
                _hotel.Owner = _parent.CreateUser(_hotel.OwnerId).Build();
            }

            return _hotel;
        }
    }

    #endregion

    #region Room Builder

    public RoomBuilder CreateRoom(int id = 1)
    {
        return new RoomBuilder(this, id);
    }

    public class RoomBuilder
    {
        private readonly TestDataBuilder _parent;
        private readonly Room _room;

        public RoomBuilder(TestDataBuilder parent, int id)
        {
            _parent = parent;
            _room = new Room
            {
                Id = id,
                Type = RoomType.Standard,
                Discount = 100m,
                MaxAdults = 2,
                MaxChildren = 1,
                Quantity = 5,
                HotelId = 1,
                RoomNumber = "A101",
                ImageUrl = $"https://example.com/room{id}.jpg"
            };
        }

        public RoomBuilder WithType(RoomType type)
        {
            _room.Type = type;
            return this;
        }

        public RoomBuilder WithPrice(decimal price)
        {
            _room.Discount = price;
            return this;
        }

        public RoomBuilder WithMaxAdults(int maxAdults)
        {
            _room.MaxAdults = maxAdults;
            return this;
        }

        public RoomBuilder WithMaxChildren(int maxChildren)
        {
            _room.MaxChildren = maxChildren;
            return this;
        }

        public RoomBuilder WithQuantity(int quantity)
        {
            _room.Quantity = quantity;
            return this;
        }

        public RoomBuilder WithHotel(int hotelId)
        {
            _room.HotelId = hotelId;
            return this;
        }

        public RoomBuilder WithRoomNumber(string roomNumber)
        {
            _room.RoomNumber = roomNumber;
            return this;
        }

        public RoomBuilder WithImageUrl(string imageUrl)
        {
            _room.ImageUrl = imageUrl;
            return this;
        }

        public Room Build()
        {
            // Don't set Hotel navigation property here to avoid EF tracking issues
            return _room;
        }
    }

    #endregion

    #region Booking Builder

    public BookingBuilder CreateBooking(int id = 1)
    {
        return new BookingBuilder(id);
    }

    public class BookingBuilder
    {
        private readonly Booking _booking;

        public BookingBuilder(int id)
        {
            _booking = new Booking
            {
                Id = id,
                Status = BookingStatus.Approved,
                UserId = 1,
                TotalPrice = 100,
                BookingDate = DateTime.UtcNow,
                ContactName = "Test User",
                ContactPhone = "123456789",
                ContactEmail = "test@test.com",
                PaymentMethod = "Credit Card",
                SpecialRequests = "None"
            };
        }

        public BookingBuilder WithStatus(BookingStatus status)
        {
            _booking.Status = status;
            return this;
        }

        public BookingBuilder WithUser(int userId)
        {
            _booking.UserId = userId;
            return this;
        }

        public BookingBuilder WithTotalPrice(decimal totalPrice)
        {
            _booking.TotalPrice = totalPrice;
            return this;
        }

        public BookingBuilder WithContactInfo(string name, string phone, string email)
        {
            _booking.ContactName = name;
            _booking.ContactPhone = phone;
            _booking.ContactEmail = email;
            return this;
        }

        public Booking Build() => _booking;
    }

    #endregion

    #region Seeding Methods

    /// <summary>
    /// Seeds a complete room with hotel, city, and owner data
    /// </summary>
    public async Task<Room> SeedRoomWithHotelAsync(
        int roomId = 1, 
        int hotelId = 1, 
        RoomType type = RoomType.Standard, 
        decimal price = 100m)
    {
        var hotel = CreateHotel(hotelId)
            .WithCity(1)
            .WithOwner(1)
            .Build();

        var room = CreateRoom(roomId)
            .WithHotel(hotelId)
            .WithType(type)
            .WithPrice(price)
            .Build();

        _context.Hotels.Add(hotel);
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return room;
    }

    /// <summary>
    /// Seeds multiple rooms for pagination testing
    /// </summary>
    public async Task<List<Room>> SeedMultipleRoomsAsync(
        int count = 15, 
        int hotelId = 1, 
        RoomType type = RoomType.Standard, 
        decimal price = 100m)
    {
        var hotel = CreateHotel(hotelId).Build();
        _context.Hotels.Add(hotel);

        var rooms = new List<Room>();
        for (var i = 1; i <= count; i++)
        {
            var room = CreateRoom(i)
                .WithHotel(hotelId)
                .WithType(type)
                .WithPrice(price)
                .WithRoomNumber($"Room {i}")
                .Build();
            
            rooms.Add(room);
            _context.Rooms.Add(room);
        }

        await _context.SaveChangesAsync();
        return rooms;
    }

    /// <summary>
    /// Seeds hotels with different active states for filtering tests
    /// </summary>
    public async Task<(Hotel activeHotel, Hotel inactiveHotel, Room activeRoom, Room inactiveRoom)> 
        SeedActiveInactiveHotelsWithRoomsAsync()
    {
        var activeHotel = CreateHotel(1)
            .WithActiveStatus(true)
            .WithCity(1)
            .WithOwner(1)
            .Build();

        var inactiveHotel = CreateHotel(2)
            .WithActiveStatus(false)
            .WithCity(2)
            .WithOwner(2)
            .Build();

        var activeRoom = CreateRoom(1).WithHotel(1).Build();
        var inactiveRoom = CreateRoom(2).WithHotel(2).Build();

        _context.Hotels.AddRange(activeHotel, inactiveHotel);
        _context.Rooms.AddRange(activeRoom, inactiveRoom);
        await _context.SaveChangesAsync();

        return (activeHotel, inactiveHotel, activeRoom, inactiveRoom);
    }

    /// <summary>
    /// Seeds a room with booking data for deletion tests
    /// </summary>
    public async Task<(Room room, Booking booking, BookingItem bookingItem)> 
        SeedRoomWithBookingAsync(BookingStatus bookingStatus = BookingStatus.Approved)
    {
        var room = await SeedRoomWithHotelAsync();
        
        var booking = CreateBooking(1)
            .WithStatus(bookingStatus)
            .WithUser(1)
            .Build();

        var bookingItem = new BookingItem
        {
            Id = 1,
            RoomId = room.Id,
            BookingId = booking.Id,
            Room = room,
            Booking = booking
        };

        _context.Bookings.Add(booking);
        _context.BookingItems.Add(bookingItem);
        await _context.SaveChangesAsync();

        return (room, booking, bookingItem);
    }

    #endregion
}
