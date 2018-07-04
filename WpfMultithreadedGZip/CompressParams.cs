using System.Threading;

namespace WpfMultithreadedGZip
{
    public class CompressParams
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public int FilePart { get; set; }
        public int TotalParts { get; set; }
        public CountdownEvent CountdownEvent { get; set; }
    }
}
