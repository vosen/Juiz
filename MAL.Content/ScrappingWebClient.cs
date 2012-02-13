using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Vosen.MAL.Content
{
    class ScrappingWebClient : WebClient
    {

        public ScrappingWebClient()
            : base()
        {
            Proxy = null;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest req = (HttpWebRequest)base.GetWebRequest(address);
            req.ServicePoint.ConnectionLimit = Int32.MaxValue;
            req.UserAgent = "Mozilla/5.0 (X11; Linux i686; rv:9.0.1) Gecko/20100101 Firefox/9.0.1";
            return (WebRequest)req;
        }

        public Task<string> DownloadStringTask(string address)
        {
            return DownloadStringTask(new Uri(address));
        }

        /// <summary>Downloads the resource with the specified URI as a string, asynchronously.</summary>
        /// <param name="webClient">The WebClient.</param>
        /// <param name="address">The URI from which to download data.</param>
        /// <returns>A Task that contains the downloaded string.</returns>
        public Task<string> DownloadStringTask(Uri address)
        {
            // Create the task to be returned
            var tcs = new TaskCompletionSource<string>(address);

            // Setup the callback event handler
            DownloadStringCompletedEventHandler handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, () => e.Result, () => DownloadStringCompleted -= handler);
            DownloadStringCompleted += handler;

            // Start the async work
            try
            {
                DownloadStringAsync(address, tcs);
            }
            catch (Exception exc)
            {
                // If something goes wrong kicking off the async work,
                // unregister the callback and cancel the created task
                DownloadStringCompleted -= handler;
                tcs.TrySetException(exc);
            }

            // Return the task that represents the async operation
            return tcs.Task;
        }

        internal static void HandleCompletion<T>(TaskCompletionSource<T> tcs, AsyncCompletedEventArgs e, Func<T> getResult, Action unregisterHandler)
        {
            // Transfers the results from the AsyncCompletedEventArgs and getResult() to the
            // TaskCompletionSource, but only AsyncCompletedEventArg's UserState matches the TCS
            // (this check is important if the same WebClient is used for multiple, asynchronous
            // operations concurrently).  Also unregisters the handler to avoid a leak.
            if (e.UserState == tcs)
            {
                if (e.Cancelled) tcs.TrySetCanceled();
                else if (e.Error != null) tcs.TrySetException(e.Error);
                else tcs.TrySetResult(getResult());
                unregisterHandler();
            }
        }
    }
}
