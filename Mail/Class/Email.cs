namespace Mail.Class
{
    internal class Email
    {
        public string Address { get; }

        public string Name { get; }

        public Email(string Address, string Name)
        {
            this.Address = Address;
            this.Name = Name;
        }
    }
}
