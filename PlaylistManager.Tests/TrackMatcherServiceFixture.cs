using Moq;
using NzbDrone.Core.Music;
using PlaylistManager.Services;
using Xunit;

namespace PlaylistManager.Tests
{
    public class TrackMatcherServiceFixture
    {
        private readonly Mock<IArtistService> _artistService;
        private readonly Mock<ITrackService> _trackService;
        private readonly TrackMatcherService _sut;

        public TrackMatcherServiceFixture()
        {
            _artistService = new Mock<IArtistService>();
            _trackService = new Mock<ITrackService>();
            _sut = new TrackMatcherService(_artistService.Object, _trackService.Object);
        }

        [Fact]
        public void MatchToTrackId_returns_null_when_artist_empty()
        {
            var result = _sut.MatchToTrackId("", "Some Title");
            Assert.Null(result);
        }

        [Fact]
        public void MatchToTrackId_returns_null_when_title_empty()
        {
            var result = _sut.MatchToTrackId("Artist", "");
            Assert.Null(result);
        }

        [Fact]
        public void MatchToTrackId_returns_null_when_artist_not_found()
        {
            _artistService.Setup(x => x.FindByName(It.IsAny<string>())).Returns((Artist?)null);
            _artistService.Setup(x => x.FindByNameInexact(It.IsAny<string>())).Returns((Artist?)null);

            var result = _sut.MatchToTrackId("Unknown Artist", "Track");
            Assert.Null(result);
        }
    }
}
