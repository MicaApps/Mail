
using CommunityToolkit.Authentication;
using CommunityToolkit.Graph.Extensions;
using Mail.Class;
using Microsoft.Graph;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mail.Servives
{
    internal class OutlookService : OAuthMailService
    {
        protected override string[] Scopes { get; } = new string[]
        {
            "User.Read",
            "User.ReadBasic.All",
            "People.Read",
            "MailboxSettings.Read",
            "Calendars.ReadWrite",
            "Contacts.Read",
            "Contacts.ReadWrite",
            "Mail.ReadWrite",
            "offline_access"
        };

        public OutlookService() : base(WebAccountProviderType.Msa)
        {

        }

        public async Task<IReadOnlyList<Email>> GetEmailAsync()
        {
            var contacts = await Provider.GetClient().Me.Contacts.Request().GetAsync();

            System.Diagnostics.Trace.WriteLine(contacts);
            System.Diagnostics.Trace.WriteLine(JsonConvert.SerializeObject(contacts));
            System.Diagnostics.Trace.WriteLine(contacts.Count);

            return contacts.Select((contact) => contact.EmailAddresses.LastOrDefault()).OfType<EmailAddress>().Select((Address) => new Email(Address.Address, Address.Name)).ToArray();
        }
    }
}
