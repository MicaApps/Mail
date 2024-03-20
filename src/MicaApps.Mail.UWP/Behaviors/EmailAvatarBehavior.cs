using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mail.Models.Enums;
using Microsoft.Graph.Models;
using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MicaApps.Mail.Behaviors
{
    public class EmailAvatarBehavior : Behavior<Windows.UI.Xaml.Controls.PersonPicture>
    {
        private static readonly SHA256 s_sha256 = SHA256.Create();
        private static readonly HttpClient s_httpClient = new();

        private bool _changedImage;
        private ImageSource _originImage;
        private CancellationTokenSource _cancellation;

        private string _emailAddress;
        private GravatarFallback _fallback;


        public string EmailAddress
        {
            get => _emailAddress; 
            set
            {
                _emailAddress = value;
                StartImageUpdating();
            }
        }
        public GravatarFallback Fallback
        {
            get => _fallback;
            set
            {
                _fallback = value;
                StartImageUpdating();
            }
        }


        string GetEmailSha256(string email)
        {
            email = email.ToLower();
            byte[] inputBytes = Encoding.UTF8.GetBytes(email);
            byte[] hashBytes = s_sha256.ComputeHash(inputBytes);
            StringBuilder sb = new(hashBytes.Length * 2);
            foreach (var b in hashBytes)
                sb.AppendFormat("{0:x2}", b);

            return sb.ToString();
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            StartImageUpdating();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (_changedImage)
                AssociatedObject.ProfilePicture = _originImage;
        }


        void StartImageUpdating()
        {
            if (AssociatedObject == null)
                return;
            if (string.IsNullOrWhiteSpace(EmailAddress))
                return;

            if (_cancellation != null)
                _cancellation.Cancel();

            _cancellation = new CancellationTokenSource();

            // run on dispatcher
            _ = RequestAndSetAvatar(_cancellation.Token);
        }

        async Task RequestAndSetAvatar(CancellationToken cancellationToken)
        {
            string emailAddressHash = GetEmailSha256(EmailAddress);
            string fallback = Fallback switch
            {
                GravatarFallback.None => "404",
                _ => Fallback.ToString().ToLower(),
            };

            string uri = $"https://gravatar.com/avatar/{emailAddressHash}?d={fallback}";

            try
            {
                var response = await s_httpClient.GetAsync(uri, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;
                if (!response.IsSuccessStatusCode)
                    return;

                var imageStream = await response.Content.ReadAsStreamAsync();

                BitmapImage bitmapImage = new();
                await bitmapImage.SetSourceAsync(imageStream.AsRandomAccessStream());

                if (cancellationToken.IsCancellationRequested)
                    return;

                // it might be null here,,,
                if (AssociatedObject == null)
                    return;

                _originImage = AssociatedObject.ProfilePicture;
                _changedImage = true;

                AssociatedObject.ProfilePicture = bitmapImage;
            }
            catch (HttpRequestException)
            {
                // do nothing
            }
            catch (OperationCanceledException)
            {
                // do nothing
            }
            catch (Exception ex)
            {
                // TODO: log something here
            }
        }
    }
}
