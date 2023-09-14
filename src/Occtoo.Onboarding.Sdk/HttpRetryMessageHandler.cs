using Polly;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Occtoo.Onboarding.Sdk
{
    internal class HttpRetryMessageHandler : DelegatingHandler
    {
        private static readonly Random RandomJitter = new Random();
        public HttpRetryMessageHandler(HttpMessageHandler handler) : base(handler) { }

        protected override Task<HttpResponseMessage> SendAsync(
           HttpRequestMessage request,
           CancellationToken cancellationToken) =>
           Policy
               .Handle<HttpRequestException>()
               .Or<TaskCanceledException>()
               .OrResult<HttpResponseMessage>(x => !x.IsSuccessStatusCode)
               .WaitAndRetryAsync(
                   3,
                   retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                     + TimeSpan.FromMilliseconds(RandomJitter.Next(0, 100)))
               .ExecuteAsync(() => base.SendAsync(request, cancellationToken));
    }
}
