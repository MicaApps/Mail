using Mail.Services.Data;
using Nito.AsyncEx;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace Mail.Services.Collection
{
    internal sealed class MailIncrementalLoadingObservableCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        private readonly MailFolderDetailData MailFolder;
        private readonly IMailService Service;
        private readonly Func<MailMessageData, T> Transformer;
        private readonly AsyncLock IncrementalLoadingLocker = new AsyncLock();

        public bool HasMoreItems => Count < MailFolder.MessageCount;

        public uint MinIncrementalLoadingStep { get; }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            return AsyncInfo.Run((CancelToken) => LoadMoreItemsAsync(CancelToken, count));
        }

        private async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken CancelToken, uint RequestedCount)
        {
            uint LoadCounter = 0;

            try
            {
                using (await IncrementalLoadingLocker.LockAsync(CancelToken))
                {
                    await foreach (MailMessageData Data in Service.GetMailMessageAsync(MailFolder.Id, (uint)Count, Math.Max(MinIncrementalLoadingStep, RequestedCount), CancelToken))
                    {
                        CancelToken.ThrowIfCancellationRequested();
                        Add(Transformer(Data));
                        LoadCounter++;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //No need to handle this exception
            }
            catch (Exception)
            {
                //Handle exception here
            }

            return new LoadMoreItemsResult { Count = LoadCounter };
        }

        public MailIncrementalLoadingObservableCollection(IMailService Service, MailFolderDetailData MailFolder, Func<MailMessageData, T> Transformer, uint MinIncrementalLoadingStep = 30)
        {
            this.Service = Service;
            this.MailFolder = MailFolder;
            this.Transformer = Transformer;
            this.MinIncrementalLoadingStep = MinIncrementalLoadingStep;
        }
    }
}
