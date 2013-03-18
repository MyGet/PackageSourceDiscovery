using Xunit;

namespace NuGet.Test
{
    public class PackageSourceDiscoveryDocumentExtensionsTest
    {
        [Fact]
        public void PackageSourceDiscoveryDocumentExtensionsReturnNullWhenNoEndpointIsGiven()
        {
            // Arrange
            var discoveryDocument = new PackageSourceDiscoveryDocument();

            // Act
            var packageSource = discoveryDocument.AsPackageSource();

            // Assert
            Assert.Null(packageSource);
        }

        [Fact]
        public void PackageSourceDiscoveryDocumentExtensionsReturnPackageSource()
        {
            // Arrange
            var discoveryDocument = new PackageSourceDiscoveryDocument();
            discoveryDocument.Title = "test";
            discoveryDocument.Endpoints.Add(new PackageSourceEndpoint { Name = "nuget-v2-packages", ApiLink = "http://test/" });

            // Act
            var packageSource = discoveryDocument.AsPackageSource();

            // Assert
            Assert.NotNull(packageSource);
            Assert.Equal("test", packageSource.Name);
            Assert.Equal("http://test/", packageSource.Source);
        }
    }
}