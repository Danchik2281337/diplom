using FormaDrive.Data;

namespace FormaDrive.Services;

public static class InspectionUploads
{
    public static async Task<Inspection> Read(int staffId, string? notes, string[] zones, List<IFormFile> photos)
    {
        if (string.IsNullOrWhiteSpace(notes) || notes.Length > 500)
            throw new RentalError("Заполните результат осмотра, до 500 символов.");
        if (zones.Any(z => !TripSummary.DamageZones.ContainsKey(z)))
            throw new RentalError("Выберите зону повреждения из схемы.");
        if (photos.Count > 6) throw new RentalError("За один осмотр можно добавить до 6 фотографий.");

        var inspection = new Inspection
        {
            StaffId = staffId, Notes = notes.Trim(),
            DamageZones = string.Join(",", zones.Distinct())
        };
        foreach (var file in photos)
        {
            if (file.Length is <= 0 or > 5 * 1024 * 1024)
                throw new RentalError("Каждая фотография должна быть размером до 5 МБ.");
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            byte[] bytes = stream.ToArray();
            bool jpeg = bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff;
            bool png = bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            if (!jpeg && !png) throw new RentalError("Допустимы фотографии JPEG и PNG.");
            inspection.Photos.Add(new InspectionPhoto { Content = bytes, ContentType = jpeg ? "image/jpeg" : "image/png" });
        }
        return inspection;
    }
}

