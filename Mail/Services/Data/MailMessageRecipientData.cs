using Newtonsoft.Json;

namespace Mail.Services.Data
{
    internal sealed class MailMessageRecipientData
    {
        public string Name { get; set; }
        public string Address { get; set; }

        /// <summary>
        /// Outlook Message Compatible
        /// </summary>
        [JsonProperty("EmailAddress")]
        public object EmailAddress => new { Name, Address };

        public MailMessageRecipientData(string Name, string Address)
        {
            this.Name = Name;
            this.Address = Address;
        }
    }
}