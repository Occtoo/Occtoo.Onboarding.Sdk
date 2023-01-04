# Introduction 
Wrapped httpclient used to create a [Nuget package](https://www.nuget.org/packages/Occtoo.Onboarding.Sdk) for calling onboarding.



# Gettings started with Onboarding Service Client
* Open your solution and add the package through [nuget](https://www.nuget.org/packages/Occtoo.Onboarding.Sdk). 
* Create a instance of the OnboardingServiceClient and provide your Provider-Id and -Secret.

    (Follow [these steps](https://docs.occtoo.com/docs/get-started/provide-data#12-create-data-provider) to setup your Provider, if you do not have one.)
* Call one of the four overloads of StartEntityImportAsync

## Quick Start Example
```cs
private readonly string dataProviderId = config["providerid"];
private readonly string dataProviderSecret= config["providersecret"];

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
