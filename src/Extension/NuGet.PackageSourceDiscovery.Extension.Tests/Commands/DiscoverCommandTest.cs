using Moq;
using Xunit.Extensions;

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
        public void CommandThrowsIfUrlIsInvalidUrl(string url)
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var discoverCommand = new DiscoverCommandMock();
            discoverCommand.SetPackageSourceProvider(packageSourceProvider.Object);
            discoverCommand.Url = url;

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(discoverCommand.ExecuteCommand, "The URL specified is invalid. Please provide a valid URL.");
        }
    }
}