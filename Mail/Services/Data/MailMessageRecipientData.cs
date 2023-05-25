namespace Mail.Services.Data
{
    internal sealed class MailMessageRecipientData
    {
        public string Name { get; set; }

        public string Address { get; set; }

        public MailMessageRecipientData(string Name, string Address)
        {
            this.Name = Name;
            this.Address = Address;
        }
    }
}