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
    internal sealed class MailIncrementalLoadingObservableCollection :
        ObservableCollection<MailMessageListDetailViewModel>,
        ISupportIncrementalLoading
    {
        private readonly MailFolderData MailFolder;
        private readonly Func<uint, CancellationToken, IAsyncEnumerable<MailMessageData>> FetchDataSet;
        private readonly AsyncLock IncrementalLoadingLocker = new AsyncLock();
        public bool HasMoreItems => Count < MailFolder.TotalItemCount;

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
                    await foreach (var data in FetchDataSet(RequestedCount, CancelToken))
                    {
                        CancelToken.ThrowIfCancellationRequested();
                        Add(new MailMessageListDetailViewModel(data));
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

        public MailIncrementalLoadingObservableCollection(IMailService Service,
            MailFolderData MailFolder, uint MinIncrementalLoadingStep = 30,
            bool IsFocusTab = false)
        {
            this.MailFolder = MailFolder;
            this.MinIncrementalLoadingStep = MinIncrementalLoadingStep;
            FetchDataSet = (RequestCount, Token) =>
            {
                var option = new LoadMailMessageOption(MailFolder.Id, IsFocusTab)
                {
                    StartIndex = Count,
                    LoadCount = (int)Math.Max(MinIncrementalLoadingStep, RequestCount)
                };
                return IsFocusTab && Service is IMailService.IFocusFilterSupport filterSupport
                    ? filterSupport.GetMailMessageAsync(option, CancelToken: Token)
                    : Service.GetMailMessageAsync(option, CancelToken: Token);
            };
        }
    }
}