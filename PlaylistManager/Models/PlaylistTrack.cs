namespace PlaylistManager.Models
{
    public class PlaylistTrack
    {
        public int Id { get; set; }
        public int PlaylistId { get; set; }
        public int TrackId { get; set; }
        public int Position { get; set; }
    }
}
