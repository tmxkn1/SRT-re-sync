using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using SRT_resync.Annotations;
using SubtitlesParser.Classes;

namespace SRT_resync
{
    public class SubtitleModel : INotifyPropertyChanged
    {
        private List<SubtitleItemExt> _subList;
        private bool _isSubLoaded;
        private bool _canApplyAdjustment;

        public string FileName { get; set; }

        public List<SubtitleItemExt> SubList
        {
            get => _subList;
            set
            {
                _subList = value;
                OnPropertyChanged();
            }
        }

        public Encoding Encoding { get; set; }

        public bool IsSubLoaded
        {
            get => _isSubLoaded;
            set
            {
                if (value == _isSubLoaded) return;
                _isSubLoaded = value;
                OnPropertyChanged();
            }
        }

        public bool CanApplyAdjustment
        {
            get => _canApplyAdjustment;
            internal set
            {
                if (value == _canApplyAdjustment) return;
                _canApplyAdjustment = value;
                OnPropertyChanged();
            }
        }

        public bool IsAdjustmentApplied { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SubtitleItemExt : SubtitleItem
    {
        public SubtitleItemExt(SubtitleItem item)
        {
            StartTime = item.StartTime;
            EndTime = item.EndTime;
            Lines = item.Lines.ToList();
        }

        public override string ToString()
        {
            var stime = TimeSpan.FromMilliseconds(StartTime);
            var etime = TimeSpan.FromMilliseconds(EndTime);
            return $"{stime:g} --> {etime:g}{Environment.NewLine}{string.Join(Environment.NewLine, Lines)}";
        }

        public bool Contains(string s)
        {
            foreach (var l in Lines)
            {
                if (l.ToUpper().Contains(s.ToUpper()))
                    return true;
            }

            return false;
        }
    }
}