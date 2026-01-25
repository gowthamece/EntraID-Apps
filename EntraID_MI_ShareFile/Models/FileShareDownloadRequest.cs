namespace EntraID_MI_ShareFile.Models
{
    public class FileShareDownloadRequest
    {
        public string StorageAcount { get; set; }
        public string FileShare { get; set; }

        public string? Directory { get; set; }
        public string FileNameRegex { get; set; }
    }
}
