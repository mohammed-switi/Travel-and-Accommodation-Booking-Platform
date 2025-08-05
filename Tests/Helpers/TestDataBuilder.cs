using System.Collections.ObjectModel;
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
                IsActive = true,
                StarRating = 4,
                Amenities = Enums.Amenities.Wifi | Enums.Amenities.Pool
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

        public HotelBuilder WithStarRating(int starRating)
        {
            _hotel.StarRating = starRating;
            return this;
        }

        public HotelBuilder WithAmenities(Enums.Amenities amenities)
        {
            _hotel.Amenities = amenities;
            return this;
        }

        public Hotel Build()
        {
            // Don't set navigation properties to avoid EF tracking issues
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
                Type = RoomType.Standard,// Changed from Discount to Price
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
            _room.PricePerNight = price;
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

    #region HotelImage Builder

    public HotelImageBuilder CreateHotelImage(int id = 1)
    {
        return new HotelImageBuilder(id);
    }

    public class HotelImageBuilder
    {
        private readonly HotelImage _hotelImage;

        public HotelImageBuilder(int id)
        {
            _hotelImage = new HotelImage
            {
                Id = id,
                Url = $"https://example.com/hotel-image-{id}.jpg",
                HotelId = 1
            };
        }

        public HotelImageBuilder WithUrl(string url)
        {
            _hotelImage.Url = url;
            return this;
        }

        public HotelImageBuilder WithHotel(int hotelId)
        {
            _hotelImage.HotelId = hotelId;
            return this;
        }

        public HotelImage Build() => _hotelImage;
    }

    #endregion

    #region Review Builder

    public ReviewBuilder CreateReview(int id = 1)
    {
        return new ReviewBuilder(this, id);
    }

    public class ReviewBuilder
    {
        private readonly TestDataBuilder _parent;
        private readonly Review _review;

        public ReviewBuilder(TestDataBuilder parent, int id)
        {
            _parent = parent;
            _review = new Review
            {
                Id = id,
                UserId = 1,
                HotelId = 1,
                Rating = 4,
                Comment = "Great hotel!",
                CreatedAt = DateTime.UtcNow
            };
        }

        public ReviewBuilder WithUser(int userId)
        {
            _review.UserId = userId;
            return this;
        }

        public ReviewBuilder WithHotel(int hotelId)
        {
            _review.HotelId = hotelId;
            return this;
        }

        public ReviewBuilder WithRating(int rating)
        {
            _review.Rating = rating;
            return this;
        }

        public ReviewBuilder WithComment(string comment)
        {
            _review.Comment = comment;
            return this;
        }

        public Review Build()
        {

            return _review;
        }
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
        decimal price = 100m,
        City city = null,
        User owner = null,
        Hotel hotel = null)
    {
        if (city == null)
        {
            city = CreateCity(1).Build();
            _context.Cities.Add(city);
        }
        if (owner == null)
        {
            owner = CreateUser(1).Build();
            _context.Users.Add(owner);
        }
        if (hotel == null)
        {
            hotel = CreateHotel(hotelId)
                .WithCity(city.Id)
                .WithOwner(owner.Id)
                .Build();
            _context.Hotels.Add(hotel);
        }
        
        var room = CreateRoom(roomId)
            .WithHotel(hotelId)
            .WithType(type)
            .WithPrice(price)
            .Build();


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
        var city = CreateCity(1).Build();
        var owner = CreateUser(1).Build();
        var hotel = CreateHotel(hotelId).Build();
        
        _context.Cities.Add(city);
        _context.Users.Add(owner);
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
        var city1 = CreateCity(1).Build();
        var city2 = CreateCity(2).Build();
        var owner1 = CreateUser(1).Build();
        var owner2 = CreateUser(2).Build();

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

        _context.Cities.AddRange(city1, city2);
        _context.Users.AddRange(owner1, owner2);
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
        var user = CreateUser(2).Build();
        
        var booking = CreateBooking(1)
            .WithStatus(bookingStatus)
            .WithUser(2)
            .Build();

        _context.Users.Add(user);
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var bookingItem = new BookingItem
        {
            Id = 1,
            RoomId = room.Id,
            BookingId = booking.Id,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            Price = 200m
        };

        _context.BookingItems.Add(bookingItem);
        await _context.SaveChangesAsync();

        return (room, booking, bookingItem);
    }

    /// <summary>
    /// Seeds a complete hotel with city, owner, images, and reviews
    /// </summary>
    public async Task<(Hotel,City,User, HotelImage,Review)> SeedHotelWithRelatedDataAsync(
        int hotelId = 1,
        string hotelName = "Test Hotel",
        int starRating = 4,
        bool isActive = true,
        Amenities amenities = Enums.Amenities.Wifi | Enums.Amenities.Pool)
    {
        var city = CreateCity(1).WithName("Test City").Build();
        var owner = CreateUser(1).WithName("Hotel Owner").Build();
        var mainImage = CreateHotelImage(1).WithHotel(hotelId).Build();
        
        var hotel = CreateHotel(hotelId)
            .WithName(hotelName)
            .WithCity(city.Id)
            .WithOwner(owner.Id)
            .WithActiveStatus(isActive)
            .WithStarRating(starRating)
            .WithAmenities(amenities)
            .Build();

        hotel.MainImageId = mainImage.Id;

        var review = CreateReview(1)
            .WithHotel(hotelId)
            .WithUser(owner.Id)
            .WithRating(5)
            .WithComment("Excellent service!")
            .Build();

        _context.Cities.Add(city);
        _context.Users.Add(owner);
        _context.HotelImages.Add(mainImage);
        _context.Hotels.Add(hotel);
        _context.Reviews.Add(review);
        
        await _context.SaveChangesAsync();

        return (hotel, city, owner, mainImage, review);
    }

    /// <summary>
    /// Seeds multiple hotels for pagination and filtering tests
    /// </summary>
    public async Task<List<Hotel>> SeedMultipleHotelsAsync(
        int count = 15,
        bool mixActiveInactive = false)
    {
        var city = CreateCity(1).Build();
        var owner = CreateUser(1).Build();
        
        _context.Cities.Add(city);
        _context.Users.Add(owner);

        var hotels = new List<Hotel>();
        for (var i = 1; i <= count; i++)
        {
            var isActive = mixActiveInactive ? i % 2 == 0 : true;
            var starRating = (i % 5) + 1; // Simple int calculation
            var hotel = CreateHotel(i)
                .WithName($"Hotel {i}")
                .WithCity(1)
                .WithOwner(1)
                .WithActiveStatus(isActive)
                .WithStarRating(starRating)
                .Build();

            hotels.Add(hotel);
            _context.Hotels.Add(hotel);
        }

        await _context.SaveChangesAsync();
        return hotels;
    }

    /// <summary>
    /// Seeds hotel with booking data for deletion constraint tests
    /// </summary>
    public async Task<(Hotel hotel, Booking booking, Room room)> 
        SeedHotelWithBookingConstraintsAsync(BookingStatus bookingStatus = BookingStatus.Approved)
    {
        var collection = await SeedHotelWithRelatedDataAsync();
        var hotel = collection.Item1;
        var room = CreateRoom(1)
            .WithHotel(hotel.Id)
            .Build();
        var user = CreateUser(2).Build();
        
        var booking = CreateBooking(1)
            .WithStatus(bookingStatus)
            .WithUser(2)
            .Build();

        _context.Users.Add(user);
        _context.Bookings.Add(booking);
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        var bookingItem = new BookingItem
        {
            Id = 1,
            RoomId = room.Id,
            BookingId = booking.Id,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            Price = 200m
        };

        _context.BookingItems.Add(bookingItem);
        await _context.SaveChangesAsync();

        return (hotel, booking, room);
    }

    #endregion
}