using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mail.Models.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mail.Models;

namespace Mail.ViewModels
{
    public sealed class MailMessageListDetailViewModel
    {
        public MailMessage MailMessage { get; }

        public bool IsDraft { get; set; }
        public bool IsDeliveryReceiptRequested { get; set; }

        public EditInfoViewModel EditInfo { get; private set; }

        public bool HasEmptyMailMessageId => string.IsNullOrEmpty(MailMessage.Id);

        public MailMessageListDetailViewModel(MailMessage mailMessage)
        {
            MailMessage = mailMessage;
        }

        public static MailMessageListDetailViewModel Empty(MailMessageRecipient Sender)
        {
            return new MailMessageListDetailViewModel(MailMessage.Empty(Sender))
            {
                EditInfo = new EditInfoViewModel()
            };
        }
    }

    public class EditInfoViewModel : INotifyPropertyChanged
    {
        private string title;
        private string sender;
        private string receiver;
        private string content;

        public string Title
        {
            get => title;
            set => SetValue(ref title, value);
        }

        public string Sender
        {
            get => sender;
            set => SetValue(ref sender, value);
        }

        public string Receiver
        {
            get => receiver;
            set => SetValue(ref receiver, value);
        }

        public string Content
        {
            get => content;
            set => SetValue(ref content, value);
        }

        protected bool SetValue<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return false;

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