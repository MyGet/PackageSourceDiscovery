using NuGet.Commands;

namespace NuGet.Test.Commands
{
    public class DiscoverCommandMock : DiscoverCommand
    {
        internal void SetPackageSourceProvider(IPackageSourceProvider packageSourceProvider)
        {
            SourceProvider = packageSourceProvider;
        }
    }
}