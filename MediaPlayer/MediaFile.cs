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
        public string Name { get; set; }
        public string totalTime { get; set; }
        public string currentPlayedTime { get; set; }

        public string filePath { get; set; }

        public MediaFile(string name, string tolalTime, string currentPlayedTime, string filePath)
        {
            this.Name = name;
            this.totalTime = tolalTime;
            this.currentPlayedTime = currentPlayedTime;
            this.filePath = filePath;
        }

        public static TimeSpan getCurrentTime(string currentPlayedTime)
        {
            string[] tokens = currentPlayedTime.Split(new string[] { ":" }, StringSplitOptions.None);
            return new TimeSpan(0, Convert.ToInt32(tokens[0]), Convert.ToInt32(tokens[1]), Convert.ToInt32(tokens[2]), 0);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
