namespace Mail.Services.Data
{
    internal sealed class AccountModel
    {
        public string Name { get; }

        public string Address { get; }

        public AccountModel(string Name, string Address)
        {
            this.Address = Address;
            this.Name = Name;
        }
    }
}