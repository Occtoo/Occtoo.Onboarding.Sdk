using System.Net.Http;
using System.Reflection;

namespace Occtoo.Onboarding.Sdk.Tests
{
    // Unlike the rest of the suite these need no credentials and make no network calls: constructing a
    // client only reads its arguments and takes an HttpClient out of the pool.
    public class RequestTimeoutTest
    {
        private const string AnyId = "id";
        private const string AnySecret = "secret";

        [Fact]
        public void DefaultRequestTimeoutIsFiveMinutes()
        {
            Assert.Equal(TimeSpan.FromMinutes(5), OnboardingServiceClient.DefaultRequestTimeout);
        }

        [Fact]
        public void ClientUsesTheDefaultTimeoutWhenNoneIsGiven()
        {
            using var client = new OnboardingServiceClient(AnyId, AnySecret);

            Assert.Equal(OnboardingServiceClient.DefaultRequestTimeout, HttpClientOf(client).Timeout);
        }

        [Fact]
        public void ClientUsesTheTimeoutItWasGiven()
        {
            var timeout = TimeSpan.FromMinutes(30);

            using var client = new OnboardingServiceClient(AnyId, AnySecret, timeout);

            Assert.Equal(timeout, HttpClientOf(client).Timeout);
        }

        [Fact]
        public void AnInfiniteTimeoutIsAllowed()
        {
            using var client = new OnboardingServiceClient(AnyId, AnySecret, Timeout.InfiniteTimeSpan);

            Assert.Equal(Timeout.InfiniteTimeSpan, HttpClientOf(client).Timeout);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ATimeoutThatIsNotPositiveIsRejected(int seconds)
        {
            // Rejected at construction rather than surfacing from whichever request happens to run first.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new OnboardingServiceClient(AnyId, AnySecret, TimeSpan.FromSeconds(seconds)));
        }

        [Fact]
        public void ClientsWantingTheSameTimeoutShareOneHttpClient()
        {
            // A client per instance exhausts sockets, so the pool has to hand the same HttpClient to every
            // instance asking for the same deadline. Nothing public reveals which one an instance received.
            using var first = new OnboardingServiceClient(AnyId, AnySecret, TimeSpan.FromMinutes(7));
            using var second = new OnboardingServiceClient("other", "other", TimeSpan.FromMinutes(7));

            Assert.Same(HttpClientOf(first), HttpClientOf(second));
        }

        [Fact]
        public void ClientsWantingDifferentTimeoutsGetDifferentHttpClients()
        {
            using var first = new OnboardingServiceClient(AnyId, AnySecret, TimeSpan.FromMinutes(8));
            using var second = new OnboardingServiceClient(AnyId, AnySecret, TimeSpan.FromMinutes(9));

            Assert.NotSame(HttpClientOf(first), HttpClientOf(second));
        }

        [Fact]
        public void DisposingOneClientLeavesTheSharedHttpClientUsable()
        {
            var timeout = TimeSpan.FromMinutes(11);
            var abandoned = new OnboardingServiceClient(AnyId, AnySecret, timeout);
            using var survivor = new OnboardingServiceClient(AnyId, AnySecret, timeout);

            abandoned.Dispose();

            // Assigning Timeout throws ObjectDisposedException on a disposed HttpClient, and this one has
            // not sent a request yet, so it is a cheap way to prove the shared client is still alive.
            Assert.Null(Record.Exception(() => HttpClientOf(survivor).Timeout = timeout));
        }

        [Fact]
        public void DisposeIsIdempotent()
        {
            var client = new OnboardingServiceClient(AnyId, AnySecret);

            client.Dispose();

            Assert.Null(Record.Exception(() => client.Dispose()));
        }

        private static HttpClient HttpClientOf(OnboardingServiceClient client)
        {
            var field = typeof(OnboardingServiceClient)
                .GetField("httpClient", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(field);
            return (HttpClient)field!.GetValue(client)!;
        }
    }
}
