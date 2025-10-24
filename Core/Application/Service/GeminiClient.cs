using System.Text.Json;

namespace ClarkAI.Core.Application.Service
{
    public class GeminiClient
    {
        public readonly HttpClient _httpClient;
        public readonly string _apiKey;
        public readonly string _model;

        public GeminiClient(HttpClient httpClient, string apiKey, string model = "gemini-2.5-flash")
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _model = model;
        }

        public async Task<string> GenerateContentAsync(string prompt)
        {
            var request = new
            {
                content = new[]
                {
                    new
                    {
                        role = "user",
                        part = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(url, request);
                if(!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"[Gemini Error] Status: {response.StatusCode}, Details: {errorContent}";
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if(doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                               .GetProperty("content")
                               .GetProperty("parts")[0]
                               .GetProperty("text")
                               .GetString();

                    return string.IsNullOrEmpty(text) ? "[Gemini] Empty response." : text.Trim(); 
                }
                return "[Gemini] No canditates returned";
            }
            catch(Exception ex)
            {
                return $"[Gemini Exception] {ex.Message}";
            }
        }
    }
}
