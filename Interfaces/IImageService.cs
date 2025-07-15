using Final_Project.Models;

namespace Final_Project.Services;

public interface IImageService
{
    List<string> GetHotelImageUrls(Hotel hotel);
}
