# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AarhusSpaceProgram.MissionLogWorker/AarhusSpaceProgram.MissionLogWorker.csproj", "AarhusSpaceProgram.MissionLogWorker/"]
RUN dotnet restore "AarhusSpaceProgram.MissionLogWorker/AarhusSpaceProgram.MissionLogWorker.csproj"

COPY . .
WORKDIR /src/AarhusSpaceProgram.MissionLogWorker
RUN dotnet publish "AarhusSpaceProgram.MissionLogWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AarhusSpaceProgram.MissionLogWorker.dll"]
