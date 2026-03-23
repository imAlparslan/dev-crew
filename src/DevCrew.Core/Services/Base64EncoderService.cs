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
}