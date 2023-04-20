using System.Collections.Generic;

namespace Occtoo.Onboarding.Sdk.Models
{
    public class UploadLinksRequest
    {
        public UploadLinksRequest(List<FileUploadFromLink> links)
        {
            Links = links;
        }
        public List<FileUploadFromLink> Links { get; set; }
    }

    public class FileUploadFromLink
    {
        public FileUploadFromLink(string link, string filename)
        {
            Link = link;
            Filename = filename;
        }
        public string Link { get; set; }
        public string Filename { get; set; }
        public string UniqueIdentifier { get; set; }
    }
}
