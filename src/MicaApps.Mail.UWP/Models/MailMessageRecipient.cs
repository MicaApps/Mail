using Mail.Models.Enums;

namespace Mail.Models;

public class MailMessageRecipient
{
    public MailMessageRecipient()
    {
    }

    public MailMessageRecipient(string name, string address)
    {
        Name = name;
        Address = address;
    }

    public string Name { get; set; }
    public string Address { get; set; }
    public RecipientType RecipientType { get; set; }
}