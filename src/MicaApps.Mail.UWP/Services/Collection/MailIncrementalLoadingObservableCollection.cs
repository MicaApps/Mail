using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using Mail.Models;
using Mail.Services.Data;
using Nito.AsyncEx;

namespace Mail.Services.Collection
{
    internal sealed class MailIncrementalLoadingObservableCollection : ObservableCollection<MailMessageListDetailViewModel>, ISupportIncrementalLoading
    {
        private readonly AsyncLock IncrementalLoadingLocker = new AsyncLock();
        private readonly Func<MailIncrementalLoadingObservableCollection, uint, CancellationToken, IAsyncEnumerable<MailMessageData>> FetchDataDelegate;

        public bool HasMoreItems => Count < TotalItemCount;

        public uint MinIncrementalLoadingStep { get; }

        public uint TotalItemCount { get; }

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
                    await foreach (MailMessageData Data in FetchDataDelegate(this, RequestedCount, CancelToken))
                    {
                        CancelToken.ThrowIfCancellationRequested();
                        Add(new MailMessageListDetailViewModel(Data));
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

        public MailIncrementalLoadingObservableCollection(Func<MailIncrementalLoadingObservableCollection, uint, CancellationToken, IAsyncEnumerable<MailMessageData>> FetchDataDelegate, uint TotalItemCount, uint MinIncrementalLoadingStep = 30)
        {
            this.TotalItemCount = TotalItemCount;
            this.MinIncrementalLoadingStep = MinIncrementalLoadingStep;
            this.FetchDataDelegate = FetchDataDelegate;
        }
    }
}