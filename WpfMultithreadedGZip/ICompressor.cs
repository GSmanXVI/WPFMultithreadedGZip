using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMultithreadedGZip
{
    interface ICompressor
    {
        void Compress(string inputFile, string outputFile, int threads = 1);
        void Decompress(string inputFile, string outputFile, int threads = 1);
        event CompressionEventHandler ProgressTick;
        event EventHandler Completed;
    }
}
