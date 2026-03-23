namespace DevCrew.Core.Services;

/// <summary>
/// Default Base64 encoder implementation.
/// </summary>
public class Base64EncoderService : IBase64EncoderService
{
    /// <inheritdoc/>
    public Base64EncodeResult Encode(byte[] input)
    {
        if (input == null || input.Length == 0)
        {
            return new Base64EncodeResult
            {
                IsSuccess = false,
                ErrorMessage = "Kodlanacak dosya verisi bulunamadi"
            };
        }

        try
        {
            var output = Convert.ToBase64String(input);

            return new Base64EncodeResult
            {
                IsSuccess = true,
                Output = output
            };
        }
        catch (Exception ex)
        {
            return new Base64EncodeResult
            {
                IsSuccess = false,
                ErrorMessage = $"Base64 encoding hatasi: {ex.Message}"
            };
        }
    }

    /// <inheritdoc/>
    public Base64DecodeResult Decode(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new Base64DecodeResult
            {
                IsSuccess = false,
                ErrorMessage = "Cozulecek Base64 verisi bulunamadi"
            };
        }

        try
        {
            var bytes = Convert.FromBase64String(input.Trim());

            return new Base64DecodeResult
            {
                IsSuccess = true,
                Output = bytes
            };
        }
        catch (FormatException)
        {
            return new Base64DecodeResult
            {
                IsSuccess = false,
                ErrorMessage = "Gecersiz Base64 formati"
            };
        }
        catch (Exception ex)
        {
            return new Base64DecodeResult
            {
                IsSuccess = false,
                ErrorMessage = $"Base64 cozme hatasi: {ex.Message}"
            };
        }
    }
}