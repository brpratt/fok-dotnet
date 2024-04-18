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
            _logger.LogInformation("DiceRoll: {EventType} {Name}", watchEventType, diceroll.Metadata.Name);

            if (watchEventType == WatchEventType.Added || watchEventType == WatchEventType.Modified)
            {
                if (NeedsToBeRolled(diceroll))
                {
                    _logger.LogInformation("DiceRoll: Rolling {Name}", diceroll.Metadata.Name);
                    Roll(diceroll);
                    await UpdateStatus(diceroll);
                }
            }
        }
    }

    private bool NeedsToBeRolled(DiceRoll diceRoll)
    {
        if (diceRoll.Status == null)
        {
            return true;
        }

        if (diceRoll.Spec.Dice.Length != diceRoll.Status.Results.Count)
        {
            return true;
        }

        return diceRoll.Spec.Dice
            .Select((die, index) => (die, index))
            .Any(pair => pair.die != diceRoll.Status.Results[pair.index].Die);
    }

    private void Roll(DiceRoll diceRoll)
    {
        var results = diceRoll.Spec.Dice.Select(
            die => new DiceRollResult
            {
                Die = die,
                Value = Roll(die)
            }).ToList();

        var total = results.Sum(result => result.Value);

        diceRoll.Status = new DiceRollStatus
        {
            Total = total,
            Results = results
        };
    }

    private int Roll(string die) => die switch
    {
        "D4" => new Random().Next(1, 5),
        "D6" => new Random().Next(1, 7),
        "D8" => new Random().Next(1, 9),
        "D10" => new Random().Next(1, 11),
        "D12" => new Random().Next(1, 13),
        "D20" => new Random().Next(1, 21),
        "D100" => new Random().Next(1, 101),
        _ => 1
    };

    private Task UpdateStatus(DiceRoll diceRoll) =>
        _kubernetes.CustomObjects.PatchNamespacedCustomObjectStatusAsync<DiceRoll>(
            new V1Patch(diceRoll, V1Patch.PatchType.MergePatch),
            diceRoll.ApiGroup(),
            diceRoll.ApiGroupVersion(),
            diceRoll.Namespace(),
            "dicerolls",
            diceRoll.Metadata.Name);
}
