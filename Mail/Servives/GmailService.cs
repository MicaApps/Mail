using CommunityToolkit.Authentication;

namespace Mail.Servives
{
    internal class GmailService : OAuthMailService
    {
        protected override string[] Scopes { get; } = new string[] { "" };

        public GmailService() : base(WebAccountProviderType.Any)
        {

        }
    }
}
