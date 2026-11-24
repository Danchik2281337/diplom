using FormaDrive.Data;

namespace FormaDrive.Services;

public static class CarGuide
{
    public static string Luggage(Car car) => car.Body switch
    {
        "Купе" => "1 небольшой чемодан",
        "Хэтчбек" => "2 небольших чемодана",
        "Кроссовер" => "3–4 чемодана",
        _ => "3 чемодана"
    };

    public static string Equipment(Car car) => car.Premium
        ? "Климат-контроль, подогрев сидений, парктроник"
        : "Кондиционер, Bluetooth, USB";

    public static string Purpose(string? purpose) => purpose switch
    {
        "city" => "Компактные хэтчбеки для городских маршрутов.",
        "family" => "Пять мест и вместительный багажник кроссовера для семейной поездки.",
        "weekend" => "Городские автомобили с автоматической коробкой для поездки на выходные.",
        "occasion" => "Премиальные автомобили для особого случая.",
        _ => ""
    };
}

