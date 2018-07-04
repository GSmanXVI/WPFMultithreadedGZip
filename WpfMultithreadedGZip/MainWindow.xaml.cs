using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace WpfMultithreadedGZip
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new AppViewModel(new Compressor());
        }
    }
}
