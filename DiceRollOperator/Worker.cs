using k8s;
using k8s.Models;

namespace DiceRollOperator;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IKubernetes _kubernetes;

    public Worker(ILogger<Worker> logger, IKubernetes kubernetes)
    {
        _logger = logger;
        _kubernetes = kubernetes;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = new GenericClient(_kubernetes, "example.com", "v1", "dicerolls");

        await foreach (var (watchEventType, diceroll) in client.WatchAsync<DiceRoll>(cancel: stoppingToken))
        {
            _logger.LogInformation("DiceRoll: {0} {1}", watchEventType, diceroll.Metadata.Name);

            if (watchEventType == WatchEventType.Added)
            {
                diceroll.Status = new DiceRollStatus
                {
                    Result = new Random().Next(1, 7)
                };

                _kubernetes.CustomObjects.PatchNamespacedCustomObjectStatus<DiceRoll>(
                    new V1Patch(diceroll, V1Patch.PatchType.MergePatch),
                    diceroll.ApiGroup(),
                    diceroll.ApiGroupVersion(),
                    diceroll.Namespace(),
                    "dicerolls",
                    diceroll.Metadata.Name
                );
            }
        }
    }
}
