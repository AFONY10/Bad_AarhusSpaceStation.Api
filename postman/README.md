# Postman Testing

Import these two files into Postman:

- `AarhusSpaceProgram.postman_collection.json`
- `AarhusSpaceProgram.postman_environment.json`

Select the `Aarhus Space Program - Local Docker` environment, then run the full collection in order.

The collection:

- logs in as the seeded Manager user
- stores the JWT in `authToken`
- tests `GET /api/missions`
- tests `POST /api/missions`
- tests `GET /api/missions/{id}`
- tests `PUT /api/missions/{id}`
- tests `DELETE /api/missions/{id}`
- verifies protected mission creation rejects missing authentication

Run the app first:

```powershell
docker compose up --build
```

The collection uses `baseUrl`, credentials, token, and temporary mission ids as Postman environment variables.

Database connection strings are not used by Postman directly. Keep those in Docker Compose, `.env`, or user-secrets; Postman should only know the API URL and request credentials.
