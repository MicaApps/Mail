using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mail.Services;
using Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace MicaApps.Mail.PageModels
{
    internal class LoginPageModel : ObservableObject
    {
        private string errorMessage;
        private bool errorMessageIsOpen;

        public ICommand OutlookLoginCommand { get; set; }
        public string ErrorMessage { get => errorMessage; set => SetProperty(ref errorMessage, value); }
        public bool ErrorMessageIsOpen { get => errorMessageIsOpen; set => SetProperty(ref errorMessageIsOpen,value); }

        public LoginPageModel() 
        {
            OutlookLoginCommand = new AsyncRelayCommand(OutlookLogin);
        }

        private async Task OutlookLogin()
        {
            try
            {
                await App.Services.GetService<OutlookService>().SignInAsync();
            }
            catch (Exception ex)
            {
                ErrorMessageIsOpen = true;
                ErrorMessage = ex.Message;
            }
        }
    }
}
