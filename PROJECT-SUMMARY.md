# PlaylistManager — Project Summary

Overview for contributors and anyone exploring the repo.

---

## What This Project Is

**PlaylistManager** is a **standalone Lidarr plugin** (no dependency on Tubifarry or other projects). It gives users:

1. **Local playlists** – Create and manage playlists from the Lidarr library. Data lives in a **plugin-owned SQLite database** (not Lidarr's main DB). Export to **M3U** and **XSPF**.
2. **Import from track lists** – Send a list of `{ artist, title }` (e.g. from Spotify or any source). The plugin **matches** them to the Lidarr library (exact + fuzzy) and creates a local playlist, reporting which tracks matched and which did not.
3. **REST API** – Full CRUD for playlists and tracks, plus export and import endpoints. **Important:** These routes only work if Lidarr is patched to register the plugin assembly as an MVC application part (see README).

The plugin is **C# .NET 8**, follows Lidarr plugin conventions, and is released via **GitHub Actions** when a version tag (e.g. `v1.0.3`) is pushed.

---

## Repo Layout (High Level)

- **`PlaylistManager/`** – Main plugin project (plugin code, API, services, data).
- **`PlaylistManager.Tests/`** – xUnit tests (e.g. repository, track matcher); uses Moq.
- **`Submodules/Lidarr/`** – Lidarr submodule (reference only; we use its APIs, we don't modify it for normal development).
- **`.github/workflows/`** – CI: build on tag push or manual run, package plugin, publish GitHub Release.
- **Default branch:** `master` (not `main`).

Key technical points:

- **Database:** Plugin uses its **own SQLite file** under `AppData/plugins/TypNull/PlaylistManager/playlistmanager.db`. Tables are created in code (`CREATE TABLE IF NOT EXISTS`); we do **not** use Lidarr's migrations or FluentMigrator.
- **API discovery:** Lidarr does **not** load plugin assemblies as application parts by default. So the PlaylistManager API is only available if Lidarr's `Startup.cs` is patched to add our assembly. Document this for users; don't assume the API works out of the box in stock Lidarr.
- **Version:** Set in `PlaylistManager/PlaylistManager.csproj`: `<Version>` and `<AssemblyVersion>`. Bump for each release.

---

## Quick Reference

| Topic | Detail |
|-------|--------|
| **Branch** | `master` |
| **Version** | `PlaylistManager/PlaylistManager.csproj` → `<Version>`, `<AssemblyVersion>` |
| **Release** | Push tag `v*` (e.g. `v1.0.4`) → GitHub Actions builds and publishes release |
| **Build** | Prefer CI. Local: `dotnet build PlaylistManager.sln -c Release` (may fail due to submodule). |
| **Tests** | `dotnet test PlaylistManager.sln -c Release` |
| **Plugin output** | `_plugins/net8.0/PlaylistManager/` |
| **Our DB** | SQLite, path from `IAppFolderInfo.GetPluginPath()` + `TypNull/PlaylistManager/playlistmanager.db` |
| **API base** | `/api/v1/playlist` (only if Lidarr is patched to add our assembly) |
