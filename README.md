# PlaylistManager

A standalone Lidarr plugin for managing local playlists and importing from Spotify.

## Features (planned)

- **Local playlist builder**: Create and manage playlists from your Lidarr library; export to M3U/XSPF.
- **Spotify import**: Import a Spotify playlist and match tracks to your library; create a local playlist from matches.

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

## License

See repository.
