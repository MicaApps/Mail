﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mail.Models.Enums;
using Newtonsoft.Json;

namespace Mail.Models
{
    public sealed class MailMessageListDetailViewModel
    {
        [JsonProperty("subject")]
        public string Title
        {
            get => InnerData.Title;
            set => InnerData.Title = value;
        }


        public string Id
        {
            get => InnerData.Id;
            set => InnerData.Id = value;
        }

        [JsonProperty("bodyPreview")] // Outlook: first 255 char
        public string PreviewText
        {
            get => InnerData.Content.ContentPreview;
            set => InnerData.Content.ContentType = Enum.TryParse(value, out MailMessageContentType result)
                ? result
                : MailMessageContentType.Text;
        }

        public string SenderName
        {
            get
            {
                return InnerData.Sender?.Name ?? string.Empty;
            }
            set => InnerData.Sender.Name = value;
        }

        public MailMessageRecipient Sender
        {
            get => InnerData.Sender;
            set => InnerData.Sender = value;
        }

        public IList<MailMessageRecipient> ToRecipients => InnerData.To;

        public IList<MailMessageRecipient> BccRecipients => InnerData.Bcc;

        public IList<MailMessageRecipient> CcRecipients => InnerData.CC;

        public string BodyPreview
        {
            get => InnerData.Content.ContentPreview;
            set => InnerData.Content.ContentPreview = value;
        }

        public bool IsDraft { get; set; }
        public bool IsDeliveryReceiptRequested { get; set; }

        public MailMessageContent Body => InnerData.Content;

        public string Content
        {
            get => InnerData.Content.Content;
            set => InnerData.Content.Content = value;
        }

        public MailMessageContentType ContentType
        {
            get => InnerData.Content.ContentType;
            set => InnerData.Content.ContentType = value;
        }

        public string SentTime => InnerData.SentTime.HasValue
            ? InnerData.SentTime.Value.ToString("yyyy/M/dd")
            : string.Empty;

        private readonly MailMessage InnerData;

        public MailMessageListDetailViewModel(MailMessage Data)
        {
            InnerData = Data;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Id);

        public static MailMessageListDetailViewModel Empty(MailMessageRecipient Sender)
        {
            return new MailMessageListDetailViewModel(MailMessage.Empty(Sender))
            {
                EditInfo = new EditInfoViewModel()
            };
        }

        public EditInfoViewModel? EditInfo { get; private set; }
    }

    public class EditInfoViewModel : INotifyPropertyChanged
    {
        private string? title;
        private string? sender;
        private string? receiver;
        private string? content;

        public string? Title
        {
            get => title;
            set => SetValue(ref title, value);
        }

        public string? Sender
        {
            get => sender;
            set => SetValue(ref sender, value);
        }

        public string? Receiver
        {
            get => receiver;
            set => SetValue(ref receiver, value);
        }

        public string? Content
        {
            get => content;
            set => SetValue(ref content, value);
        }

        protected bool SetValue<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue)) return false;

            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}