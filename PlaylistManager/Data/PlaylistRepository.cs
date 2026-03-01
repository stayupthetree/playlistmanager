using System.Data.SQLite;
using Dapper;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NLog;
using PlaylistManager.Models;

namespace PlaylistManager.Data
{
    public class PlaylistRepository : IPlaylistRepository
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly object _initLock = new();
        private bool _initialized;

        public PlaylistRepository(IAppFolderInfo appFolderInfo)
        {
            _appFolderInfo = appFolderInfo;
        }

        private string DatabasePath
        {
            get
            {
                var pluginPath = _appFolderInfo.GetPluginPath();
                var dir = Path.Combine(pluginPath, PluginInfo.Author, PluginInfo.Name);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                return Path.Combine(dir, "playlistmanager.db");
            }
        }

        private string ConnectionString => new SQLiteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Version = 3
        }.ConnectionString;

        public void EnsureDatabase()
        {
            lock (_initLock)
            {
                if (_initialized)
                {
                    return;
                }

                using var conn = new SQLiteConnection(ConnectionString);
                conn.Open();

                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS Playlists (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    )");
                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS PlaylistTracks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PlaylistId INTEGER NOT NULL,
                        TrackId INTEGER NOT NULL,
                        Position INTEGER NOT NULL,
                        FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id) ON DELETE CASCADE
                    )");
                conn.Execute("CREATE INDEX IF NOT EXISTS IX_PlaylistTracks_PlaylistId ON PlaylistTracks(PlaylistId)");

                _initialized = true;
                _logger.Info("PlaylistManager database initialized at {Path}", DatabasePath);
            }
        }

        public IReadOnlyList<Playlist> GetAll()
        {
            EnsureDatabase();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            var list = conn.Query<Playlist>("SELECT Id, Name, Description, CreatedAt, UpdatedAt FROM Playlists ORDER BY UpdatedAt DESC").AsList();
            return list;
        }

        public Playlist? GetById(int id)
        {
            EnsureDatabase();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            return conn.QueryFirstOrDefault<Playlist>("SELECT Id, Name, Description, CreatedAt, UpdatedAt FROM Playlists WHERE Id = @Id", new { Id = id });
        }

        public Playlist Create(string name, string? description = null)
        {
            EnsureDatabase();
            var now = DateTime.UtcNow;
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            conn.Execute(
                "INSERT INTO Playlists (Name, Description, CreatedAt, UpdatedAt) VALUES (@Name, @Description, @CreatedAt, @UpdatedAt)",
                new { Name = name, Description = description ?? (object)DBNull.Value, CreatedAt = now, UpdatedAt = now });
            var id = (int)conn.LastInsertRowId;
            return GetById(id)!;
        }

        public void Update(Playlist playlist)
        {
            EnsureDatabase();
            playlist.UpdatedAt = DateTime.UtcNow;
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            conn.Execute(
                "UPDATE Playlists SET Name = @Name, Description = @Description, UpdatedAt = @UpdatedAt WHERE Id = @Id",
                playlist);
        }

        public void Delete(int id)
        {
            EnsureDatabase();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            conn.Execute("DELETE FROM PlaylistTracks WHERE PlaylistId = @Id", new { Id = id });
            conn.Execute("DELETE FROM Playlists WHERE Id = @Id", new { Id = id });
        }

        public IReadOnlyList<PlaylistTrack> GetTracks(int playlistId)
        {
            EnsureDatabase();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            var list = conn.Query<PlaylistTrack>(
                "SELECT Id, PlaylistId, TrackId, Position FROM PlaylistTracks WHERE PlaylistId = @PlaylistId ORDER BY Position",
                new { PlaylistId = playlistId }).AsList();
            return list;
        }

        public void SetTracks(int playlistId, IReadOnlyList<int> trackIds)
        {
            EnsureDatabase();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            conn.Execute("DELETE FROM PlaylistTracks WHERE PlaylistId = @PlaylistId", new { PlaylistId = playlistId });
            for (var i = 0; i < trackIds.Count; i++)
            {
                conn.Execute(
                    "INSERT INTO PlaylistTracks (PlaylistId, TrackId, Position) VALUES (@PlaylistId, @TrackId, @Position)",
                    new { PlaylistId = playlistId, TrackId = trackIds[i], Position = i });
            }

            conn.Execute(
                "UPDATE Playlists SET UpdatedAt = @UpdatedAt WHERE Id = @Id",
                new { Id = playlistId, UpdatedAt = DateTime.UtcNow });
        }

        public void AddTrack(int playlistId, int trackId)
        {
            EnsureDatabase();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            var maxPos = conn.ExecuteScalar<int?>("SELECT MAX(Position) FROM PlaylistTracks WHERE PlaylistId = @PlaylistId", new { PlaylistId = playlistId }) ?? -1;
            conn.Execute(
                "INSERT INTO PlaylistTracks (PlaylistId, TrackId, Position) VALUES (@PlaylistId, @TrackId, @Position)",
                new { PlaylistId = playlistId, TrackId = trackId, Position = maxPos + 1 });
            conn.Execute("UPDATE Playlists SET UpdatedAt = @UpdatedAt WHERE Id = @Id", new { Id = playlistId, UpdatedAt = DateTime.UtcNow });
        }

        public void RemoveTrack(int playlistId, int trackId)
        {
            EnsureDatabase();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            conn.Execute("DELETE FROM PlaylistTracks WHERE PlaylistId = @PlaylistId AND TrackId = @TrackId", new { PlaylistId = playlistId, TrackId = trackId });
            conn.Execute("UPDATE Playlists SET UpdatedAt = @UpdatedAt WHERE Id = @Id", new { Id = playlistId, UpdatedAt = DateTime.UtcNow });
        }

        public void ReorderTracks(int playlistId, IReadOnlyList<int> orderedTrackIds)
        {
            SetTracks(playlistId, orderedTrackIds);
        }
    }
}
