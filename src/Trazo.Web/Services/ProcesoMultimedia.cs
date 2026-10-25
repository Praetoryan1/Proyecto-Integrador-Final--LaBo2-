using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Trazo.Web.Services;

public class ProcesoMultimedia(IWebHostEnvironment environment, IConfiguration configuration, ILogger<ProcesoMultimedia> logger)
{
    public string Ejecutable(string nombre)
    {
        var configured = configuration[$"Media:{(nombre == "ffmpeg" ? "FfmpegPath" : "FfprobePath")}"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var local = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "../../tools/ffmpeg/bin", nombre + ".exe"));
        return OperatingSystem.IsWindows() && File.Exists(local) ? local : nombre;
    }

    public async Task<string> EjecutarAsync(string nombre, IEnumerable<string> argumentos, CancellationToken ct)
    {
        var info = new ProcessStartInfo(Ejecutable(nombre))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in argumentos) info.ArgumentList.Add(arg); // Sin shell ni interpolación de comandos.
        using var process = new Process { StartInfo = info };
        try { process.Start(); }
        catch (Win32Exception) { throw new PublicacionException(503, "El procesamiento de GIF/MP4 no está configurado. Ejecutá tools/Preparar-Medios.ps1 y volvé a intentar."); }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));
        var output = LeerLimitadoAsync(process.StandardOutput);
        var error = LeerLimitadoAsync(process.StandardError);
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(output, error);
            ct.ThrowIfCancellationRequested();
            throw new PublicacionException(400, "El archivo requiere demasiado tiempo de procesamiento. Reducí su resolución o duración.");
        }
        var stdout = await output;
        var stderr = await error;
        if (process.ExitCode != 0)
        {
            logger.LogWarning("{Herramienta} rechazó el archivo: {Diagnostico}", nombre, stderr);
            throw new PublicacionException(400, "No se pudo decodificar el archivo. Revisá que sea un GIF válido o un MP4 H.264/AAC compatible.");
        }
        return stdout;
    }

    private static async Task<string> LeerLimitadoAsync(StreamReader reader)
    {
        var result = new StringBuilder();
        var buffer = new char[4096];
        int read;
        while ((read = await reader.ReadAsync(buffer)) > 0)
            if (result.Length < 65536) result.Append(buffer, 0, Math.Min(read, 65536 - result.Length));
        return result.ToString();
    }
}
