using CSharpFunctionalExtensions;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Occtoo.Onboarding.Sdk
{
    public interface IOnboardingServiceClient
    {
        //Asynchronous
        Task<StartImportResponse> StartEntityImportAsync(string dataSource, IReadOnlyList<DynamicEntity> entities, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        Task<StartImportResponse> StartEntityImportAsync(string dataSource, IReadOnlyList<DynamicEntity> entities, string token, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        Task<string> GetTokenAsync(CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> GetFileAsync(string id, CancellationToken? cancellationToken = null);
        Task<ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>>> GetFilesBatchAsync(GetMediaByUniqueIdentifiers identifiers, CancellationToken? cancellationToken = null);
        Task<ApiResult<PartialSuccessResponse<string, UploadDto, UploadCreateError>>> UploadFromLinksAsync(UploadLinksRequest request, CancellationToken? cancellationToken = null);
        Task<ApiResult<UploadDto>> GetUploadStatusAsync(string uploadId, CancellationToken? cancellationToken = null);
        Task<ApiResult> DeleteFileAsync(string fileId, CancellationToken? cancellationToken = null);

        //Synchronous
        StartImportResponse StartEntityImport(string dataSource, IReadOnlyList<DynamicEntity> entities, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        StartImportResponse StartEntityImport(string dataSource, IReadOnlyList<DynamicEntity> entities, string token, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        string GetToken(CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> GetFile(string id, CancellationToken? cancellationToken = null);
        ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>> GetFilesBatch(GetMediaByUniqueIdentifiers identifiers, CancellationToken? cancellationToken = null);
        ApiResult<PartialSuccessResponse<string, UploadDto, UploadCreateError>> UploadFromLinks(UploadLinksRequest request, CancellationToken? cancellationToken = null);
        ApiResult<UploadDto> GetUploadStatus(string uploadId, CancellationToken? cancellationToken = null);
        ApiResult DeleteFile(string fileId, CancellationToken? cancellationToken = null);
    }

    public class OnboardingServiceClient : IOnboardingServiceClient, IDisposable
    {
        private static readonly HttpClient httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://ingest.occtoo.com")
        };
        private readonly string cachekey = "token";
        private readonly string dataProviderId;
        private readonly string dataProviderSecret;
        private readonly IMemoryCache cache;

        public OnboardingServiceClient(string dataProviderId, string dataProviderSecret)
        {
            this.dataProviderId = dataProviderId;
            this.dataProviderSecret = dataProviderSecret;
            cache = new MemoryCache(new MemoryCacheOptions());
        }

        public StartImportResponse StartEntityImport(string dataSource, IReadOnlyList<DynamicEntity> entities, Guid? correlationId = null, CancellationToken? cancellationToken = null)
        {
            return StartEntityImportAsync(dataSource, entities, correlationId, cancellationToken).GetAwaiter().GetResult();
        }
        public StartImportResponse StartEntityImport(string dataSource, IReadOnlyList<DynamicEntity> entities, string token, Guid? correlationId = null, CancellationToken? cancellationToken = null)
        {
            return StartEntityImportAsync(dataSource, entities, token, correlationId, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<StartImportResponse> StartEntityImportAsync(string dataSource, IReadOnlyList<DynamicEntity> entities, Guid? correlationId = null, CancellationToken? cancellationToken = null)
        {
            var validEntities = ValidateParametes(dataSource, entities, cancellationToken);
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            var response = await EntityImportAsync(dataSource, validEntities, token, valueOrDefaultCancelToken, correlationId);

            return response;
        }

        public async Task<StartImportResponse> StartEntityImportAsync(string dataSource, IReadOnlyList<DynamicEntity> entities, string token, Guid? correlationId = null, CancellationToken? cancellationToken = null)
        {
            var validEntities = ValidateParametes(dataSource, entities, cancellationToken);
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var response = await EntityImportAsync(dataSource, validEntities, token, valueOrDefaultCancelToken, correlationId);

            return response;
        }

        public string GetToken(CancellationToken? cancellationToken = null)
        { 
            return GetTokenAsync(cancellationToken).GetAwaiter().GetResult();
        }
        public async Task<string> GetTokenAsync(CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "dataProviders/tokens")
            {
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    id = dataProviderId,
                    secret = dataProviderSecret
                }), Encoding.UTF8, "application/json")
            };
            var tokenResponse = await httpClient.SendAsync(tokenRequest, valueOrDefaultCancelToken);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new ArgumentException("Couldn't obtain a token please check your dataprovider details");
            }

            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenDocument = JsonConvert.DeserializeObject<TokenResponse>(tokenResponseContent);
            return tokenDocument.result.accessToken;
        }

        public ApiResult<MediaFileDto> GetFile(string id, CancellationToken? cancellationToken = null)
        {
            return GetFileAsync(id, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<ApiResult<MediaFileDto>> GetFileAsync(string id, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            var message = new HttpRequestMessage(HttpMethod.Get, $"media/files/{id}")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
                }
            };
            var response = await httpClient.SendAsync(message, valueOrDefaultCancelToken);
            var apiResult = JsonConvert.DeserializeObject<ApiResult<MediaFileDto>>(await response.Content.ReadAsStringAsync());
            apiResult.StatusCode = (int)response.StatusCode;
            return apiResult;
        }

        public ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>> GetFilesBatch(GetMediaByUniqueIdentifiers uniqueIdentifiers, CancellationToken? cancellationToken = null)
        {
            return GetFilesBatchAsync(uniqueIdentifiers, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>>> GetFilesBatchAsync(GetMediaByUniqueIdentifiers uniqueIdentifiers, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            var message = new HttpRequestMessage(HttpMethod.Post, "media/files/batch")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token}" },
                },
                Content = new StringContent(JsonConvert.SerializeObject(uniqueIdentifiers), Encoding.UTF8, "application/json")
            };
            var response = await httpClient.SendAsync(message, valueOrDefaultCancelToken);
            var apiResult = JsonConvert.DeserializeObject<ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>>>(await response.Content.ReadAsStringAsync());
            apiResult.StatusCode = (int)response.StatusCode;
            return apiResult;
        }

        public ApiResult<PartialSuccessResponse<string, UploadDto, UploadCreateError>> UploadFromLinks(UploadLinksRequest request, CancellationToken? cancellationToken = null)
        {
            return UploadFromLinksAsync(request, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initiates asynchronous upload of files using URL to them. 
        /// Since the upload is asynchronous the client should periodiacally 
        /// check it's state using GetUploadStatusAsync method.
        /// Will skip file if UniqueIdentifier on the file already exists.
        /// </summary>
        /// <param name="request">List of links to upload</param>
        /// <param name="cancellationToken">Own cancellation token can be provided</param>
        /// <returns></returns>
        public async Task<ApiResult<PartialSuccessResponse<string, UploadDto, UploadCreateError>>> UploadFromLinksAsync(UploadLinksRequest request, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            var message = new HttpRequestMessage(HttpMethod.Put, "media/uploads/links")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token}" },
                },
                Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
            };
            var response = await httpClient.SendAsync(message, valueOrDefaultCancelToken);
            var apiResult = JsonConvert.DeserializeObject<ApiResult<PartialSuccessResponse<string, UploadDto, UploadCreateError>>>(await response.Content.ReadAsStringAsync());
            apiResult.StatusCode = (int)response.StatusCode;
            return apiResult;
        }

        public ApiResult<UploadDto> GetUploadStatus(string uploadId, CancellationToken? cancellationToken = null)
        {
            return GetUploadStatusAsync(uploadId, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the upload information and state using the upload id
        /// </summary>
        /// <param name="uploadId">Id of the upload to check</param>
        /// <param name="cancellationToken">Own cancellation token can be provided</param>
        /// <returns></returns>
        public async Task<ApiResult<UploadDto>> GetUploadStatusAsync(string uploadId, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            var message = new HttpRequestMessage(HttpMethod.Get, $"media/uploads/{uploadId}")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
                }
            };
            var response = await httpClient.SendAsync(message, valueOrDefaultCancelToken);
            var apiResult = JsonConvert.DeserializeObject<ApiResult<UploadDto>>(await response.Content.ReadAsStringAsync());
            apiResult.StatusCode = (int)response.StatusCode;
            return apiResult;
        }

        public ApiResult DeleteFile(string fileId, CancellationToken? cancellationToken = null)
        {
            return DeleteFileAsync(fileId, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<ApiResult> DeleteFileAsync(string fileId, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            var message = new HttpRequestMessage(HttpMethod.Delete, $"media/files/{fileId}")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
                }
            };
            var response = await httpClient.SendAsync(message, valueOrDefaultCancelToken);
            if (response.IsSuccessStatusCode)
            {
                return new ApiResult
                {
                    StatusCode = (int)response.StatusCode
                };
            }
            else
            {
                var apiResult = JsonConvert.DeserializeObject<ApiResult>(await response.Content.ReadAsStringAsync());
                apiResult.StatusCode = (int)response.StatusCode;
                return apiResult;
            }
        }

        private static async Task<StartImportResponse> EntityImportAsync(string dataSource, IEnumerable<DynamicEntity> validEntities, string token, CancellationToken cancellationToken, Guid? correlationId = null)
        {
            string requestUri = $"import/{dataSource}";
            if (correlationId.HasValue && correlationId != default(Guid))
            {
                requestUri += $"?correlationId={correlationId.Value}";
            }

            var ingestRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            ingestRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            ingestRequest.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                Entities = validEntities
            }), Encoding.UTF8, "application/json");
            var ingestResponse = await httpClient.SendAsync(ingestRequest, cancellationToken);
            if (!ingestResponse.IsSuccessStatusCode)
            {
                switch (ingestResponse.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        throw new UnauthorizedAccessException(ingestResponse.ReasonPhrase + ". Check your dataprovider details and datasource name");
                }
            }

            var responseContent = await ingestResponse.Content.ReadAsStringAsync();
            var response = new StartImportResponse
            {
                Result = JsonConvert.DeserializeObject<ImportBatchResult>(responseContent),
                StatusCode = (int)ingestResponse.StatusCode,
                Message = ingestResponse.ReasonPhrase
            };

            return response;
        }

        private async Task<string> GetTokenThroughCache(CancellationToken cancellationToken)
        {
            if (cache.TryGetValue<string>(cachekey, out var token))
            {
                return token;
            }

            token = await GetTokenAsync(cancellationToken);
            cache.Set(cachekey, token, DateTime.UtcNow.AddMinutes(59)); // Store data in the cache for 59 minutes from now
            return token;
        }

        private static IEnumerable<DynamicEntity> ValidateParametes(string dataSource, IReadOnlyList<DynamicEntity> entities, CancellationToken? cancellationToken)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            if (string.IsNullOrWhiteSpace(dataSource))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(dataSource));
            }

            cancellationToken?.ThrowIfCancellationRequested();
            var validEntities = ValidatePayload(entities);
            cancellationToken?.ThrowIfCancellationRequested();
            return validEntities;
        }

        private static IEnumerable<DynamicEntity> ValidatePayload(IEnumerable<DynamicEntity> entities)
        {
            var validEntitites = entities.Where(e => e != null).Select(e =>
            {
                if (e.Properties == null)
                {
                    e.Properties = new List<DynamicProperty>();
                }

                return e;
            });

            if (validEntitites.Any(e => string.IsNullOrWhiteSpace(e.Key)))
            {
                throw new ArgumentException("Entities must not have null or empty Key identifiers.");
            }

            var groupedEntites = validEntitites.GroupBy(e => e.Key).Where(k => k.Count() > 1);
            if (groupedEntites.Any())
            {
                throw new ArgumentException($"Collection contains duplicate keys: {string.Join(",", groupedEntites.Select(e => e.Key))}.");
            }

            var groupedEntitiesOnProperties = validEntitites.Where(e => e.Properties.GroupBy(p => (p.Id, p.Language)).Any(g => g.Count() > 1));
            if (groupedEntitiesOnProperties.Any())
            {
                throw new ArgumentException($"Entities: {string.Join(",", groupedEntitiesOnProperties.Select(e => e.Key))} contain duplicated properties");
            }

            return validEntitites;
        }

        #region TUS, open protocol for resumable file uploads https://tus.io/

        public async Task<HttpResponseMessage> CreateFileAsync(int contentLength, string metadata, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            var message = new HttpRequestMessage(HttpMethod.Post, "media/uploads/files")
            {
                Headers =
                {
                    {"Authorization", $"Bearer {token}" },
                    {"Tus-Resumable", "1.0.0"},
                    {"Upload-Length", contentLength.ToString()},
                    {"Upload-Metadata", metadata},
                    {"Upload-Offset", "0"}
                }
            };
            return await httpClient.SendAsync(message, valueOrDefaultCancelToken);
        }

        public async Task<HttpResponseMessage> PatchFileAsync(string fileId, int bufferLength, long currentOffset, MemoryStream memoryStream, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            var message = new HttpRequestMessage(new HttpMethod("patch"), $"media/uploads/files/{fileId}")
            {
                Headers =
                {
                    {"Authorization", $"Bearer {token}" },
                    {"Tus-Resumable", "1.0.0"},
                    {"Upload-Offset", currentOffset.ToString()},
                },
                Content = new StreamContent(memoryStream, bufferLength)
                {
                    Headers = { { "Content-Type", "application/offset+octet-stream" } }
                }
            };
            return await httpClient.SendAsync(message, valueOrDefaultCancelToken);
        }

        public async Task<Result<long, Error>> GetCurrentOffset(string fileId, CancellationToken? cancellationToken = null)
        {
            var response = await GetOffset(fileId, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new Error("Not found");
            if (!response.Headers.TryGetValues("Upload-Offset", out var offset))
                return 0;
            return Int64.Parse(offset.Single());
        }



        private async Task<HttpResponseMessage> GetOffset(string fileId, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            var message = new HttpRequestMessage(HttpMethod.Head, $"media/uploads/files/{fileId}")
            {
                Headers =
                {
                    {"Authorization", $"Bearer {token}" },
                    {"Tus-Resumable", "1.0.0"}
                }
            };
            return await httpClient.SendAsync(message, valueOrDefaultCancelToken);
        }
        #endregion

        public void Dispose() => httpClient?.Dispose();
    }
}