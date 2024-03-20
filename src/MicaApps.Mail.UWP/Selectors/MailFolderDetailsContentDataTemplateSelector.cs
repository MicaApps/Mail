using Mail.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Mail.Selectors
{
    internal sealed class MailFolderDetailsContentDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate EditTemplate { get; set; }

        public DataTemplate DefaultTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item is MailMessageListDetailViewModel { IsEmpty: true } ? EditTemplate : DefaultTemplate;
        }
    }
}
