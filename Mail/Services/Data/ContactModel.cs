namespace Mail.Services.Data
{
    internal sealed class ContactModel
    {
        public string Name { get; }

        public string Address { get; }

        public ContactModel(string Name, string Address)
        {
            this.Address = Address;
            this.Name = Name;
        }
    }
}
