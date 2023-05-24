using Mail.Services.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mail.Models
{
    internal sealed class MailMessageListDetailViewModel
    {
        public string Title => InnerData.Title;

        public string Id => InnerData.Id;

        public string PreviewText => InnerData.Content.ContentPreview;

        public string SenderName => InnerData.Sender.Name;

        public MailMessageRecipientData Sender => InnerData.Sender;

        public IReadOnlyList<MailMessageRecipientData> To => InnerData.To;

        public string Content => InnerData.Content.Content;

        public MailMessageContentType ContentType => InnerData.Content.ContentType;

        public string SentTime => InnerData.SentTime.HasValue ? InnerData.SentTime.Value.DateTime.ToString("yyyy/M/dd") : string.Empty;

        private readonly MailMessageData InnerData;

        public MailMessageListDetailViewModel(MailMessageData Data)
        {
            InnerData = Data;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Id);

        public static MailMessageListDetailViewModel Empty(MailMessageRecipientData Sender)
        {
            return new MailMessageListDetailViewModel(MailMessageData.Empty(Sender))
            {
                EditInfo = new EditInfoViewModel()
            };
        }

        public EditInfoViewModel EditInfo { get; private set; }
    }

    public class EditInfoViewModel : INotifyPropertyChanged
    {
        private string title;
        private string sender;
        private string receiver;
        private string content;

        public string Title { get => title; set => SetValue(ref title, value); }

        public string Sender { get => sender; set => SetValue(ref sender, value); }

        public string Receiver { get => receiver; set => SetValue(ref receiver, value); }

        public string Content { get => content; set => SetValue(ref content, value); }

        protected bool SetValue<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue)) return false;

            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
