using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace WpfMultithreadedGZip
{
    public delegate void CompressionEventHandler(object sender, CompressionEventArgs e);

    public class CompressionEventArgs : EventArgs
    {
        public int ThreadNumber { get; set; }
        public int ProgressValue { get; set; }

        public CompressionEventArgs(int threadNumber, int progressValue) : base()
        {
            ThreadNumber = threadNumber;
            ProgressValue = progressValue;
        }        
    }

    class Compressor : ICompressor
    {
        public event CompressionEventHandler ProgressTick;
        public event EventHandler Completed;

        private byte[][] allBytes;

        public void Compress(string inputFile, string outputFile, int threads = 1)
        {
            var fileSize = new FileInfo(inputFile).Length;
            allBytes = new byte[threads][];

            using (var countdownEvent = new CountdownEvent(threads))
            {
                for (int i = 0; i < threads; i++)
                {
                    ThreadPool.QueueUserWorkItem(CompressBytes, new CompressParams
                    {
                        FileName = inputFile,
                        FileSize = fileSize,
                        FilePart = i,
                        TotalParts = threads,
                        CountdownEvent = countdownEvent
                    });
                }

                countdownEvent.Wait();
            }

            using (var fileStream = new FileStream(outputFile, FileMode.Create))
            {
                foreach (var item in allBytes)
                {
                    fileStream.Write(item, 0, item.Length);
                }
            }

            OnCompleted();
        }

        public void Decompress(string inputFile, string outputFile, int threads = 1)
        {
            //TODO
        }

        private void OnProgressTick(int threadNumber, int progressValue)
        {
            ProgressTick?.Invoke(this, new CompressionEventArgs(threadNumber, progressValue));
        }

        private void OnCompleted()
        {
            Completed?.Invoke(this, EventArgs.Empty);
        }

        private void CompressBytes(object obj)
        {
            var param = obj as CompressParams;
            var partSize = param.FileSize / param.TotalParts;
            var filePosition = partSize * param.FilePart;
            var overflow = param.FileSize - partSize * param.FilePart;
            partSize = overflow < 0 ? partSize + overflow : partSize;

            //403 / 4 == 100 
            //100
            //100
            //100
            //103

            try
            {
                byte[] chunk = new byte[partSize];

                lock (this)
                {
                    using (var fileStream = new FileStream(param.FileName, FileMode.Open))
                    {
                        fileStream.Position = filePosition;
                        long bytesLeft = partSize;
                        do
                        {
                            bytesLeft -= Int32.MaxValue;
                            if (bytesLeft > 0)
                                fileStream.Read(chunk, 0, Int32.MaxValue);
                            else
                                fileStream.Read(chunk, 0, (int)partSize);

                        } while (bytesLeft > 0);
                    }
                }

                using (var memoryStream = new MemoryStream())
                {
                    using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, false))
                    {
                        var smallPartSize = (int)(partSize / 100);
                        for (int i = 0; i < 100; i++)
                        {
                            gZipStream.Write(chunk, smallPartSize * i, smallPartSize);
                            OnProgressTick(param.FilePart, i + 1);
                        }
                        gZipStream.Write(chunk, smallPartSize * 100, (int)(partSize - smallPartSize * 100));
                    }

                    allBytes[param.FilePart] = memoryStream.ToArray();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                param.CountdownEvent.Signal();
            }
        }

        private IEnumerable<int> GetHeaderPositions(byte[] file, byte[] header)
        {
            int success = 0;
            for (int i = 0; i < file.Length; i++)
            {
                if (file[i] == header[success]) success++;
                else success = 0;

                if (success == header.Length)
                {
                    yield return i - header.Length + 1;
                    success = 0;
                }
            }
        }
    }
}
