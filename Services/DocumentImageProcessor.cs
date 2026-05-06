using Paynest.Core.Interfaces;
using Paynest.Core.Models.Profile;
using SkiaSharp;

namespace Paynest.Services;

public partial class DocumentImageProcessor : IDocumentImageProcessor
{
    private const int MaxUploadBytes = 5 * 1024 * 1024;
    private const int MinimumQuality = 55;
    private const double ResizeStepFactor = 0.85;
    private const int MinimumLongSide = 960;

    public async Task<PreparedUploadFile> PrepareForUploadAsync(
        FileResult file,
        DocumentType documentType,
        ImageUploadFormat format = ImageUploadFormat.Webp,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var detectedFormat = DetectSourceFormat(file);
        if (detectedFormat is null)
        {
            throw new InvalidOperationException(
                "La imagen seleccionada no es compatible. Usa JPG, JPEG, PNG, WEBP, HEIC o HEIF.");
        }

        await using var sourceStream = await file.OpenReadAsync();
        using var sourceBuffer = new MemoryStream();
        await sourceStream.CopyToAsync(sourceBuffer, ct);

        if (sourceBuffer.Length == 0)
            throw new InvalidOperationException("No pudimos leer la imagen seleccionada.");

        var sourceBytes = sourceBuffer.ToArray();
        var preset = GetPreset(documentType);

        byte[]? bestAttempt = null;
        var currentLongSide = preset.TargetLongSide;

        while (currentLongSide >= MinimumLongSide)
        {
            for (var quality = preset.StartQuality; quality >= MinimumQuality; quality -= 7)
            {
                ct.ThrowIfCancellationRequested();

                var attempt = ConvertToTargetFormat(sourceBytes, currentLongSide, quality, format);
                if (attempt.Length <= MaxUploadBytes)
                {
                    return new PreparedUploadFile(
                        BuildOutputFileName(file.FileName, documentType, format),
                        GetContentType(format),
                        attempt);
                }

                if (bestAttempt is null || attempt.Length < bestAttempt.Length)
                    bestAttempt = attempt;
            }

            currentLongSide = (int)Math.Floor(currentLongSide * ResizeStepFactor);
        }

        if (bestAttempt is not null && bestAttempt.Length <= MaxUploadBytes)
        {
            return new PreparedUploadFile(
                BuildOutputFileName(file.FileName, documentType, format),
                GetContentType(format),
                bestAttempt);
        }

        throw new InvalidOperationException(
            "No pudimos comprimir la imagen por debajo de 5 MB sin perder demasiada calidad. Intenta con una foto más nítida o más cercana.");
    }

    public static (int MaxLongSide, int StartQuality) GetRecommendedSettings(DocumentType documentType)
    {
        var preset = GetPreset(documentType);
        return (preset.TargetLongSide, preset.StartQuality);
    }

    private static UploadPreset GetPreset(DocumentType documentType)
        => documentType switch
        {
            DocumentType.Selfie => new UploadPreset(TargetLongSide: 1600, StartQuality: 80),
            DocumentType.IdentificacionOficialFrente => new UploadPreset(TargetLongSide: 2000, StartQuality: 86),
            DocumentType.IdentificacionOficialReverso => new UploadPreset(TargetLongSide: 2000, StartQuality: 86),
            DocumentType.ComprobanteDomicilioFrente => new UploadPreset(TargetLongSide: 2200, StartQuality: 88),
            DocumentType.ComprobanteDomicilioReverso => new UploadPreset(TargetLongSide: 2200, StartQuality: 88),
            _ => new UploadPreset(TargetLongSide: 2000, StartQuality: 84)
        };

    private static string BuildOutputFileName(string? originalFileName, DocumentType documentType, ImageUploadFormat format)
    {
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = documentType.ToString();

        var extension = format switch
        {
            ImageUploadFormat.Webp => ".webp",
            ImageUploadFormat.Jpeg => ".jpg",
            _ => ".img"
        };

        return $"{baseName}{extension}";
    }

    private static string GetContentType(ImageUploadFormat format)
        => format switch
        {
            ImageUploadFormat.Webp => "image/webp",
            ImageUploadFormat.Jpeg => "image/jpeg",
            _ => "application/octet-stream"
        };

    private static string? DetectSourceFormat(FileResult file)
    {
        var contentType = file.ContentType?.Trim().ToLowerInvariant();
        if (contentType is "image/jpeg" or "image/jpg" or "image/png" or "image/webp" or "image/heic" or "image/heif")
            return contentType;

        var ext = Path.GetExtension(file.FileName)?.Trim().ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            _ => null
        };
    }

    private sealed record UploadPreset(int TargetLongSide, int StartQuality);

    private static byte[] ConvertToTargetFormat(byte[] sourceBytes, int maxLongSide, int quality, ImageUploadFormat format)
    {
        using var managedStream = new MemoryStream(sourceBytes, writable: false);
        using var codec = SKCodec.Create(managedStream)
            ?? throw new InvalidOperationException("No pudimos leer la imagen seleccionada.");

        var sourceInfo = codec.Info;
        if (sourceInfo.Width <= 0 || sourceInfo.Height <= 0)
            throw new InvalidOperationException("No pudimos leer la imagen seleccionada.");

        var targetDimensions = GetTargetDimensions(sourceInfo.Width, sourceInfo.Height, maxLongSide);
        var sourceBitmapInfo = new SKImageInfo(
            sourceInfo.Width,
            sourceInfo.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using var sourceBitmap = new SKBitmap(sourceBitmapInfo);
        var decodeResult = codec.GetPixels(sourceBitmapInfo, sourceBitmap.GetPixels());
        if (decodeResult is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
            throw new InvalidOperationException("No pudimos procesar la imagen seleccionada.");

        var targetBitmapInfo = new SKImageInfo(
            targetDimensions.Width,
            targetDimensions.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using var finalBitmap = targetDimensions.Width == sourceInfo.Width && targetDimensions.Height == sourceInfo.Height
            ? sourceBitmap.Copy()
            : sourceBitmap.Resize(targetBitmapInfo, SKFilterQuality.High)
                ?? throw new InvalidOperationException("No pudimos redimensionar la imagen.");

        using var image = SKImage.FromBitmap(finalBitmap);
        using var encoded = image.Encode(ToSkEncodedFormat(format), quality);
        if (encoded is null)
            throw new InvalidOperationException($"No pudimos convertir la imagen a {format}.");

        return encoded.ToArray();
    }

    private static SKEncodedImageFormat ToSkEncodedFormat(ImageUploadFormat format)
        => format switch
        {
            ImageUploadFormat.Webp => SKEncodedImageFormat.Webp,
            ImageUploadFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            _ => SKEncodedImageFormat.Jpeg
        };

    private static (int Width, int Height) GetTargetDimensions(int width, int height, int maxLongSide)
    {
        var currentLongSide = Math.Max(width, height);
        if (currentLongSide <= maxLongSide)
            return (width, height);

        var scale = (double)maxLongSide / currentLongSide;
        return (
            Width: Math.Max(1, (int)Math.Round(width * scale)),
            Height: Math.Max(1, (int)Math.Round(height * scale)));
    }
}
