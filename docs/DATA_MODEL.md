# Data Model

Entity relationship overview for the SQLite database (EF Core). Schema defined in `Data/AppDbContext.cs`; initial migration `20260522203035_InitialCreate`.

```mermaid
erDiagram
    Roster ||--o{ RaidWeek : has
    Roster ||--o{ Player : has
    RaidWeek ||--o{ RaidSession : contains
    RaidSession ||--o{ SoftReserve : has
    RaidSession ||--o{ LootAward : has
    RaidSession ||--o{ SessionReservationResult : has
    RaidSession ||--o{ UploadedFile : archives
    Player ||--o{ SoftReserve : makes
    Player ||--o{ LootAward : wins
    Player ||--o{ PlusOneBalance : tracks
    Player ||--o{ SessionReservationResult : has

    Roster {
        guid Id PK
        string Name
        guid AccessToken UK
        datetime CreatedAt
    }

    RaidWeek {
        int Id PK
        guid RosterId FK
        int WeekNumber UK_per_roster
        datetime PeriodStart UK_per_roster
        datetime PeriodEnd
    }

    RaidSession {
        int Id PK
        int RaidWeekId FK
        enum RaidType "Ssc|Tk"
        date SessionDate
        string SoftresId
        datetime CreatedAt
    }

    Player {
        int Id PK
        guid RosterId FK
        string Name
        string NormalizedName UK_per_roster
    }

    SoftReserve {
        int Id PK
        int RaidSessionId FK
        int PlayerId FK
        int ItemId
        string BossSource
        string PlayerClass
        string Spec
        string Note
        datetime ReservedAt
    }

    LootAward {
        int Id PK
        int RaidSessionId FK
        int ItemId
        int WinnerPlayerId FK_nullable
        string AwardedToRaw
        bool SoftReserveWin
        bool IsDisenchanted
        datetime AwardedAt
        string SoftresId
    }

    SessionReservationResult {
        int Id PK
        int RaidSessionId FK
        int PlayerId FK
        int ItemId
        bool ItemDropped
        bool PlayerReceived
        int PlusOneDelta
        enum PlusOneReason
        int AwardedToPlayerId FK_nullable
    }

    PlusOneBalance {
        int Id PK
        int PlayerId FK
        int ItemId UK_with_player
        int CurrentCount
    }

    UploadedFile {
        int Id PK
        int RaidSessionId FK
        string OriginalFileName
        string StoredFileName
        enum UploadFileType
        datetime UploadedAt
    }
```

## Entity descriptions

### Roster

A raid team. Each roster has a unique **AccessToken** (`Guid`) used in URLs. Created via `HomeController` → `RosterService.CreateAsync()`. Multiple rosters are supported in the data model; the UI currently focuses on one roster at a time via its link.

### RaidWeek

Represents a **Raid-ID** / raid window (`PeriodStart` … `PeriodEnd`). Created automatically on first import whose reference timestamp falls in the window. `WeekNumber` increments per roster.

### RaidSession

One raid instance, typically one Gargul `softresID` group. A single upload may create **multiple sessions** when Gargul contains multiple groups.

- `RaidType`: detected from Gargul loot item IDs for that group
- `SoftresId`: Gargul group key (nullable)
- `SessionDate`: date from Softres CSV (same for all sessions from one upload)

### Player

Character name per roster. Created on first appearance in an import.

- `Name`: display name (from CSV as-is, or from Gargul with `-Realm` stripped)
- `NormalizedName`: `Trim().ToLowerInvariant()` for unique lookup per roster

### SoftReserve

One row per player/item from Softres CSV, attached only to the session whose raid type matches the CSV.

### LootAward

One row per Gargul loot event. Duplicate token drops produce **multiple rows** for the same `ItemId`. Disenchant awards have `IsDisenchanted = true` and `WinnerPlayerId = null`.

### SessionReservationResult

Computed on each import recalculation: for each soft reserve in that session, the +1 delta, flags, reason, and optional `AwardedToPlayerId`. Persisted by `RaidImportService.RecalculatePlusOneAsync()`.

### PlusOneBalance

Current cumulative +1 per `(PlayerId, ItemId)` after processing all sessions chronologically. **Only rows with `CurrentCount > 0` are stored.** Reset implicitly when count returns to 0 (row deleted on recalc).

### UploadedFile

Metadata for archived uploads. File bytes on disk:

```
App_Data/archives/{raidSessionId}/{timestamp}_{hash}_{originalFileName}
```

`UploadFileType`: `SoftresCsv` | `GargulJson`.

## Source file mapping

| Source | Written to | Also determines |
|--------|------------|-----------------|
| Softres CSV | `SoftReserve`, archived `UploadedFile` | CSV raid type, `SessionDate`, raid week reference time |
| Gargul JSON | `LootAward`, archived `UploadedFile` | Session split by `softresID`, per-group raid type |

## Indexes (unique constraints)

| Entity | Index |
|--------|-------|
| `Roster` | `AccessToken` |
| `RaidWeek` | `(RosterId, PeriodStart)`, `(RosterId, WeekNumber)` |
| `Player` | `(RosterId, NormalizedName)` |
| `PlusOneBalance` | `(PlayerId, ItemId)` |
| `SoftReserve` | `(RaidSessionId, PlayerId, ItemId)` (non-unique) |
| `SessionReservationResult` | `(RaidSessionId, PlayerId, ItemId)` (non-unique) |
| `RaidSession` | `(RaidWeekId, RaidType, SessionDate, SoftresId)` (non-unique) |

## Key services

| Service | Role |
|---------|------|
| `RaidImportService` | Orchestrates parse → persist → recalc |
| `PlusOneCalculator` | Pure +1 logic over session inputs |
| `SoftresCsvParser` / `GargulJsonParser` | File parsing |
| `RaidWindowCalculator` | Raid week boundaries |
| `RaidTypeDetector` | Boss name → SSC/TK (CSV) |
| `ItemRaidCatalog` | Item ID sets → SSC/TK (Gargul) |
| `FileArchiveService` | Disk storage for uploads |
