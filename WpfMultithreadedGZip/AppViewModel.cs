using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Threading.Tasks;

namespace WpfMultithreadedGZip
{
    class AppViewModel : ViewModelBase
    {
        private ObservableCollection<int> progress;
        public ObservableCollection<int> Progress
        {
            get => progress;
            set => Set(ref progress, value);
        }

        private string fileName;
        public string FileName
        {
            get => fileName;
            set => Set(ref fileName, value);
        }

        private int threadsCount;
        public int ThreadsCount
        {
            get => threadsCount;
            set => Set(ref threadsCount, value);
        }

        private int time;
        public int Time
        {
            get => time;
            set => Set(ref time, value);
        }

        private bool ready;
        public bool Ready
        {
            get => ready;
            set => Set(ref ready, value);
        }

        public ICompressor Compressor { get; }

        public AppViewModel(ICompressor compressor)
        {
            Progress = new ObservableCollection<int>() { 0, 0, 0, 0 };
            ThreadsCount = 1;
            Ready = true;
            Compressor = compressor;
            Compressor.ProgressTick += CompressorProgressTick;
            Compressor.Completed += CompressorCompleted;
        }

        private RelayCommand browseCommand;
        public RelayCommand BrowseCommand
        {
            get
            {
                return browseCommand ?? (browseCommand = new RelayCommand(
                () =>
                {
                    Clear();
                    var dialog = new OpenFileDialog();
                    dialog.ShowDialog();
                    FileName = dialog.FileName;
                }
                ));
            }
        }

        private RelayCommand compressCommand;
        public RelayCommand CompressCommand
        {
            get
            {
                return compressCommand ?? (compressCommand = new RelayCommand(
                async () =>
                {
                    Ready = false;
                    await XZ();
                },
                () => !String.IsNullOrEmpty(FileName) && Ready));
            }
        }

        private async Task XZ()
        {
            await Task.Run(() =>
            {
                try
                {
                    var start = DateTime.Now;
                    var timer = new Timer(new TimerCallback(param => Time = (int)(DateTime.Now - start).TotalMilliseconds));
                    timer.Change(0, 100);
                    Compressor.Compress(FileName, $"{FileName}.gz", ThreadsCount);
                    timer.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            });
        }

        private RelayCommand decompressCommand;
        public RelayCommand DecompressCommand
        {
            get
            {
                return decompressCommand ?? (decompressCommand = new RelayCommand(
                () =>
                {
                    //TODO                
                },
                () => !String.IsNullOrEmpty(FileName) && Ready));
            }
        }

        private void CompressorCompleted(object sender, EventArgs e)
        {
            Ready = true;
            CompressCommand.RaiseCanExecuteChanged();
            DecompressCommand.RaiseCanExecuteChanged();
        }

        private void CompressorProgressTick(object sender, CompressionEventArgs e)
        {
            Progress[e.ThreadNumber] = e.ProgressValue;
        }

        private void Clear()
        {
            for (int i = 0; i < Progress.Count; i++) Progress[i] = 0;
            Time = 0;
        }
    }
}
