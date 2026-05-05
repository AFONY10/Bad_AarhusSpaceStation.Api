# Docker

The Docker setup runs the current API plus its two infrastructure dependencies:

- `api`: the ASP.NET Core Web API
- `sqlserver`: SQL Server for EF Core data
- `mongodb`: MongoDB for request logging today, and mission logs in Assignment 4

Create a local `.env` from `.env.example` and set a strong `MSSQL_SA_PASSWORD`, then start the stack:

```powershell
docker compose up --build
```

The API is available at `http://localhost:8080` by default.

The container startup seeds three roles and, when the matching `.env` values are present, one development user per role:

- `astronaut@asp.local`
- `scientist@asp.local`
- `manager@asp.local`

Useful endpoints:

- `GET /health`
- `POST /api/auth/login`
- `GET /openapi/v1.json`
- Scalar API reference at `/scalar/v1`

To reset the local container databases:

```powershell
docker compose down -v
```

## Configuration

Secrets and machine-specific connection strings should stay out of committed `appsettings*.json` files.

For Docker, `docker-compose.yml` injects configuration through environment variables:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__AccessTokenMinutes`
- `Jwt__Key`
- `SeedUsers__Astronaut__UserName`
- `SeedUsers__Astronaut__Password`
- `SeedUsers__Astronaut__StaffFullName`
- `SeedUsers__Scientist__UserName`
- `SeedUsers__Scientist__Password`
- `SeedUsers__Scientist__StaffFullName`
- `SeedUsers__Manager__UserName`
- `SeedUsers__Manager__Password`
- `SeedUsers__Manager__StaffFullName`
- `Serilog__MongoDbUrl`
- `Serilog__MongoDbCollection`
- `MongoDb__ConnectionString`
- `MongoDb__DatabaseName`
- `MongoDb__CollectionName`

For non-Docker local development, prefer user secrets or environment variables. Example:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=AarhusSpaceProgramDb;User Id=sa;Password=your-local-password;TrustServerCertificate=True;Encrypt=False" --project AarhusSpaceProgram.Api
dotnet user-secrets set "Jwt:Key" "your-at-least-32-byte-local-dev-secret" --project AarhusSpaceProgram.Api
dotnet user-secrets set "Serilog:MongoDbUrl" "mongodb://localhost:27017/AarhusSpaceProgramLogs" --project AarhusSpaceProgram.Api
```

The Assignment 4 worker can be added as a new Compose service once the worker project exists.
