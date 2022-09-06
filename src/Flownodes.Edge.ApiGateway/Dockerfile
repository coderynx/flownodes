FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Flownodes.Edge.ApiGateway/Flownodes.Edge.ApiGateway.csproj", "Flownodes.Edge.ApiGateway/"]
RUN dotnet restore "Flownodes.Edge.ApiGateway/Flownodes.Edge.ApiGateway.csproj"
COPY . .
WORKDIR "/src/Flownodes.Edge.ApiGateway"
RUN dotnet build "Flownodes.Edge.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Flownodes.Edge.ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Flownodes.Edge.ApiGateway.dll"]
