FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Flownodes.Cluster.Node/Flownodes.Cluster.Node.csproj", "Flownodes.Cluster.Node/"]
COPY ["Flownodes.Cluster.Node/appsettings.json", "Flownodes.Cluster.Node/"]
COPY ["Flownodes.Cluster.Node/appsettings.Development.json", "Flownodes.Cluster.Node/"]
RUN dotnet restore "Flownodes.Cluster.Node/Flownodes.Cluster.Node.csproj"
COPY . .
WORKDIR "/src/Flownodes.Cluster.Node"
RUN dotnet build "Flownodes.Cluster.Node.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Flownodes.Cluster.Node.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Flownodes.Cluster.Node.dll"]
