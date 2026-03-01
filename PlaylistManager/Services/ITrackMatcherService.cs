namespace PlaylistManager.Services
{
    public interface ITrackMatcherService
    {
        int? MatchToTrackId(string artistName, string trackTitle);
    }
}
