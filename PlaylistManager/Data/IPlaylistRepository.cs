using PlaylistManager.Models;

namespace PlaylistManager.Data
{
    public interface IPlaylistRepository
    {
        void EnsureDatabase();

        IReadOnlyList<Playlist> GetAll();
        Playlist? GetById(int id);
        Playlist Create(string name, string? description = null);
        void Update(Playlist playlist);
        void Delete(int id);

        IReadOnlyList<PlaylistTrack> GetTracks(int playlistId);
        void SetTracks(int playlistId, IReadOnlyList<int> trackIds);
        void AddTrack(int playlistId, int trackId);
        void RemoveTrack(int playlistId, int trackId);
        void ReorderTracks(int playlistId, IReadOnlyList<int> orderedTrackIds);
    }
}
