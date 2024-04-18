using Npgsql;

using Simplenetes.Server;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var connectionStringBuilder = new NpgsqlConnectionStringBuilder
{
    Host = app.Configuration["Database:Host"],
    Username = "postgres",
    Password = "postgres",
    Database = "simplenetes"
};

await using var dataSource = NpgsqlDataSource.Create(connectionStringBuilder.ConnectionString);

app.MapGet("/containers", async (ILogger<Program> logger) =>
{
    try
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

        return Results.Ok(containers);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while retrieving containers");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.MapPost("/containers", async (ILogger<Program> logger, Container container) =>
{
    try
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
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while creating container {Name} with image {Image}", container.Name, container.Image);
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/containers/{name}", async (ILogger<Program> logger, string name) =>
{
    try
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
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while retrieving container {Name}", name);
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.MapDelete("/containers/{name}", async (ILogger<Program> logger, string name) =>
{
    try
    {
        await using var command = dataSource.CreateCommand("DELETE FROM container WHERE name = @Name");

        command.Parameters.AddWithValue("Name", name);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while deleting container {Name}", name);
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.Run();
