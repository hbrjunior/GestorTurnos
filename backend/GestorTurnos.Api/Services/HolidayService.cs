
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace GestorTurnos.Api.Services;

public sealed record HolidayInfo(
    DateOnly Date,
    string Name
);

public sealed class HolidayService(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<HolidayService> logger)
{
    public async Task<IReadOnlyDictionary<DateOnly, string>?>
        GetHolidaysAsync(
            string countryCode,
            int year,
            CancellationToken ct)
    {
        countryCode = countryCode.Trim().ToUpperInvariant();

        if (countryCode.Length != 2)
            return null;

        var cacheKey = $"holidays:{countryCode}:{year}";

        if (cache.TryGetValue(
            cacheKey,
            out IReadOnlyDictionary<DateOnly, string>? cached))
        {
            return cached;
        }

        try
        {
            var url =
                $"api/v3/PublicHolidays/{year}/{countryCode}";

            using var response = await httpClient.GetAsync(
                url, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Nager.Date retornou {StatusCode} para {Country}/{Year}",
                    response.StatusCode,
                    countryCode,
                    year);

                return null;
            }

            var holidays = await response.Content
                .ReadFromJsonAsync<List<NagerHoliday>>(
                    cancellationToken: ct);

            if (holidays is null)
                return null;

            var result = holidays
                .Where(h =>
                    h.Global &&
                    h.Types is not null &&
                    h.Types.Contains(
                        "Public",
                        StringComparer.OrdinalIgnoreCase))
                .GroupBy(h => h.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().LocalName
                        ?? g.First().Name
                        ?? "Feriado nacional");

            cache.Set(
                cacheKey,
                result,
                TimeSpan.FromHours(12));

            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(
                ex,
                "Falha HTTP ao consultar feriados.");

            return null;
        }
        catch (TaskCanceledException ex)
            when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(
                ex,
                "Timeout na consulta de feriados.");

            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Resposta inválida da Nager.Date.");

            return null;
        }
    }

    private sealed class NagerHoliday
    {
        public DateOnly Date { get; set; }
        public string? LocalName { get; set; }
        public string? Name { get; set; }
        public bool Global { get; set; }
        public string[]? Types { get; set; }
    }
}
