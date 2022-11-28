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

        ObservableCollection<MediaFile> _mediaFilesInPlaylist = new ObservableCollection<MediaFile>();

        ObservableCollection<MediaFile> _recentlyPlayedFiles = new ObservableCollection<MediaFile>();


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string filename = "playlist.txt";
            string recentPlayed = "recentPlayedFiles.txt";
            
            string[] lines = File.ReadAllLines(filename);
            string[] lines2 = File.ReadAllLines(recentPlayed);
            
            for(int i = 0; i < lines.Length; i++)
            {
                string[] tokens = lines[i].Split(new string[] { " " }, StringSplitOptions.None);

                _mediaFilesInPlaylist.Add(new MediaFile(tokens[0], tokens[1], tokens[2]));
            }

            for (int i = 0; i < lines2.Length; i++)
            {
                string[] tokens = lines2[i].Split(new string[] { " " }, StringSplitOptions.None);

                _recentlyPlayedFiles.Add(new MediaFile(tokens[0], tokens[1], tokens[2]));
            }

            DataContext = this;
        }

        private void SliDuration_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {

        }

        private void ViewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (mediaGrid.ColumnDefinitions[1].Width == new GridLength(0))
            {
                mediaGrid.ColumnDefinitions[1].Width = new GridLength(184, GridUnitType.Star);
                playListView.ItemsSource = _mediaFilesInPlaylist;
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

        public string Keyword { get; set; } // search playlist
        public string SearchWord { get; set; } // search recent file
        private void keywordTextBox_TextChanged(object sender, TextChangedEventArgs e) // text change playlist
        {
            if (Keyword == "")
            {
                playListView.ItemsSource = _mediaFilesInPlaylist;
            }
            else
            {
                var mediaFiles = new ObservableCollection<MediaFile>(_mediaFilesInPlaylist.Where(
                                mediaFile => mediaFile.Name.ToLower().Contains(Keyword.ToLower())).ToList());

                playListView.ItemsSource = mediaFiles;
            }
        }

        private void ViewRecentPlayedFiles_Click(object sender, RoutedEventArgs e)
        {

            if (mediaGrid.ColumnDefinitions[2].Width == new GridLength(0))
            {
                mediaGrid.ColumnDefinitions[2].Width = new GridLength(184, GridUnitType.Star);
                recentFilesView.ItemsSource = _recentlyPlayedFiles;
            }
            else
            {
                mediaGrid.ColumnDefinitions[2].Width = new GridLength(0);
            }
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e) // text change recent files
        {
            if (SearchWord == "")
            {
                recentFilesView.ItemsSource = _recentlyPlayedFiles;
            }
            else
            {
                var mediaFiles = new ObservableCollection<MediaFile>(_recentlyPlayedFiles.Where(
                                                mediaFile => mediaFile.Name.ToLower().Contains(SearchWord.ToLower())).ToList());

                recentFilesView.ItemsSource = mediaFiles;
            }
        }
    }
}
