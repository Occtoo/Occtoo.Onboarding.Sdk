using CSharpFunctionalExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Occtoo.Onboarding.Sdk.Models
{
    public class UploadMetadata
    {
        public string Filename { get; }
        public string MimeType { get; }
        public long Size { get; }
        public string UniqueIdentifier { get; set; }
        private const char ValuesSeparator = ',';
        private const char KeyValueSeparator = ' ';
        public UploadMetadata(string filename, string mimeType, long size, string uniqueIdentifier = null)
        {
            Filename = filename;
            MimeType = mimeType;
            Size = size;
            UniqueIdentifier = uniqueIdentifier;
        }

        public static Result<string, MetadataParsingError> Serialize<T>(T obj)
        {
            Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(obj));
            return string.Join($"{ValuesSeparator}", dict
                .Where(pair => pair.Value != null)
                .Select(pair => $"{pair.Key}{KeyValueSeparator}{Convert.ToBase64String(Encoding.UTF8.GetBytes(pair.Value))}"));
        }

    }

    public class MetadataParsingError : Error
    {
        public MetadataParsingError(string message) : base(message) { }
    }
}
