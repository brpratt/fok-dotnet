using System.Text.Json;
using System.Web;

namespace Simplenetes.Controller;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _dockerClient;
    private readonly HttpClient _serverClient;

    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _dockerClient = httpClientFactory.CreateClient("docker");
        _serverClient = httpClientFactory.CreateClient("server");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var filters = JsonSerializer.Serialize(new { label = new string[] { "simplenetes" } });
        var dockerEndpoint = new Uri($"/v1.43/containers/json?filters={HttpUtility.UrlEncode(filters)}", UriKind.Relative);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1_000, stoppingToken);

            var dockerResponse = await _dockerClient.GetAsync(dockerEndpoint, stoppingToken);
            var serverResponse = await _serverClient.GetAsync("/containers", stoppingToken);

            _logger.LogInformation("Docker Response: {0}", await dockerResponse.Content.ReadAsStringAsync());
            _logger.LogInformation("Server Response: {0}", await serverResponse.Content.ReadAsStringAsync());
        }
    }
}
