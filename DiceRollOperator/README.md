# DiceRoll Operator

DiceRoll Operator is a basic operator for demonstrating:

- how to extend the Kubernetes API using a [CustomResourceDefinition](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definitions/) (CRD)
- how to call the Kubernetes API using a [client library](https://kubernetes.io/docs/reference/using-api/client-libraries/)

## Running

Running this example involves three steps:

1. Create the cluster
1. Apply the CustomResourceDefinition
1. Run the controller

These steps are explained in more detail in the following sections.

### Create the cluster

Assuming Docker and minikube are installed per the [prerequisites](../README.md#prerequisites), the simplest way to create a cluster is to run the following:

```
$ minikube start --driver=docker
```

This will create a cluster inside a container.

**Note:** If you want to use a different cluster, make sure you have sufficient permissions to manage CustomResourceDefinitions, ClusterRoles, and ClusterRoleBindings.

### Apply the CustomResourceDefinition

We can send the CustomResourceDefinition to the cluster as follows:

```
$ minikube kubectl -- apply -f ./manifests/crd.yaml
```

### Run the controller

#### From outside the cluster

Running the controller outside the cluster is the most convenient way to make code changes and troubleshoot issues. Simply run the `DiceRollOperator.csproj` project from your IDE or run the following from within the `DiceRollOperator` directory:

```
$ dotnet run
```

The controller will access the cluster using the default context from your [kubeconfig](https://kubernetes.io/docs/concepts/configuration/organize-cluster-access-kubeconfig/).

#### From inside the cluster

Controllers typically run from inside the cluster in a production setting. To run this controller from inside the cluster, we first need to build and load the controller image into the cluster:

```
$ docker build . -t dice-roll-operator:1.0.0
$ minikube image load dice-roll-operator:1.0.0
```

**Note:** Typically you'll push your image to a container registry, but loading the image directly into the cluster is useful for demos.

Next, we'll apply the necessary resources to run the controller inside the cluster:

```
$ minikube kubectl -- apply -f ./manifests/operator.yaml
```

Verify the controller is running:

```
$ minikube kubectl -- get pods
NAME                                  READY   STATUS    RESTARTS   AGE
dice-roll-operator-5769b47798-68h2m   1/1     Running   0          11s
```

If `STATUS` is `Running`, then everything is working.

## Examples

### Creating a DiceRoll resource via explicit API calls

First expose the Kubernetes API server as follows:

```
$ minikube kubectl -- proxy
```

Then, in a separate terminal, run the following to create the DiceRoll:

```
# macOS/Linux
$ curl -X POST -H 'Content-Type: application/json' http://localhost:8001/apis/example.com/v1/namespaces/default/dicerolls -d '{ "apiVersion": "example.com/v1", "kind": "DiceRoll", "metadata": { "name": "my-dice-roll" }, "spec": { "dice": ["D4", "D20"] } }'

# Windows
> Invoke-RestMethod -Method Post -ContentType application/json -Uri http://localhost:8001/apis/example.com/v1/namespaces/default/dicerolls -Body '{ "apiVersion": "example.com/v1", "kind": "DiceRoll", "metadata": { "name": "my-dice-roll" }, "spec": { "dice": ["D4", "D20"] } }'
```

We can see the result of the roll as follows:

```
# macOS/Linux
$ curl http://localhost:8001/apis/example.com/v1/namespaces/default/dicerolls/my-dice-roll | jq '.status'

# Windows
> (Invoke-RestMethod -Uri http://localhost:8001/apis/example.com/v1/namespaces/default/dicerolls/my-dice-roll).status
```

Finally, we can delete the resource:

```
# macOS/Linux
$ curl -X DELETE http://localhost:8001/apis/example.com/v1/namespaces/default/dicerolls/my-dice-roll

# Windows
> Invoke-RestMethod -Method Delete http://localhost:8001/apis/example.com/v1/namespaces/default/dicerolls/my-dice-roll
```

**Note:** You can stop the kubectl proxy command when you're done sending requests to the API directly.

### Creating a DiceRoll resource with kubectl

First, create a manifest as follows:

```yaml
apiVersion: example.com/v1
kind: DiceRoll
metadata:
  name: my-dice-roll
spec:
  dice:
  - D4
  - D20
```

**Note:** this example manifest can be found at [`manifests/example-dice-roll.yaml`](./manifests/example-dice-roll.yaml)

Next, create the resource:

```
$ minikube kubectl -- apply -f ./manifests/example-dice-roll.yaml
```

We can look at the resource as YAML:

```
$ minikube kubectl -- get diceroll my-dice-roll -o yaml
```

The following will fetch the status as JSON:

```
$ minikube kubectl -- get diceroll my-dice-roll -o jsonpath='{.status}'
```

Finally, we can delete the resource:

```
$ minikube kubectl -- delete diceroll my-dice-roll
```
