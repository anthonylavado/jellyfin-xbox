namespace Jellyfin.Models.ServiceModels.Movie
{
    public class Mediasource
    {
        public string Protocol { get; set; }
        public string Id { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public string Container { get; set; }
        public long Size { get; set; }
        public string Name { get; set; }
        public bool IsRemote { get; set; }
        public string ETag { get; set; }
        public long RunTimeTicks { get; set; }
        public bool ReadAtNativeFramerate { get; set; }
        public bool IgnoreDts { get; set; }
        public bool IgnoreIndex { get; set; }
        public bool GenPtsInput { get; set; }
        public bool SupportsTranscoding { get; set; }
        public bool SupportsDirectStream { get; set; }
        public bool SupportsDirectPlay { get; set; }
        public bool IsInfiniteStream { get; set; }
        public bool RequiresOpening { get; set; }
        public bool RequiresClosing { get; set; }
        public bool RequiresLooping { get; set; }
        public bool SupportsProbing { get; set; }
        public string VideoType { get; set; }
        public Mediastream[] MediaStreams { get; set; }
        public object[] MediaAttachments { get; set; }
        public object[] Formats { get; set; }
        public int Bitrate { get; set; }
        public int DefaultAudioStreamIndex { get; set; }
        public int DefaultSubtitleStreamIndex { get; set; }
    }
}