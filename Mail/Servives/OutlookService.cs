
using System.Threading.Tasks;
using Microsoft.Graph;
using CommunityToolkit.Authentication;
using CommunityToolkit.Graph.Extensions;
using System;
using Mail.Pages;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Net;

namespace Mail.Servives
{
    internal class OutlookService : OAuthMailService
    {
        static string[] scopes = new string[] {
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

        internal async Task<List<Email>> GetEmail()
        {
            var emails = new List<Email>();
            if (graphClient != null) {

                var contacts = await graphClient.Me.Contacts
                                        .Request()
                                        .GetAsync();
                System.Diagnostics.Trace.WriteLine(contacts);
                System.Diagnostics.Trace.WriteLine(JsonConvert.SerializeObject(contacts));
                
                System.Diagnostics.Trace.WriteLine(contacts.Count);
                for (int i = 0; i < contacts.Count; i++)
                {
                    if (contacts[i].EmailAddresses.Count() > 0)
                    {
                        emails.Add(new Email
                        {
                            name = contacts[i].EmailAddresses.LastOrDefault().Name,
                            address = contacts[i].EmailAddresses.LastOrDefault().Address
                        });
                    }
                    
                }
            }
            return  emails;
        }
    }
}
