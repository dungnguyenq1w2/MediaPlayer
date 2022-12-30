using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using Newtonsoft.Json;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private enum ListType
        {
            PlayList,
            RecentPlayed,
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        #region local attribute
        private bool _isMuted = false;
        private bool _isPlayed = false;
        private bool _shuffle = false;
        private bool _isSetting = false;
        //
        private int _playingPlaylistIndex = -1;
        private int _playingRecentPlayedIndex = -1;
        //
        private double _videoSpeed = 1;
        private bool _ended = false;
        private int _repeat = 0; // 0: default, 1: play auto next video, 2: repeat video
        private double _rotateAngle = 0;
        private bool _fullscreen = false;
        //
        private BindingList<MediaFile> _mediaFilesInPlaylist = new BindingList<MediaFile>();
        private BindingList<MediaFile> _recentlyPlayedFiles = new BindingList<MediaFile>();
        private DispatcherTimer _timer;
        private Random _random = new Random();
        public string Keyword { get; set; } // search playlist
        public string SearchWord { get; set; } // search recent file
        //
        private const string _audioExtension = "All Media Files|*.mp4;*.mp3;*.wav;*.m4v;*.MP4;*.MP3;*.M4V;*.WAV";
        private const string _playListDataFile = "playlist.json";
        private const string _recentPlayedDataFile = "recently_played.json";
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Load last size and position of screen
            if (Properties.Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                this.Top = Properties.Settings.Default.Top;
                this.Left = Properties.Settings.Default.Left;
                this.Height = Properties.Settings.Default.Height;
                this.Width = Properties.Settings.Default.Width;
            }

            //_mediaFilesInPlaylist = loadMediaFiles(_playListDataFile, ListType.PlayList);
            _mediaFilesInPlaylist = loadMediaFiles(_playListDataFile);
            _recentlyPlayedFiles = loadMediaFiles(_recentPlayedDataFile);

            playListView.ItemsSource = _mediaFilesInPlaylist;
            recentFilesView.ItemsSource = _recentlyPlayedFiles;
            DataContext = this;

            //if (Properties.Settings.Default.PlayingVideoIndex != -1)
            //{
            //    _playingPlaylistIndex = Properties.Settings.Default.PlayingVideoIndex;
            //    playFileInPlayList(_playingPlaylistIndex);
            //}
        }

        private void ViewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (mediaGrid.ColumnDefinitions[1].Width == new GridLength(0))
            {
                mediaGrid.ColumnDefinitions[1].Width = new GridLength(184, GridUnitType.Star);
                playListView.ItemsSource = _mediaFilesInPlaylist;
            }
        }

        private void KeywordTextBox_TextChanged(object sender, TextChangedEventArgs e) // text change playlist
        {
            if (Keyword == "")
            {
                playListView.ItemsSource = _mediaFilesInPlaylist;
            }
            else
            {
                var mediaFiles = new BindingList<MediaFile>(_mediaFilesInPlaylist.Where(
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

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) // text change recent files
        {
            if (SearchWord == "")
            {
                recentFilesView.ItemsSource = _recentlyPlayedFiles;
            }
            else
            {
                var mediaFiles = new BindingList<MediaFile>(_recentlyPlayedFiles.Where(
                                                mediaFile => mediaFile.Name.ToLower().Contains(SearchWord.ToLower())).ToList());

                recentFilesView.ItemsSource = mediaFiles;
            }
        }

        private void AddFilesPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var screen = new Microsoft.Win32.OpenFileDialog
            {
                Filter = _audioExtension, // Dùng để thêm nhiều loại file vào 
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
            //
            saveMediaFiles(_playListDataFile, ListType.PlayList);
        }
        private void playListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                string[] tokens = _audioExtension.Split('|');
                string[] extensions = tokens[1].Split(';');
                foreach (var filename in files)
                {
                    try
                    {
                        foreach (string extension in extensions)
                        {
                            if (extension.Contains(System.IO.Path.GetExtension(filename)))
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
                                break;
                            }
                            else { continue; }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Không có hỗ trợ file - {ex}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }
                }
            }
            //
            saveMediaFiles(_playListDataFile, ListType.PlayList);
        }

        private void OpenMediaFile_Click(object sender, RoutedEventArgs e)
        {
            var screen = new Microsoft.Win32.OpenFileDialog
            {
                Filter = _audioExtension, // Dùng để thêm nhiều loại file vào 
            };
            if (screen.ShowDialog() == true)
            {
                string filePath = screen.FileName;

                PauseAudio.Visibility = Visibility.Hidden;
                if (IsAudio(filePath))
                {
                    GifAudio.Visibility = Visibility.Visible;
                    mediaElement.ScrubbingEnabled = false;
                }
                else
                {
                    GifAudio.Visibility = Visibility.Hidden;
                    mediaElement.ScrubbingEnabled = true;
                    mediaElementPreview.Source = new Uri(filePath, UriKind.Absolute);
                    mediaElementPreview.Stop();
                }
                progressSlider.Value = 0;

                _isPlayed = true;
                mediaElement.Source = new Uri(filePath, UriKind.Absolute);
                mediaElement.Play();
                mediaElement.SpeedRatio = _videoSpeed;
                _timer = new DispatcherTimer();

                switch (_videoSpeed)
                {
                    case 1:
                        {
                            _timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                            break;
                        }
                    case 1.5: // speed up
                        {
                            _timer.Interval = new TimeSpan(0, 0, 0, 0, 666);
                            break;
                        }
                    case 0.5: // slow down
                        {
                            _timer.Interval = new TimeSpan(0, 0, 0, 2, 0);
                            break;
                        }
                }

                _timer.Tick += _timer_Tick;

                _timer.Start();

                if (_playingPlaylistIndex != -1)
                {
                    _mediaFilesInPlaylist[_playingPlaylistIndex].IsPlaying = false;
                    _playingPlaylistIndex = -1;
                }

                if (_playingRecentPlayedIndex != -1)
                {
                    _recentlyPlayedFiles[_playingRecentPlayedIndex].IsPlaying = false;
                    _playingRecentPlayedIndex = -1;
                }

                //
                AddFileToRecentPlayedList(filePath);
            }
            //
            saveMediaFiles(_recentPlayedDataFile, ListType.RecentPlayed);
        }

        private void AddFileToRecentPlayedList(string filePath)
        {
            ////
            MediaFile newRecentFile = new MediaFile()
            {
                Name = System.IO.Path.GetFileName(filePath),
                FilePath = filePath,
                IsPlaying = false,
            };

            int sameFileIndex = _recentlyPlayedFiles.ToList().FindIndex((file) => file.FilePath == newRecentFile.FilePath);
            if (sameFileIndex != -1)
            {
                _recentlyPlayedFiles.RemoveAt(sameFileIndex);
            }

            _recentlyPlayedFiles.Insert(0, newRecentFile);
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            int hours = mediaElement.Position.Hours;
            int minutes = mediaElement.Position.Minutes;
            int seconds = mediaElement.Position.Seconds;
            txblockCurrentTime.Text = $"{hours.ToString().PadLeft(2, '0')}:{minutes.ToString().PadLeft(2, '0')}:{seconds.ToString().PadLeft(2, '0')}";
            progressSlider.Value = mediaElement.Position.TotalSeconds;
        }

        private void player_MediaOpened(object sender, RoutedEventArgs e)
        {
            int hours = mediaElement.NaturalDuration.TimeSpan.Hours;
            int minutes = mediaElement.NaturalDuration.TimeSpan.Minutes;
            int seconds = mediaElement.NaturalDuration.TimeSpan.Seconds;
            txblockTotalTime.Text = $"{hours.ToString().PadLeft(2, '0')}:{minutes.ToString().PadLeft(2, '0')}:{seconds.ToString().PadLeft(2, '0')}";

            // cập nhật max value của slider
            progressSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            volumeSlider.Value = mediaElement.Volume;

            var value = Math.Round((double)volumeSlider.Value * 100, MidpointRounding.ToEven);

            txblockVolume.Text = $"{value}%";
        }

        private void player_MediaEnded(object sender, RoutedEventArgs e)
        {
            _ended = true;
            if (IsAudio(mediaElement.Source.ToString()))
            {
                GifAudio.Visibility = Visibility.Hidden;
                PauseAudio.Visibility = Visibility.Visible;
            }
            else
            {
                GifAudio.Visibility = Visibility.Hidden;
                PauseAudio.Visibility = Visibility.Hidden;
            }

            Debug.WriteLine(_playingPlaylistIndex);
            Debug.WriteLine(_playingRecentPlayedIndex);

            // true if playing file in playlist, false if playing in file in recent played
            bool isPlayingPlaylist = _playingPlaylistIndex != -1 ? true : false;

            if (isPlayingPlaylist)
            {
                // Tự động chơi tập tin kế tiếp khi có play shuffle 
                if (_shuffle && _mediaFilesInPlaylist.Count > 0)
                {
                    GifAudio.Visibility = Visibility.Hidden;
                    PauseAudio.Visibility = Visibility.Hidden;

                    int index = _random.Next(0, _mediaFilesInPlaylist.Count);
                    if (_mediaFilesInPlaylist.Count > 1)
                        while (index == _playingPlaylistIndex)
                            index = _random.Next(0, _mediaFilesInPlaylist.Count);

                    mediaElement.Source = new Uri(_mediaFilesInPlaylist[index].FilePath, UriKind.Absolute);

                    _mediaFilesInPlaylist[index].IsPlaying = true;
                    if (_playingPlaylistIndex != index && _playingPlaylistIndex != -1)
                    {
                        _mediaFilesInPlaylist[_playingPlaylistIndex].IsPlaying = false; // Xóa highlight của video đang chạy
                        _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = "00:00:00"; //do video kết thúc nên restart lại trước khi chuyển video mới
                    }

                    _playingPlaylistIndex = index; // gán lại giá trị đang chạy
                    if (IsAudio(_mediaFilesInPlaylist[index].FilePath))
                        GifAudio.Visibility = Visibility.Visible;
                    else
                        GifAudio.Visibility = Visibility.Hidden;

                    playFileInPlayList(_playingPlaylistIndex);
                }
                else if (!_shuffle && _repeat == 0)
                {
                    _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = "00:00:00"; //do video kết thúc nên restart lại trước khi chuyển video mới
                    progressSlider.Value = 0;
                    TimeSpan newPosition = TimeSpan.FromSeconds(0);
                    mediaElement.Position = newPosition;
                    _timer.Stop();
                    mediaElement.Stop();
                    _isPlayed = false;
                    txblockCurrentTime.Text = "00:00:00";
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(@"Images/play-button-arrowhead.png", UriKind.Relative);
                    bitmap.EndInit();

                    PlayButtonIcon.Source = bitmap;
                }
                else if (_repeat == 1)
                {
                    _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = "00:00:00";
                    BtnNext_Click(sender, e);
                }
                else if (_repeat == 2)
                {
                    if (IsAudio(mediaElement.Source.ToString()))
                        GifAudio.Visibility = Visibility.Visible;
                    else
                        GifAudio.Visibility = Visibility.Hidden;
                    PauseAudio.Visibility = Visibility.Hidden;
                    progressSlider.Value = 0;
                    mediaElement.Play();
                }
            }
            else
            {
                // Tự động chơi tập tin kế tiếp khi có play shuffle 
                if (_shuffle && _recentlyPlayedFiles.Count > 0)
                {
                    GifAudio.Visibility = Visibility.Hidden;
                    PauseAudio.Visibility = Visibility.Hidden;

                    Debug.WriteLine($"xxx: {_playingRecentPlayedIndex}");
                    int index = 0;
                    if (_recentlyPlayedFiles.Count > 1)
                        index = _random.Next(1, _recentlyPlayedFiles.Count);

                    if (_playingRecentPlayedIndex != index && _playingRecentPlayedIndex != -1)
                    {
                        _recentlyPlayedFiles[_playingRecentPlayedIndex].IsPlaying = false; // Xóa highlight của video đang chạy
                        _recentlyPlayedFiles[_playingRecentPlayedIndex].CurrentPlayedTime = "00:00:00"; //do video kết thúc nên restart lại trước khi chuyển video mới
                    }

                    mediaElement.Source = new Uri(_recentlyPlayedFiles[index].FilePath, UriKind.Absolute);
                    _recentlyPlayedFiles[index].IsPlaying = true;
                    _playingRecentPlayedIndex = 0; // gán lại giá trị đang chạy

                    playFileInRecentPlayed(index);
                }
                else if (!_shuffle && _repeat == 0)
                {
                    _recentlyPlayedFiles[_playingRecentPlayedIndex].CurrentPlayedTime = "00:00:00"; //do video kết thúc nên restart lại trước khi chuyển video mới
                    progressSlider.Value = 0;
                    TimeSpan newPosition = TimeSpan.FromSeconds(0);
                    mediaElement.Position = newPosition;
                    _timer.Stop();
                    mediaElement.Stop();
                    _isPlayed = false;
                    txblockCurrentTime.Text = "00:00:00";
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(@"Images/play-button-arrowhead.png", UriKind.Relative);
                    bitmap.EndInit();

                    PlayButtonIcon.Source = bitmap;
                }
                else if (_repeat == 2)
                {
                    if (IsAudio(mediaElement.Source.ToString()))
                        GifAudio.Visibility = Visibility.Visible;
                    else
                        GifAudio.Visibility = Visibility.Hidden;
                    PauseAudio.Visibility = Visibility.Hidden;
                    progressSlider.Value = 0;
                    mediaElement.Play();
                }
            }

            //
            saveMediaFiles(_playListDataFile, ListType.PlayList);
            saveMediaFiles(_recentPlayedDataFile, ListType.RecentPlayed);
        }

        #region Helper

        private int GetIndexFromName(string tagName, ListType type)
        {
            int index = -1;
            if (type == ListType.PlayList)
            {
                index = _mediaFilesInPlaylist.ToList().FindIndex((file) => file.Name == tagName);
            }
            else if (type == ListType.RecentPlayed)
            {
                index = _recentlyPlayedFiles.ToList().FindIndex((file) => file.Name == tagName);
            }
            return index;
        }
        //private BindingList<MediaFile> loadMediaFiles(string fileName, ListType type)
        private BindingList<MediaFile> loadMediaFiles(string fileName)
        {
            var mediaFiles = new BindingList<MediaFile>();

            string exeFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = $"{exeFolderPath}\\{fileName}";

            try
            {
                if (System.IO.File.Exists(filePath) == true)
                {
                    //string[] lines = System.IO.File.ReadAllLines(filePath);

                    //foreach (string line in lines)
                    //{
                    //    string[] tokens = line.Split(new string[] { "|" }, StringSplitOptions.None);

                    //    mediaFiles.Add(new MediaFile()
                    //    {
                    //        Name = tokens[0],
                    //        TotalTime = tokens[1],
                    //        CurrentPlayedTime = tokens[2],
                    //        FilePath = tokens[3],
                    //    });
                    //}

                    string jsonString = System.IO.File.ReadAllText(filePath);
                    mediaFiles = JsonConvert.DeserializeObject<BindingList<MediaFile>>(jsonString) ?? new BindingList<MediaFile>();
                }

                return mediaFiles;
            }
            catch (Exception)
            {
                return new BindingList<MediaFile>();
            }
        }

        private void saveMediaFiles(string fileName, ListType type)
        {
            string exeFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = $"{exeFolderPath}\\{fileName}";
            List<string> lines = new List<string>();

            List<MediaFile>? mediaFiles;

            if (type == ListType.PlayList)
            {
                //mediaFiles = new List<MediaFile>(_mediaFilesInPlaylist);
                mediaFiles = _mediaFilesInPlaylist
                    .ToList()
                    .ConvertAll(
                        (mediaFile) => new MediaFile()
                        {
                            Name = mediaFile.Name,
                            TotalTime = mediaFile.TotalTime,
                            CurrentPlayedTime = mediaFile.CurrentPlayedTime,
                            FilePath = mediaFile.FilePath,
                        }
                    );
            }
            else if (type == ListType.RecentPlayed)
            {
                mediaFiles = _recentlyPlayedFiles
                    .ToList()
                    .ConvertAll(
                        (mediaFile) => new MediaFile()
                        {
                            Name = mediaFile.Name,
                            TotalTime = mediaFile.TotalTime,
                            CurrentPlayedTime = mediaFile.CurrentPlayedTime,
                            FilePath = mediaFile.FilePath,
                        }
                    );
            }
            else
            {
                return;
            }

            // Save to JSON file
            try
            {
                //foreach (var mediaFile in mediaFiles)
                //{
                //    string line = string.Join(
                //        "|",
                //        new string[]
                //        {
                //            mediaFile.Name,
                //            mediaFile.TotalTime,
                //            mediaFile.CurrentPlayedTime,
                //            mediaFile.FilePath,
                //        }
                //    );

                //    lines.Add(line);
                //}

                //System.IO.File.WriteAllLines(filePath, lines);

                string jsonString = JsonConvert.SerializeObject(mediaFiles, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(filePath, jsonString);
            }
            catch (Exception)
            {
                return;
            }
        }

        private bool IsAudio(string path)
        {
            return System.IO.Path.GetExtension(path).ToLower().Equals(".mp3") || System.IO.Path.GetExtension(path).ToLower().Equals(".wav");
        }

        private void playFileInPlayList(int index)
        {
            if (System.IO.File.Exists(_mediaFilesInPlaylist[index].FilePath) == false)
            {
                MessageBox.Show("This file has been moved or deleted", "Item not found", MessageBoxButton.OK, MessageBoxImage.Warning);
                _mediaFilesInPlaylist.RemoveAt(index);
                return;
            }

            if (_isPlayed)
            {
                _timer.Stop();
            }
            else
            {
                mediaElement.Play();
            }

            string filePath = _mediaFilesInPlaylist[index].FilePath;

            if (IsAudio(filePath))
            {
                GifAudio.Visibility = Visibility.Visible;
                mediaElement.ScrubbingEnabled = false;
            }
            else
            {
                GifAudio.Visibility = Visibility.Hidden;
                mediaElement.ScrubbingEnabled = true;
            }

            PauseAudio.Visibility = Visibility.Hidden;
            mediaElement.Source = new Uri(filePath, UriKind.Absolute);
            txblockCurrentTime.Text = _mediaFilesInPlaylist[index].CurrentPlayedTime;

            double curPos = MediaFile.getCurrentTime(_mediaFilesInPlaylist[index].CurrentPlayedTime).TotalSeconds;
            mediaElement.Position = TimeSpan.FromSeconds(curPos);
            progressSlider.Value = mediaElement.Position.TotalSeconds;

            if (_mediaFilesInPlaylist[index].CurrentPlayedTime != "00:00:00")
            {
                mediaElement.Pause();
                MessageBoxResult result = MessageBox.Show($"Continue watching at {_mediaFilesInPlaylist[index].CurrentPlayedTime}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    progressSlider.Value = 0;
                    mediaElement.Position = TimeSpan.FromSeconds(progressSlider.Value);
                }
            }

            _timer = new DispatcherTimer();
            switch (_videoSpeed)
            {
                case 1:
                    {
                        _timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                        break;
                    }
                case 1.5: // speed up
                    {
                        _timer.Interval = new TimeSpan(0, 0, 0, 0, 666);
                        break;
                    }
                case 0.5: // slow down
                    {
                        _timer.Interval = new TimeSpan(0, 0, 0, 2, 0);
                        break;
                    }
            }

            _timer.Tick += _timer_Tick;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(@"Images/pause.png", UriKind.Relative);
            bitmap.EndInit();

            PlayButtonIcon.Source = bitmap;

            mediaElement.Play();

            mediaElement.SpeedRatio = _videoSpeed;
            _timer.Start();

            _mediaFilesInPlaylist[index].IsPlaying = true;
            _isPlayed = true;

            mediaElementPreview.Source = new Uri(filePath, UriKind.Absolute);
            mediaElementPreview.Stop();

            //
            AddFileToRecentPlayedList(filePath);
            //
            saveMediaFiles(_playListDataFile, ListType.PlayList);
            saveMediaFiles(_recentPlayedDataFile, ListType.RecentPlayed);

            Debug.WriteLine(_mediaFilesInPlaylist[index].IsPlaying);
        }

        private void playFileInRecentPlayed(int index)
        {
            if (System.IO.File.Exists(_recentlyPlayedFiles[index].FilePath) == false)
            {
                MessageBox.Show("This file has been moved or deleted", "Item not found", MessageBoxButton.OK, MessageBoxImage.Warning);
                _recentlyPlayedFiles.RemoveAt(index);
                return;
            }

            if (_isPlayed)
            {
                _timer.Stop();
            }
            else
            {
                mediaElement.Play();
            }

            string filePath = _recentlyPlayedFiles[index].FilePath;

            if (IsAudio(filePath))
            {
                GifAudio.Visibility = Visibility.Visible;
                mediaElement.ScrubbingEnabled = false;
            }
            else
            {
                GifAudio.Visibility = Visibility.Hidden;
                mediaElement.ScrubbingEnabled = true;
            }

            PauseAudio.Visibility = Visibility.Hidden;
            mediaElement.Source = new Uri(filePath, UriKind.Absolute);

            txblockCurrentTime.Text = "00:00:00";
            progressSlider.Value = 0;
            mediaElement.Position = TimeSpan.FromSeconds(0);

            _timer = new DispatcherTimer();
            switch (_videoSpeed)
            {
                case 1:
                    {
                        _timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                        break;
                    }
                case 1.5: // speed up
                    {
                        _timer.Interval = new TimeSpan(0, 0, 0, 0, 666);
                        break;
                    }
                case 0.5: // slow down
                    {
                        _timer.Interval = new TimeSpan(0, 0, 0, 2, 0);
                        break;
                    }
            }

            _timer.Tick += _timer_Tick;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(@"Images/pause.png", UriKind.Relative);
            bitmap.EndInit();

            PlayButtonIcon.Source = bitmap;

            mediaElement.Play();

            mediaElement.SpeedRatio = _videoSpeed;
            _timer.Start();

            mediaElementPreview.Source = new Uri(filePath, UriKind.Absolute);
            mediaElementPreview.Stop();

            //
            AddFileToRecentPlayedList(filePath);
            //
            _recentlyPlayedFiles[0].IsPlaying = true;
            _isPlayed = true;
            //
            saveMediaFiles(_recentPlayedDataFile, ListType.RecentPlayed);

            foreach (var mediaFile in _recentlyPlayedFiles)
            {
                Debug.WriteLine(mediaFile.IsPlaying);
            }
        }
        #endregion

        #region btn
        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                if (_playingPlaylistIndex == -1)
                {
                    MessageBox.Show("Play your playlist first to use this feature", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (_playingPlaylistIndex == 0)
                {
                    MessageBox.Show("You are at the start of the playlist");
                    return;
                }
                else
                {
                    _mediaFilesInPlaylist[_playingPlaylistIndex].IsPlaying = false;
                    if (_ended)
                    {
                        _ended = false;
                        _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = "00:00:00";
                    }
                    else
                    {
                        _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = txblockCurrentTime.Text;
                    }
                    GifAudio.Visibility = Visibility.Hidden;
                    PauseAudio.Visibility = Visibility.Hidden;
                    _playingPlaylistIndex--;
                }


                playFileInPlayList(_playingPlaylistIndex);
            }
            else
            {
                MessageBox.Show("Play your playlist first to use this feature", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                //MessageBox.Show("There is no media playing!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            //
            saveMediaFiles(_playListDataFile, ListType.PlayList);
            saveMediaFiles(_recentPlayedDataFile, ListType.RecentPlayed);
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {

                if (_playingPlaylistIndex == -1)
                {
                    MessageBox.Show("Play your playlist first to use this feature", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //bool isPlayingPlaylist = _playingPlaylistIndex != -1 ? true : false;


                if (_playingPlaylistIndex == _mediaFilesInPlaylist.Count - 1)
                {
                    if (_repeat == 1 && _ended)
                    {
                        mediaElement.Play();
                        _mediaFilesInPlaylist[_playingPlaylistIndex].IsPlaying = false;
                        GifAudio.Visibility = Visibility.Hidden;
                        PauseAudio.Visibility = Visibility.Hidden;
                        _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = "00:00:00";

                        _playingPlaylistIndex = 0;
                        _ended = false;
                    }
                    else
                    {
                        MessageBox.Show("You are at the end of the playlist");
                        return;
                    }
                }
                else
                {
                    _mediaFilesInPlaylist[_playingPlaylistIndex].IsPlaying = false;
                    if ((_repeat == 1 && _ended) || _ended)
                    {
                        _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = "00:00:00";
                        _ended = false;
                    }
                    else
                    {
                        _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = txblockCurrentTime.Text;
                    }
                    GifAudio.Visibility = Visibility.Hidden;
                    PauseAudio.Visibility = Visibility.Hidden;
                    _playingPlaylistIndex++;
                }

                playFileInPlayList(_playingPlaylistIndex);
            }
            else
            {
                MessageBox.Show("Play your playlist first to use this feature", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                //MessageBox.Show("There is no media playing!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            //
            saveMediaFiles(_playListDataFile, ListType.PlayList);
            saveMediaFiles(_recentPlayedDataFile, ListType.RecentPlayed);
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                _ended = false;
                if (_isPlayed)
                {
                    _isPlayed = !_isPlayed;
                    mediaElement.Pause();
                    _timer.Stop();
                    GifAudio.Visibility = Visibility.Hidden;
                    if (IsAudio(mediaElement.Source.ToString()))
                        PauseAudio.Visibility = Visibility.Visible;
                    else
                        PauseAudio.Visibility = Visibility.Hidden;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(@"Images/play-button-arrowhead.png", UriKind.Relative);
                    bitmap.EndInit();

                    PlayButtonIcon.Source = bitmap;
                }
                else
                {
                    _isPlayed = !_isPlayed;
                    mediaElement.Play();
                    _timer.Start();

                    PauseAudio.Visibility = Visibility.Hidden;
                    if (IsAudio(mediaElement.Source.ToString()))
                        GifAudio.Visibility = Visibility.Visible;
                    else
                        GifAudio.Visibility = Visibility.Hidden;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(@"Images/pause.png", UriKind.Relative);
                    bitmap.EndInit();

                    PlayButtonIcon.Source = bitmap;
                }
            }
            else
            {
                MessageBox.Show("There is no media playing!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            //
            saveMediaFiles(_playListDataFile, ListType.PlayList);
            saveMediaFiles(_recentPlayedDataFile, ListType.RecentPlayed);
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                _timer.Stop();
                _isPlayed = false;
                progressSlider.Value = 0;
                txblockCurrentTime.Text = "00:00:00";
                mediaElement.Position = TimeSpan.FromSeconds(progressSlider.Value);
                mediaElement.Stop();
                if (IsAudio(mediaElement.Source.ToString()))
                {
                    GifAudio.Visibility = Visibility.Hidden;
                    PauseAudio.Visibility = Visibility.Visible;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/play-button-arrowhead.png", UriKind.Relative);
                bitmap.EndInit();

                PlayButtonIcon.Source = bitmap;
            }
            else
            {
                MessageBox.Show("There is no media playing!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            //
            saveMediaFiles(_playListDataFile, ListType.PlayList);
            saveMediaFiles(_recentPlayedDataFile, ListType.RecentPlayed);
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnShuffle_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source == null || (_playingPlaylistIndex == -1 && _playingRecentPlayedIndex == -1) || (_playingPlaylistIndex != -1 && _mediaFilesInPlaylist.Count == 0) || (_playingRecentPlayedIndex != -1 && _recentlyPlayedFiles.Count == 0))
            {
                MessageBox.Show("Play your Playlist/Recently Played first to use this feature", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                if (_repeat > 0)
                {
                    var bitmapRepeat = new BitmapImage();
                    bitmapRepeat.BeginInit();
                    bitmapRepeat.UriSource = new Uri(@"Images/repeat-button.png", UriKind.Relative);
                    bitmapRepeat.EndInit();
                    _repeat = 0;

                    RepeatIcon.Source = bitmapRepeat;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                if (_shuffle)
                    bitmap.UriSource = new Uri(@"Images/shuffle-button.png", UriKind.Relative);
                else
                    bitmap.UriSource = new Uri(@"Images/shuffle-click.png", UriKind.Relative);
                _shuffle = !_shuffle;
                bitmap.EndInit();
                ShuffleIcon.Source = bitmap;

                //if (_ended)
                //{
                //    _ended = !_ended;
                //    GifAudio.Visibility = Visibility.Hidden;
                //    PauseAudio.Visibility = Visibility.Hidden;
                //    
                //    int index = _random.Next(0, _mediaFilesInPlaylist.Count);
                //    if (_mediaFilesInPlaylist.Count > 1)
                //        while (index == _playingPlaylistIndex)
                //            index = _random.Next(0, _mediaFilesInPlaylist.Count);

                //    mediaElement.Source = new Uri(_mediaFilesInPlaylist[index].FilePath, UriKind.Absolute);

                //    _mediaFilesInPlaylist[index].IsPlaying = true;
                //    if (_playingPlaylistIndex != index && _playingPlaylistIndex != -1)
                //    {
                //        _mediaFilesInPlaylist[_playingPlaylistIndex].IsPlaying = false; // Xóa highlight của video đang chạy
                //        _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = txblockCurrentTime.Text;// save current time
                //    }

                //    _playingPlaylistIndex = index; // gán lại giá trị đang chạy

                //    if (IsAudio(_mediaFilesInPlaylist[index].FilePath))
                //        GifAudio.Visibility = Visibility.Visible;
                //    else
                //        GifAudio.Visibility = Visibility.Hidden;

                //    playFileInPlayList(_playingPlaylistIndex);
                //}
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

        private void PlayCurrentFile_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string)((Button)sender).Tag;
            //ListType listType = _playingPlaylistIndex != -1 ? ListType.PlayList : ListType.RecentPlayed;
            //int index = GetIndexFromName(tag, listType);
            int index = GetIndexFromName(tag, ListType.PlayList);

            if (index >= 0)
            {
                PauseAudio.Visibility = Visibility.Hidden;
                if (IsAudio(_mediaFilesInPlaylist[index].FilePath))
                    GifAudio.Visibility = Visibility.Visible;
                else
                    GifAudio.Visibility = Visibility.Hidden;


                if (_playingPlaylistIndex != -1 && _mediaFilesInPlaylist.Count > 0)
                {
                    _mediaFilesInPlaylist[_playingPlaylistIndex].IsPlaying = false;
                    if (progressSlider.Value != progressSlider.Maximum)
                    {
                        _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = txblockCurrentTime.Text;
                    }
                }

                if (_playingRecentPlayedIndex != -1 && _recentlyPlayedFiles.Count > 0)
                {
                    //_recentlyPlayedFiles[_playingRecentPlayedIndex].IsPlaying = false;
                    _recentlyPlayedFiles[0].IsPlaying = false;
                }


                playFileInPlayList(index);

                _playingPlaylistIndex = index;
                _playingRecentPlayedIndex = -1;

                //if (listType == ListType.PlayList)
                //{
                //    playFileInPlayList(index);

                //    _playingPlaylistIndex = index;
                //    _playingRecentPlayedIndex = -1;
                //}
                //else
                //{
                //    playFileInRecentPlayed(index);

                //    _playingRecentPlayedIndex = index;
                //    _playingPlaylistIndex = -1;
                //}
            }
        }

        private void PlayRecentFile_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string)((Button)sender).Tag;
            int index = GetIndexFromName(tag, ListType.RecentPlayed);

            if (index >= 0)
            {
                PauseAudio.Visibility = Visibility.Hidden;
                if (IsAudio(_recentlyPlayedFiles[index].FilePath))
                    GifAudio.Visibility = Visibility.Visible;
                else
                    GifAudio.Visibility = Visibility.Hidden;


                if (_playingPlaylistIndex != -1 && _mediaFilesInPlaylist.Count > 0)
                {
                    _mediaFilesInPlaylist[_playingPlaylistIndex].IsPlaying = false;
                    if (progressSlider.Value != progressSlider.Maximum)
                    {
                        _mediaFilesInPlaylist[_playingPlaylistIndex].CurrentPlayedTime = txblockCurrentTime.Text;
                    }
                }

                if (_playingRecentPlayedIndex != -1 && _recentlyPlayedFiles.Count > 0)
                {
                    //_recentlyPlayedFiles[_playingRecentPlayedIndex].IsPlaying = false;
                    _recentlyPlayedFiles[0].IsPlaying = false;
                }

                _playingPlaylistIndex = -1;
                _playingRecentPlayedIndex = 0;

                playFileInRecentPlayed(index);

                if (_repeat == 1)
                {
                    _repeat = 0;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(@"Images/repeat-button.png", UriKind.Relative);
                    bitmap.EndInit();

                    RepeatIcon.Source = bitmap;

                    MessageBox.Show("We currently DO NOT support 'REPEAT PLAYLIST' MODE when playing files in Recently Played so it has been turned off." +
                        " You can still use 'REPEAT SINGLE FILE' MODE :>");
                }
            }
        }

        private void SpeedUp_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                SpeedUpIcon.Visibility = Visibility.Visible;
                NormalSpeedIcon.Visibility = Visibility.Hidden;
                SlowDownIcon.Visibility = Visibility.Hidden;
                _videoSpeed = 1.5;
                _timer.Interval = new TimeSpan(0, 0, 0, 0, 666);
                mediaElement.SpeedRatio = _videoSpeed;
            }
        }

        private void NormalSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                SpeedUpIcon.Visibility = Visibility.Hidden;
                NormalSpeedIcon.Visibility = Visibility.Visible;
                SlowDownIcon.Visibility = Visibility.Hidden;

                _videoSpeed = 1;
                _timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                mediaElement.SpeedRatio = _videoSpeed;
            }
        }

        private void SlowDown_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                SpeedUpIcon.Visibility = Visibility.Hidden;
                NormalSpeedIcon.Visibility = Visibility.Hidden;
                SlowDownIcon.Visibility = Visibility.Visible;

                _videoSpeed = 0.5;
                _timer.Interval = new TimeSpan(0, 0, 0, 2, 0);
                mediaElement.SpeedRatio = _videoSpeed;
            }
        }

        private void BtnSnapshot_Click(object sender, RoutedEventArgs e)
        {

            if (IsAudio(mediaElement.Source.ToString()))
            {
                return;
            }


            _isPlayed = false;
            mediaElement.Pause();
            _timer.Stop();

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(@"Images/play-button-arrowhead.png", UriKind.Relative);
            bitmap.EndInit();

            PlayButtonIcon.Source = bitmap;

            double naturalWidth = mediaElement.NaturalVideoWidth;
            double naturalHeight = mediaElement.NaturalVideoHeight;

            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)naturalWidth, (int)naturalHeight, 96, 96, PixelFormats.Pbgra32);

            VisualBrush sourceBrush = new VisualBrush(mediaElement);
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            using (drawingContext)
            {
                drawingContext.DrawRectangle(
                    sourceBrush,
                    null,
                    new Rect(
                        new System.Windows.Point(0, 0),
                        new System.Windows.Point(naturalWidth, naturalHeight)
                    )
                );
            }

            renderTargetBitmap.Render(drawingVisual);

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "JPG Image (*.jpg)|*.jpg|PNG Image (*.png)|*.png";
            //saveFileDialog.FilterIndex = 2;

            if (saveFileDialog.ShowDialog() == true)
            {
                string fileName = saveFileDialog.FileName;
                string fileExtension = System.IO.Path.GetExtension(fileName);

                BitmapEncoder encoder;
                if (fileExtension.ToLower() == ".jpg")
                {
                    encoder = new JpegBitmapEncoder();
                }
                else if (fileExtension.ToLower() == ".png")
                {
                    encoder = new PngBitmapEncoder();
                }
                else
                {
                    return;
                }

                BitmapFrame bitmapFrame = BitmapFrame.Create(renderTargetBitmap);
                encoder.Frames.Add(bitmapFrame);

                FileStream fs = new FileStream(fileName, FileMode.Create);
                encoder.Save(fs);
                fs.Close();
            }
        }

        private void BtnRepeat_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                if (_shuffle)
                {
                    var bitmapShuffle = new BitmapImage();
                    bitmapShuffle.BeginInit();
                    bitmapShuffle.UriSource = new Uri(@"Images/shuffle-button.png", UriKind.Relative);
                    bitmapShuffle.EndInit();
                    _shuffle = !_shuffle;

                    ShuffleIcon.Source = bitmapShuffle;
                }

                //
                bool isPlayingPlaylist = _playingPlaylistIndex != -1 ? true : false;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();

                if (isPlayingPlaylist)
                {
                    if (_repeat == 0)
                    {
                        _repeat = 1;
                        bitmap.UriSource = new Uri(@"Images/repeat-auto.png", UriKind.Relative);
                    }
                    else if (_repeat == 1)
                    {
                        _repeat = 2;
                        bitmap.UriSource = new Uri(@"Images/repeat-one.png", UriKind.Relative);
                    }
                    else
                    {
                        _repeat = 0;
                        bitmap.UriSource = new Uri(@"Images/repeat-button.png", UriKind.Relative);
                    }
                }
                else
                {
                    if (_repeat == 0)
                    {
                        _repeat = 2;
                        bitmap.UriSource = new Uri(@"Images/repeat-one.png", UriKind.Relative);
                    }
                    else
                    {
                        _repeat = 0;
                        bitmap.UriSource = new Uri(@"Images/repeat-button.png", UriKind.Relative);
                    }
                }

                bitmap.EndInit();
                RepeatIcon.Source = bitmap;

                if (progressSlider.Value == progressSlider.Maximum)
                {
                    if (_repeat == 1)
                    {
                        BtnNext_Click(sender, e);
                    }
                }
            }
            else
            {
                MessageBox.Show("There is no media playing!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            if (IsAudio(mediaElement.Source.ToString()))
            {
                return;
            }

            _rotateAngle += 90;
            if (_rotateAngle >= 360)
            {
                _rotateAngle -= 360;
            }

            TransformGroup transformGroup = new TransformGroup();
            //double renderWidth = mediaElement.ActualWidth;
            //double renderHeight = mediaElement.ActualHeight;
            //transformGroup.Children.Add(new TranslateTransform(renderWidth, renderHeight));
            transformGroup.Children.Add(new RotateTransform(_rotateAngle));

            //mediaElement.RenderTransform = transformGroup;
            mediaElement.LayoutTransform = transformGroup;
        }

        private void BtnFullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (!_fullscreen)
            {
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                topMenu.Height = 0;
                mediaGrid.ColumnDefinitions[1].Width = new GridLength(0);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/fullscreen-in.png", UriKind.Relative);
                bitmap.EndInit();

                FullscreenIcon.Source = bitmap;
            }
            else
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowState = WindowState.Normal;
                topMenu.Height = 22;
                mediaGrid.ColumnDefinitions[1].Width = new GridLength(184, GridUnitType.Star);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/fullscreen-out.png", UriKind.Relative);
                bitmap.EndInit();

                FullscreenIcon.Source = bitmap;
            }

            _fullscreen = !_fullscreen;
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

        private void BtnSetting_Click(object sender, RoutedEventArgs e)
        {
            if (_isSetting)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/settings.png", UriKind.Relative);
                bitmap.EndInit();

                SettingIcon.Source = bitmap;

                _isSetting = false;
            }
            else
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"Images/settings-click.png", UriKind.Relative);
                bitmap.EndInit();

                SettingIcon.Source = bitmap;
                _isSetting = true;
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

                if (IsAudio(mediaElement.Source.ToString()))
                {
                    GifAudio.Visibility = Visibility.Visible;
                    PauseAudio.Visibility = Visibility.Hidden;
                }
                else
                {
                    GifAudio.Visibility = Visibility.Hidden;
                    PauseAudio.Visibility = Visibility.Hidden;
                }
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
                if (_isPlayed && index == _playingPlaylistIndex)
                {
                    if (_playingPlaylistIndex == _mediaFilesInPlaylist.Count - 1)
                    {
                        _timer.Stop();
                        txblockCurrentTime.Text = "00:00:00";
                        txblockTotalTime.Text = "00:00:00";
                        txblockVolume.Text = "0%";

                        progressSlider.Value = 0;

                        _isPlayed = false;
                        GifAudio.Visibility = Visibility.Hidden;
                        PauseAudio.Visibility = Visibility.Hidden;
                        mediaElement.Source = null;
                        _playingPlaylistIndex = -1;
                    }
                    else
                    {
                        // Play next video
                        BtnNext_Click(sender, e);
                        _playingPlaylistIndex--;
                    }
                }
                else
                {
                    if (index < _playingPlaylistIndex)
                    {
                        _playingPlaylistIndex--;
                    }
                }
                _mediaFilesInPlaylist.RemoveAt(index);
            }

            if (_mediaFilesInPlaylist.Count == 0)
            {
                if (_repeat > 0)
                {
                    var bitmapRepeat = new BitmapImage();
                    bitmapRepeat.BeginInit();
                    bitmapRepeat.UriSource = new Uri(@"Images/repeat-button.png", UriKind.Relative);
                    bitmapRepeat.EndInit();
                    _repeat = 0;

                    RepeatIcon.Source = bitmapRepeat;
                }

                if (_shuffle)
                {
                    var bitmapShuffle = new BitmapImage();
                    bitmapShuffle.BeginInit();
                    bitmapShuffle.UriSource = new Uri(@"Images/shuffle-button.png", UriKind.Relative);
                    bitmapShuffle.EndInit();
                    _shuffle = !_shuffle;

                    ShuffleIcon.Source = bitmapShuffle;
                }
            }
        }

        private void RemoveFileFromRecentPlayed_Click(object sender, RoutedEventArgs e)
        {
            int index = recentFilesView.SelectedIndex;

            if (index >= 0)
            {
                //if (_isPlayed && index == 0)
                if (index == _playingRecentPlayedIndex)
                {
                    _timer.Stop();
                    txblockCurrentTime.Text = "00:00:00";
                    txblockTotalTime.Text = "00:00:00";
                    txblockVolume.Text = "0%";

                    progressSlider.Value = 0;

                    _isPlayed = false;

                    GifAudio.Visibility = Visibility.Hidden;
                    PauseAudio.Visibility = Visibility.Hidden;
                    mediaElement.Source = null;

                    _playingRecentPlayedIndex = -1;
                }

                _recentlyPlayedFiles.RemoveAt(index);
            }

            if (_recentlyPlayedFiles.Count == 0)
            {
                if (_repeat > 0)
                {
                    var bitmapRepeat = new BitmapImage();
                    bitmapRepeat.BeginInit();
                    bitmapRepeat.UriSource = new Uri(@"Images/repeat-button.png", UriKind.Relative);
                    bitmapRepeat.EndInit();
                    _repeat = 0;

                    RepeatIcon.Source = bitmapRepeat;
                }

                if (_shuffle)
                {
                    var bitmapShuffle = new BitmapImage();
                    bitmapShuffle.BeginInit();
                    bitmapShuffle.UriSource = new Uri(@"Images/shuffle-button.png", UriKind.Relative);
                    bitmapShuffle.EndInit();
                    _shuffle = !_shuffle;

                    ShuffleIcon.Source = bitmapShuffle;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
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
            Properties.Settings.Default.PlayingVideoIndex = _playingPlaylistIndex;
            Properties.Settings.Default.Save();

            //
            saveMediaFiles(_playListDataFile, ListType.PlayList);
            saveMediaFiles(_recentPlayedDataFile, ListType.RecentPlayed);
        }
    }
}
