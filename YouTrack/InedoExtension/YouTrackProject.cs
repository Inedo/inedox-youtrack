namespace Inedo.Extensions.YouTrack
{
    internal sealed class YouTrackProject
    {
        public YouTrackProject(string id, string name, string shortName)
        {
            this.Id = id;
            this.Name = name;
            this.ShortName = shortName;
        }

        public string Id { get; }
        public string Name { get; }
        public string ShortName { get; }
    }
}
