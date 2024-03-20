using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using Nito.AsyncEx;
using Mail.ViewModels;

namespace Mail.Models.Collections
{
    internal sealed class MailIncrementalLoadingObservableCollection : ObservableCollection<MailMessageListDetailViewModel>, ISupportIncrementalLoading
    {
        private readonly AsyncLock IncrementalLoadingLocker = new AsyncLock();
        private readonly Func<MailIncrementalLoadingObservableCollection, uint, CancellationToken, IAsyncEnumerable<MailMessage>> FetchDataDelegate;

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
                    await foreach (MailMessage Data in FetchDataDelegate(this, RequestedCount, CancelToken))
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

        public MailIncrementalLoadingObservableCollection(Func<MailIncrementalLoadingObservableCollection, uint, CancellationToken, IAsyncEnumerable<MailMessage>> FetchDataDelegate, uint TotalItemCount, uint MinIncrementalLoadingStep = 30)
        {
            this.TotalItemCount = TotalItemCount;
            this.MinIncrementalLoadingStep = MinIncrementalLoadingStep;
            this.FetchDataDelegate = FetchDataDelegate;
        }
    }
}