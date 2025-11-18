using System.Text.Json.Serialization;

namespace Guessnica_backend.Services
{
    public record FacebookTokenDebugResponse(
        [property: JsonPropertyName("data")] FacebookTokenData Data
    );

    public record FacebookTokenData(
        [property: JsonPropertyName("is_valid")] bool IsValid,
        [property: JsonPropertyName("app_id")] string AppId,
        [property: JsonPropertyName("user_id")] string UserId
    );
    
    public record FacebookUserInfo(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("email")] string? Email
    );

    public interface IFacebookAuthService
    {
        Task<(bool ok, string? userId)> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default);
        Task<FacebookUserInfo?> GetUserInfoAsync(string accessToken, CancellationToken ct = default);
    }

    public sealed class FacebookAuthService : IFacebookAuthService
    {
        private readonly HttpClient _http;
        private readonly string _appId;
        private readonly string _appSecret;

        public FacebookAuthService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _appId = cfg["Facebook:AppId"] ?? throw new InvalidOperationException("Missing Facebook:AppId");
            _appSecret = cfg["Facebook:AppSecret"] ?? throw new InvalidOperationException("Missing Facebook:AppSecret");
        }

        public async Task<(bool ok, string? userId)> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default)
        {
            var appAccessToken = $"{_appId}|{_appSecret}";
            var url = $"https://graph.facebook.com/debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appAccessToken)}";

            var resp = await _http.GetFromJsonAsync<FacebookTokenDebugResponse>(url, cancellationToken: ct);
            if (resp?.Data is null) return (false, null);

            var ok = resp.Data.IsValid && resp.Data.AppId == _appId;
            return (ok, resp.Data.UserId);
        }

        public async Task<FacebookUserInfo?> GetUserInfoAsync(string accessToken, CancellationToken ct = default)
        {
            var fields = "id,name,email";
            var url = $"https://graph.facebook.com/me?fields={fields}&access_token={Uri.EscapeDataString(accessToken)}";
            return await _http.GetFromJsonAsync<FacebookUserInfo>(url, cancellationToken: ct);
        }
    }
}