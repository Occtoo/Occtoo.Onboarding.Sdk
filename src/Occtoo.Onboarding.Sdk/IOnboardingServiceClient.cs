using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Occtoo.Onboarding.Sdk
{
    public interface IOnboardingServiceClient
    {
        //Asynchronous
        Task<StartImportResponse> StartEntityImportAsync(string dataSource, IReadOnlyList<DynamicEntity> entities, string token = null, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        Task<string> GetTokenAsync(CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> GetFileAsync(string fileId, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> GetFileFromUniqueIdAsync(string UniqueIdentifier, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>>> GetFilesBatchAsync(List<string> identifiers, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> UploadFromLinkAsync(FileUploadFromLink link, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<PartialSuccessResponse<string, UploadDto, Error>>> UploadFromLinksAsync(List<FileUploadFromLink> links, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> UploadFileAsync(Stream content, UploadMetadata metadata, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> UploadFileIfNotExistAsync(Stream content, UploadMetadata metadata, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<UploadDto>> GetUploadStatusAsync(string uploadId, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult> DeleteFileAsync(string fileId, string token = null, CancellationToken? cancellationToken = null);

        //Synchronous
        StartImportResponse StartEntityImport(string dataSource, IReadOnlyList<DynamicEntity> entities, string token = null, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        string GetToken(CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> GetFile(string fileId, string token = null, CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> GetFileFromUniqueId(string UniqueIdentifier, string token = null, CancellationToken? cancellationToken = null);
        ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>> GetFilesBatch(List<string> identifiers, string token = null, CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> UploadFromLink(FileUploadFromLink link, string token = null, CancellationToken? cancellationToken = null);
        ApiResult<PartialSuccessResponse<string, UploadDto, Error>> UploadFromLinks(List<FileUploadFromLink> links, string token = null, CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> UploadFile(Stream content, UploadMetadata metadata, string token = null, CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> UploadFileIfNotExist(Stream content, UploadMetadata metadata, string token = null, CancellationToken? cancellationToken = null);
        ApiResult<UploadDto> GetUploadStatus(string uploadId, string token = null, CancellationToken? cancellationToken = null);
        ApiResult DeleteFile(string fileId, string token = null, CancellationToken? cancellationToken = null);
    }
}