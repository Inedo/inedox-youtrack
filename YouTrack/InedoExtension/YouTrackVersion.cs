using Newtonsoft.Json.Linq;

namespace Inedo.Extensions.YouTrack
{
    internal sealed class YouTrackVersion
    {
        public YouTrackVersion(JObject obj)
        {
            this.Id = (string)obj.Property("id");
            this.Name = (string)obj.Property("name");
            this.Released = (bool)obj.Property("released");
            this.Archived = (bool)obj.Property("archived");
        }

        public string Id { get; }
        public string Name { get; }
        public bool Released { get; }
        public bool Archived { get; }
    }
}
