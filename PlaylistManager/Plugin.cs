using NzbDrone.Core.Plugins;

namespace PlaylistManager
{
    public class PlaylistManager : Plugin
    {
        public override string Name => PluginInfo.Name;
        public override string Owner => PluginInfo.Author;
        public override string GithubUrl => PluginInfo.RepoUrl;
    }
}
