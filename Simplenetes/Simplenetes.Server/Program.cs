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
    if (container.Name == null || container.Image == null)
    {
        return Results.BadRequest("Name and Image are required");
    }

    await using var command = dataSource.CreateCommand("INSERT INTO container (name, image) VALUES (@Name, @Image)");

    command.Parameters.AddWithValue("Name", container.Name);
    command.Parameters.AddWithValue("Image", container.Image);

    try
    {
        await command.ExecuteNonQueryAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")
    {
        return Results.Conflict("Container already exists");
    }

    return Results.Created($"/containers/{container.Name}", container);
});

app.MapGet("/containers/{name}", async (string name) =>
{
    await using var command = dataSource.CreateCommand("SELECT name, image FROM container WHERE name = @Name");

    command.Parameters.AddWithValue("Name", name);

    await using var reader = await command.ExecuteReaderAsync();

    if (await reader.ReadAsync())
    {
        var container = new Container(
            Name: reader.GetString(0),
            Image: reader.GetString(1)
        );

        return Results.Ok(container);
    }

    return Results.NotFound();
});

app.MapDelete("/containers/{name}", async (string name) =>
{
    await using var command = dataSource.CreateCommand("DELETE FROM container WHERE name = @Name");

    command.Parameters.AddWithValue("Name", name);

    var rowsAffected = await command.ExecuteNonQueryAsync();

    if (rowsAffected == 0)
    {
        return Results.NotFound();
    }

    return Results.NoContent();
});

app.Run();

record Container(string Name, string Image);
