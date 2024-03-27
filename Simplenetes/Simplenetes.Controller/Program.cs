using System.IO.Pipes;
using System.Net.Sockets;

using Simplenetes.Controller;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddHttpClient("docker")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        ConnectCallback = async (ctx, ct) =>
        {
            if (OperatingSystem.IsWindows())
            {
                var pipeClientStream = new NamedPipeClientStream(
                    serverName: ".",
                    pipeName: "docker_engine",
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                await pipeClientStream.ConnectAsync(ct);

                return pipeClientStream;
            }

            var socketPath = "/var/run/docker.sock";
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            var endpoint = new UnixDomainSocketEndPoint(socketPath);

            await socket.ConnectAsync(endpoint, ct);

            return new NetworkStream(socket, ownsSocket: true);
        }
    })
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("http://localhost");
    });

builder.Services.AddHttpClient("server")
    .ConfigureHttpClient((sp, c) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var host = configuration["Server:Host"];
        c.BaseAddress = new Uri($"http://{host}:5000");
    });

var host = builder.Build();
host.Run();
