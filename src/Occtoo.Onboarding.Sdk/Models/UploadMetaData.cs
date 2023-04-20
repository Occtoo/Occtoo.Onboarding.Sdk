namespace Occtoo.Onboarding.Sdk.Models
{
    public class UploadMetadata
    {
        public UploadMetadata(string filename, string mimeType, long size)
        {
            Filename = filename;
            MimeType = mimeType;
            Size = size;
        }
        public string Filename { get; }
        public string MimeType { get; }
        public long Size { get; }
        public string UniqueIdentifier { get; set; }
    }
}
