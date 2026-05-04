# 🚀 Aarhus Space Program – Assignment 4 Development Flow

## Overview

This document describes how to extend the existing Web API to fulfill **Assignment 4 requirements**:

* Authentication & Authorization (Microsoft Identity + JWT)
* Experiments domain feature
* Background service (Mission Log Generator)
* MongoDB integration
* API-based communication
* Postman testing
* Docker containerization

⚠️ Important:
We are **extending the existing API**, not rebuilding it.

---

# 🧱 High-Level Architecture

```
[ Web API ]
   ├── SQL Server (Missions, Users, Experiments)
   ├── Identity + JWT
   ├── MongoDB (Mission Logs)
   └── REST Endpoints

[ Background Service ]
   ├── Calls Web API via HTTP
   └── Generates Mission Logs

[ MongoDB ]
   └── Stores Mission Logs

[ Docker Compose ]
   └── Runs everything together
```

---

# 🪜 Development Order (IMPORTANT)

Follow this order:

1. Authentication & Authorization
2. Experiments Feature
3. MongoDB Integration
4. Background Service
5. Mission Log Endpoint
6. Postman Testing
7. Dockerization

---

# 🔐 1. Authentication & Authorization

## Goal

Add login + JWT + role-based access control.

## Steps

### 1.1 Add Identity

* Install packages:

  * `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
  * `Microsoft.AspNetCore.Authentication.JwtBearer`

* Update DbContext:

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
```

---

### 1.2 Create ApplicationUser

```csharp
public class ApplicationUser : IdentityUser
{
}
```

---

### 1.3 Configure Identity in Program.cs

```csharp
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
```

---

### 1.4 Configure JWT Authentication

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { ... });
```

---

### 1.5 Create Login Endpoint

```text
POST /api/auth/login
```

Returns:

```json
{
  "token": "JWT_TOKEN"
}
```

---

### 1.6 Seed Roles

* Astronaut
* Scientist
* Manager

---

### 1.7 Apply Authorization Rules

| Role      | Permissions      |
| --------- | ---------------- |
| Astronaut | GET only         |
| Scientist | CRUD Experiments |
| Manager   | Full access      |

Example:

```csharp
[Authorize(Roles = "Manager")]
```

---

### 1.8 Public Endpoint

```text
GET /api/missions
```

Must be:

```csharp
[AllowAnonymous]
```

---

# 🧪 2. Experiments Feature

## Goal

Add experiments linked to missions and scientists.

---

### 2.1 Create Entity

```csharp
public class Experiment
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public int MissionId { get; set; }
    public Mission Mission { get; set; }

    public string ScientistId { get; set; }
    public ApplicationUser Scientist { get; set; }
}
```

---

### 2.2 Update DbContext

```csharp
public DbSet<Experiment> Experiments { get; set; }
```

---

### 2.3 Create Migration

```bash
dotnet ef migrations add AddExperiments
dotnet ef database update
```

---

### 2.4 Create Controller

```text
/api/experiments
```

Endpoints:

* GET all
* GET by id
* POST
* PUT
* DELETE

---

### 2.5 Apply Authorization

| Action          | Access              |
| --------------- | ------------------- |
| GET             | All roles           |
| POST/PUT/DELETE | Scientist + Manager |

---

# 🗄️ 3. MongoDB Integration

## Goal

Store mission logs in MongoDB.

---

### 3.1 Install MongoDB Driver

```bash
MongoDB.Driver
```

---

### 3.2 Create Model

```csharp
public class MissionLog
{
    public string Id { get; set; }
    public int MissionId { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

### 3.3 Create Repository

```csharp
public interface IMissionLogRepository
{
    Task CreateAsync(MissionLog log);
    Task<List<MissionLog>> GetByMissionIdAsync(int missionId);
}
```

---

### 3.4 Implement Mongo Service

Handles:

* Insert logs
* Query logs

---

### 3.5 Configure in appsettings.json

```json
"MongoDb": {
  "ConnectionString": "mongodb://mongodb:27017",
  "DatabaseName": "MissionLogsDb",
  "CollectionName": "MissionLogs"
}
```

---

# 🔄 4. Background Service

## Goal

Generate logs periodically using API calls.

---

### 4.1 Create Worker Project

```bash
dotnet new worker
```

---

### 4.2 Behavior

Every 10–30 seconds:

1. Call:

```text
GET /api/missions?status=Active
```

2. For each mission:

* Generate log
* Send to API

---

### 4.3 Send Logs via API

```text
POST /api/missions/{id}/logs
```

---

### 4.4 Important Rule

❌ DO NOT access SQL database directly
✅ MUST use HTTP to API

---

# 📡 5. Mission Log API Endpoint

## Goal

Expose logs from MongoDB

---

### Endpoint

```text
GET /api/missions/{id}/logs
```

---

### Implementation

```csharp
public async Task<IActionResult> GetLogs(int id)
{
    var logs = await _repository.GetByMissionIdAsync(id);
    return Ok(logs);
}
```

---

# 🧪 6. Postman Testing

## Goal

Verify Mission CRUD endpoints

---

### Required Requests

* GET all missions
* GET mission by ID
* POST mission
* PUT mission
* DELETE mission

---

### Include

* Status code checks
* Request bodies
* JWT authentication
* Environment variables

---

### Example Test

```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});
```

---

# 🐳 7. Docker Setup

## Goal

Run full system with Docker Compose

---

### Services

* Web API
* Background Service
* SQL Server
* MongoDB

---

### Example compose.yaml

```yaml
services:
  webapi:
    build: .
    ports:
      - "8080:8080"

  worker:
    build: ./Worker
    depends_on:
      - webapi

  sqlserver:
    image: mcr.microsoft.com/mssql/server

  mongodb:
    image: mongo
```

---

# 🧠 Final System Flow

```
User → Web API → SQL Server
                 ↓
          Background Service
                 ↓
             Web API
                 ↓
              MongoDB
                 ↓
User → GET mission logs
```

---

# ✅ Key Rules Recap

* Use JWT authentication
* Roles must be enforced
* Missions endpoint must be public
* Background service uses HTTP only
* Logs stored in MongoDB
* System runs via Docker Compose

---

# 📌 Summary

You are building:

* A secured API with Identity + JWT
* A new domain feature (Experiments)
* A distributed system (API + Worker)
* A hybrid database architecture (SQL + MongoDB)

---

👉 If you're new to the project:

Start here:

1. Run the API
2. Verify Missions work
3. Add Identity
4. Add Experiments
5. Add MongoDB
6. Add Worker
7. Dockerize everything

---

Good luck 🚀
