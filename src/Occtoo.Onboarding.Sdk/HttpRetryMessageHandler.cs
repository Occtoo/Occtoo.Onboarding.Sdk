using Polly;
using Polly.Extensions.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Occtoo.Onboarding.Sdk
{
    internal class HttpRetryMessageHandler : DelegatingHandler
    {
        private const int MaxRetries = 3;

        // HttpStatusCode.TooManyRequests does not exist in netstandard2.0, so name 429 here rather than
        // leaving a bare cast at the use site.
        private const HttpStatusCode TooManyRequests = (HttpStatusCode)429;
        private static readonly Random RandomJitter = new Random();

        public HttpRetryMessageHandler(HttpMessageHandler handler) : base(handler) { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // The first attempt consumes the request content stream, so a retry that reuses the same
            // HttpRequestMessage resends an empty body - for a media upload chunk that means a silently
            // corrupt file. Buffer the body once and give every attempt its own copy of the request.
            var body = request.Content == null
                ? null
                : await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            return await RetryPolicy
                .ExecuteAsync(ct => base.SendAsync(Clone(request, body), ct), cancellationToken)
                .ConfigureAwait(false);
        }

        // Only transient failures are worth another attempt. Retrying every non-success status meant a 400
        // or a 409 was sent four times before being returned, and when the shared HttpClient deadline ran
        // out mid-sequence the originating status was replaced by an unattributable "A task was canceled.".
        //
        // Cancellation is deliberately not retried: there is no per-attempt timeout here, so a
        // TaskCanceledException means either the caller cancelled or the overall deadline has already
        // passed. In both cases every further attempt is doomed and the backoff only delays the error.
        private static readonly IAsyncPolicy<HttpResponseMessage> RetryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError() // HttpRequestException, 5xx and 408
            .OrResult(response => response.StatusCode == TooManyRequests)
            .WaitAndRetryAsync(
                MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    + TimeSpan.FromMilliseconds(RandomJitter.Next(0, 100)),
                (outcome, delay, retryAttempt, context) =>
                {
                    // The response being discarded holds its connection until it is disposed, which on
                    // .NET Framework starves the very pool the next attempt has to draw from.
                    outcome.Result?.Dispose();
                });

        private static HttpRequestMessage Clone(HttpRequestMessage request, byte[] body)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version
            };

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (body != null)
            {
                clone.Content = new ByteArrayContent(body);

                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }
    }
}
