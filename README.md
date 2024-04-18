# Foundations of Kuberentes - .NET Edition

This repo contains the source material for my _Foundations of Kubernetes_ talk at IADNUG on April 25th, 2024.

https://www.meetup.com/iadnug/events/300182485/

Specifically, this repo contains the two demonstration projects:

- [Simplenetes](./Simplenetes/README.md)
- [DiceRollOperator](./DiceRollOperator/README.md)

## Prerequisites

In order to run these projects, you will need the following:

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Docker](https://www.docker.com/)
- [minikube](https://minikube.sigs.k8s.io/docs/)

Note: other cluster tools will likely work (e.g. [kind](https://kind.sigs.k8s.io/) or even a managed cloud cluster) but the directions in this repo assume you are using minikube.

### Codespaces

If you don't want to install any of the above tools locally, the default [GitHub Codespaces](https://github.com/features/codespaces) image contains all the necessary tools.

Note: the majority of the code was developed using GitHub Codespaces.

## Running

The README files for the demonstration projects contain specific instructions on how to run the example.
