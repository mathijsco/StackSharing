using System;

namespace StackSharing.Lib.Models
{
    public class SharedOnlineItem : OnlineItem
    {
        internal SharedOnlineItem(OnlineItem original)
        {
            FullPath = original.FullPath;
            LocalPath = original.LocalPath;
        }

        public string ShareId { get; internal set; }

        public Uri ShareUrl { get; internal set; }
    }
}
