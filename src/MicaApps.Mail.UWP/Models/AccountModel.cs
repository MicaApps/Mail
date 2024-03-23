using Mail.Models.Enums;

namespace Mail.Models
{
    internal sealed class AccountModel
    {
        public string Name { get; }

        public string Address { get; }
        public MailType MailType { get; }

        public AccountModel(string Name, string Address, MailType MailType)
        {
            this.Address = Address;
            this.MailType = MailType;
            this.Name = Name;
        }
    }
}