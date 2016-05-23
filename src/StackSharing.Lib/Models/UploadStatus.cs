using System.Threading.Tasks;

namespace StackSharing.Lib.Models
{
    public class UploadStatus
    {
        internal UploadStatus()
        {

        }

        public Task Task { get; internal set; }

        public int CurrentFileNo { get; internal set; }

        public int TotalFiles { get; internal set; }

        public float FileProgress { get; internal set; }

        public OnlineItem Result { get; internal set; }
    }
}
