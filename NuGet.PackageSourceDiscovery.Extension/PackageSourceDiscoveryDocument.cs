﻿using System.Collections.Generic;
﻿using System.Linq;

namespace NuGet
{
    public class PackageSourceDiscoveryDocument
    {
        public PackageSourceDiscoveryDocument()
        {
            Endpoints = new List<PackageSourceEndpoint>();
        }

        public PackageSourceDiscoveryDocument(string title, string url)
        {
            EngineName = "NuGet Core";
            EngineLink = "http://www.nuget.org";
            Title = title;
            Description = title;

            Endpoints = new List<PackageSourceEndpoint> {
                new PackageSourceEndpoint { ApiLink = url, Name = "nuget-v2-packages", Preferred = true }
            };
        }

        public string EngineName { get; set; }
        public string EngineLink { get; set; }
        public string HomePageLink { get; set; }
        public string Identifier { get; set; }
        public string Owner { get; set; }
        public string Creator { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<PackageSourceEndpoint> Endpoints { get; set; }

        public PackageSource AsPackageSource()
        {
            var endpoint = Endpoints.OrderByDescending(e => e.Preferred).FirstOrDefault(e => e.Name == FeedDiscoveryConstants.Discovery.PackagesV1 || e.Name == FeedDiscoveryConstants.Discovery.PackagesV2);
            if (endpoint == null)
            {
                return null;
            }
            return new PackageSource(endpoint.ApiLink, Title);
        }
    }
}