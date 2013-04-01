using System;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PackageSourceDiscoveryTest
    {
        private class PackageSourceDiscoveryWithMockDownloadData
            : PackageSourceDiscovery
        {
            private readonly Func<string, string> _provider;

            public PackageSourceDiscoveryWithMockDownloadData(Func<string, string> provider)
            {
                _provider = provider;
            }

            protected override string DownloadAsString(Uri uri)
            {
                return _provider(uri.ToString());
            }
        }

        [Theory]
        [InlineData(new object[] { null })]
        public void DiscoverPackageSourcesThrowsIfUrlIsNull(Uri uri)
        {
            // Arrange
            var discovery = new PackageSourceDiscovery();

            // Act and Assert
            ExceptionAssert.Throws<ArgumentNullException>(() => discovery.FetchDiscoveryDocuments(uri));
        }

        [Fact]
        public void DiscoverPackageSources()
        {
            // Arrange
            var discovery = new PackageSourceDiscoveryWithMockDownloadData(url =>
            {
                switch (url)
                {
                    case "http://discover/":
                        return @"
  <link rel=""nuget"" type=""application/atom+xml"" title=""Sample feed"" href=""http://www.nuget.org/sample/"" />
  <link rel=""nuget"" type=""application/rsd+xml"" href=""http://www.nuget.org/discover/feed/"" />
  <link rel=""nuget"" type=""application/nfd+xml"" href=""http://www.nuget.org/nugetext/discover-feeds/"" />
</head>
<body>
";

                    case "http://www.nuget.org/sample/":
                        return @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<service xml:base=""http://www.nuget.org/api/v2/"" xmlns:atom=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" xmlns=""http://www.w3.org/2007/app"">
  <workspace>
    <atom:title>Default</atom:title>
    <collection href=""Packages"">
      <atom:title>Packages</atom:title>
    </collection>
  </workspace>
</service>";

                    case "http://www.nuget.org/discover/feed/":
                        return @"<rsd version=""1.0"" xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns=""http://archipelago.phrasewise.com/rsd"">
  <service>
    <engineName>NuGet</engineName>
    <engineLink>http://www.nuget.org/</engineLink>
    <dc:identifier>http://www.nuget.org/anothersample/</dc:identifier>
    <dc:owner>nuget</dc:owner>
    <dc:creator>nuget</dc:creator>
    <dc:title>Another sample NuGet feed</dc:title>
    <dc:description>Another sample NuGet feed</dc:description>
    <homePageLink>http://www.nuget.org/</homePageLink>
    <apis>
      <api name=""nuget-v2-packages"" blogID="""" preferred=""true"" apiLink=""http://www.nuget.org/anothersample/"">
		<settings>
			<setting name=""apiKey"">foo</setting>
		</settings>
	  </api>
      <api name=""nuget-v1-packages"" blogID="""" preferred=""false"" apiLink=""http://www.nuget.org/anothersample/api/v1/"" />
    </apis>
  </service>
</rsd>";
                    case "http://www.nuget.org/nugetext/discover-feeds/":
                        return @"<?xml version=""1.0"" encoding=""utf-8""?><feedList xmlns=""http://nugetext.org/schemas/nuget-feed-discovery/1.0.0""><feed><guid>00000001-0000-0000-0000-000000000000</guid><name>Default</name><url>/nuget/Default</url><pushUrl>/api/v2/package/Default</pushUrl><htmlUrl>/feeds/Default</htmlUrl></feed></feedList>";
                }
                return "Page not found.";
            });

            // Act 
            var packageSources = discovery.FetchDiscoveryDocuments(new Uri("http://discover/"));

            // Assert
            Assert.Equal(3, packageSources.Count());
            Assert.Equal("Sample feed", packageSources.First().AsPackageSource().Name);
            Assert.Equal("http://www.nuget.org/sample/", packageSources.First().AsPackageSource().Source);

            Assert.Equal("Another sample NuGet feed", packageSources.Skip(1).First().Title);
            Assert.Equal("http://www.nuget.org/anothersample/", packageSources.Skip(1).First().Endpoints.First().ApiLink);

            Assert.Equal("Default", packageSources.Skip(2).First().Title);
            Assert.Equal("http://www.nuget.org/nuget/Default", packageSources.Skip(2).First().Endpoints.First().ApiLink);
        }
    }
}