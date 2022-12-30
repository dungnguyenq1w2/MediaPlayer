using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPlayer
{
    public class MediaFile : INotifyPropertyChanged
    {
        private string _name = "";
        private string _totalTime = "00:00:00";
        private string _currentPlayedTime = "00:00:00";
        private string _filePath = "";
        private bool _isPlaying = false;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        public string TotalTime
        {
            get { return _totalTime; }
            set
            {
                _totalTime = value;
            }
        }
        public string CurrentPlayedTime
        {
            get { return _currentPlayedTime; }
            set
            {
                _currentPlayedTime = value;
            }
        }

        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
            set { _isPlaying = value; }
        }

        public MediaFile() { }
        public MediaFile(string name, string totalTime, string currentPlayedTime, string filePath)
        {
            this.Name = name;
            this.TotalTime = totalTime;
            this.CurrentPlayedTime = currentPlayedTime;
            this.FilePath = filePath;
        }

        public static TimeSpan getCurrentTime(string currentPlayedTime)
        {
            string[] tokens = currentPlayedTime.Split(new string[] { ":" }, StringSplitOptions.None);
            return new TimeSpan(0, Convert.ToInt32(tokens[0]), Convert.ToInt32(tokens[1]), Convert.ToInt32(tokens[2]), 0);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

    }
}
