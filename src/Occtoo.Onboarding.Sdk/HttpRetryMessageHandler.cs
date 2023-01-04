using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Occtoo.Onboarding.Sdk
{
    internal class HttpRetryMessageHandler : DelegatingHandler
    {
        private static readonly Random RandomJitter = new Random();

        private static readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(15.0);

        public HttpRetryMessageHandler(HttpClientHandler handler)
            : base(handler)
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return HttpPolicyExtensions.HandleTransientHttpError().Or<TimeoutRejectedException>().WaitAndRetryAsync(6, delegate (int retryAttempt)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Pow(2.0, retryAttempt)) + TimeSpan.FromMilliseconds(RandomJitter.Next(0, 100));
                return (!(timeSpan < MaxWaitTime)) ? MaxWaitTime : timeSpan;
            })
                .ExecuteAsync(() => base.SendAsync(request, cancellationToken));
        }
    }
}
