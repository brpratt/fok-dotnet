using DiceRollOperator;
using k8s;

var builder = Host.CreateApplicationBuilder(args);

var kubernetesClientConfig = KubernetesClientConfiguration.BuildDefaultConfig();
builder.Services.AddSingleton<IKubernetes>(_ => new Kubernetes(kubernetesClientConfig));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
