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
        Task<StartImportResponse> StartEntityImportAsync(string dataSource, IReadOnlyList<DynamicEntity> entities, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        Task<StartImportResponse> StartEntityImportAsync(string dataSource, IReadOnlyList<DynamicEntity> entities, string token, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        Task<string> GetTokenAsync(CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> GetFileAsync(string fileId, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> GetFileFromUniqueIdAsync(string UniqueIdentifier, CancellationToken? cancellationToken = null);
        Task<ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>>> GetFilesBatchAsync(List<string> identifiers, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> UploadFromLinkAsync(FileUploadFromLink link, CancellationToken? cancellationToken = null);
        Task<ApiResult<PartialSuccessResponse<string, UploadDto, Error>>> UploadFromLinksAsync(List<FileUploadFromLink> links, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> UploadFileAsync(Stream content, UploadMetadata metadata, CancellationToken? cancellationToken = null);
        Task<ApiResult<UploadDto>> GetUploadStatusAsync(string uploadId, CancellationToken? cancellationToken = null);
        Task<ApiResult> DeleteFileAsync(string fileId, CancellationToken? cancellationToken = null);

        //Synchronous
        StartImportResponse StartEntityImport(string dataSource, IReadOnlyList<DynamicEntity> entities, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        StartImportResponse StartEntityImport(string dataSource, IReadOnlyList<DynamicEntity> entities, string token, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        string GetToken(CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> GetFile(string fileId, CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> GetFileFromUniqueId(string UniqueIdentifier, CancellationToken? cancellationToken = null);
        ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>> GetFilesBatch(List<string> identifiers, CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> UploadFromLink(FileUploadFromLink link, CancellationToken? cancellationToken = null);
        ApiResult<PartialSuccessResponse<string, UploadDto, Error>> UploadFromLinks(List<FileUploadFromLink> links, CancellationToken? cancellationToken = null);
        ApiResult<MediaFileDto> UploadFile(Stream content, UploadMetadata metadata, CancellationToken? cancellationToken = null);
        ApiResult<UploadDto> GetUploadStatus(string uploadId, CancellationToken? cancellationToken = null);
        ApiResult DeleteFile(string fileId, CancellationToken? cancellationToken = null);
    }
}