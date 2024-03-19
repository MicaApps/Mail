#nullable enable
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;
using Mail.Models;
using System.Net.Mail;
using System.Net.Http;
using System.Diagnostics;

namespace Mail.Converters;

public class ContactToAvatarConverter : IValueConverter
{
    private ICacheService? CacheService;
    private Dictionary<string, BitmapImage?> _instantCache = new();
    private readonly HttpClient httpClient =new HttpClient();

    public ContactToAvatarConverter()
    {
        CacheService = App.Services.GetService<ICacheService>();
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            var contacts = CacheService?.GetCache<IReadOnlyList<ContactModel>>();
            if (value is string recipient)
            {
                byte[]? resultByte = null;
                
                if (contacts is not null && contacts.FirstOrDefault(t => t.Address == recipient) is { } contact)
                {
                    contact.Avatar.Seek(0, SeekOrigin.Begin);
                    var ret = new StreamReader(contact.Avatar).ReadToEnd();
                    resultByte = System.Convert.FromBase64String(ret);
                }
                else
                {
                    if (true)
                    {
                        if (_instantCache.ContainsKey(recipient))
                            return _instantCache[recipient];
                        var domainSplit = new MailAddress(recipient).Host.Split('.');
                        if (domainSplit.Length > 3 && (domainSplit[domainSplit.Length - 2] == "co" ||
                                                       domainSplit[domainSplit.Length - 2] == "com"))
                        {
                            recipient = domainSplit[domainSplit.Length - 3] + "." +
                                             domainSplit[domainSplit.Length - 2] + "." +
                                             domainSplit[domainSplit.Length - 1];
                        }
                        else
                        {
                            recipient = domainSplit[domainSplit.Length - 2] + "." +
                                             domainSplit[domainSplit.Length - 1];
                        }
                        if (_instantCache.ContainsKey(recipient))
                            return _instantCache[recipient];

                        if (false)
                        {
                            // I'M EVIL
                            try
                            {
                                // BAD PRACTICE!
                                resultByte = httpClient.GetByteArrayAsync($"https://{recipient}/favicon.ico").GetAwaiter().GetResult();
                                if (resultByte[0] == 0x00 && resultByte[1] == 0x00 && resultByte[2] == 0x01 && resultByte[3] == 0x00)
                                {
                                    // noting
                                }
                                else
                                {
                             
                                    throw new BadImageFormatException();
                                }

                            }
                            catch (Exception ex)
                            {
                                resultByte = null;
                            }
                        }
                        else
                        {
                            var bitmap = new BitmapImage();
                            bitmap.UriSource = new Uri("https://" + recipient + "/favicon.ico");
                            _instantCache[recipient] = bitmap;
                            return bitmap;
                        }



                    }
                }
                if (resultByte != null)
                {
                    var bitmap = new BitmapImage();
                    MemoryStream ms = new MemoryStream(resultByte);
                    bitmap.SetSourceAsync(ms.AsRandomAccessStream());
                    _instantCache[recipient] = bitmap;
                }
                else
                {
                    _instantCache[recipient] = null;
                }

                return _instantCache[recipient]!;
            }
        }
        catch
        {
            // ignore
        }

        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}