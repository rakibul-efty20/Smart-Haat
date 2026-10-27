using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using SmartHaat.Application.Catalog;

namespace SmartHaat.Infrastructure.Catalog;

public sealed class LocalProductImageStorage(IWebHostEnvironment environment) : IProductImageStorage
{
    private const int MaxBytes = 5 * 1024 * 1024;
    private static readonly Regex SafeKey = new("^[a-f0-9]{32}\\.(jpg|png|webp)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private string Root => Path.Combine(environment.ContentRootPath, "wwwroot", "media", "products");

    public async Task<string> SaveAsync(Stream content, CancellationToken ct)
    {
        var key = Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        Directory.CreateDirectory(Root);
        var temp = Path.Combine(Root, $"{key}.tmp");
        try
        {
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920]; long total = 0; int read;
                while ((read = await content.ReadAsync(buffer, ct)) > 0)
                {
                    total += read; if (total > MaxBytes) throw new CatalogValidationException("Product image must be 5 MB or smaller.");
                    await output.WriteAsync(buffer.AsMemory(0, read), ct);
                }
            }
            var signature = new byte[12];
            await using (var input = File.OpenRead(temp)) await input.ReadExactlyAsync(signature.AsMemory(0, (int)Math.Min(new FileInfo(temp).Length, signature.Length)), ct);
            var extension = DetectExtension(signature);
            var finalKey = $"{key}.{extension}";
            File.Move(temp, Path.Combine(Root, finalKey));
            return finalKey;
        }
        catch { if (File.Exists(temp)) File.Delete(temp); throw; }
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        if (SafeKey.IsMatch(key))
        {
            var path = Path.Combine(Root, key);
            if (File.Exists(path)) File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private static string DetectExtension(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "jpg";
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A) return "png";
        if (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return "webp";
        throw new CatalogValidationException("Product image must be a valid JPEG, PNG, or WebP file.");
    }
}
