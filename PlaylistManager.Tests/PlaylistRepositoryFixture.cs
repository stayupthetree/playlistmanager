using System.IO;
using Moq;
using NzbDrone.Common.EnvironmentInfo;
using PlaylistManager.Data;
using PlaylistManager.Models;
using Xunit;

namespace PlaylistManager.Tests
{
    public class PlaylistRepositoryFixture
    {
        private static string GetTempBaseDir()
        {
            var baseDir = Path.Combine(Path.GetTempPath(), "PlaylistManagerTests", Guid.NewGuid().ToString("N"));
            var pluginSubDir = Path.Combine(baseDir, "plugins", "TypNull", "PlaylistManager");
            Directory.CreateDirectory(pluginSubDir);
            return baseDir;
        }

        private static IPlaylistRepository CreateRepository()
        {
            var appDataFolder = GetTempBaseDir();
            var appFolder = new Mock<IAppFolderInfo>();
            appFolder.Setup(x => x.AppDataFolder).Returns(appDataFolder);
            return new PlaylistRepository(appFolder.Object);
        }

        [Fact]
        public void Create_and_GetById_roundtrip()
        {
            var repo = CreateRepository();
            repo.EnsureDatabase();
            var created = repo.Create("Test List", "Description");
            Assert.True(created.Id > 0);
            Assert.Equal("Test List", created.Name);
            Assert.Equal("Description", created.Description);

            var got = repo.GetById(created.Id);
            Assert.NotNull(got);
            Assert.Equal(created.Id, got.Id);
            Assert.Equal("Test List", got.Name);
        }

        [Fact]
        public void SetTracks_and_GetTracks_roundtrip()
        {
            var repo = CreateRepository();
            repo.EnsureDatabase();
            var playlist = repo.Create("With Tracks", null);
            repo.SetTracks(playlist.Id, new[] { 1, 2, 3 });
            var tracks = repo.GetTracks(playlist.Id);
            Assert.Equal(3, tracks.Count);
            Assert.Equal(1, tracks[0].TrackId);
            Assert.Equal(2, tracks[1].TrackId);
            Assert.Equal(3, tracks[2].TrackId);
        }
    }
}
