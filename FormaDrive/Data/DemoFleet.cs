namespace FormaDrive.Data;

internal static class DemoFleet
{
    public static Car[] Cars =>
    [
        Make(1, "Volkswagen Golf", "golf", "Хэтчбек", "Белый", 95),
        Make(2, "Škoda Octavia", "octavia", "Лифтбек", "Синий", 115),
        Make(3, "Renault Clio", "clio", "Хэтчбек", "Красный", 75, gear: "Механика"),
        Make(4, "Toyota Corolla", "corolla", "Седан", "Серебристый", 120),
        Make(5, "Hyundai Tucson", "tucson", "Кроссовер", "Оливковый", 165),
        Make(6, "Kia Sportage", "sportage", "Кроссовер", "Тёмно-синий", 160),
        Make(7, "Porsche 911 Carrera", "carrera", "Купе", "Серебристый", 490, premium: true, seats: 4),
        Make(8, "BMW 530i", "530i", "Седан", "Графитовый", 285, premium: true)
    ];

    private static Car Make(int id, string name, string photo, string body, string color, int rate,
        string gear = "Автомат", bool premium = false, int seats = 5) => new()
    {
        Id = id, Name = name, Photo = photo, Body = body, Color = color, Rate = rate,
        Transmission = gear, Premium = premium, Seats = seats, Deposit = premium ? 600 : 200, Mileage = 18400
    };
}
