using System;

namespace api.shutt.re.Models.RequestBody
{
    public class QueuedImage
    {
        public const int StatusNotQueued = 0;
        public const int StatusQueued = 1;
        public const int StatusInProgress = 2;
        public const int StatusCompleted = 3;
        public const int StatusFailed = 4;

        public ulong QueuedImageId { get; set; }
        public ulong UserId { get; set; }
        public ulong AlbumId { get; set; }
        public string Path { get; set; }
        
        /*
         * 0 = Not queued
         * 1 = Queued
         * 2 = In progress
         * 3 = Completed (can be deleted)
         * 4 = Failed
         */
        public int Status { get; set; }
        public string StatusMsg { get; set; }
    }
}