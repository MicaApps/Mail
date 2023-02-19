using Microsoft.Graph.SecurityNamespace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mail.Servives
{
    internal class GmailService : OAuthMailService
    {
        static string[] scopes = { "" };
        public GmailService(): base(scopes, new CommunityToolkit.Authentication.WebAccountProviderConfig
        {
            ClientId = "",
            WebAccountProviderType = CommunityToolkit.Authentication.WebAccountProviderType.Any
        })
        {

        }


    }
}
