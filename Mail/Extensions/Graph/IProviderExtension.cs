using CommunityToolkit.Authentication;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mail.Extensions.Graph
{
    public static class IProviderExtension
    {
        public static GraphServiceClient GetClient(this IProvider provider)
        {
            return new GraphServiceClient(new BaseBearerTokenAuthenticationProvider(new TokenProvider(provider)));
        }
    }

    internal class TokenProvider : IAccessTokenProvider
    {
        private IProvider Provider { get; set; }
        public TokenProvider(IProvider Provider)
        {
            this.Provider = Provider;
        }

        public AllowedHostsValidator AllowedHostsValidator { get; }

        public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            return await Provider.GetTokenAsync();
        }
    }
}
