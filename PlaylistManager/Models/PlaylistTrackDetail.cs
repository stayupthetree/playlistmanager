namespace PlaylistManager.Models
{
    public class PlaylistTrackDetail
    {
        public int TrackId { get; set; }
        public int Position { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
    }
}
