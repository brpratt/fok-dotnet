using System.Net.Sockets;

namespace Simplenetes.Controller;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var httpHandler = new SocketsHttpHandler
        {
            ConnectCallback = async (ctx, ct) =>
            {
                var socketPath = "/var/run/docker.sock";
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                var endpoint = new UnixDomainSocketEndPoint(socketPath);
                
                await socket.ConnectAsync(endpoint, ct);
                
                return new NetworkStream(socket, ownsSocket: true);
            }
        };

        var httpClient = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1_000, stoppingToken);

            var response = await httpClient.GetAsync("/v1.43/containers/json", stoppingToken);

            _logger.LogInformation("Response: {0}", await response.Content.ReadAsStringAsync());
        }
    }
}
