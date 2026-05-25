# Softreserve Weekly Tracker

Web app for **Nüsslisalat** (TBC Anniversary) to track soft reserve +1 points across SSC and Tempest Keep raid weeks.

**Stack:** ASP.NET Core 10 MVC · Entity Framework Core · SQLite

## Features

- 📥 Bulk import (Softres CSV + Gargul JSON, auto-detect and pair by date)
- 📅 Raid weeks (Wed 05:00 → next Wed 03:00, server local time)
- 🏰 Raid type from CSV boss names or Gargul loot item IDs
- 🔀 Split Gargul nights by `softresID` when SSC and TK run the same evening
- ➕ +1 calculation per player and item, **once per raid ID** (`RaidWeek` + raid type), accumulated across raid IDs
- 📊 Session softres/loot, week, player, and item overviews (DataTables; item view searchable by item name/ID and player name)
- 🎲 Player page with roll history (MS/OS, roll amount); softres **notes** from CSV where present
- 📦 Item detail (drops, rolls, softres/+1 history); **Entzaubert** and **Drops ohne SR** overview pages
- 📋 Session **loot log** (full Gargul awards per evening, separate from softres matrix)
- 📁 Archive of uploaded export files
- 🔗 Roster links protected by GUID (no login)
- 🌍 German/English UI, Wowhead TBC tooltips
- 🎨 Dark UI inspired by [softres.it](https://softres.it/)

## 🚀 Quick start

```bash
cd src/SoftreserveTracker.Web
dotnet run
```

Open the URL from the console (`https://localhost:5xxx`). Migrations run on startup.

Optional dev settings:

```bash
cp src/SoftreserveTracker.Web/appsettings.Development.json.example \
   src/SoftreserveTracker.Web/appsettings.Development.json
```

### Typical workflow

1. Create a roster on the home page
2. Share `/r/{guid}` with the raid
3. Upload Softres CSV and Gargul `.txt` files from raid night(s)

### EF CLI

From the repo root:

```bash
dotnet tool restore
dotnet ef migrations list --project src/SoftreserveTracker.Web/SoftreserveTracker.Web.csproj
```

## 📚 Documentation

| Doc | Topic |
|-----|--------|
| [docs/WORKFLOW.md](docs/WORKFLOW.md) | Import flow, routes, UI |
| [docs/DATA_MODEL.md](docs/DATA_MODEL.md) | Database schema |
| [docs/PLUS_ONE_LOGIC.md](docs/PLUS_ONE_LOGIC.md) | +1 rules |

## 📂 Project layout

```
src/SoftreserveTracker.Web/
  Controllers/     MVC (/r/{token}/…)
  Data/            DbContext, migrations
  Infrastructure/  RosterAccessFilter, debug filter
  Models/          Entities, view models
  Resources/       de/en .resx
  Services/        Import, parsing, +1, storage
  Views/           Razor + partials
  wwwroot/         CSS, JS, logo
docs/
```

## ⚙️ Configuration

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App_Data/softreserve.db"
  }
}
```

Uploads are stored under `App_Data/archives/{sessionId}/` (not in git).

## 🐛 Debug

`/debug` is available when `ASPNETCORE_ENVIRONMENT=Development` **or** `Debug:Enabled=true` in `appsettings.json`. The navbar link appears only in Development; the URL works whenever debug is enabled (useful for officer testing on Production). Clear imports or delete test rosters there.

## 📥 Import notes

- Softres CSV vs Gargul JSON is detected automatically; bulk pairing matches CSV date to any Gargul export containing loot on that date
- Re-import of the same date/raid/softresID is skipped (use Debug to clear imports first)
- Continued raid evenings without new CSV: softres carry-forward from the same `softresID` (see [docs/WORKFLOW.md](docs/WORKFLOW.md))
- Bulk upload runs in one database transaction per batch

## 🔒 Not in this repository

These stay on your machine only (see `.gitignore`):

- `deploy/` – server scripts, IPs, deployment notes
- `publish/` – build output
- `DATA_input/`, `DATA_verify/` – real guild export files
- `App_Data/`, `*.db`, `appsettings.Development.json`

## 📜 License

[Nüsslisalat Softreserve Tracker License v1.0](LICENSE)

| | |
|--|--|
| Use, fork, modify | Free |
| Other guild, no ads on the site | Free |
| Other guild **with** ads (banners, sponsors) | Paid license to Nüsslisalat |
| Nüsslisalat (with or without ads) | Free |

"Commercial" means another WoW guild runs it and shows paid or third-party ads to visitors. Guild logo and Discord link do not count as ads.

Commercial license: contact Nüsslisalat officers.
