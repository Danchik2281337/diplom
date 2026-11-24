using FormaDrive.Data;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace FormaDrive.Services;

public sealed class TripDocuments
{
    public TripDocuments(IWebHostEnvironment environment)
    {
        if (GlobalFontSettings.FontResolver == null)
        {
            using var source = environment.WebRootFileProvider.GetFileInfo("fonts/RobotoCondensed.ttf").CreateReadStream();
            using var buffer = new MemoryStream();
            source.CopyTo(buffer);
            GlobalFontSettings.FontResolver = new DocumentFont(buffer.ToArray());
        }
    }

    public byte[] Create(Booking booking, bool returned)
    {
        using var document = new PdfDocument();
        document.Info.Title = $"Forma Drive — акт {(returned ? "возврата" : "выдачи")} №{booking.Id}";
        var font = new XFont("Forma", 12);
        var heading = new XFont("Forma", 21);
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        var graphics = XGraphics.FromPdfPage(page);
        try
        {
            double y = 42;
            graphics.DrawString("FORMA / DRIVE", heading, XBrushes.Black, 40, y);
            y += 34;
            Line($"Акт {(returned ? "возврата" : "выдачи")} автомобиля. Бронь №{booking.Id}");
            Line($"Автомобиль: {booking.Car.Name}. Цвет: {booking.Car.Color}");
            Line($"Клиент: {booking.Member.Name}. Почта: {booking.Member.Email}");
            Line($"Аренда: {booking.Start:dd.MM.yyyy HH:mm} — {booking.End:dd.MM.yyyy HH:mm}");
            Line($"Пункт: {Tariff.Place(booking.Location)}. Время минское.");
            Line($"Оформлено: {(returned ? booking.ReturnedAt : booking.IssuedAt):dd.MM.yyyy HH:mm}");
            Line($"Пробег: {(returned ? booking.InKm : booking.OutKm)} км");
            if (returned) Line($"За поездку пройдено: {booking.InKm - booking.OutKm} км");
            Line($"Стоимость аренды: {booking.Total} BYN. Оплачено: {TripSummary.Paid(booking)} BYN.");
            Line($"Залог получен: {TripSummary.DepositReceived(booking)} BYN.");
            Line($"Залог возвращён: {TripSummary.DepositRefunded(booking)} BYN.");
            Line($"Удержано: {TripSummary.DepositWithheld(booking)} BYN. Остаток: {TripSummary.DepositBalance(booking)} BYN.");
            var inspection = booking.Inspections.SingleOrDefault(i => i.IsReturn == returned);
            Line("Результат осмотра:");
            Line(inspection?.Notes ?? (returned ? booking.InNotes : booking.OutNotes) ?? "Данные отсутствуют");
            Line("Отмеченные зоны: " + (string.IsNullOrEmpty(inspection?.DamageZones) ? "Нет" : TripSummary.Zones(inspection.DamageZones)));
            Line("Фотографии осмотра приложены на следующих страницах.");
            y += 15;
            Line("Автомобиль передал __________________  Принял __________________");
            Line("Дата __________________  Подписи __________________");
            Line("Документ сформирован из данных сервиса. Подписи заполняются при передаче.");

            void Line(string text)
            {
                // Wrap by measured width, preserving Cyrillic and long notes.
                var line = "";
                foreach (char character in text.Replace('\n', ' ').Replace('\r', ' '))
                {
                    if (graphics.MeasureString(line + character, font).Width > page.Width.Point - 80)
                    {
                        DrawLine(line);
                        line = "";
                    }
                    line += character;
                }
                DrawLine(line);
                y += 3;
            }

            void DrawLine(string text)
            {
                if (y > page.Height.Point - 45)
                {
                    graphics.Dispose();
                    page = document.AddPage();
                    page.Size = PdfSharp.PageSize.A4;
                    graphics = XGraphics.FromPdfPage(page);
                    y = 42;
                }
                graphics.DrawString(text, font, XBrushes.Black, 40, y);
                y += 18;
            }
        }
        finally { graphics.Dispose(); }

        var selectedInspection = booking.Inspections.SingleOrDefault(i => i.IsReturn == returned);
        foreach (var photo in selectedInspection?.Photos ?? [])
        {
            var photoPage = document.AddPage();
            photoPage.Size = PdfSharp.PageSize.A4;
            using var photoGraphics = XGraphics.FromPdfPage(photoPage);
            photoGraphics.DrawString($"Бронь №{booking.Id} · Осмотр {selectedInspection!.CreatedAt:dd.MM.yyyy HH:mm}", font, XBrushes.Black, 40, 40);
            try
            {
                using var photoStream = new MemoryStream(photo.Content);
                using var image = XImage.FromStream(photoStream);
                double scale = Math.Min((photoPage.Width.Point - 80) / image.PixelWidth, (photoPage.Height.Point - 100) / image.PixelHeight);
                photoGraphics.DrawImage(image, 40, 65, image.PixelWidth * scale, image.PixelHeight * scale);
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or NotSupportedException or InvalidDataException)
            {
                photoGraphics.DrawString("Фотография доступна в карточке поездки.", font, XBrushes.Black, 40, 80);
            }
        }
        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private sealed class DocumentFont(byte[] bytes) : IFontResolver
    {
        public string DefaultFontName => "Forma";
        public byte[] GetFont(string faceName) => bytes;
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new("Forma");
    }
}

