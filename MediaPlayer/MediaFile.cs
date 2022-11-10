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

        public MediaFile(string name, string tolalTime, string currentPlayedTime)
        {
            this.Name = name;
            this.totalTime = tolalTime;
            this.currentPlayedTime = currentPlayedTime;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
