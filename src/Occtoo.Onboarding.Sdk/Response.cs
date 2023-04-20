using System;
using System.Collections.Generic;

namespace Occtoo.Onboarding.Sdk
{
    public abstract class Response<T>
    {
        public int StatusCode { get; internal set; }
        public string Message { get; internal set; }
        public T Result { get; internal set; }
        public override string ToString()
        {
            return $"{StatusCode} - {Message}. {Result}";
        }
    }

    public class StartImportResponse : Response<ImportBatchResult>
    {
    }

    public class ImportBatchResult
    {
        public Guid Id { get; set; }

        public override string ToString() => string.Format("Id: {0}", (object)this.Id);
    }

    public class TokenResponse
    {
        public TokenInfo result { get; set; }
        public List<object> errors { get; set; }
        public string requestId { get; set; }
    }

    public class TokenInfo
    {
        public string accessToken { get; set; }
        public int expiresIn { get; set; }
        public string tokenType { get; set; }
        public object refreshToken { get; set; }
        public string scope { get; set; }
    }

    public class ApiResult<T>
    {
        public T Result { get; set; }
        public Error[] Errors { get; set; }
        public string RequestId { get; set; }
        public int StatusCode { get; set; }
    }
    public class ApiResult
    {
        public Error[] Errors { get; set; }
        public string RequestId { get; set; }
        public int StatusCode { get; set; }
    }

    public class Error
    {
        public string Message { get; set; }
    }
}