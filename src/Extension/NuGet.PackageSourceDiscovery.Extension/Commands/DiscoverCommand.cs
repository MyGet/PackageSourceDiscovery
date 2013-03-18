using System;
using System.Linq;
using System.Text;

namespace NuGet.Commands
{
    [Command(typeof(PsdResources), "discover", "DiscoverCommandDescription", UsageSummaryResourceName = "DiscoverCommandUsageSummary",
        MinArgs = 0, MaxArgs = 1)]
    public class DiscoverCommand : Command
    {
        private const string ApiKeyHeader = "NuGet-ApiKey";

        [Option(typeof(PsdResources), "DiscoverCommandUrlDescription")]
        public string Url { get; set; }

        [Option(typeof(PsdResources), "DiscoverCommandUserNameDescription")]
        public string UserName { get; set; }

        [Option(typeof(PsdResources), "DiscoverCommandPasswordDescription")]
        public string Password { get; set; }

        [Option(typeof(PsdResources), "DiscoverCommandApiKeyDescription")]
        public string ApiKey { get; set; }

        public override void ExecuteCommand()
        {
            if (SourceProvider == null)
            {
                throw new InvalidOperationException(PsdResources.Error_SourceProviderIsNull);
            }

            DiscoverPackageSources();
        }

        private void DiscoverPackageSources()
        {
            if (String.IsNullOrEmpty(Url))
            {
                throw new CommandLineException(PsdResources.DiscoverCommandUrlRequired);
            }

            Uri uri;
            if (!Uri.TryCreate(Url, UriKind.Absolute, out uri))
            {
                throw new CommandLineException(PsdResources.DiscoverCommandValidUrlRequired);
            }

            ValidateCredentials();

            var discovery = new PackageSourceDiscovery();
            discovery.SendingRequest += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(UserName))
                {
                    var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                    args.Request.Headers.Add("Authorization", string.Format("Basic {0}", credentials));
                }

                if (!string.IsNullOrEmpty(ApiKey))
                {
                    args.Request.Headers.Add(ApiKeyHeader, ApiKey);
                }
            };

            var discoveryDocuments = discovery.FetchDiscoveryDocuments(uri);

            var sourceList = SourceProvider.LoadPackageSources().ToList();
            int counter = 0;
            foreach (var discoveryDocument in discoveryDocuments)
            {
                // REVIEW: Is this correct behaviour? Shouldn't we always overwrite?
                // Check if a package source already exists. Do not overwrite existing package sources.
                if (!sourceList.Any(ps => String.Equals(discoveryDocument.Title, ps.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    var source = discoveryDocument.AsPackageSource();
                    sourceList.Add(source);

                    var pushEndpoint = discoveryDocument.Endpoints.OrderByDescending(e => e.Preferred).FirstOrDefault(e => e.Name == FeedDiscoveryConstants.Discovery.PushV1 || e.Name == FeedDiscoveryConstants.Discovery.PushV2);
                    if (pushEndpoint != null && pushEndpoint.Settings["apiKey"] != null)
                    {
                        Settings.SetEncryptedValue(CommandLineUtility.ApiKeysSectionName, source.Source, pushEndpoint.Settings["apiKey"]);
                    }
                    counter++;
                }
            }

            SourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(PsdResources.DiscoverCommandSuccessful, counter);
        }

        private void ValidateCredentials()
        {
            bool userNameEmpty = String.IsNullOrEmpty(UserName);
            bool passwordEmpty = String.IsNullOrEmpty(Password);

            if (userNameEmpty ^ passwordEmpty)
            {
                // If only one of them is set, throw.
                throw new CommandLineException(PsdResources.DiscoverCommandCredentialsRequired);
            }
        }
    }
}