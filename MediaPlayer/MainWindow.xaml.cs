using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        #region local attribute
        private bool _isMuted = false;
        private bool _isPlayed = false;
        private bool _shuffle = false;
        private int _playingVideoIndex = -1;
        private double _videoSpeed = 1;
        private const string audioExtension = "All Media Files|*.mp4;*.mp3";
        private BindingList<MediaFile> _mediaFilesInPlaylist = new BindingList<MediaFile>();
        private BindingList<MediaFile> _recentlyPlayedFiles = new BindingList<MediaFile>();
        private DispatcherTimer _timer;
        public string Keyword { get; set; } // search playlist
        public string SearchWord { get; set; } // search recent file
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Load last size and position of screen
            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Width;
            if (Properties.Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }

            string filename = "playlist.txt";
            string recentPlayed = "recentPlayedFiles.txt";

            _mediaFilesInPlaylist = loadPlayList(filename);
            _recentlyPlayedFiles = loadPlayList(recentPlayed);

            playListView.ItemsSource = _mediaFilesInPlaylist;
            DataContext = this;

            if (Properties.Settings.Default.PlayingVideoIndex != -1)
            {
                _playingVideoIndex = Properties.Settings.Default.PlayingVideoIndex;
                playVideoInPlayList(_playingVideoIndex);
            }
        }

        private void ViewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (mediaGrid.ColumnDefinitions[1].Width == new GridLength(0))
            {
                mediaGrid.ColumnDefinitions[1].Width = new GridLength(184, GridUnitType.Star);
                playListView.ItemsSource = _mediaFilesInPlaylist;
            }
        }

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
                Filter = audioExtension, // Dùng để thêm nhiều loại file vào 
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
                            MessageBox.Show("File đã tồn tại trong playlist", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Không có hỗ trợ file - {ex}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }
                }
                playListView.ItemsSource = _mediaFilesInPlaylist;
            }
        }
        private void OpenMediaFile_Click(object sender, RoutedEventArgs e)
        {
            var screen = new Microsoft.Win32.OpenFileDialog
            {
                Filter = audioExtension, // Dùng để thêm nhiều loại file vào 
            };
            if (screen.ShowDialog() == true)
            {
                string fileName = screen.FileName;
                if (Is_Mp3(fileName))
                {
                    GifMp3.Visibility = Visibility.Visible;
                    mediaElement.ScrubbingEnabled = false;
                }
                else
                {
                    GifMp3.Visibility = Visibility.Hidden;
                    mediaElement.ScrubbingEnabled = true;
                    mediaElementPreview.Source = new Uri(fileName, UriKind.Absolute);
                    mediaElementPreview.Stop();
                }
                progressSlider.Value = 0;

                _isPlayed = true;
                mediaElement.Source = new Uri(fileName, UriKind.Absolute);
                mediaElement.Play();
                mediaElement.SpeedRatio = _videoSpeed;
                _timer = new DispatcherTimer();
                _timer.Interval = new TimeSpan(0, 0, 0, 1, 0); ;
                _timer.Tick += _timer_Tick;

                _timer.Start();

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
            // Tự động chơi tập tin kế tiếp khi có play shuffle 
            if (_shuffle && _mediaFilesInPlaylist.Count() > 0)
            {
                GifMp3.Visibility = Visibility.Hidden;
                Random random = new Random();

                int index = random.Next(0, _mediaFilesInPlaylist.Count());
                if (_mediaFilesInPlaylist.Count() > 1)
                    while (index == _playingVideoIndex)
                        index = random.Next(0, _mediaFilesInPlaylist.Count());

                mediaElement.Source = new Uri(_mediaFilesInPlaylist[index].FilePath, UriKind.Absolute);

                _mediaFilesInPlaylist[index].IsPlaying = true;
                if (_playingVideoIndex != index && _playingVideoIndex != -1)
                {
                    _mediaFilesInPlaylist[_playingVideoIndex].IsPlaying = false; // Xóa highlight của video đang chạy
                    _mediaFilesInPlaylist[_playingVideoIndex].CurrentPlayedTime = "0:0:0"; //do video kết thúc nên restart lại trước khi chuyển video mới
                }

                _playingVideoIndex = index; // gán lại giá trị đang chạy
                if (Is_Mp3(_mediaFilesInPlaylist[index].FilePath))
                    GifMp3.Visibility = Visibility.Visible;
                else
                    GifMp3.Visibility = Visibility.Hidden;

                //---code cũ của Duy----
                //progressSlider.Value = 0;

                //double curPos = MediaFile.getCurrentTime(_mediaFilesInPlaylist[index].CurrentPlayedTime).TotalSeconds;
                //mediaElement.Position = TimeSpan.FromSeconds(curPos);
                //progressSlider.Value = mediaElement.Position.TotalSeconds;

                //mediaElement.Play();
                //---code cũ của Duy---

                playVideoInPlayList(_playingVideoIndex);
            }

            if (!_shuffle)
            {
                _mediaFilesInPlaylist[_playingVideoIndex].CurrentPlayedTime = "0:0:0"; //do video kết thúc nên restart lại trước khi chuyển video mới
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/play-button-arrowhead.png", UriKind.Relative);
                bitmap.EndInit();

                PlayButtonIcon.Source = bitmap;
            }
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

        private bool Is_Mp3(string path)
        {
            return System.IO.Path.GetExtension(path).Equals(".mp3");
        }
        private void playVideoInPlayList(int index)
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

            if (Is_Mp3(fileName))
            {
                GifMp3.Visibility = Visibility.Visible;
                mediaElement.ScrubbingEnabled = false;
            }
            else
            {
                GifMp3.Visibility = Visibility.Hidden;
                mediaElement.ScrubbingEnabled = true;
            }

            mediaElement.Source = new Uri(fileName, UriKind.Absolute);
            txblockCurrentTime.Text = _mediaFilesInPlaylist[index].CurrentPlayedTime;

            double curPos = MediaFile.getCurrentTime(_mediaFilesInPlaylist[index].CurrentPlayedTime).TotalSeconds;
            mediaElement.Position = TimeSpan.FromSeconds(curPos);
            progressSlider.Value = mediaElement.Position.TotalSeconds;

            if (_mediaFilesInPlaylist[index].CurrentPlayedTime != "0:0:0")
            {
                mediaElement.Pause();
                MessageBoxResult result = MessageBox.Show($"Xem tiếp tục ở vị trí {_mediaFilesInPlaylist[index].CurrentPlayedTime}?", "Thông báo", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    progressSlider.Value = 0;
                    mediaElement.Position = TimeSpan.FromSeconds(progressSlider.Value);
                }
            }

            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            _timer.Tick += _timer_Tick;

            mediaElement.SpeedRatio = _videoSpeed;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(@"Images/pause.png", UriKind.Relative);
            bitmap.EndInit();

            PlayButtonIcon.Source = bitmap;

            mediaElement.Play();
            _timer.Start();

            _mediaFilesInPlaylist[index].IsPlaying = true;
            _isPlayed = true;

            mediaElementPreview.Source = new Uri(fileName, UriKind.Absolute);
            mediaElementPreview.Stop();
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
                _timer.Stop();
                _isPlayed = false;
                progressSlider.Value = 0;
                mediaElement.Position = TimeSpan.FromSeconds(progressSlider.Value);
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
                    _mediaFilesInPlaylist[_playingVideoIndex].IsPlaying = false;
                    GifMp3.Visibility = Visibility.Hidden;
                    _mediaFilesInPlaylist[_playingVideoIndex].CurrentPlayedTime = txblockCurrentTime.Text;//dòng này khoa thêm current time
                    _playingVideoIndex++;
                }

                playVideoInPlayList(_playingVideoIndex);

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
                    _mediaFilesInPlaylist[_playingVideoIndex].IsPlaying = false;
                    _mediaFilesInPlaylist[_playingVideoIndex].CurrentPlayedTime = txblockCurrentTime.Text;//dòng này khoa thêm current time
                    GifMp3.Visibility = Visibility.Hidden;
                    _playingVideoIndex--;
                }


                playVideoInPlayList(_playingVideoIndex);
            }
        }

        private void BtnShuffle_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null && _mediaFilesInPlaylist.Count() > 0)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                if (_shuffle)
                    bitmap.UriSource = new Uri(@"Images/shuffle-button.png", UriKind.Relative);
                else
                    bitmap.UriSource = new Uri(@"Images/shuffle-click.png", UriKind.Relative);
                _shuffle = !_shuffle;
                bitmap.EndInit();
                ShuffleIcon.Source = bitmap;

                if (progressSlider.Value == progressSlider.Maximum)
                {
                    GifMp3.Visibility = Visibility.Hidden;
                    Random random = new Random();
                    int index = random.Next(0, _mediaFilesInPlaylist.Count());
                    if (_mediaFilesInPlaylist.Count() > 1)
                        while (index == _playingVideoIndex)
                            index = random.Next(0, _mediaFilesInPlaylist.Count());

                    mediaElement.Source = new Uri(_mediaFilesInPlaylist[index].FilePath, UriKind.Absolute);

                    _mediaFilesInPlaylist[index].IsPlaying = true;
                    if (_playingVideoIndex != index && _playingVideoIndex != -1)
                    {
                        _mediaFilesInPlaylist[_playingVideoIndex].IsPlaying = false; // Xóa highlight của video đang chạy
                        _mediaFilesInPlaylist[_playingVideoIndex].CurrentPlayedTime = txblockCurrentTime.Text;// dòng này khoa thêm vô để save current time
                    }


                    _playingVideoIndex = index; // gán lại giá trị đang chạy


                    if (Is_Mp3(_mediaFilesInPlaylist[index].FilePath))
                        GifMp3.Visibility = Visibility.Visible;
                    else
                        GifMp3.Visibility = Visibility.Hidden;

                    //---code cũ của Duy----
                    //progressSlider.Value = 0;
                    //double curPos = MediaFile.getCurrentTime(_mediaFilesInPlaylist[index].CurrentPlayedTime).TotalSeconds;
                    //mediaElement.Position = TimeSpan.FromSeconds(curPos);
                    //progressSlider.Value = mediaElement.Position.TotalSeconds;

                    //mediaElement.Play();
                    //---code cũ của Duy----

                    playVideoInPlayList(_playingVideoIndex);
                }
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

        private int GetIndexFromName(string tagName)
        {
            for (int i = 0; i < _mediaFilesInPlaylist.Count; i++)
            {
                if (_mediaFilesInPlaylist[i].Name == tagName)
                    return i;
            }
            return -1;
        }

        private void PlayCurrentFile_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string)((Button)sender).Tag;
            int index = GetIndexFromName(tag);
            if (index >= 0)
            {
                if (Is_Mp3(_mediaFilesInPlaylist[index].FilePath))
                    GifMp3.Visibility = Visibility.Visible;
                else
                    GifMp3.Visibility = Visibility.Hidden;


                if (_playingVideoIndex >= 0)
                {
                    _mediaFilesInPlaylist[_playingVideoIndex].IsPlaying = false;
                    if (progressSlider.Value != progressSlider.Maximum)
                    {
                        _mediaFilesInPlaylist[_playingVideoIndex].CurrentPlayedTime = txblockCurrentTime.Text;
                    }
                }


                playVideoInPlayList(index);

                _playingVideoIndex = index;
            }
        }

        private void SpeedUp_Click(object sender, RoutedEventArgs e)
        {
            _videoSpeed = 10;
            mediaElement.SpeedRatio = _videoSpeed;
        }

        private void NormalSpeed_Click(object sender, RoutedEventArgs e)
        {
            _videoSpeed = 1;
            mediaElement.SpeedRatio = _videoSpeed;
        }

        private void SlowDown_Click(object sender, RoutedEventArgs e)
        {
            _videoSpeed = 0.1;
            mediaElement.SpeedRatio = _videoSpeed;
        }

        private void BtnRepeat_Click(object sender, RoutedEventArgs e)
        {

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

            if (progressSlider.Value != progressSlider.Maximum && _isPlayed)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/pause.png", UriKind.Relative);
                bitmap.EndInit();

                PlayButtonIcon.Source = bitmap;
            }
        }

        private void progressSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            mediaElement.Stop();
        }

        private void progressSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            mediaElement.Play();
        }
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaElement.Volume = (double)volumeSlider.Value;
            var value = Math.Round((double)volumeSlider.Value * 100, MidpointRounding.ToEven);
            txblockVolume.Text = $"{value}%";
        }
        private void progressSlider_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.Tedavi_Popup.IsOpen)
                this.Tedavi_Popup.IsOpen = true;

            var mousePosition = e.GetPosition(this.progressSlider);
            this.Tedavi_Popup.HorizontalOffset = mousePosition.X - 70;
            this.Tedavi_Popup.VerticalOffset = -110;
            double progressValue = progressSlider.Maximum * (mousePosition.X / progressSlider.ActualWidth);
            TimeSpan newPosition = TimeSpan.FromSeconds(progressValue);
            mediaElementPreview.Position = newPosition;
        }

        private void progressSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Tedavi_Popup.IsOpen = false;
        }
        #endregion

        private void RemoveFileFromPlayList_Click(object sender, RoutedEventArgs e)
        {
            int index = playListView.SelectedIndex;

            if (index >= 0)
            {
                if (_isPlayed && index == _playingVideoIndex)
                {
                    if (_playingVideoIndex == _mediaFilesInPlaylist.Count - 1)
                    {
                        _timer.Stop();
                        txblockCurrentTime.Text = "00:00";
                        txblockTotalTime.Text = "00:00";
                        txblockVolume.Text = "0%";

                        progressSlider.Value = 0;

                        _isPlayed = false;
                        GifMp3.Visibility = Visibility.Hidden;
                        mediaElement.Source = null;
                        _playingVideoIndex = -1;
                    }
                    else
                    {
                        // Play next video
                        BtnNext_Click(sender, e);
                        _playingVideoIndex--;
                    }
                }
                else
                {
                    if (index < _playingVideoIndex)
                    {
                        _playingVideoIndex--;
                    }
                }
                _mediaFilesInPlaylist.RemoveAt(index);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Save window state
            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
                Properties.Settings.Default.Maximized = true;
            }
            else
            {
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
                Properties.Settings.Default.Maximized = false;
            }
            Properties.Settings.Default.PlayingVideoIndex = _playingVideoIndex;
            Properties.Settings.Default.Save();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine(e.Key);
            switch (e.Key)
            {
                case Key.Up:
                    volumeSlider.Value += 0.02;
                    break;
                case Key.Down:
                    volumeSlider.Value -= 0.02;
                    break;
                case Key.Left:
                    progressSlider.Value -= 5;
                    break;
                case Key.Right:
                    progressSlider.Value += 5;
                    break;
                case Key.Space:
                case Key.MediaPlayPause:
                    BtnPlay_Click(sender, e);
                    break;
                case Key.MediaPreviousTrack:
                    BtnPrevious_Click(sender, e);
                    break;
                case Key.MediaNextTrack:
                    BtnNext_Click(sender, e);
                    break;
                default:
                    break;

            }
        }
    }
}
