# OnboardingServiceClient 
Wrapped [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=net-8.0) used to call [onboard data to Occtoo](https://docs.occtoo.com/docs/get-started/provide-data).



# Gettings started with Onboarding Service Client
* Open your solution and add the package through nuget.
* Create a instance of the OnboardingServiceClient and provide your Provider-Id and -Secret.

    (Follow [these steps](https://docs.occtoo.com/docs/get-started/provide-data#12-create-data-provider) to setup your Provider, if you do not have one.)
* Call one of the four overloads of StartEntityImportAsync

## Quick Start Example
```cs
private readonly string dataProviderId = config["providerid"];
private readonly string dataProviderSecret= config["providersecret"];
private readonly string dataSource = "MyFirstOcctoDataSource";

static async Task Main(string[] args)
{
    var enties = new List<DynamicEntity>
    {
        new DynamicEntity
        {
            Key = "1",
            Properties= {new DynamicProperty { Id= "name", Value = "number one" }}
        },
        new DynamicEntity
        {
            Key = "2",
            Properties= {new DynamicProperty { Id= "name", Value = "number two" }}
        }
    };

    var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
    var response = await onboardingServliceClient.StartEntityImportAsync(dataSource, enties);
    if(response.StatusCode == 202)
    {
        // Data was onboarded!
    }
}
```

## Request timeout
Every call has a deadline covering the whole request, retries included. It defaults to
`OnboardingServiceClient.DefaultRequestTimeout`, which is 5 minutes - generous enough for a large media
upload over a constrained connection pool.

Pass your own if that does not suit your workload:

```cs
var onboardingServliceClient = new OnboardingServiceClient(
    dataProviderId,
    dataProviderSecret,
    TimeSpan.FromMinutes(30));
```

Clients asking for the same timeout share one underlying `HttpClient`, so creating several is cheap.
Pass `Timeout.InfiniteTimeSpan` to drop the deadline altogether and control it through the
`cancellationToken` parameter instead.

> **Uploading from .NET Framework?** `ServicePointManager` caps outbound connections at two per host by
> default, and time spent waiting for a free connection counts against the deadline. Raise
> `ServicePointManager.DefaultConnectionLimit` if you upload files concurrently.

[Code repository on github](https://github.com/Occtoo/Occtoo.Onboarding.Sdk)

## Release Notes 2.0.2
Bugfix for GetFileFromUniqueIdAsync to return 404 instead of 202 when no file found.