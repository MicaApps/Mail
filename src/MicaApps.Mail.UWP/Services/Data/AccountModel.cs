using Mail.Services.Data.Enums;
using Windows.UI.Xaml.Media.Imaging;

namespace Mail.Services.Data
{
    internal sealed class AccountModel
    {
        public string Name { get; }

        public string Address { get; }
        public MailType MailType { get; }

        public BitmapImage? Avatar { get; }

        public AccountModel(string Name, string Address, MailType MailType, BitmapImage? Avatar)
        {
            this.Address = Address;
            this.MailType = MailType;
            this.Name = Name;
            this.Avatar = Avatar;
        }
    }
}