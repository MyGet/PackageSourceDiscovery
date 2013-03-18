using System;
using System.Collections.Generic;
using System.Net;
using Moq;
using NuGet.Common;
using Xunit;
using Xunit.Extensions;
using Console = NuGet.Common.Console;

namespace NuGet.Test.Commands
{
    public class DiscoverCommandTest
    {
        [Theory]
        [InlineData(new object[] { null })]
        [InlineData(new object[] { "" })]
        public void CommandThrowsIfUrlIsNullOrEmpty(string url)
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var discoverCommand = new DiscoverCommandMock();
            discoverCommand.SetPackageSourceProvider(packageSourceProvider.Object);
            discoverCommand.Url = url;

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(discoverCommand.ExecuteCommand, "The URL specified cannot be empty. Please provide a valid URL.");
        }

        [Theory]
        [InlineData(new object[] { "foo" })]
        public void CommandThrowsIfUrlIsUnreachable(string url)
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var discoverCommand = new DiscoverCommandMock();
            discoverCommand.SetPackageSourceProvider(packageSourceProvider.Object);
            discoverCommand.Url = url;
                
            // Act and Assert
            ExceptionAssert.Throws<WebException>(discoverCommand.ExecuteCommand, "The remote name could not be resolved: 'foo'");
        }

        [Theory]
        [InlineData("www.xavierdecoster.com")]
        [InlineData("http://www.xavierdecoster.com")]
        public void CommandDoesNotThrowIfUrlIsValidUrl(string url)
        {
            // Arrange
            var console = new Mock<IConsole>(MockBehavior.Strict);
            console.Setup(c => c.WriteLine(PsdResources.DiscoverCommandSuccessful, 1));

            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(p => p.LoadPackageSources()).Returns(() => new List<PackageSource>());
            packageSourceProvider.Setup(p => p.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()));

            var discoverCommand = new DiscoverCommandMock();
            discoverCommand.Console = console.Object;
            discoverCommand.SetPackageSourceProvider(packageSourceProvider.Object);
            discoverCommand.Url = url;
            Action act = discoverCommand.ExecuteCommand;

            // Act and Assert
            Assert.DoesNotThrow(() => act());
        }
    }
}