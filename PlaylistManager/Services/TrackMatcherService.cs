using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Music;
using NLog;

namespace PlaylistManager.Services
{
    public class TrackMatcherService : ITrackMatcherService
    {
        private static readonly Regex ParentheticalRegex = new Regex(@"\s*[(\[]([^)\]]*)[)\]]\s*", RegexOptions.Compiled);
        private static readonly Regex CollapseSpace = new Regex(@"\s+", RegexOptions.Compiled);
        private readonly IArtistService _artistService;
        private readonly ITrackService _trackService;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private const double TitleFuzzThreshold = 0.7;

        public TrackMatcherService(IArtistService artistService, ITrackService trackService)
        {
            _artistService = artistService;
            _trackService = trackService;
        }

        public int? MatchToTrackId(string artistName, string trackTitle)
        {
            if (string.IsNullOrWhiteSpace(artistName) || string.IsNullOrWhiteSpace(trackTitle))
            {
                return null;
            }

            var artist = _artistService.FindByName(artistName.Trim());
            if (artist == null)
            {
                artist = _artistService.FindByNameInexact(artistName.Trim());
            }

            if (artist == null)
            {
                return null;
            }

            var tracks = _trackService.GetTracksByArtist(artist.Id);
            if (tracks == null || tracks.Count == 0)
            {
                return null;
            }

            var cleanTitle = trackTitle.Trim();
            var exact = tracks.FirstOrDefault(t => string.Equals(t.Title?.Trim(), cleanTitle, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact.Id;
            }

            var cleanTrackTitle = CleanTitleForFuzzyMatch(cleanTitle);
            var best = tracks
                .Select(t => new { Track = t, Score = CleanTitleForFuzzyMatch((t.Title ?? string.Empty).Trim()).FuzzyMatch(cleanTrackTitle) })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            if (best != null && best.Score >= TitleFuzzThreshold)
            {
                return best.Track.Id;
            }

            return null;
        }

        /// <summary>
        /// Normalize track title for fuzzy matching (remove parentheticals, collapse spaces).
        /// Replaces Lidarr's CleanArtistName which is not in NzbDrone.Common.
        /// </summary>
        private static string CleanTitleForFuzzyMatch(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            var cleaned = ParentheticalRegex.Replace(title.Trim(), " ");
            return CollapseSpace.Replace(cleaned, " ").Trim();
        }
    }
}
