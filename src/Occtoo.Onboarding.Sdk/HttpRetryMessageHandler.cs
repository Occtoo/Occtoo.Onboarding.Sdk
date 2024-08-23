using Polly;
using Polly.Extensions.Http;
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

#if NET8_0_OR_GREATER

        protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        {
            var policy = GetRetryPolicy();

            return policy.ExecuteAsync(() => base.SendAsync(request, cancellationToken));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // Handles HttpRequestException, 5xx and 408 responses
                .OrResult(msg => msg.StatusCode != System.Net.HttpStatusCode.OK)
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    6,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                  + TimeSpan.FromMilliseconds(RandomJitter.Next(0, 100))
                );
        }
#else
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
#endif
    }
}
