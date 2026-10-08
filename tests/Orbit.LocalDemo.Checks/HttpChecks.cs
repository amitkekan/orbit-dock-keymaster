using System.Net;

namespace Orbit.LocalDemo.Checks;

internal static class HttpChecks
{
    public static async Task RunAsync()
    {
        using var handler = new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false, UseProxy = false };
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:5081"),
            Timeout = TimeSpan.FromSeconds(10)
        };
        var count = 0;
        async Task<string> CheckAsync(string path, HttpStatusCode expected, string? host = null, HttpMethod? method = null)
        {
            using var request = new HttpRequestMessage(method ?? HttpMethod.Get, path);
            if (host is not null)
                request.Headers.Host = host;
            request.Headers.Add("X-Forwarded-Host", "attacker.invalid");
            request.Headers.Add("X-Forwarded-For", "203.0.113.1");
            using var response = await client.SendAsync(request);
            if (response.StatusCode != expected ||
                response.Headers.Contains("Set-Cookie") ||
                !response.Headers.TryGetValues("Content-Security-Policy", out var policies) ||
                !policies.Single().Contains("frame-ancestors 'none'", StringComparison.Ordinal))
                throw new InvalidOperationException($"HTTP check failed: {path}, host {host}, status {response.StatusCode}");
            count++;
            return await response.Content.ReadAsStringAsync();
        }

        var landing = await CheckAsync("/", HttpStatusCode.OK);
        if (!landing.Contains("Authorization simulation, not sign-in.", StringComparison.Ordinal))
            throw new InvalidOperationException("Missing simulation boundary notice.");
        await CheckAsync("/site.css", HttpStatusCode.OK);
        await CheckAsync("/healthz", HttpStatusCode.OK);
        var editor = await CheckAsync("/?persona=blair&fleet=aurora", HttpStatusCode.OK);
        if (!editor.Contains("Would allow", StringComparison.Ordinal) || !editor.Contains("Would deny", StringComparison.Ordinal))
            throw new InvalidOperationException("Ownership decisions not rendered.");
        var denied = await CheckAsync("/?persona=blair&fleet=nebula", HttpStatusCode.OK);
        if (!denied.Contains("Fleet unavailable", StringComparison.Ordinal) ||
            denied.Contains("Deep Space Beacon", StringComparison.Ordinal))
            throw new InvalidOperationException("Cross-fleet page disclosed missions.");
        var encoded = await CheckAsync("/?persona=%3Cscript%3Ealert(1)%3C%2Fscript%3E", HttpStatusCode.OK);
        if (encoded.Contains("<script>", StringComparison.Ordinal))
            throw new InvalidOperationException("Unencoded request input.");
        await CheckAsync("/simulation/alex/fleets", HttpStatusCode.OK);
        await CheckAsync("/simulation/blair/fleets/aurora/missions", HttpStatusCode.OK);
        await CheckAsync("/simulation/blair/fleets/nebula/missions", HttpStatusCode.NotFound);
        await CheckAsync("/simulation/unknown/fleets/aurora/missions", HttpStatusCode.NotFound);
        await CheckAsync("/simulation/alex/fleets/aurora/missions/deep-space", HttpStatusCode.NotFound);
        await CheckAsync("/", HttpStatusCode.BadRequest, "attacker.invalid:5081");
        await CheckAsync("/", HttpStatusCode.BadRequest, "localhost:5081");
        await CheckAsync("/", HttpStatusCode.BadRequest, "127.0.0.1:9999");
        await CheckAsync("/", HttpStatusCode.MethodNotAllowed, method: HttpMethod.Post);
        await CheckAsync("/simulation/alex/fleets", HttpStatusCode.MethodNotAllowed, method: HttpMethod.Delete);
        await CheckAsync("/", HttpStatusCode.OK, method: HttpMethod.Head);
        Console.WriteLine($"Passed {count} localhost HTTP checks.");
    }
}
