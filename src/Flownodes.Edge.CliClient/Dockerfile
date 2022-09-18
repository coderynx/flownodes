FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Flownodes.Edge.CliClient/Flownodes.Edge.CliClient.csproj", "Flownodes.Edge.CliClient/"]
RUN dotnet restore "Flownodes.Edge.CliClient/Flownodes.Edge.CliClient.csproj"
COPY . .
WORKDIR "/src/Flownodes.Edge.CliClient"
RUN dotnet build "Flownodes.Edge.CliClient.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Flownodes.Edge.CliClient.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Flownodes.Edge.CliClient.dll"]
