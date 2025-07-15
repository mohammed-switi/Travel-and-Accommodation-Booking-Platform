namespace Final_Project.Enums;


[Flags]
public enum Amenities
{
    None = 0,
    Wifi = 1,
    Pool = 2,
    Gym = 4,
    Parking = 8,
    Spa = 16,
    Restaurant = 32,
    Bar = 64
}