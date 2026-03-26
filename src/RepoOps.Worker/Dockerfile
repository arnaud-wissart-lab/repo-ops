FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/RepoOps.Worker/RepoOps.Worker.csproj", "src/RepoOps.Worker/"]
RUN dotnet restore "src/RepoOps.Worker/RepoOps.Worker.csproj"

COPY . .
WORKDIR /src/src/RepoOps.Worker
RUN dotnet publish "RepoOps.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RepoOps.Worker.dll"]
