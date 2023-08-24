using Microsoft.Extensions.Configuration;
using Occtoo.Onboarding.Sdk.Models;

namespace Occtoo.Onboarding.Sdk.Tests
{
    public class OnboardingServiceClientTest
    {
        private readonly string dataProviderId;
        private readonly string dataProviderSecret;
        private readonly string dataSource = "nugetTester";
        private static readonly Random random = new Random();
        private readonly IConfiguration config;

        public OnboardingServiceClientTest()
        {
            var builder = new ConfigurationBuilder().AddUserSecrets<OnboardingServiceClientTest>();
            config = builder.Build();
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
                var response = await onboardingServliceClient.StartEntityImportAsync(dataSource, enties, null, null, cancelToken.Token);
            }
            catch (OperationCanceledException e)
            {
                Assert.Equal("The operation was canceled.", e.Message);
            }
        }

        [Fact]
        public async Task UploadImagesFromLinks()
        {
            var request = new List<FileUploadFromLink>
                {
                   new FileUploadFromLink(config["fileUrl1"], config["fileName1"], config["fileUniqueId1"]),
                   new FileUploadFromLink(config["fileUrl2"], config["fileName2"], config["fileUniqueId2"]),
                };
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var response = await onboardingServliceClient.UploadFromLinksAsync(request);
            Assert.False(response.Errors.Any());
        }

        [Fact]
        public async Task UploadImageFromLink()
        {
            var fileToUpload = new FileUploadFromLink(config["fileUrl1"], config["fileName1"], config["fileUniqueId1"]);
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var response = await onboardingServliceClient.UploadFromLinkAsync(fileToUpload);
            Console.WriteLine(response.Result.PublicUrl);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task GetImageById()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var response = await onboardingServliceClient.GetFileAsync(config["fileId"]);
            Console.WriteLine(response.Result.PublicUrl);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task GetImageByUniqueId()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var response = await onboardingServliceClient.GetFileFromUniqueIdAsync(config["fileUniqueId2"]);
            Console.WriteLine(response.Result.PublicUrl);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task GetImages()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var response = await onboardingServliceClient.GetFilesBatchAsync(
                new List<string> { config["fileUniqueId1"], config["fileUniqueId2"] }
            );
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task DeleteImage()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var getResponse = await onboardingServliceClient.GetFilesBatchAsync(
                new List<string> { config["fileUniqueId2"] }
            );
            var fileIdToDelete = getResponse.Result.Succeeded.First().Value.Id;
            var deleteResponse = await onboardingServliceClient.DeleteFileAsync(fileIdToDelete);
            Assert.Equal(204, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task TryingToDeleteImageThatDoesnotExist()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var fileIdToDelete = "foo";
            var deleteResponse = await onboardingServliceClient.DeleteFileAsync(fileIdToDelete);
            Assert.Equal(404, deleteResponse.StatusCode);
        }


        [Fact]
        public async Task UploadFileFromStream()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var httpClient = new HttpClient();
            var fileByteArray = await httpClient.GetByteArrayAsync("https://www.occtoo.com/hs-fs/hubfs/Petter.jpg?width=200&height=200&name=Petter.jpg");
            var metadata = new UploadMetadata(config["fileName2"], "image/jpeg", fileByteArray.Length, RandomString(4));
            var response = await onboardingServliceClient.UploadFileAsync(new MemoryStream(fileByteArray), metadata);
            Console.WriteLine(response.Result.PublicUrl);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task UploadFileThatAlreadyExistFromStream()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var httpClient = new HttpClient();
            var fileByteArray = await httpClient.GetByteArrayAsync("https://www.occtoo.com/hs-fs/hubfs/Petter.jpg?width=200&height=200&name=Petter.jpg");
            var metadata = new UploadMetadata(config["fileName2"], "image/jpeg", fileByteArray.Length, RandomString(4));
            var response = await onboardingServliceClient.UploadFileIfNotExistAsync(new MemoryStream(fileByteArray), metadata);
            Console.WriteLine(response.Result.PublicUrl);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task UploadFileFromStreamShouldGiveAlreadyExistError()
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var httpClient = new HttpClient();
            var fileByteArray = await httpClient.GetByteArrayAsync("https://www.occtoo.com/hs-fs/hubfs/Petter.jpg?width=200&height=200&name=Petter.jpg");
            var metadata = new UploadMetadata(config["fileName2"], "image/jpeg", fileByteArray.Length, config["fileUniqueId3"]);
            var response = await onboardingServliceClient.UploadFileAsync(new MemoryStream(fileByteArray), metadata);
            Assert.Equal(409, response.StatusCode);
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}