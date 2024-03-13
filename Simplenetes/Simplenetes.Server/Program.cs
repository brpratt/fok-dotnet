using Npgsql;

var connectionString = "Host=database;Username=postgres;Password=postgres;Database=simplenetes";
await using var dataSource = NpgsqlDataSource.Create(connectionString);

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/containers", async () =>
{
    await using var command = dataSource.CreateCommand("SELECT name, image FROM container");
    await using var reader = await command.ExecuteReaderAsync();

    var containers = new List<Container>();

    while (await reader.ReadAsync())
    {
        var container = new Container(
            Name: reader.GetString(0),
            Image: reader.GetString(1)
        );

        containers.Add(container);
    }

    return containers;
});

app.MapPost("/containers", async (Container container) =>
{
    await using var command = dataSource.CreateCommand("INSERT INTO container (name, image) VALUES (@Name, @Image)");

    command.Parameters.AddWithValue("Name", container.Name);
    command.Parameters.AddWithValue("Image", container.Image);

    await command.ExecuteNonQueryAsync();

    return Results.Created($"/containers/{container.Name}", container);
});

app.Run();

record Container(string Name, string Image);
