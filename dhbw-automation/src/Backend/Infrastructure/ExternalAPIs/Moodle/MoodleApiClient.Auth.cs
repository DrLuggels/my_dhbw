using System.Text.Json;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// MoodleApiClient - Authentication methods
/// </summary>
public partial class MoodleApiClient
{
    /// <summary>
    /// Login with username and password to get a token
    /// Endpoint: /login/token.php
    /// </summary>
    /// <param name="username">Moodle username</param>
    /// <param name="password">Moodle password</param>
    /// <param name="service">Service name (default: moodle_mobile_app)</param>
    /// <returns>Token if successful, null on error</returns>
    public async Task<MoodleLoginResult> LoginAndGetTokenAsync(string username, string password, string service = "moodle_mobile_app")
    {
        try
        {
            var loginParams = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password },
                { "service", service }
            };

            var response = await _httpClient.PostAsync(
                "/login/token.php",
                new FormUrlEncodedContent(loginParams));

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Moodle Login Response: {Json}", json);

            var result = JsonSerializer.Deserialize<MoodleTokenResponse>(json, JsonOptions);

            if (result?.Token != null)
            {
                _token = result.Token;
                return new MoodleLoginResult
                {
                    Success = true,
                    Token = result.Token
                };
            }

            return new MoodleLoginResult
            {
                Success = false,
                ErrorMessage = result?.Error ?? "Unknown login error"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during Moodle login for user {Username}", username);
            return new MoodleLoginResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
