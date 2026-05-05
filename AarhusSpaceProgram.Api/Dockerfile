# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AarhusSpaceProgram.Api/AarhusSpaceProgram.Api.csproj", "AarhusSpaceProgram.Api/"]
RUN dotnet restore "AarhusSpaceProgram.Api/AarhusSpaceProgram.Api.csproj"

COPY . .
WORKDIR /src/AarhusSpaceProgram.Api
RUN dotnet publish "AarhusSpaceProgram.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AarhusSpaceProgram.Api.dll"]
