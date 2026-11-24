using FormaDrive.Data;

namespace FormaDrive.Controllers;

public record Comparison(List<Car> Cars, DateTime? Start, DateTime? End, string Location, HashSet<int> Available);

