using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Mail.Services;
using Mail.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Mail.Converters;

public class ContactToAvatarConverter : IValueConverter
{
    private ICacheService? CacheService;

    public ContactToAvatarConverter()
    {
        CacheService = App.Services.GetService<ICacheService>();
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var contacts = CacheService?.GetCache<IReadOnlyList<ContactModel>>();
        if (value is string recipient && contacts is not null)
        {
            if (contacts.FirstOrDefault(t => t.Address == recipient) is { } contact)
            {
                contact.Avatar.Seek(0, SeekOrigin.Begin);
                var ret = new StreamReader(contact.Avatar).ReadToEnd();
                byte[] bytes = System.Convert.FromBase64String(ret);
                var bitmap = new BitmapImage();
                MemoryStream ms = new MemoryStream(bytes);
                bitmap.SetSourceAsync(ms.AsRandomAccessStream());

                return bitmap;
            }
        }

        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}