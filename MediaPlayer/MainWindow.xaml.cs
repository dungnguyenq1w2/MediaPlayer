using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
        }

        ObservableCollection<MediaFile> _mediaFiles = new ObservableCollection<MediaFile>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string filename = "playlist.txt";
            string[] lines = File.ReadAllLines(filename);

            for(int i = 0; i < lines.Length; i++)
            {
                string[] tokens = lines[i].Split(new string[] { " " }, StringSplitOptions.None);

                _mediaFiles.Add(new MediaFile(tokens[0], tokens[1], tokens[2]));
            }

            DataContext = this;
            playListView.ItemsSource = _mediaFiles;
        }

        private void SliDuration_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {

        }

        private void ViewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (mediaGrid.ColumnDefinitions[1].Width == new GridLength(0))
            {
                mediaGrid.ColumnDefinitions[1].Width = new GridLength(184, GridUnitType.Star);
            }
            else
            {
                mediaGrid.ColumnDefinitions[1].Width = new GridLength(0);
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {

        }

        public string Keyword { get; set; }
        private void keywordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Keyword == "")
            {
                playListView.ItemsSource = _mediaFiles;
            }
            else
            {
                var mediaFiles = new ObservableCollection<MediaFile>(_mediaFiles.Where(
                                    mediaFile => mediaFile.Name.ToLower().Contains(Keyword.ToLower())).ToList());
                playListView.ItemsSource = mediaFiles;
            }
        }
    }
}
