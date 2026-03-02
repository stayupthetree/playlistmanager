# PlaylistManager

A standalone Lidarr plugin for managing local playlists and importing from Spotify.

## Features

- **Local playlist builder**: Create and manage playlists from your Lidarr library; persist in a plugin-owned SQLite DB; export to M3U/XSPF.
- **Import from track list**: POST a list of `{ artist, title }` (e.g. from Spotify); match to Lidarr library (exact + fuzzy); create a playlist and report unmatched tracks.
- **Web GUI**: Manage playlists in your browser (create, open, delete, import from list, export M3U/XSPF). Open `/api/v1/playlist/ui` when the API is enabled.
- **REST API**: CRUD for playlists and tracks; export endpoints; import endpoint. *Requires* Lidarr to register the plugin assembly (see below).

## Requirements

- Lidarr (with plugin support)
- .NET 8.0

## Build (local)

1. Clone and init the Lidarr submodule:
   ```bash
   git clone <your-repo-url> PlaylistManager
   cd PlaylistManager
   git submodule update --init --recursive
   ```
2. Build:
   ```bash
   dotnet build PlaylistManager.sln -c Release
   ```
3. Output is in `_plugins/net8.0/PlaylistManager/`. Copy that folder into Lidarr's plugin directory.

## Install in Lidarr

1. In Lidarr go to **System** → **Plugins**.
2. Add a new plugin and enter the GitHub URL, e.g.:
   - `https://github.com/TypNull/PlaylistManager`
   - or `PlaylistManager@TypNull` (name@owner).
3. Lidarr will download the latest release zip and install it. The zip contains `Lidarr.Plugin.PlaylistManager.dll` (and related files) from the tagged release.

## Release (CI)

Push a version tag (e.g. `v1.0.0`) to trigger the GitHub Actions workflow:

1. **Build** – restores submodules, builds the solution, produces plugin files under `_plugins/net8.0/PlaylistManager/`.
2. **Package** – zips files matching `*.PlaylistManager.*` / `*.Plugin.PlaylistManager.*` (e.g. `Lidarr.Plugin.PlaylistManager.dll`, `.deps.json`, `.pdb`) plus release notes.
3. **Publish** – creates a GitHub Release for the tag and attaches the zip (e.g. `PlaylistManager-v1.0.0.net8.0.zip`).

Same pattern as [Tubifarry](https://github.com/TypNull/Tubifarry): tag `v*` → build → package → release.

## Tests

```bash
dotnet test PlaylistManager.sln -c Release
```

## Using the GUI

Once the plugin is installed and the API is enabled (see below), open the **PlaylistManager** web UI at:

- **URL:** `http://<your-lidarr-host>:<port>/api/v1/playlist/ui` (e.g. `http://localhost:8686/api/v1/playlist/ui`)

From the GUI you can create/delete playlists, view and remove tracks, **Import from list** (paste "Artist - Title" lines or JSON), and **Export** as M3U or XSPF. Use the same host/port and auth as Lidarr.

## REST API (plugin assembly registration)

Lidarr does not load plugin assemblies as MVC application parts, so by default the PlaylistManager API routes are **not** registered. To enable them you must patch Lidarr’s startup so the plugin assembly is added as an application part.

In `NzbDrone.Host/Startup.cs`, in the `AddControllers()` chain (around the existing `.AddApplicationPart(...)` calls), add:

```csharp
.AddApplicationPart(typeof(PlaylistManager.PlaylistManager).Assembly)
```

Then rebuild Lidarr. After that, the following base path will be available (when the plugin is installed):

- `GET /api/v1/playlist/ui` – **web UI** (open in browser)  
- `GET/POST /api/v1/playlist` – list / create playlists  
- `GET/PUT/DELETE /api/v1/playlist/{id}` – get / update / delete  
- `GET /api/v1/playlist/{id}/tracks/details` – list tracks with title (for UI)  
- `GET/PUT /api/v1/playlist/{id}/tracks` – list / set tracks  
- `POST /api/v1/playlist/{id}/tracks` – add track (body: `{ "trackId": 123 }`)  
- `DELETE /api/v1/playlist/{id}/tracks/{trackId}` – remove track  
- `GET /api/v1/playlist/{id}/export/m3u` – export as M3U  
- `GET /api/v1/playlist/{id}/export/xspf` – export as XSPF  
- `POST /api/v1/playlist/import` – import from track list (body: `{ "playlistName": "...", "tracks": [ { "artist": "...", "title": "..." } ] }`)

## License

See repository.
