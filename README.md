# CampusPulse API

The backend for **CampusPulse**, a campus community app built as a two-sprint Agile group project. This is the ASP.NET Core Web API — the companion .NET MAUI client lives in a separate repo.

> Related repo: `CampusPulse` (MAUI client) — add your GitHub link here.

## Tech Stack

- **ASP.NET Core Web API** (.NET 8)
- **Entity Framework Core** + **SQLite**
- **JWT Bearer authentication** with role-based authorization
- **BCrypt.Net** for password hashing
- **Swagger / OpenAPI** for interactive endpoint docs (`/swagger` in Development)

## Features

- **Auth** — register/login, hashed passwords, JWT issued with a role claim. New accounts are always `Student`; role escalation only ever happens server-side.
- **Posts** — create/edit/delete, category filtering, a "following" feed (posts from categories you follow).
- **Comments** — create/edit/delete, with Admin moderation (hide + logged reason).
- **Reactions** — Like / Helpful / Interested, one per user per post (re-reacting with the same type toggles it off; a different type replaces it).
- **Events** — join/leave with capacity, cancellation, and past-date checks enforced server-side; Admin create/cancel.
- **Reports** — Students file reports against posts/comments; Admins review through Pending → Reviewed/ActionTaken/Dismissed, with an undo back to Pending.
- **Follows** — Students follow/unfollow categories; duplicate follows are prevented at the database level (unique constraint) and handled idempotently by the API.
- **Admin** — dashboard stats, user search/deactivate/reactivate.
- **Categories** — public browse, Admin-only create/delete.

Every write endpoint derives the acting user from the JWT (`ClaimsExtensions.GetUserId()`/`GetRole()`), never from a client-supplied ID — this is what makes "a Student can only manage their own content" true by construction rather than by a check that could be forgotten on a new endpoint.

## Getting Started

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download)

```bash
git clone <this-repo-url>
cd CampusPulse.Api
dotnet restore
dotnet ef database update   # creates campuspulse.db and applies migrations
dotnet run
```

On first run, the seeder creates:
- Default categories (General, Events, Announcements)
- A seeded Admin account: **`admin@campuspulse.com`** / **`Admin123!`**
- A few sample posts/events

The seeder is idempotent and self-healing — safe to restart, and it corrects the Admin account's role/active status if an older schema version left it stale.

### Running for multiple devices (not just localhost)

By default `launchSettings.json` binds to `0.0.0.0`, so the API listens on your machine's actual network interface, not just loopback. To let other devices (phones, other laptops) connect:

1. Find your machine's LAN IP: `ipconfig` (Windows) → look for **IPv4 Address** under your active adapter.
2. Make sure Windows Firewall allows inbound connections on port `5162` (it may prompt the first time).
3. Every device must be on the **same network** — school/public Wi-Fi sometimes blocks device-to-device traffic (client isolation); a personal hotspot is often more reliable for a demo.
4. Point the MAUI app's `ServerIp` (in `ApiClient.cs`) at that IP.

Only one instance of the API should run at a time for a shared demo — everyone's app should point at the same running instance and its one database file, not each run their own copy.

## Project Structure

```
Controllers/   - one controller per resource (Posts, Comments, Reactions, Events, Reports, Follows, Admin, Categories, Auth)
Models/        - EF Core entities
DTOs/          - request/response shapes (deliberately excludes client-supplied UserId/PasswordHash)
Data/          - AppDbContext (relationships, unique constraints) and DbSeeder
Helpers/       - ClaimsExtensions (JWT → current user id/role)
```

## Security Notes

- Passwords are hashed with BCrypt — never stored or returned in plain text.
- The API never returns a password hash to the client (`UserDto`, not the raw `User` entity).
- `[Authorize]` / `[Authorize(Roles = "Admin")]` on every non-public endpoint; ownership is re-checked server-side even where the client UI already hides the option.
- The JWT signing key and CORS policy in `appsettings.json`/`Program.cs` are dev-appropriate (a fallback key, `AllowAll` CORS) — replace both before any real deployment.

## Team

Kyran (team lead), Nathanael, Aki.
