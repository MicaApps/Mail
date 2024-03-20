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
using System.Security.Cryptography;
using System.Text;

namespace Mail.Converters;

public class EmailToAvatarConverter : IValueConverter
{
    private readonly ICacheService _cacheService;
    private readonly SHA256 _sha256 = SHA256.Create();

    public EmailAvatarFallback Fallback { get; set; }

    public EmailToAvatarConverter()
    {
        _cacheService = App.Services.GetRequiredService<ICacheService>();
    }

    string GetEmailSha256(string email)
    {
        email = email.ToLower();
        byte[] inputBytes = Encoding.UTF8.GetBytes(email);
        byte[] hashBytes = _sha256.ComputeHash(inputBytes);
        StringBuilder sb = new(hashBytes.Length * 2);
        foreach (var b in hashBytes)
            sb.AppendFormat("{0:x2}", b);

        return sb.ToString();
    }

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null)
            return null;

        if (value is not string emailAddress)
            throw new ArgumentException();

        string emailAddressHash = GetEmailSha256(emailAddress);
        string fallback = Fallback switch
        {
            EmailAvatarFallback.None => "404",
            _ => Fallback.ToString().ToLower(),
        };

        BitmapImage bitmapImage = new();
        bitmapImage.UriSource = new Uri($"https://gravatar.com/avatar/{emailAddressHash}?d={fallback}");

        return bitmapImage;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
