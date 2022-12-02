using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
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
using System.Windows.Threading;
using static System.Net.WebRequestMethods;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isMuted = false;
        private bool _isPlayed = false;
        private int _playingVideoIndex = -1;

        public MainWindow()
        {
            InitializeComponent();
        }

        //ObservableCollection<MediaFile> _mediaFilesInPlaylist = new ObservableCollection<MediaFile>();

        private BindingList<MediaFile> _mediaFilesInPlaylist = new BindingList<MediaFile>();

        private BindingList<MediaFile> _recentlyPlayedFiles = new BindingList<MediaFile>();

        private DispatcherTimer _timer;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string filename = "playlist.txt";
            string recentPlayed = "recentPlayedFiles.txt";

            _mediaFilesInPlaylist = loadPlayList(filename);
            _recentlyPlayedFiles = loadPlayList(recentPlayed);

            playListView.ItemsSource = _mediaFilesInPlaylist;
            DataContext = this;
        }

        private void ViewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (mediaGrid.ColumnDefinitions[1].Width == new GridLength(0))
            {
                mediaGrid.ColumnDefinitions[1].Width = new GridLength(184, GridUnitType.Star);
                playListView.ItemsSource = _mediaFilesInPlaylist;
            }
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

        private void AddFilesPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var screen = new Microsoft.Win32.OpenFileDialog
            {
                //Filter = audioExtension, :  Dùng để thêm nhiều loại file vào 
                Multiselect = true
            };
            if (screen.ShowDialog() == true)
            {

                foreach (var filename in screen.FileNames)
                {
                    try
                    {
                        MediaFile mediaFile = new MediaFile()
                        {
                            Name = System.IO.Path.GetFileName(filename),
                            FilePath = filename,
                            IsPlaying = false,
                        };
                        MediaFile file = _mediaFilesInPlaylist.SingleOrDefault(e => e.FilePath == mediaFile.FilePath)!;
                        if (file == null)
                        {
                            _mediaFilesInPlaylist.Add(mediaFile);

                        }
                        else
                        {
                            MessageBox.Show("File da ton tai");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Cannot play that file - {ex} ");
                        continue;
                    }
                }
                playListView.ItemsSource = _mediaFilesInPlaylist;
            }
        }
        private void OpenMediaFile_Click(object sender, RoutedEventArgs e)
        {
            var screen = new OpenFileDialog();
            if (screen.ShowDialog() == true)
            {
                string fileName = screen.FileName;
                mediaElement.Source = new Uri(fileName, UriKind.Absolute);

                mediaElement.Stop();

                _timer = new DispatcherTimer();
                _timer.Interval = new TimeSpan(0, 0, 0, 1, 0); ;
                _timer.Tick += _timer_Tick;
            }
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            int hours = mediaElement.Position.Hours;
            int minutes = mediaElement.Position.Minutes;
            int seconds = mediaElement.Position.Seconds;
            txblockCurrentTime.Text = $"{hours}:{minutes}:{seconds}";
            progressSlider.Value = mediaElement.Position.TotalSeconds;
        }

        private void player_MediaOpened(object sender, RoutedEventArgs e)
        {
            int hours = mediaElement.NaturalDuration.TimeSpan.Hours;
            int minutes = mediaElement.NaturalDuration.TimeSpan.Minutes;
            int seconds = mediaElement.NaturalDuration.TimeSpan.Seconds;
            txblockTotalTime.Text = $"{hours}:{minutes}:{seconds}";

            // cập nhật max value của slider
            progressSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            volumeSlider.Value = mediaElement.Volume;

            var value = Math.Round((double)volumeSlider.Value * 100, MidpointRounding.ToEven);

            txblockVolume.Text = $"{value}%";
        }

        private void player_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Tự động chơi tập tin kế tiếp ở đây
        }

        #region Helper
        private BindingList<MediaFile> loadPlayList(string fileName)
        {
            BindingList<MediaFile> playlist = new BindingList<MediaFile>();
            string[] lines = System.IO.File.ReadAllLines(fileName);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] tokens = lines[i].Split(new string[] { "|" }, StringSplitOptions.None);

                playlist.Add(new MediaFile(tokens[0], tokens[1], tokens[2], tokens[3]));
            }
            return playlist;
        }
        #endregion

        #region btn
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                if (_isPlayed)
                {
                    _isPlayed = false;
                    mediaElement.Pause();
                    _timer.Stop();

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(@"Images/play-button-arrowhead.png", UriKind.Relative);
                    bitmap.EndInit();

                    PlayButtonIcon.Source = bitmap;
                }
                else
                {
                    _isPlayed = true;
                    mediaElement.Play();
                    _timer.Start();

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(@"Images/pause.png", UriKind.Relative);
                    bitmap.EndInit();

                    PlayButtonIcon.Source = bitmap;
                }
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                mediaElement.Stop();

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/play-button-arrowhead.png", UriKind.Relative);
                bitmap.EndInit();

                PlayButtonIcon.Source = bitmap;
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                if (_playingVideoIndex == _mediaFilesInPlaylist.Count - 1 && _playingVideoIndex == 0)
                {
                    return;
                }
                else if (_playingVideoIndex == _mediaFilesInPlaylist.Count - 1)
                {
                    return;
                }
                else
                {
                    _playingVideoIndex++;
                }

                string fileName = _mediaFilesInPlaylist[_playingVideoIndex].FilePath;

                mediaElement.Source = new Uri(fileName, UriKind.Absolute);

                double curPos = MediaFile.getCurrentTime(_mediaFilesInPlaylist[_playingVideoIndex].CurrentPlayedTime).TotalSeconds;
                mediaElement.Position = TimeSpan.FromSeconds(curPos);
                progressSlider.Value = mediaElement.Position.TotalSeconds;

                _timer = new DispatcherTimer();
                _timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                _timer.Tick += _timer_Tick;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/pause.png", UriKind.Relative);
                bitmap.EndInit();

                PlayButtonIcon.Source = bitmap;

                mediaElement.Play();
                _timer.Start();

                _isPlayed = true;
            }
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                if (_playingVideoIndex == _mediaFilesInPlaylist.Count - 1 && _playingVideoIndex == 0)
                {
                    return;
                }
                else if (_playingVideoIndex == 0)
                {
                    return;
                }
                else
                {
                    _playingVideoIndex--;
                }

                string fileName = _mediaFilesInPlaylist[_playingVideoIndex].FilePath;

                mediaElement.Source = new Uri(fileName, UriKind.Absolute);

                double curPos = MediaFile.getCurrentTime(_mediaFilesInPlaylist[_playingVideoIndex].CurrentPlayedTime).TotalSeconds;
                mediaElement.Position = TimeSpan.FromSeconds(curPos);
                progressSlider.Value = mediaElement.Position.TotalSeconds;

                _timer = new DispatcherTimer();
                _timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                _timer.Tick += _timer_Tick;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/pause.png", UriKind.Relative);
                bitmap.EndInit();

                PlayButtonIcon.Source = bitmap;

                mediaElement.Play();
                _timer.Start();

                _isPlayed = true;
            }
        }

        private void BtnShuffle_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {

            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isMuted)
            {
                _isMuted = false;
                volumeSlider.Value = 0.5;
                mediaElement.Volume = (double)volumeSlider.Value;
                var value = Math.Round((double)volumeSlider.Value * 100, MidpointRounding.ToEven);
                txblockVolume.Text = $"{value}%";

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/volume.png", UriKind.Relative);
                bitmap.EndInit();

                MuteButtonIcon.Source = bitmap;
            }
            else
            {
                _isMuted = true;
                volumeSlider.Value = 0;
                mediaElement.Volume = (double)volumeSlider.Value;
                var value = Math.Round((double)volumeSlider.Value * 100, MidpointRounding.ToEven);
                txblockVolume.Text = $"{value}%";

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/mute-volume-control.png", UriKind.Relative);
                bitmap.EndInit();

                MuteButtonIcon.Source = bitmap;
            }
        }

        private void BtnClosePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (mediaGrid.ColumnDefinitions[1].Width != new GridLength(0))
            {
                mediaGrid.ColumnDefinitions[1].Width = new GridLength(0);
            }
        }

        private void BtnCloseRecentFilesList_Click(object sender, RoutedEventArgs e)
        {
            if (mediaGrid.ColumnDefinitions[2].Width != new GridLength(0))
            {
                mediaGrid.ColumnDefinitions[2].Width = new GridLength(0);
            }
        }
        #endregion

        #region slider
        private void progressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = progressSlider.Value;
            TimeSpan newPosition = TimeSpan.FromSeconds(value);
            mediaElement.Position = newPosition;
            mediaElement.Volume = (double)volumeSlider.Value;
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaElement.Volume = (double)volumeSlider.Value;
            var value = Math.Round((double)volumeSlider.Value * 100, MidpointRounding.ToEven);
            txblockVolume.Text = $"{value}%";
        }
        #endregion


        private void PlayCurrentFile_Click(object sender, RoutedEventArgs e)
        {
            int index = playListView.SelectedIndex;

            if (index >= 0)
            {
                if (_isPlayed)
                {
                    _timer.Stop();
                }
                else
                {
                    mediaElement.Play();
                }

                string fileName = _mediaFilesInPlaylist[index].FilePath;

                mediaElement.Source = new Uri(fileName, UriKind.Absolute);

                double curPos = MediaFile.getCurrentTime(_mediaFilesInPlaylist[index].CurrentPlayedTime).TotalSeconds;
                mediaElement.Position = TimeSpan.FromSeconds(curPos);
                progressSlider.Value = mediaElement.Position.TotalSeconds;

                _timer = new DispatcherTimer();
                _timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                _timer.Tick += _timer_Tick;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/pause.png", UriKind.Relative);
                bitmap.EndInit();

                PlayButtonIcon.Source = bitmap;

                _playingVideoIndex = index;

                mediaElement.Play();
                _timer.Start();

                _isPlayed = true;
            }
        }

        private void RemoveFileFromPlayList_Click(object sender, RoutedEventArgs e)
        {
            int index = playListView.SelectedIndex;

            if (index >= 0)
            {
                if (_isPlayed && index == _playingVideoIndex)
                {
                    if (_playingVideoIndex == _mediaFilesInPlaylist.Count - 1 && _playingVideoIndex == 0)
                    {
                        _timer.Stop();
                        txblockCurrentTime.Text = "00:00";
                        txblockTotalTime.Text = "00:00";
                        txblockVolume.Text = "0%";

                        progressSlider.Value = 0;
                        volumeSlider.Value = 0;

                        mediaElement.Source = null;
                    }
                    else
                    {
                        // Play next video
                        BtnNext_Click(sender, e);
                    }
                }
                _mediaFilesInPlaylist.RemoveAt(index);
            }
        }

        private void SaveCurrentProgress_Click(object sender, RoutedEventArgs e)
        {
            int index = playListView.SelectedIndex;

            if (index >= 0)
            {
                _mediaFilesInPlaylist[index].CurrentPlayedTime = txblockCurrentTime.Text;
            }

            playListView.ItemsSource = _mediaFilesInPlaylist;
        }
    }
}
