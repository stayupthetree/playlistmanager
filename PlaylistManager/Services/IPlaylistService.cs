using PlaylistManager.Models;

namespace PlaylistManager.Services
{
    public interface IPlaylistService
    {
        void EnsureDatabase();

        IReadOnlyList<Playlist> GetAllPlaylists();
        Playlist? GetPlaylist(int id);
        Playlist CreatePlaylist(string name, string? description = null);
        void UpdatePlaylist(Playlist playlist);
        void DeletePlaylist(int id);

        IReadOnlyList<PlaylistTrack> GetPlaylistTracks(int playlistId);
        void SetPlaylistTracks(int playlistId, IReadOnlyList<int> trackIds);
        void AddTrackToPlaylist(int playlistId, int trackId);
        void RemoveTrackFromPlaylist(int playlistId, int trackId);
        void ReorderPlaylistTracks(int playlistId, IReadOnlyList<int> orderedTrackIds);

        string ExportToM3u(int playlistId, string baseUrl);
        string ExportToXspf(int playlistId, string baseUrl);
    }
}
