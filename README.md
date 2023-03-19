<div align="center">

<h1 align="center">
    <picture>
        <source media="(prefers-color-scheme: dark)" srcset="./assets/logo-rectangle_white.png">
        <img alt="Flownodes logo" src="./assets/logo-rectangle_black.png" width="500">
    </picture>
</h1>

**A distributed automation platform.**
<br />
<br />

<div align="left">

Flownodes is an experimental automation distributed platform designed to integrate devices and different kinds of data sources. The main focus of the application is to provide easy extendability with a user friendly SDK.

## Architecture

```mermaid
graph TD
    Devices(Devices) --- Flownodes
    DataSources(Data sources) --- Flownodes
    Flownodes --- ClusteringDb("Clustering database (Redis)")
    Flownodes --- StorageDb("Storage database (MongoDB)")
    Flownodes --- ApiGateway(Api gateway)
```

## Additional notes

This project is still in its early stages and not ready for production. You may encounter bugs and other kinds of issues.
