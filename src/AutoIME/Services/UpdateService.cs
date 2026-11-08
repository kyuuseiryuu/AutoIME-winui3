using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoIME.Services
{
    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public string CurrentVersion { get; set; } = "v2.0.0";
        public string LatestVersion { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string HtmlUrl { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public static class UpdateService
    {
        private const string GITHUB_REPO = "kyuuseiryuu/AutoIME";
        private const string CURRENT_VERSION = "v2.0.0";

        public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            var result = new UpdateCheckResult
            {
                CurrentVersion = CURRENT_VERSION
            };

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AutoIME-WinUI3");
                client.Timeout = TimeSpan.FromSeconds(6);

                string url = $"https://api.github.com/repos/{GITHUB_REPO}/releases/latest";
                string json = await client.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
                result.LatestVersion = tagName;
                result.HtmlUrl = root.GetProperty("html_url").GetString() ?? $"https://github.com/{GITHUB_REPO}/releases";

                if (root.TryGetProperty("body", out var bodyEl))
                {
                    result.ReleaseNotes = bodyEl.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("assets", out var assets) && assets.GetArrayLength() > 0)
                {
                    result.DownloadUrl = assets[0].GetProperty("browser_download_url").GetString() ?? result.HtmlUrl;
                }
                else
                {
                    result.DownloadUrl = result.HtmlUrl;
                }

                // Check version comparison
                if (!string.IsNullOrEmpty(tagName) &&
                    !string.Equals(tagName.Trim(), CURRENT_VERSION.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    result.HasUpdate = true;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }
}

