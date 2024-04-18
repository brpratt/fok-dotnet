using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace Simplenetes.Controller;

record Container(string Name, string Image);

record DockerContainer(string[] Names, string Image);

enum ActionKind { Create, Delete }

record Action(ActionKind Kind, Container Container);

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
                var actions = CalculateActions(desired, actual);
                await Process(actions, stoppingToken);
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

    private List<Action> CalculateActions(List<Container> desired, List<Container> actual)
    {
        var toCreate = desired.Except(actual).Select(container => new Action(ActionKind.Create, container));
        var toDelete = actual.Except(desired).Select(container => new Action(ActionKind.Delete, container));

        return toCreate.Concat(toDelete).ToList();
    }

    private async Task Process(List<Action> actions, CancellationToken stoppingToken)
    {
        if (!actions.Any())
        {
            _logger.LogInformation("No actions to perform");
            return;
        }

        foreach (var action in actions)
        {
            switch (action.Kind)
            {
                case ActionKind.Create:
                    await CreateContainer(action.Container, stoppingToken);
                    break;
                case ActionKind.Delete:
                    await DeleteContainer(action.Container, stoppingToken);
                    break;
            }
        }
    }

    private async Task CreateContainer(Container container, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pulling image {Image}", container.Image);
        var response = await _dockerClient.PostAsync($"/images/create?fromImage={container.Image}", null, stoppingToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Creating container {Name}", container.Name);
        var createContainerContent = JsonContent.Create(new { Image = container.Image, Labels = new { simplenetes = "" } });
        response = await _dockerClient.PostAsync($"/containers/create?name={container.Name}", createContainerContent, stoppingToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Starting container {Name}", container.Name);
        response = await _dockerClient.PostAsync($"/containers/{container.Name}/start", null, stoppingToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteContainer(Container container, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Deleting container {Name}", container.Name);
        var response = await _dockerClient.DeleteAsync($"/containers/{container.Name}?force=true", stoppingToken);
        response.EnsureSuccessStatusCode();
    }
}
