﻿using CSharpFunctionalExtensions;
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
using System.Reactive.Linq;
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
        Task<ApiResult<MediaFileDto>> UploadFileAsync(Stream content, UploadMetadata metadata, CancellationToken? cancellationToken = null);
        Task<ApiResult<UploadDto>> GetUploadStatusAsync(string uploadId, CancellationToken? cancellationToken = null);
        Task<ApiResult> DeleteFileAsync(string fileId, CancellationToken? cancellationToken = null);

        //Synchronous
        StartImportResponse StartEntityImport(string dataSource, IReadOnlyList<DynamicEntity> entities, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        StartImportResponse StartEntityImport(string dataSource, IReadOnlyList<DynamicEntity> entities, string token, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        string GetToken(CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> GetFile(string id, CancellationToken? cancellationToken = null);
        ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>> GetFilesBatch(GetMediaByUniqueIdentifiers identifiers, CancellationToken? cancellationToken = null);
        ApiResult<PartialSuccessResponse<string, UploadDto, UploadCreateError>> UploadFromLinks(UploadLinksRequest request, CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> UploadFile(Stream content, UploadMetadata metadata, CancellationToken? cancellationToken = null);
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
            return await GetApiResultFromResponse<MediaFileDto>(response);
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
            return await GetApiResultFromResponse<PartialSuccessResponse<string, MediaFileDto, Error>>(response); ;
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
            return await GetApiResultFromResponse<PartialSuccessResponse<string, UploadDto, UploadCreateError>>(response);
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
            return await GetApiResultFromResponse<UploadDto>(response);
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

        public ApiResult<MediaFileDto> UploadFile(Stream content, UploadMetadata metadata, CancellationToken? cancellationToken = null)
        {
            return UploadFileAsync(content, metadata, cancellationToken).GetAwaiter().GetResult();
        }
       
        public async Task<ApiResult<MediaFileDto>> UploadFileAsync(Stream content, UploadMetadata metadata, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var fileResponse = await CreateFileAsync((int)metadata.Size, UploadMetadata.Serialize(metadata).Value, cancellationToken);
            if (!fileResponse.IsSuccessStatusCode)
            {
                return new ApiResult<MediaFileDto>
                {
                    Errors = new Error[1] { new Error(await fileResponse.Content.ReadAsStringAsync()) },
                    StatusCode = 409
                };
            }

            var fileId = GetFileId(fileResponse);
            if (fileId.IsFailure)
            {
                return new ApiResult<MediaFileDto>
                {
                    Errors = new Error[1] { fileId.Error },
                    StatusCode = 500
                };
            }

            var uploadResponse = await CreateObservableUpload(fileId.Value, content, 0L, valueOrDefaultCancelToken).LastOrDefaultAsync();
            if (!uploadResponse.IsCompleted)
            {
                return new ApiResult<MediaFileDto>
                {
                    Errors = new Error[1] { new Error($"Could only complete {uploadResponse.CompletedPercentage} percentage of the file.") },
                    StatusCode = 500
                };
            }

            return await GetFileAsync(fileId.Value, valueOrDefaultCancelToken);
        }

        private static async Task<ApiResult<T>> GetApiResultFromResponse<T>(HttpResponseMessage response)
        {
            var apiResult = JsonConvert.DeserializeObject<ApiResult<T>>(await response.Content.ReadAsStringAsync());
            apiResult.StatusCode = (int)response.StatusCode;
            return apiResult;
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
        /// <summary>
        /// An empty POST request is used to create a new upload resource. 
        /// The Upload-Length header indicates the size of the entire upload in bytes.
        /// </summary>
        /// <param name="contentLength">Length of the upload in bytes</param>
        /// <param name="metadata">
        /// The Upload-Metadata requestand response header MUST consist of one or more comma-separated key-valuepairs. 
        /// The key and value MUST be separated by a space. The key MUST NOTcontain spaces and commas and MUST NOT be empty. 
        /// The key SHOULD be ASCIIencoded and the value MUST be Base64 encoded. All keys MUST be unique. Thevalue MAY be empty. 
        /// In these cases, the space, which would normally separatethe key and the value, MAY be left out. Since metadata can 
        /// contain arbitrarybinary values, Servers SHOULD carefully validate metadata values or sanitizethem before using them 
        /// as header values to avoid header smuggling.
        /// </param>
        /// <param name="cancellationToken">Own cancellation token can be provided</param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> CreateFileAsync(int contentLength, string metadata, CancellationToken? cancellationToken = null)
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

        /// <summary>
        /// All PATCH requests MUST use Content-Type: application/offset+octet-stream, 
        /// otherwise the server SHOULD return a 415 Unsupported Media Type status.'
        /// </summary>
        /// <param name="fileId">Id of the file</param>
        /// <param name="bufferLength">Length of the buffer</param>
        /// <param name="currentOffset">Current offset</param>
        /// <param name="memoryStream">The stream to patch with</param>
        /// <param name="cancellationToken">Own cancellation token can be provided</param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> PatchFileAsync(string fileId, int bufferLength, long currentOffset, MemoryStream memoryStream, CancellationToken? cancellationToken = null)
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

        private IObservable<Progress> CreateObservableUpload(string fileId, Stream content, long offset, CancellationToken cancellationToken)
        {
            int chunkSize = 4194304; // 4mb
            long currentOffset = offset;
            var observable = Observable.Create<Progress>(async observer =>
            {
                while (currentOffset < content.Length)
                {
                    var buffer = new byte[Math.Min(chunkSize, content.Length - currentOffset)];
                    var bytesRead = await content.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    HttpResponseMessage patchResponse = await PatchFileAsync(
                        fileId,
                        buffer.Length,
                        currentOffset,
                        new MemoryStream(buffer));
                    currentOffset = Int32.Parse(patchResponse.Headers.GetValues("Upload-Offset").First());
                    observer.OnNext(new Progress(content.Length, currentOffset, (currentOffset / content.Length) * 100,
                        content.Length == currentOffset));
                }

                observer.OnCompleted();
            });
            return observable;
        }

        private static Result<string, Error> GetFileId(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Location", out var location))
                return location.First().Split('/').Last();
            return Result.Failure<string, Error>(new Error("Upload failed. File creation response does not contain file location in header"));
        }
        #endregion

        public void Dispose() => httpClient?.Dispose();
    }
}