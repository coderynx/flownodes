<h1 align="center">Flownodes</h1>

<p align="center">The distributed automation platform</p>

## Introduction

Flownodes is a distributed platform designed for integrating and automating different kinds of environment. It's highly versatile and designed for either professional or home usage.

## Goals

The main goal of the project is to provide a low effort way to automate your existing environment without the strain of writing custom software.
Flownodes is also highly extendable by providing developers an SDK to develop a variety of components, such as:

- Device behaviors;
- Data object behaviors;
- Alerter drivers;
- And more to come...

## Architecture

```mermaid
graph TD
    Devices(Devices) --- Flownodes
    WebServices(Web services) --- Flownodes
    Flownodes --- OperationalDatabase("Operational database (Redis)")
    Flownodes --- StorageDatabase("Storage Database (Postgres)")
    Flownodes --- ApiGateway(Api Gateway)
    ApiGateway --- WebInterface(Web Interface)
```

## Technologies

The project is built mainly using the .NET platform.

## Additional notes

This project is still in its early stages. You may encounter bugs and other kinds of issues.
