using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mail.Servives
{
    internal interface IMailService
    {
        Task<bool> InitSerivice();

        bool IsSupported();

        bool IsSignIn();

    }
}
