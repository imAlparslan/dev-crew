namespace DevCrew.Desktop.Services;

/// <summary>
/// Detects file type from binary magic bytes and formats file size.
/// </summary>
internal static class FileTypeDetector
{
    /// <summary>
    /// Detects MIME type and likely extension from the first bytes of a file.
    /// </summary>
    public static (string MimeType, string Extension) Detect(byte[] data)
    {
        if (data == null || data.Length == 0)
            return ("application/octet-stream", "bin");

        // PDF: %PDF
        if (data.Length >= 4 &&
            data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46)
            return ("application/pdf", "pdf");

        // PNG
        if (data.Length >= 8 &&
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return ("image/png", "png");

        // JPEG
        if (data.Length >= 3 &&
            data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return ("image/jpeg", "jpg");

        // GIF
        if (data.Length >= 6 &&
            data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 &&
            data[3] == 0x38 && (data[4] == 0x37 || data[4] == 0x39) && data[5] == 0x61)
            return ("image/gif", "gif");

        // WebP: RIFF....WEBP
        if (data.Length >= 12 &&
            data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
            data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            return ("image/webp", "webp");

        // BMP
        if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D)
            return ("image/bmp", "bmp");

        // TIFF little-endian
        if (data.Length >= 4 &&
            data[0] == 0x49 && data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00)
            return ("image/tiff", "tiff");

        // TIFF big-endian
        if (data.Length >= 4 &&
            data[0] == 0x4D && data[1] == 0x4D && data[2] == 0x00 && data[3] == 0x2A)
            return ("image/tiff", "tiff");

        // ZIP (includes DOCX, XLSX, PPTX, JAR, APK…)
        if (data.Length >= 4 &&
            data[0] == 0x50 && data[1] == 0x4B && data[2] == 0x03 && data[3] == 0x04)
            return ("application/zip", "zip");

        // MP3 with ID3 tag
        if (data.Length >= 3 &&
            data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
            return ("audio/mpeg", "mp3");

        // MP3 sync word (no ID3)
        if (data.Length >= 2 && data[0] == 0xFF && (data[1] & 0xE0) == 0xE0)
            return ("audio/mpeg", "mp3");

        // MP4 / MOV: ftyp box at offset 4
        if (data.Length >= 12 &&
            data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70)
            return ("video/mp4", "mp4");

        // XML: <?xml
        if (data.Length >= 5 &&
            data[0] == 0x3C && data[1] == 0x3F && data[2] == 0x78 &&
            data[3] == 0x6D && data[4] == 0x6C)
            return ("application/xml", "xml");

        // HTML: <!DOCTYPE or <HTML
        if (data.Length >= 9)
        {
            var prefix = System.Text.Encoding.ASCII.GetString(data, 0, Math.Min(10, data.Length))
                                                   .ToUpperInvariant();
            if (prefix.StartsWith("<!DOCTYPE") || prefix.StartsWith("<HTML"))
                return ("text/html", "html");
        }

        // Plain text: no null bytes in first 512 bytes
        if (IsLikelyText(data))
            return ("text/plain", "txt");

        return ("application/octet-stream", "bin");
    }

    /// <summary>
    /// Formats a byte count as a human-readable size string.
    /// </summary>
    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }

    private static bool IsLikelyText(byte[] data)
    {
        var sample = data.Length > 512 ? data[..512] : data;
        return !sample.Any(b => b == 0x00);
    }
}
