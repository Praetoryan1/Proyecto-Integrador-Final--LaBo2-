using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;
using Trazo.Web.Domain;

namespace Trazo.Web.Services;

public record ArchivoPreparado(string Clave, TipoRecurso Tipo, string Mime, long Bytes);

public class AlmacenMedios(IWebHostEnvironment environment, IConfiguration configuration, ProcesoMultimedia proceso, ILogger<AlmacenMedios> logger)
{
    public const long Mb = 1024 * 1024;
    public const int MaxRecursos = 10;
    public const long CuotaCuenta = 500 * Mb;
    private readonly string root = Path.GetFullPath(configuration["Media:Root"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "medios"));
    private readonly SemaphoreSlim slots = new(2, 2);

    public string? Ruta(string? key, bool preview = false)
    {
        if (key is null || !Regex.IsMatch(key, "^[a-f0-9]{32}\\.(png|gif|mp4)$")) return null;
        return Path.Combine(root, preview ? Path.GetFileNameWithoutExtension(key) + ".preview.png" : key);
    }

    public void Eliminar(string? key)
    {
        foreach (var path in new[] { Ruta(key), Ruta(key, true) })
        {
            if (path is null) continue;
            try { File.Delete(path); }
            catch (IOException e) { logger.LogWarning(e, "No se pudo eliminar un archivo desvinculado; requiere limpieza operativa."); }
            catch (UnauthorizedAccessException e) { logger.LogWarning(e, "Sin permiso para eliminar un archivo desvinculado."); }
        }
    }

    public async Task<ArchivoPreparado> PrepararAsync(IFormFile file, CancellationToken ct)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var max = extension == ".mp4" ? 50 * Mb : extension == ".gif" ? 15 * Mb : 10 * Mb;
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".mp4"))
            throw new PublicacionException(400, "Formatos admitidos: JPG, PNG, WebP, GIF y MP4.");
        if (file.Length <= 0 || file.Length > max)
            throw new PublicacionException(400, $"El archivo debe pesar entre 1 byte y {max / Mb} MB.");
        if (!await slots.WaitAsync(0, ct)) throw new PublicacionException(429, "Hay dos archivos procesándose. Esperá un momento y volvé a intentar.");
        var key = Guid.NewGuid().ToString("N") + (extension is ".gif" or ".mp4" ? extension : ".png");
        var source = Path.Combine(root, Guid.NewGuid().ToString("N") + ".upload");
        try
        {
            Directory.CreateDirectory(root);
            await using (var input = file.OpenReadStream())
            await using (var output = new FileStream(source, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, ct)) > 0)
                {
                    total += read;
                    if (total > max) throw new PublicacionException(400, "El archivo supera el límite de tamaño.");
                    await output.WriteAsync(buffer.AsMemory(0, read), ct);
                }
            }
            TipoRecurso tipo;
            string mime;
            if (extension is ".gif" or ".mp4")
            {
                tipo = extension == ".gif" ? TipoRecurso.Gif : TipoRecurso.Video;
                mime = extension == ".gif" ? "image/gif" : "video/mp4";
                try { await ProcesarAnimadoAsync(source, key, tipo, ct); }
                catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
                { throw new PublicacionException(400, "El archivo contiene metadatos incompletos o incompatibles."); }
            }
            else
            {
                tipo = TipoRecurso.Imagen;
                mime = "image/png";
                ProcesarImagen(source, key, extension);
            }
            var bytes = new FileInfo(Ruta(key)!).Length + new FileInfo(Ruta(key, true)!).Length;
            var outputLimit = tipo == TipoRecurso.Video ? 50 * Mb : 25 * Mb;
            if (bytes > outputLimit) throw new PublicacionException(400, "El archivo procesado supera el límite. Reducí la resolución o duración.");
            return new(key, tipo, mime, bytes);
        }
        catch { Eliminar(key); throw; }
        finally
        {
            try { File.Delete(source); }
            finally { slots.Release(); }
        }
    }

    private void ProcesarImagen(string source, string key, string extension)
    {
        using var stream = File.OpenRead(source);
        using var codec = SKCodec.Create(stream);
        var expected = extension is ".jpg" or ".jpeg" ? SKEncodedImageFormat.Jpeg : extension == ".png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Webp;
        if (codec is null || codec.EncodedFormat != expected || codec.FrameCount > 1 || codec.Info.Width <= 0 || codec.Info.Height <= 0 || (long)codec.Info.Width * codec.Info.Height > 16_000_000)
            throw new PublicacionException(400, "La imagen no coincide con su extensión, está animada o supera 16 megapíxeles.");
        using var bitmap = new SKBitmap(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new PublicacionException(400, "La imagen está incompleta o dañada.");
        var rotated = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var w = bitmap.Width; var h = bitmap.Height;
        using var oriented = new SKBitmap(rotated ? h : w, rotated ? w : h);
        using (var canvas = new SKCanvas(oriented))
        {
            canvas.Clear(SKColors.Transparent);
            switch (codec.EncodedOrigin)
            {
                case SKEncodedOrigin.TopRight: canvas.Translate(w, 0); canvas.Scale(-1, 1); break;
                case SKEncodedOrigin.BottomRight: canvas.Translate(w, h); canvas.RotateDegrees(180); break;
                case SKEncodedOrigin.BottomLeft: canvas.Translate(0, h); canvas.Scale(1, -1); break;
                case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
                case SKEncodedOrigin.RightTop: canvas.Translate(h, 0); canvas.RotateDegrees(90); break;
                case SKEncodedOrigin.RightBottom: canvas.Translate(h, w); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
                case SKEncodedOrigin.LeftBottom: canvas.Translate(0, w); canvas.RotateDegrees(270); break;
            }
            canvas.DrawBitmap(bitmap, new SKRect(0, 0, w, h), new SKRect(0, 0, w, h), new SKSamplingOptions(SKFilterMode.Linear), null);
        }
        GuardarPng(oriented, Ruta(key)!);
        var scale = Math.Min(1d, 900d / Math.Max(oriented.Width, oriented.Height));
        using var thumbnail = oriented.Resize(new SKImageInfo(Math.Max(1, (int)(oriented.Width * scale)), Math.Max(1, (int)(oriented.Height * scale))), new SKSamplingOptions(SKFilterMode.Linear));
        GuardarPng(thumbnail, Ruta(key, true)!);
    }

    private static void GuardarPng(SKBitmap bitmap, string path)
    {
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private async Task ProcesarAnimadoAsync(string source, string key, TipoRecurso tipo, CancellationToken ct)
    {
        var header = new byte[12];
        await using (var stream = File.OpenRead(source))
        {
            if (stream.Length < header.Length) throw new PublicacionException(400, "El archivo está incompleto.");
            await stream.ReadExactlyAsync(header, ct);
        }
        if (tipo == TipoRecurso.Video && !header.AsSpan(4, 4).SequenceEqual("ftyp"u8) ||
            tipo == TipoRecurso.Gif && !header.AsSpan(0, 6).SequenceEqual("GIF89a"u8) && !header.AsSpan(0, 6).SequenceEqual("GIF87a"u8))
            throw new PublicacionException(400, "El contenido no coincide con el formato indicado.");
        var format = tipo == TipoRecurso.Gif ? "gif" : "mov";
        var json = await proceso.EjecutarAsync("ffprobe", ["-v", "error", "-max_alloc", "67108864", "-threads", "2", "-protocol_whitelist", "file", "-f", format,
            "-show_entries", "stream=codec_name,codec_type,width,height:format=duration", "-of", "json", source], ct);
        using var doc = JsonDocument.Parse(json);
        var streams = doc.RootElement.GetProperty("streams").EnumerateArray().ToArray();
        var videos = streams.Where(s => s.GetProperty("codec_type").GetString() == "video").ToArray();
        var audios = streams.Where(s => s.GetProperty("codec_type").GetString() == "audio").ToArray();
        if (videos.Length != 1 || audios.Length > (tipo == TipoRecurso.Gif ? 0 : 1) || streams.Length != videos.Length + audios.Length)
            throw new PublicacionException(400, "Se admite un solo video y, en MP4, hasta una pista de audio AAC.");
        var video = videos[0];
        if (video.GetProperty("codec_name").GetString() != (tipo == TipoRecurso.Gif ? "gif" : "h264") || audios.Any(a => a.GetProperty("codec_name").GetString() != "aac"))
            throw new PublicacionException(400, "El MP4 debe usar H.264 y audio AAC opcional.");
        var w = video.GetProperty("width").GetInt32(); var h = video.GetProperty("height").GetInt32();
        var duration = doc.RootElement.TryGetProperty("format", out var fmt) && fmt.TryGetProperty("duration", out var d) && double.TryParse(d.GetString(), CultureInfo.InvariantCulture, out var seconds) ? seconds : 0;
        var maxFrames = tipo == TipoRecurso.Gif ? 300 : 3600;
        var maxPixels = tipo == TipoRecurso.Gif ? 4_000_000 : 2_073_600;
        if (w <= 0 || h <= 0 || w > 1920 && tipo == TipoRecurso.Video || h > 1920 && tipo == TipoRecurso.Video ||
            (long)w * h > maxPixels || !double.IsFinite(duration) || duration <= 0 || duration > (tipo == TipoRecurso.Gif ? 30 : 60))
            throw new PublicacionException(400, "Excede los límites: GIF hasta 30 s, 300 cuadros, 4 MP y 120 millones de píxeles acumulados; MP4 hasta 60 s, 3600 cuadros y Full HD.");
        // Revisar dimensiones/códecs antes de decodificar para contar cuadros.
        var counted = await proceso.EjecutarAsync("ffprobe", ["-v", "error", "-max_alloc", "67108864", "-threads", "2", "-protocol_whitelist", "file", "-f", format,
            "-select_streams", "v:0", "-count_frames", "-show_entries", "stream=nb_read_frames", "-of", "json", source], ct);
        using var counts = JsonDocument.Parse(counted);
        var streamCount = counts.RootElement.GetProperty("streams")[0];
        var frames = streamCount.TryGetProperty("nb_read_frames", out var f) && int.TryParse(f.GetString(), out var n) ? n : 0;
        if (frames <= 0 || frames > maxFrames || tipo == TipoRecurso.Gif && (long)w * h * frames > 120_000_000)
            throw new PublicacionException(400, "El archivo supera el límite de cuadros o de píxeles acumulados. Reducí la duración o resolución.");
        var args = new List<string> { "-v", "error", "-nostdin", "-y", "-xerror", "-max_alloc", "67108864", "-threads", "2", "-protocol_whitelist", "file", "-f", format, "-i", source, "-map", "0:v:0", "-map_metadata", "-1", "-map_chapters", "-1", "-threads", "2", "-filter_threads", "1" };
        if (tipo == TipoRecurso.Gif)
            args.AddRange(["-vf", "split[a][b];[a]palettegen=stats_mode=single[p];[b][p]paletteuse=new=1", "-loop", "0", "-frames:v", "300", "-t", "30", "-fs", "26214401"]);
        else
            args.AddRange(["-map", "0:a:0?", "-c:v", "libx264", "-preset", "veryfast", "-crf", "20", "-pix_fmt", "yuv420p", "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2", "-c:a", "aac", "-movflags", "+faststart", "-frames:v", "3600", "-t", "60", "-fs", "52428801"]);
        args.Add(Ruta(key)!);
        await proceso.EjecutarAsync("ffmpeg", args, ct);
        await proceso.EjecutarAsync("ffmpeg", ["-v", "error", "-nostdin", "-y", "-threads", "2", "-protocol_whitelist", "file", "-i", Ruta(key)!, "-frames:v", "1", "-vf", "scale='min(900,iw)':'min(900,ih)':force_original_aspect_ratio=decrease", "-threads", "1", "-update", "1", Ruta(key, true)!], ct);
    }
}
