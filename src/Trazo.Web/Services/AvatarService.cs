using SkiaSharp;
namespace Trazo.Web.Services;

public class AvatarService(IWebHostEnvironment environment, IConfiguration configuration)
{
    public const long MaxBytes = 2 * 1024 * 1024;
    private readonly string root = Path.GetFullPath(configuration["Storage:Root"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "avatars"));
    public async Task<string> GuardarAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > MaxBytes) throw new InvalidDataException("El avatar debe pesar hasta 2 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            throw new InvalidDataException("Elegí una imagen JPG, PNG o WebP.");
        using var input = new MemoryStream();
        await file.CopyToAsync(input, ct);
        input.Position = 0;
        using var codec = SKCodec.Create(input);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp)
            || codec.Info.Width <= 0 || codec.Info.Height <= 0 || (long)codec.Info.Width * codec.Info.Height > 16_000_000 || codec.FrameCount > 1)
            throw new InvalidDataException("La imagen no es válida, es animada o supera 16 megapíxeles.");
        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null) throw new InvalidDataException("No se pudo leer la imagen.");
        using var surface = SKSurface.Create(new SKImageInfo(512, 512));
        surface.Canvas.Clear(SKColors.Transparent);
        var side = Math.Min(bitmap.Width, bitmap.Height);
        var source = SKRect.Create((bitmap.Width - side) / 2f, (bitmap.Height - side) / 2f, side, side);
        using var paint = new SKPaint { IsAntialias = true };
        surface.Canvas.DrawBitmap(bitmap, source, new SKRect(0, 0, 512, 512), new SKSamplingOptions(SKFilterMode.Linear), paint);
        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        Directory.CreateDirectory(root);
        var key = Guid.NewGuid().ToString("N") + ".png";
        await File.WriteAllBytesAsync(Path.Combine(root, key), encoded.ToArray(), ct);
        return key;
    }
    public string? ObtenerRuta(string? key)
    {
        if (key is null || !System.Text.RegularExpressions.Regex.IsMatch(key, "^[a-f0-9]{32}\\.png$")) return null;
        var path = Path.Combine(root, key);
        return File.Exists(path) ? path : null;
    }
    public void Eliminar(string? key)
    {
        var path = ObtenerRuta(key);
        if (path is not null) File.Delete(path);
    }
}
