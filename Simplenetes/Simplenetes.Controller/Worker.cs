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
            try
            {
                var desired = await GetDesiredContainers(stoppingToken);
                var actual = await GetActualContainers(stoppingToken);
                await Reconcile(desired, actual);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while reconciling containers");
            }

            await Task.Delay(1_000, stoppingToken);
        }
    }

    private async Task<List<Container>> GetDesiredContainers(CancellationToken stoppingToken)
    {
        var response = await _serverClient.GetAsync("/containers", stoppingToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Container>>(cancellationToken: stoppingToken) ?? [];
    }

    private async Task<List<Container>> GetActualContainers(CancellationToken stoppingToken)
    {
        var filters = JsonSerializer.Serialize(new { label = new string[] { "simplenetes" } });
        var endpoint = new Uri($"/containers/json?filters={HttpUtility.UrlEncode(filters)}", UriKind.Relative);

        var response = await _dockerClient.GetAsync(endpoint, stoppingToken);
        response.EnsureSuccessStatusCode();
        var dockerContainers = await response.Content.ReadFromJsonAsync<DockerContainer[]>(cancellationToken: stoppingToken) ?? [];

        return dockerContainers.Select(c => new Container(c.Names[0].TrimStart('/'), c.Image)).ToList();
    }

    private async Task Reconcile(List<Container> desired, List<Container> actual)
    {
        HttpResponseMessage? response;

        var toCreate = desired.Except(actual).ToList();
        var toDelete = actual.Except(desired).ToList();

        foreach (var container in toCreate)
        {
            _logger.LogInformation("Pulling image {Image}", container.Image);
            response = await _dockerClient.PostAsync($"/images/create?fromImage={container.Image}", null);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Creating container {Name}", container.Name);
            var createContainerContent = JsonContent.Create(new { Image = container.Image, Labels = new { simplenetes = "" } });
            response = await _dockerClient.PostAsync($"/containers/create?name={container.Name}", createContainerContent);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Starting container {Name}", container.Name);
            response = await _dockerClient.PostAsync($"/containers/{container.Name}/start", null);
            response.EnsureSuccessStatusCode();
        }

        foreach (var container in toDelete)
        {
            _logger.LogInformation("Deleting container {Name}", container.Name);
            response = await _dockerClient.DeleteAsync($"/containers/{container.Name}?force=true");
            response.EnsureSuccessStatusCode();
        }
    }
}

record DockerContainer(string[] Names, string Image);

record Container(string Name, string Image);
