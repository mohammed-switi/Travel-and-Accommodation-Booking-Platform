using Final_Project.Models;

namespace Final_Project.Services;

public class ImageService: IImageService
{
    public List<string> GetHotelImageUrls(Hotel hotel)
    {
        return hotel.Images?.Select(img => img.Url).ToList() ?? new List<string>();
    }
}