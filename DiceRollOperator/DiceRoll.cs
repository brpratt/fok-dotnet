using System.Text.Json.Serialization;
using IdentityModel.OidcClient;
using k8s;
using k8s.Models;

namespace DiceRollOperator;

// https://github.com/kubernetes-client/csharp/tree/master/examples/customResource

public class DiceRollSpec
{
    [JsonPropertyName("dice")]
    public string[] Dice { get; set; }

    [JsonPropertyName("modifier")]
    public int Modifier { get; set; }
}

public class DiceRollStatus
{
    [JsonPropertyName("result")]
    public int Result { get; set; }
}

public class DiceRoll : IKubernetesObject<V1ObjectMeta>
{
    [JsonPropertyName("metadata")]
    public V1ObjectMeta Metadata { get; set; }

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; }

    [JsonPropertyName("spec")]
    public DiceRollSpec Spec { get; set; }

    [JsonPropertyName("status")]
    public DiceRollStatus Status { get; set; }
}

// use this instead?
// https://github.com/kubernetes-client/csharp/blob/master/src/KubernetesClient/Models/KubernetesList.cs
public class DiceRollList : IKubernetesObject<V1ListMeta>
{
    [JsonPropertyName("metadata")]
    public V1ListMeta Metadata { get; set; }

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; }

    public List<DiceRoll> Items { get; set; }
}
