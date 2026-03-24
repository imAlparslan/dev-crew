namespace DevCrew.Core.Services;

public static class ErrorKeys
{
    public static class JsonFormatter
    {
        public const string InputRequired = "jsonformatter.input_required";
        public const string InvalidJson = "jsonformatter.invalid_json";
    }

    public static class JsonDiff
    {
        public const string InputsRequired = "jsondiff.inputs_required";
        public const string LeftInvalid = "jsondiff.left_invalid";
        public const string RightInvalid = "jsondiff.right_invalid";
    }

    public static class Base64
    {
        public const string EncodeInputRequired = "base64.encode_input_required";
        public const string EncodeFailed = "base64.encode_failed";
        public const string DecodeInputRequired = "base64.decode_input_required";
        public const string DecodeInvalidFormat = "base64.decode_invalid_format";
        public const string DecodeFailed = "base64.decode_failed";
    }

    public static class Jwt
    {
        public const string TokenEmpty = "jwt.token_empty";
        public const string TokenFormatInvalid = "jwt.token_format_invalid";
        public const string DecodeFailed = "jwt.decode_failed";
        public const string BuildSecretRequired = "jwt.build_secret_required";
        public const string BuildUnsupportedAlgorithm = "jwt.build_unsupported_algorithm";
        public const string BuildInvalidRsaPrivateKey = "jwt.build_invalid_rsa_private_key";
        public const string BuildFailed = "jwt.build_failed";
    }
}
