using System.Threading.Tasks;

namespace Mail.Servives.Interface
{
    internal interface IMailService
    {
        public bool IsSupported { get; }

        public bool IsSignIn { get; }

        public Task<bool> InitSeriviceAsync();
    }
}
