using System.Text;
using System.Xml.Linq;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using PlaylistManager.Data;
using PlaylistManager.Models;

namespace PlaylistManager.Services
{
    public class PlaylistService : IPlaylistService
    {
        private readonly IPlaylistRepository _repository;
        private readonly ITrackService _trackService;
        private readonly IMediaFileService _mediaFileService;

        public PlaylistService(
            IPlaylistRepository repository,
            ITrackService trackService,
            IMediaFileService mediaFileService)
        {
            _repository = repository;
            _trackService = trackService;
            _mediaFileService = mediaFileService;
        }

        public void EnsureDatabase() => _repository.EnsureDatabase();

        public IReadOnlyList<Playlist> GetAllPlaylists() => _repository.GetAll();
        public Playlist? GetPlaylist(int id) => _repository.GetById(id);
        public Playlist CreatePlaylist(string name, string? description = null) => _repository.Create(name, description);
        public void UpdatePlaylist(Playlist playlist) => _repository.Update(playlist);
        public void DeletePlaylist(int id) => _repository.Delete(id);

        public IReadOnlyList<PlaylistTrack> GetPlaylistTracks(int playlistId) => _repository.GetTracks(playlistId);

        public IReadOnlyList<PlaylistTrackDetail> GetPlaylistTracksWithDetails(int playlistId)
        {
            var pts = _repository.GetTracks(playlistId);
            if (pts.Count == 0)
            {
                return Array.Empty<PlaylistTrackDetail>();
            }

            var trackIds = pts.Select(x => x.TrackId).Distinct().ToList();
            var tracks = _trackService.GetTracks(trackIds);
            var trackMap = tracks.ToDictionary(t => t.Id, t => t);
            var result = new List<PlaylistTrackDetail>();

            foreach (var pt in pts.OrderBy(x => x.Position))
            {
                if (!trackMap.TryGetValue(pt.TrackId, out var track) || track == null)
                {
                    result.Add(new PlaylistTrackDetail { TrackId = pt.TrackId, Position = pt.Position, Title = "?", ArtistName = "—" });
                    continue;
                }

                var title = track.Title?.Trim() ?? "—";
                result.Add(new PlaylistTrackDetail { TrackId = pt.TrackId, Position = pt.Position, Title = title, ArtistName = "—" });
            }

            return result;
        }
        public void SetPlaylistTracks(int playlistId, IReadOnlyList<int> trackIds) => _repository.SetTracks(playlistId, trackIds);
        public void AddTrackToPlaylist(int playlistId, int trackId) => _repository.AddTrack(playlistId, trackId);
        public void RemoveTrackFromPlaylist(int playlistId, int trackId) => _repository.RemoveTrack(playlistId, trackId);
        public void ReorderPlaylistTracks(int playlistId, IReadOnlyList<int> orderedTrackIds) => _repository.ReorderTracks(playlistId, orderedTrackIds);

        public string ExportToM3u(int playlistId, string baseUrl)
        {
            var playlist = _repository.GetById(playlistId);
            if (playlist == null)
            {
                return string.Empty;
            }

            var entries = GetPlaylistTracksWithLidarrTracks(playlistId);
            var sb = new StringBuilder();
            sb.AppendLine("#EXTM3U");
            foreach (var (pt, track) in entries)
            {
                if (track == null)
                {
                    continue;
                }

                var duration = track.Duration;
                var title = EscapeM3uTitle(track.Title ?? "Unknown");
                sb.AppendLine($"#EXTINF:{duration},{title}");
                string location;
                if (track.TrackFileId > 0)
                {
                    var file = _mediaFileService.Get(track.TrackFileId);
                    location = file?.Path ?? string.Empty;
                }
                else
                {
                    location = string.Empty;
                }

                if (!string.IsNullOrEmpty(location))
                {
                    sb.AppendLine(location);
                }
            }

            return sb.ToString();
        }

        public string ExportToXspf(int playlistId, string baseUrl)
        {
            var playlist = _repository.GetById(playlistId);
            if (playlist == null)
            {
                return "<?xml version=\"1.0\" encoding=\"UTF-8\"?><playlist version=\"1\" xmlns=\"http://xspf.org/ns/0/\"></playlist>";
            }

            var entries = GetPlaylistTracksWithLidarrTracks(playlistId);
            var ns = XNamespace.Get("http://xspf.org/ns/0/");
            var trackList = new XElement(ns + "trackList");
            foreach (var (pt, track) in entries)
            {
                if (track == null)
                {
                    continue;
                }

                var title = track.Title ?? "Unknown";
                var durationMs = track.Duration * 1000;
                string location = string.Empty;
                if (track.TrackFileId > 0)
                {
                    var file = _mediaFileService.Get(track.TrackFileId);
                    if (!string.IsNullOrEmpty(file?.Path))
                    {
                        var path = file.Path;
                        if (Path.IsPathRooted(path))
                        {
                            location = "file:///" + path.Replace('\\', '/').TrimStart('/');
                        }
                        else
                        {
                            location = new Uri(path, UriKind.RelativeOrAbsolute).ToString();
                        }
                    }
                }

                var trackEl = new XElement(ns + "track",
                    new XElement(ns + "title", title),
                    new XElement(ns + "duration", durationMs));
                if (!string.IsNullOrEmpty(location))
                {
                    trackEl.Add(new XElement(ns + "location", location));
                }

                trackList.Add(trackEl);
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "playlist",
                    new XAttribute("version", 1),
                    new XAttribute(XNamespace.Xmlns + "ns", ns.NamespaceName),
                    new XElement(ns + "title", playlist.Name),
                    new XElement(ns + "creator", "PlaylistManager"),
                    trackList));
            return doc.ToString();
        }

        private List<(PlaylistTrack Pt, Track? Track)> GetPlaylistTracksWithLidarrTracks(int playlistId)
        {
            var pts = _repository.GetTracks(playlistId);
            if (pts.Count == 0)
            {
                return new List<(PlaylistTrack, Track?)>();
            }

            var trackIds = pts.Select(x => x.TrackId).Distinct().ToList();
            var tracks = _trackService.GetTracks(trackIds);
            var trackMap = tracks.ToDictionary(t => t.Id, t => t);
            return pts
                .OrderBy(x => x.Position)
                .Select(pt => (pt, trackMap.TryGetValue(pt.TrackId, out var t) ? t : null))
                .ToList();
        }

        private static string EscapeM3uTitle(string title)
        {
            return title.Replace(",", "&#44;", StringComparison.Ordinal);
        }
    }
}
