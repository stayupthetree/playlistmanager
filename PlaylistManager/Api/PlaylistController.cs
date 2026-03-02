using System.Reflection;
using System.Text;
using Lidarr.Http;
using Lidarr.Http.REST;
using Microsoft.AspNetCore.Mvc;
using PlaylistManager.Models;
using PlaylistManager.Services;

namespace PlaylistManager.Api
{
    [V1ApiController("playlist")]
    public class PlaylistController : Controller
    {
        private readonly IPlaylistService _playlistService;
        private readonly ITrackMatcherService _trackMatcher;

        public PlaylistController(IPlaylistService playlistService, ITrackMatcherService trackMatcher)
        {
            _playlistService = playlistService;
            _trackMatcher = trackMatcher;
        }

        [HttpGet("ui")]
        public IActionResult GetUi()
        {
            var asm = typeof(PlaylistController).Assembly;
            var name = "PlaylistManager.Ui.html";
            using var stream = asm.GetManifestResourceStream(name);
            if (stream == null)
            {
                return NotFound();
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var html = reader.ReadToEnd();
            return Content(html, "text/html", Encoding.UTF8);
        }

        [HttpGet]
        public ActionResult<IReadOnlyList<Playlist>> GetAll()
        {
            _playlistService.EnsureDatabase();
            return Ok(_playlistService.GetAllPlaylists());
        }

        [HttpGet("{id:int}")]
        public ActionResult<Playlist> GetById(int id)
        {
            var playlist = _playlistService.GetPlaylist(id);
            if (playlist == null)
            {
                return NotFound();
            }

            return Ok(playlist);
        }

        [HttpPost]
        public ActionResult<Playlist> Create([FromBody] CreatePlaylistRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name))
            {
                throw new BadRequestException("Name is required");
            }

            var playlist = _playlistService.CreatePlaylist(request.Name.Trim(), request.Description?.Trim());
            return CreatedAtAction(nameof(GetById), new { id = playlist.Id }, playlist);
        }

        [HttpPut("{id:int}")]
        public ActionResult<Playlist> Update(int id, [FromBody] UpdatePlaylistRequest request)
        {
            var playlist = _playlistService.GetPlaylist(id);
            if (playlist == null)
            {
                return NotFound();
            }

            if (request?.Name != null)
            {
                playlist.Name = request.Name.Trim();
            }

            if (request?.Description != null)
            {
                playlist.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            }

            _playlistService.UpdatePlaylist(playlist);
            return Ok(_playlistService.GetPlaylist(id));
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            if (_playlistService.GetPlaylist(id) == null)
            {
                return NotFound();
            }

            _playlistService.DeletePlaylist(id);
            return NoContent();
        }

        [HttpGet("{id:int}/tracks")]
        public ActionResult<IReadOnlyList<PlaylistTrack>> GetTracks(int id)
        {
            if (_playlistService.GetPlaylist(id) == null)
            {
                return NotFound();
            }

            return Ok(_playlistService.GetPlaylistTracks(id));
        }

        [HttpGet("{id:int}/tracks/details")]
        public ActionResult<IReadOnlyList<PlaylistTrackDetail>> GetTracksWithDetails(int id)
        {
            if (_playlistService.GetPlaylist(id) == null)
            {
                return NotFound();
            }

            return Ok(_playlistService.GetPlaylistTracksWithDetails(id));
        }

        [HttpPut("{id:int}/tracks")]
        public IActionResult SetTracks(int id, [FromBody] SetTracksRequest request)
        {
            if (_playlistService.GetPlaylist(id) == null)
            {
                return NotFound();
            }

            _playlistService.SetPlaylistTracks(id, request?.TrackIds ?? Array.Empty<int>());
            return NoContent();
        }

        [HttpPost("{id:int}/tracks")]
        public IActionResult AddTrack(int id, [FromBody] AddTrackRequest request)
        {
            if (_playlistService.GetPlaylist(id) == null)
            {
                return NotFound();
            }

            if (request?.TrackId == null || request.TrackId <= 0)
            {
                throw new BadRequestException("Valid TrackId is required");
            }

            _playlistService.AddTrackToPlaylist(id, request.TrackId.Value);
            return NoContent();
        }

        [HttpDelete("{id:int}/tracks/{trackId:int}")]
        public IActionResult RemoveTrack(int id, int trackId)
        {
            if (_playlistService.GetPlaylist(id) == null)
            {
                return NotFound();
            }

            _playlistService.RemoveTrackFromPlaylist(id, trackId);
            return NoContent();
        }

        [HttpPut("{id:int}/tracks/reorder")]
        public IActionResult ReorderTracks(int id, [FromBody] ReorderTracksRequest request)
        {
            if (_playlistService.GetPlaylist(id) == null)
            {
                return NotFound();
            }

            _playlistService.ReorderPlaylistTracks(id, request?.TrackIds ?? Array.Empty<int>());
            return NoContent();
        }

        [HttpGet("{id:int}/export/m3u")]
        public IActionResult ExportM3u(int id, [FromQuery] string? baseUrl = null)
        {
            if (_playlistService.GetPlaylist(id) == null)
            {
                return NotFound();
            }

            var content = _playlistService.ExportToM3u(id, baseUrl ?? string.Empty);
            return Content(content, "audio/x-mpegurl", System.Text.Encoding.UTF8);
        }

        [HttpGet("{id:int}/export/xspf")]
        public IActionResult ExportXspf(int id)
        {
            if (_playlistService.GetPlaylist(id) == null)
            {
                return NotFound();
            }

            var content = _playlistService.ExportToXspf(id, string.Empty);
            return Content(content, "application/xspf+xml", System.Text.Encoding.UTF8);
        }

        [HttpPost("import")]
        public ActionResult<ImportPlaylistResult> ImportFromTrackList([FromBody] ImportPlaylistRequest request)
        {
            if (request?.Tracks == null || request.Tracks.Count == 0)
            {
                throw new BadRequestException("Tracks array is required and must not be empty");
            }

            var name = string.IsNullOrWhiteSpace(request.PlaylistName)
                ? "Imported Playlist"
                : request.PlaylistName.Trim();
            var playlist = _playlistService.CreatePlaylist(name, request.Description?.Trim());
            var matched = new List<int>();
            var unmatched = new List<ImportTrackItem>();

            foreach (var item in request.Tracks)
            {
                var artist = item.Artist?.Trim() ?? string.Empty;
                var title = item.Title?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(title))
                {
                    unmatched.Add(item);
                    continue;
                }

                var trackId = _trackMatcher.MatchToTrackId(artist, title);
                if (trackId.HasValue)
                {
                    matched.Add(trackId.Value);
                }
                else
                {
                    unmatched.Add(item);
                }
            }

            _playlistService.SetPlaylistTracks(playlist.Id, matched);

            return Ok(new ImportPlaylistResult
            {
                Playlist = playlist,
                MatchedCount = matched.Count,
                UnmatchedCount = unmatched.Count,
                Unmatched = unmatched
            });
        }
    }

    public class CreatePlaylistRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdatePlaylistRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class SetTracksRequest
    {
        public IReadOnlyList<int>? TrackIds { get; set; }
    }

    public class AddTrackRequest
    {
        public int? TrackId { get; set; }
    }

    public class ReorderTracksRequest
    {
        public IReadOnlyList<int>? TrackIds { get; set; }
    }

    public class ImportPlaylistRequest
    {
        public string? PlaylistName { get; set; }
        public string? Description { get; set; }
        public List<ImportTrackItem>? Tracks { get; set; }
    }

    public class ImportTrackItem
    {
        public string? Artist { get; set; }
        public string? Title { get; set; }
    }

    public class ImportPlaylistResult
    {
        public Playlist Playlist { get; set; } = null!;
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public List<ImportTrackItem> Unmatched { get; set; } = new();
    }
}
