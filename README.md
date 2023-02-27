<h1 align="center">Flownodes</h1>
<p align="center">🦾 Experimental distributed automation platform.</p>

Flownodes is an experimental automation distributed platform designed to integrate devices and different kinds of data sources. The main focus of the application is to provide easy extendability with a user friendly SDK.

## Architecture

```mermaid
graph TD
    Devices(Devices) --- Flownodes
    DataSources(Data sources) --- Flownodes
    Flownodes --- OperationalDatabase("Operational database (Redis)")
    Flownodes --- StorageDatabase("Storage Database")
    Flownodes --- ApiGateway(Api Gateway)
    ApiGateway --- WebApp(Web App)
```

## Additional notes

This project is still in its early stages and not ready for production. You may encounter bugs and other kinds of issues.