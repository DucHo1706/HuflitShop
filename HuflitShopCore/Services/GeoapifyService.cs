using System.Text.Json;

namespace HuflitShopCore.Services
{
    public sealed record AddressSuggestion(string PlaceId, string Value, string Label);

    public sealed class GeoapifyService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeoapifyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Geoapify:ApiKey"]?.Trim() ?? string.Empty;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(
            string query,
            string? ward,
            string? district,
            string? province,
            CancellationToken cancellationToken = default)
        {
            query = query.Trim();
            if (!IsConfigured || query.Length < 3) return Array.Empty<AddressSuggestion>();

            var exactText = string.Join(", ", new[] { query, ward, district, province }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            var broadText = string.Join(", ", new[] { query, province }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            var exactTask = FetchSuggestionsAsync(exactText, cancellationToken);
            var broadTask = FetchSuggestionsAsync(broadText, cancellationToken);
            await Task.WhenAll(exactTask, broadTask);

            var candidates = exactTask.Result
                .Concat(broadTask.Result)
                .DistinctBy(x => x.PlaceId)
                .ToList();

            if (candidates.Count == 0)
                candidates.AddRange(await FetchSuggestionsAsync(query, cancellationToken));

            return candidates
                .Select((suggestion, index) => new
                {
                    Suggestion = suggestion,
                    Index = index,
                    AreaScore = GetAreaScore(suggestion.Label, ward, district, province)
                })
                .OrderByDescending(x => x.AreaScore)
                .ThenBy(x => x.Index)
                .Select(x => x.Suggestion)
                .Take(5)
                .ToList();
        }

        private async Task<IReadOnlyList<AddressSuggestion>> FetchSuggestionsAsync(
            string text,
            CancellationToken cancellationToken)
        {
            var url =
                $"v1/geocode/autocomplete?text={Uri.EscapeDataString(text)}" +
                $"&filter=countrycode:vn&lang=vi&format=json&limit=5" +
                $"&apiKey={Uri.EscapeDataString(_apiKey)}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return Array.Empty<AddressSuggestion>();

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("results", out var suggestions))
                return Array.Empty<AddressSuggestion>();

            var results = new List<AddressSuggestion>();
            foreach (var suggestion in suggestions.EnumerateArray())
            {
                var placeId = suggestion.TryGetProperty("place_id", out var placeIdElement)
                    ? placeIdElement.GetString()
                    : null;
                var label = suggestion.TryGetProperty("formatted", out var labelElement)
                    ? labelElement.GetString()
                    : null;
                var value = suggestion.TryGetProperty("address_line1", out var valueElement)
                    ? valueElement.GetString()
                    : label;

                if (string.IsNullOrWhiteSpace(value) ||
                    string.IsNullOrWhiteSpace(label)) continue;

                placeId ??= label;
                results.Add(new AddressSuggestion(placeId, value, label));
            }

            return results;
        }

        private static int GetAreaScore(
            string label,
            string? ward,
            string? district,
            string? province)
        {
            var score = 0;
            if (ContainsArea(label, ward)) score += 4;
            if (ContainsArea(label, district)) score += 2;
            if (ContainsArea(label, province)) score += 1;
            return score;
        }

        private static bool ContainsArea(string label, string? area)
        {
            return !string.IsNullOrWhiteSpace(area) &&
                   label.Contains(area.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
