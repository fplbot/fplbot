using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace Discord.Net.Endpoints.Authentication;

internal class DiscordEventsAuthenticationAuthenticationHandler : AuthenticationHandler<DiscordEventsAuthenticationOptions>
{
    private const string TimestampHeaderName = "X-Signature-Timestamp";
    private const string SignatureHeaderName = "X-Signature-Ed25519";

    private readonly string _publicKey;

    public DiscordEventsAuthenticationAuthenticationHandler(
        IOptionsMonitor<DiscordEventsAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
        if (string.IsNullOrEmpty(options.CurrentValue.PublicKey))
            throw new ArgumentNullException(nameof(DiscordEventsAuthenticationOptions.PublicKey));

        _publicKey = options.CurrentValue.PublicKey;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        IHeaderDictionary headers = Request.Headers;

        string timestamp = headers[TimestampHeaderName].FirstOrDefault();
        string signature = headers[SignatureHeaderName].FirstOrDefault();

        if (timestamp == null)
        {
            return HandleRequestResult.Fail($"Missing header {TimestampHeaderName}");
        }

        if (signature == null)
        {
            return HandleRequestResult.Fail($"Missing header {SignatureHeaderName}");
        }

        bool isNumber = long.TryParse(timestamp, out long timestampAsLong);

        if (!isNumber)
        {
            return HandleRequestResult.Fail($"Invalid header. Header {TimestampHeaderName} not a number");
        }

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        if (IsValidDiscordSignature(signature, timestampAsLong, body))
        {
            return HandleRequestResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), DiscordEventsAuthenticationConstants.AuthenticationScheme));
        }

        return HandleRequestResult.Fail("Verification of Discord request failed.");

    }

    private static readonly DateTime Seventies = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    private static long Now => (long)DateTime.UtcNow.Subtract(Seventies).TotalSeconds;

    private bool IsValidDiscordSignature(string incomingSignature, long timestamp, string body)
    {
        if (!IsWithinRange(timestamp, TimeSpan.FromMinutes(5)))
        {
            return false;
        }

        var algorithm = SignatureAlgorithm.Ed25519;
        var publicKey = PublicKey.Import(algorithm, GetBytesFromHexString(_publicKey), KeyBlobFormat.RawPublicKey);
        var data = Encoding.UTF8.GetBytes(timestamp + body);
        var verified = algorithm.Verify(publicKey, data, GetBytesFromHexString(incomingSignature));


        return verified;
    }

    private static bool IsWithinRange(long timestamp, TimeSpan tolerance)
    {
        return Now - timestamp <= tolerance.TotalSeconds;
    }

    private byte[] GetBytesFromHexString(string hex)
    {
        var length = hex.Length;
        var bytes = new byte[length / 2];

        for (int i = 0; i < length; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

        return bytes;
    }
}
