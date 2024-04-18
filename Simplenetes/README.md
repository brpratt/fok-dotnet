# Simplenetes

Simplenetes is a basic container orchestrator modeled after Kubernetes. It was created to demonstrate the interactions between:

- The Kubernetes API server
- The resource database
- The controller responsible for managing containers (i.e. the kubelet)
- An external client that manipulates the resources

## Running

### Running with Docker Compose

The simplest way to run this demo is with Docker Compose. From within the `Simplenetes` directory, run:

```
$ docker compose up
```

### Running the components independently

The `Simplenetes.Server` and `Simplenetes.Controller` services can run outside of Docker. This is useful if you want to make code changes and/or use a debugger

You typically want to start the database first:

```
$ docker compose up database
```

Then, you can run the projects using `dotnet run` or from within your IDE:

```
$ dotnet run --project Simplenetes/Simplenetes.Server.csproj
$ dotnet run --project Simplenetes/Simplenetes.Controller.csproj
```

## Simplenetes.Server

The API server listens on port 5000 and exposes the following endpoints:

```
   GET /containers        - get all containers
   GET /containers/{name} - get a container by name
  POST /containers        - create a container
DELETE /containers/{name} - delete a container by name
```

The container resource manipulated by these endpoints is a simple object with two fields:

- `name`: the name of the container
- `image`: the image to use for the container

## Simplenetes.Controller

The controller will use the local docker socket to create and delete containers as appropriate. Containers managed by the controller have the `simplenetes` label, so any containers without this label are ignored by the controller.

## Examples

Create a new container:

```
$ curl -X POST -H 'Content-Type: application/json' http://localhost:5000/containers -e '{ "name": "redis", "image": "redis:latest" }'
```

Get all of the containers:

```
$ curl http://localhost:5000/containers
```

Delete a container called `redis`:

```
$ curl -X DELETE http://localhost:5000/containers/redis
```
