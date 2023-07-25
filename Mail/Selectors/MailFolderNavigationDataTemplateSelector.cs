using Mail.Services.Data;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Mail.Selectors
{
    internal class MailFolderNavigationDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Divider { get; set; }

        public DataTemplate Content { get; set; }

        public DataTemplate ContentWithChild { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is MailFolderData folder)
            {
                if (folder.ChildFolders != null) return ContentWithChild;

                return Content;
            }
            else
            {
                return Divider;
            }
        }
    }
}
