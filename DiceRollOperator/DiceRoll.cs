using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace DiceRollOperator;

// https://github.com/kubernetes-client/csharp/tree/master/examples/customResource

public class DiceRollSpec
{
    [JsonPropertyName("dice")]
    public string[] Dice { get; set; } = [];
}

public class DiceRollResult
{
    [JsonPropertyName("die")]
    public required string Die { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public class DiceRollStatus
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("results")]
    public List<DiceRollResult> Results { get; set; } = [];
}

public class DiceRoll : IKubernetesObject<V1ObjectMeta>
{
    [JsonPropertyName("metadata")]
    public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("spec")]
    public DiceRollSpec Spec { get; set; } = new DiceRollSpec();

    [JsonPropertyName("status")]
    public DiceRollStatus? Status { get; set; }
}
