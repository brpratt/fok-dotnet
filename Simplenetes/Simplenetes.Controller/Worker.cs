using System.Net.Http.Json;
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
        while (!stoppingToken.IsCancellationRequested)
        {
            var desired = await GetDesiredContainers(stoppingToken);
            var actual = await GetActualContainers(stoppingToken);
            await Reconcile(desired, actual);
            await Task.Delay(1_000, stoppingToken);
        }
    }

    private async Task<List<Container>> GetDesiredContainers(CancellationToken stoppingToken)
    {
        var response = await _serverClient.GetAsync("/containers", stoppingToken);
        return await response.Content.ReadFromJsonAsync<List<Container>>(cancellationToken: stoppingToken) ?? [];
    }

    private async Task<List<Container>> GetActualContainers(CancellationToken stoppingToken)
    {
        var filters = JsonSerializer.Serialize(new { label = new string[] { "simplenetes" } });
        var endpoint = new Uri($"/v1.43/containers/json?filters={HttpUtility.UrlEncode(filters)}", UriKind.Relative);

        var response = await _dockerClient.GetAsync(endpoint, stoppingToken);
        var dockerContainers = await response.Content.ReadFromJsonAsync<DockerContainer[]>(cancellationToken: stoppingToken) ?? [];

        return dockerContainers.Select(c => new Container(c.Names[0].TrimStart('/'), c.Image)).ToList();
    }

    private async Task Reconcile(List<Container> desired, List<Container> actual)
    {
        var toCreate = desired.Except(actual).ToList();
        var toDelete = actual.Except(desired).ToList();

        foreach (var container in toCreate)
        {
            _logger.LogInformation("Creating container {Name}", container.Name);
            var content = JsonContent.Create(new { Image = container.Image, Labels = new { simplenetes = "" } });
            await _dockerClient.PostAsync($"/v1.43/containers/create?name={container.Name}", content);
            await _dockerClient.PostAsync($"/v1.43/containers/{container.Name}/start", null);
        }

        foreach (var container in toDelete)
        {
            _logger.LogInformation("Deleting container {Name}", container.Name);
            await _dockerClient.DeleteAsync($"/v1.43/containers/{container.Name}?force=true");
        }
    }
}

record DockerContainer(string[] Names, string Image);

record Container(string Name, string Image);
