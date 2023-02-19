
using System.Threading.Tasks;
using Microsoft.Graph;
using CommunityToolkit.Authentication;
using CommunityToolkit.Graph.Extensions;

namespace Mail.Servives
{
    internal class OutlookService : OAuthMailService
    {
        static string[] scopes = new string[] { "Mail.ReadWrite", "offline_access" };

        static string clientId = Secrect.AadClientId;


        private GraphServiceClient graphClient = null;

        public OutlookService() : base(scopes, new WebAccountProviderConfig
        {
            ClientId = clientId,
            WebAccountProviderType = WebAccountProviderType.Msa
        })
        {
            graphClient = Provider.GetClient();
        }

    }
}
