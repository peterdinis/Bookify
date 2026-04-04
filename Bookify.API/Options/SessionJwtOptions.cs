namespace Bookify.API.Options;

public class SessionJwtOptions
{
    public const string SectionName = "SessionJwt";

    public string Issuer { get; set; } = "Bookify";

    public string Audience { get; set; } = "BookifyApi";

    /// <summary>HMAC key — minimálne 32 znakov (256 bitov pre SHA-256).</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 10080;
}
