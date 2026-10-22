# Gjubrarko (Ѓубрарко)

A national platform for North Macedonia where citizens report trash and pollution in their municipality, attach photos,
and tag the authority responsible for cleaning it up. Reports can be sent anonymously (approved by a moderator before
they go public) or from an account (public right away).

- Backend: ASP.NET Core (.NET 10), EF Core, PostgreSQL + PostGIS
- Frontend: Angular, Leaflet + OpenStreetMap
- Photos: S3-compatible storage (SeaweedFS locally), re-encoded to WebP with metadata removed (Magick.NET)

## Layout

```
src/Gjubrarko.Api             Web API: controllers, JWT auth, rate limiting
src/Gjubrarko.Application     use cases (ReportService, ModerationService), DTOs, interfaces
src/Gjubrarko.Domain          entities and enums
src/Gjubrarko.Infrastructure  EF Core + PostGIS, migrations, seeding, S3 storage, image processing
tests/Gjubrarko.UnitTests     xUnit tests
web/                          Angular app
seed/                         municipalities, authority types and categories (loaded on start in Development)
```

## Run locally

Prerequisites: .NET 10 SDK, Node 22.22+ or 24+, Docker.

```bash
docker compose up -d                       # Postgres/PostGIS, SeaweedFS (S3 storage), Mailpit
cd src/Gjubrarko.Api && dotnet run         # http://localhost:5063, applies migrations and seeds data
cd web && npm install && npm start         # http://localhost:4200, proxies /api to the API
```

In Development the API migrates the database, loads `seed/` and creates the photo bucket on start.
Sent emails are visible in Mailpit at http://localhost:8025.

To make someone a moderator, register them in the app and then add the role in SQL:

```sql
INSERT INTO "AspNetUserRoles"
SELECT u."Id", r."Id" FROM "AspNetUsers" u, "AspNetRoles" r
WHERE u."Email" = 'you@example.com' AND r."Name" = 'Moderator';
```

## Tests

```bash
dotnet test
cd web && npx ng test --watch=false
```

## Configuration for production

Set these through environment variables or a secret store, never in `appsettings.json`:

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__Database` | Postgres connection string (PostGIS extension required) |
| `Jwt__SigningKey` | At least 32 random characters |
| `Storage__ServiceUrl`, `Storage__AccessKey`, `Storage__SecretKey`, `Storage__Bucket` | S3-compatible photo bucket |
| `Turnstile__SecretKey` | Cloudflare Turnstile secret; when empty, captcha checks are skipped |
| `Cors__Origins__0` | The public site's origin |

## Seed data notes

- `seed/municipalities.json` holds the 80 municipalities plus the City of Skopje (parent of its 10 municipalities).
  Albanian names (`nameSq`) are placeholders copied from the Latin names; replace them with the official ones.
  Regions, centroids and boundaries are not filled yet: import boundaries from OpenStreetMap (admin_level 7).
- Municipal authorities (communal inspection, public utility, and so on) are per municipality and are added by admins.
  National bodies are seeded once.

## Not built yet

Refresh tokens and password reset, email notifications, authority staff dashboard and status changes,
"I see this too" confirmations, comments, Turnstile widget in the frontend, Albanian and English UI translations.
