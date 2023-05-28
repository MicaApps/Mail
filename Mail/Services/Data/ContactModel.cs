using System.IO;

namespace Mail.Services.Data
{
    internal sealed class ContactModel
    {
        public string Name { get; }

        public string Address { get; }
        public Stream? Avatar { get; }

        public ContactModel(string Name, string Address, Stream? Avatar)
        {
            this.Address = Address;
            this.Avatar = Avatar;
            this.Name = Name;
        }
    }
}