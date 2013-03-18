using System.Collections.Generic;

namespace NuGet
{
    public class PackageSourceEndpoint
    {
        public PackageSourceEndpoint()
        {
            Settings = new Dictionary<string, string>();
        }

        public string Name { get; set; }
        public string ApiLink { get; set; }
        public bool Preferred { get; set; }
        public Dictionary<string, string> Settings { get; set; }
    }
}