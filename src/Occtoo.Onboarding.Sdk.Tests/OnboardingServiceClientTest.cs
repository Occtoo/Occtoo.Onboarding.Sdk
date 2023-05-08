using CSharpFunctionalExtensions;
using Microsoft.Extensions.Configuration;
using Occtoo.Onboarding.Sdk.Models;

namespace Occtoo.Onboarding.Sdk.Tests
{
    public class OnboardingServiceClientTest
    {
        private readonly string dataProviderId;
        private readonly string dataProviderSecret;
        private readonly string dataSource = "nugetTester";

        public OnboardingServiceClientTest()
        {
            var builder = new ConfigurationBuilder().AddUserSecrets<OnboardingServiceClientTest>();
            var config = builder.Build();
            dataProviderId = config["providerid"];
            dataProviderSecret = config["providersecret"];
        }

        [Fact]
        public async Task GetToken()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var token = await onboardingServliceClient.GetTokenAsync();
            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task EntitiesShouldValidateAndOnboard()
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
            Assert.Equal(202, response.StatusCode);
        }

        [Fact]
        public async Task UploadingToSourceWithOutAccessShouldThrowError()
        {
            var enties = new List<DynamicEntity>
            {
                new DynamicEntity
                {
                    Key = Guid.NewGuid().ToString()
                },
                new DynamicEntity
                {
                    Key = Guid.NewGuid().ToString()
                }
            };

            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            try
            {
                var response = await onboardingServliceClient.StartEntityImportAsync("NotValidDataSource", enties);
            }
            catch (UnauthorizedAccessException ex)
            {
                Assert.EndsWith("Check your dataprovider details and datasource name", ex.Message);
            }
        }

        [Fact]
        public async Task UploadingWithWrongCredentialsThrowError()
        {
            var enties = new List<DynamicEntity>
            {
                new DynamicEntity
                {
                    Key = Guid.NewGuid().ToString()
                },
                new DynamicEntity
                {
                    Key = Guid.NewGuid().ToString()
                }
            };

            var onboardingServliceClient = new OnboardingServiceClient("test", dataProviderSecret);
            try
            {
                var response = await onboardingServliceClient.StartEntityImportAsync(dataSource, enties);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("Couldn't obtain a token please check your dataprovider details", ex.Message);
            }
        }

        [Fact]
        public async Task SendingEntitiesWithOutIdSet()
        {
            var enties = new List<DynamicEntity>
            {
                new DynamicEntity
                {
                    Key = string.Empty
                },
                new DynamicEntity
                {
                    Key = null
                }
            };

            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            try
            {
                var response = await onboardingServliceClient.StartEntityImportAsync(dataSource, enties);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("Entities must not have null or empty Key identifiers.", ex.Message);
            }
        }

        [Fact]
        public async Task UploadEntityBatchWithDuplicateIds()
        {
            var enties = new List<DynamicEntity>
            {
                new DynamicEntity
                {
                    Key = "3",
                    Properties= {new DynamicProperty { Id= "name", Value = "number three" }}
                },
                new DynamicEntity
                {
                    Key = "3",
                    Properties= {new DynamicProperty { Id= "name", Value = "number three" }}
                },
                 new DynamicEntity
                {
                    Key = "4",
                    Properties= {new DynamicProperty { Id= "name", Value = "number four" }}
                },
                new DynamicEntity
                {
                    Key = "4",
                    Properties= {new DynamicProperty { Id= "name", Value = "number four" }}
                }
            };

            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            try
            {
                var response = await onboardingServliceClient.StartEntityImportAsync(dataSource, enties);
            }
            catch (ArgumentException e)
            {
                Assert.Equal("Collection contains duplicate keys: 3,4.", e.Message);
            }
        }

        [Fact]
        public async Task UploadEntityBatchWithDuplicatePropertyIds()
        {
            var enties = new List<DynamicEntity>
            {
                new DynamicEntity
                {
                    Key = "3",
                    Properties= {
                        new DynamicProperty { Id= "name", Value = "number three" },
                        new DynamicProperty { Id= "name", Value = "number three" }
                    }
                },
                new DynamicEntity
                {
                    Key = "4",
                    Properties= {
                        new DynamicProperty { Id= "name", Value = "number four" },
                        new DynamicProperty { Id= "name", Value = "number four" }
                    }
                },
                 new DynamicEntity
                {
                    Key = "5",
                    Properties= {
                        new DynamicProperty { Id= "name", Value = "number five", Language = "sv" },
                        new DynamicProperty { Id= "name", Value = "number five", Language = "en" },
                    }
                }
            };

            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            try
            {
                var response = await onboardingServliceClient.StartEntityImportAsync(dataSource, enties);
            }
            catch (ArgumentException e)
            {
                Assert.Equal("Entities: 3,4 contain duplicated properties", e.Message);
            }
        }

        [Fact]
        public async Task CancelUpload()
        {
            var enties = new List<DynamicEntity>
            {
                new DynamicEntity
                {
                    Key = "3",
                    Properties= {
                        new DynamicProperty { Id= "name", Value = "number three" }
                    }
                },
                new DynamicEntity
                {
                    Key = "4",
                    Properties= {
                        new DynamicProperty { Id= "name", Value = "number four" }
                    }
                },
            };

            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            try
            {
                var cancelToken = new CancellationTokenSource();
                cancelToken.Cancel();
                var response = await onboardingServliceClient.StartEntityImportAsync(dataSource, enties, null, cancelToken.Token);
            }
            catch (OperationCanceledException e)
            {
                Assert.Equal("The operation was canceled.", e.Message);
            }
        }

        [Fact]
        public async Task UploadImagesFromLinks()
        {
            var request = new UploadLinksRequest(new List<FileUploadFromLink>
                {
                   new FileUploadFromLink("https://media.occtoo.com/53fa5a53-821b-446a-91cd-bf6e39a9c5e9/427e1546-e11d-54d5-84f7-e7a2d8a2d86e/310048005_a_keb_lite_trousers_fjaellraeven_1.jpg", "310048005_a_keb_lite_trousers_fjaellraeven_1.jpg")
                   {
                       UniqueIdentifier = "file1"
                   },
                   new FileUploadFromLink("https://media.occtoo.com/53fa5a53-821b-446a-91cd-bf6e39a9c5e9/0009bf6d-9b44-5e91-8884-d739a2960d91/360_285482002_ay_anjan_3_gt_hilleberg.jpg", "360_285482002_ay_anjan_3_gt_hilleberg.jpg")
                   {
                        UniqueIdentifier = "file2"
                   }
                }
            );
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var response = await onboardingServliceClient.UploadFromLinksAsync(request);
            Assert.False(response.Errors.Any());
        }

        [Fact]
        public async Task GetImage()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var response = await onboardingServliceClient.GetFileAsync("5f639717-c581-4924-82e0-434b006fe149");
            Console.WriteLine(response.Result.PublicUrl);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task GetImages()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var response = await onboardingServliceClient.GetFilesBatchAsync(new GetMediaByUniqueIdentifiers {
                UniqueIdentifiers = new List<string> { "file1", "file2" }
            });
            Console.WriteLine(response.RequestId);
            Assert.Equal(200, response.StatusCode);
        }
    }
}