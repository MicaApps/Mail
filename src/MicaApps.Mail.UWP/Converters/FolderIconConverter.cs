#nullable enable
using Mail.Services.Data;
using System;
using Windows.UI.Xaml.Data;

namespace Mail.Converters
{
    internal class FolderIconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MailFolderType folderType)
            {
                return folderType switch
                {
                    MailFolderType.Inbox => "\uE10F",
                    MailFolderType.Deleted => "\uE107",
                    MailFolderType.Drafts => "\uEC87",
                    MailFolderType.SentItems => "\uE122",
                    MailFolderType.Junk => "\uE107",
                    MailFolderType.Archive => "\uE7B8",
                    MailFolderType.Other => "\uE8B7",
                    _ => "\uE8B7"
                };
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
