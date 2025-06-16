namespace Minify
{
    internal class TokenInfo
    {
        public string AccessToken { get; set; } = string.Empty; // The access token for Spotify API authentication
        public string RefreshToken { get; set; } = string.Empty; // Token usef for refresh
    }
}
