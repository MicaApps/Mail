using Mail.Services.Data;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Mail.Selectors
{
    internal sealed class MailFolderNavigationDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Divider { get; set; }

        public DataTemplate Content { get; set; }

        public DataTemplate ContentWithChild { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return item is MailFolderData folder ? folder.ChildFolders != null ? ContentWithChild : Content : Divider;
        }
    }
}
