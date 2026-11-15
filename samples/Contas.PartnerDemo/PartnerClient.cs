using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Contas.PartnerDemo;

internal sealed class PartnerClient(HttpClient client, IOptions<PartnerOptions> options, ILogger<PartnerClient> logger)
{
    private readonly PartnerOptions settings = options.Value;

    internal async Task<IResult> Query(Guid id, CancellationToken ct)
    {
        try
        {
            using var credentials = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.ClientId,
                ["client_secret"] = settings.Secret
            });
            using var tokenResponse = await client.PostAsync(settings.TokenEndpoint, credentials, ct);
            if (!tokenResponse.IsSuccessStatusCode) return Unavailable();
            using var token = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!token.RootElement.TryGetProperty("access_token", out var value) || value.GetString() is not { } access)
                return Unavailable();
            using var request = new HttpRequestMessage(HttpMethod.Get,
                settings.AccountsApi + settings.ResourcePath.Replace("{id}", id.ToString(), StringComparison.Ordinal));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            using var response = await client.SendAsync(request, ct);
            return Results.Text(await response.Content.ReadAsStringAsync(ct), "application/json", statusCode: (int)response.StatusCode);
        }
        catch (Exception error) when (error is HttpRequestException or JsonException ||
                                     error is OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning("A integração com a API de contas está indisponível: {ErrorType}", error.GetType().Name);
            return Unavailable();
        }
    }

    private static IResult Unavailable() => Results.Json(new
    {
        data = (object?)null,
        error = new { code = "partner.upstream_unavailable", message = "Integração temporariamente indisponível." },
        traceId = System.Diagnostics.Activity.Current?.TraceId.ToString()
    }, statusCode: 503);
}
