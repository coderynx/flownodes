FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Flownodes.Instance/Flownodes.Instance.csproj", "Flownodes.Instance/"]
RUN dotnet restore "Flownodes.Instance/Flownodes.Instance.csproj"
COPY . .
WORKDIR "/src/Flownodes.Instance"
RUN dotnet build "Flownodes.Instance.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Flownodes.Instance.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Flownodes.Instance.dll"]
