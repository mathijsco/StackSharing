using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackSharing.Lib.Models
{
    public class SharedOnlineItem : OnlineItem
    {
        internal SharedOnlineItem(OnlineItem original)
        {
            this.FullPath = original.FullPath;
            this.LocalPath = original.LocalPath;
        }

        public string ShareId { get; internal set; }

        public Uri ShareUrl { get; internal set; }
    }
}
