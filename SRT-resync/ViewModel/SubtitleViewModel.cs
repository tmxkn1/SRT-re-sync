using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SubtitlesParser.Classes;

namespace SRT_resync
{
    public class SubtitleViewModel:INotifyPropertyChanged
    {
        private SubtitleModel SubtitleModel { get; set; }
        private string _selectedLineTime;
        private int _selectedLine;
        private string _searchText;
        private string _movieTime;
        private int _adjustMillisecond;
        private bool _isBackupBeforeSave = true;

        public SubtitleViewModel()
        {
            SubtitleModel = new SubtitleModel();
            ApplyCommand = new ApplyCommand(this);
            LoadFileCommand = new LoadFileCommand(this);
            SaveFileCommand = new SaveFileCommand(this);
        }

        #region commands
        public ICommand ApplyCommand { get; }

        public ICommand LoadFileCommand { get; }

        public ICommand SaveFileCommand { get; }
        #endregion

        #region can/is properties
        public bool CanApplyAdjustment => SubtitleModel.CanApplyAdjustment;
        public bool IsSubLoaded => SubtitleModel.IsSubLoaded;
        public bool IsAdjustmentApplied => SubtitleModel.IsAdjustmentApplied;

        public bool IsBackupBeforeSave
        {
            get => _isBackupBeforeSave;
            set
            {
                _isBackupBeforeSave = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region time properties
        public int SelectedLine
        {
            get => _selectedLine;
            set
            {
                _selectedLine = value;

                // set selected line time text
                if (FilteredDisplaySubList != null)
                    SelectedLineTime = _selectedLine >= 0 ? ToTimeString(FilteredDisplaySubList.ToList()[value].StartTime) : "";
                // set movie time text
                Adjustment = Adjustment;
                OnPropertyChanged();
            }
        }

        public string SelectedLineTime
        {
            get => _selectedLineTime;
            private set
            {
                if (value == _selectedLineTime) return;
                _selectedLineTime = value;
                OnPropertyChanged();
            }
        }

        public string MovieTime
        {
            get => _movieTime;
            set
            {
                SubtitleModel.CanApplyAdjustment = false;
                _movieTime = value;
                var val = ParseSrtTimecode(value);
                if (val == null)
                    return;

                _adjustMillisecond = (int)(val - ParseSrtTimecode(SelectedLineTime));
                SubtitleModel.CanApplyAdjustment = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Adjustment));
            }
        }

        public string Adjustment
        {
            get => ToTimeString(_adjustMillisecond);
            set
            {
                SubtitleModel.CanApplyAdjustment = false;
                var val = ParseSrtTimecode(value);
                if (val == null)
                    return;
                _adjustMillisecond = (int)val;

                SubtitleModel.CanApplyAdjustment = true;
                OnPropertyChanged();

                // try change movie line time
                if (!string.IsNullOrEmpty(SelectedLineTime))
                    _movieTime = ToTimeString(_adjustMillisecond + (int)ParseSrtTimecode(SelectedLineTime));
                OnPropertyChanged(nameof(MovieTime));
            }
        }
        #endregion

        #region sub text properties
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredDisplaySubList));
            }
        }

        public IEnumerable<SubtitleItemExt> FilteredDisplaySubList => string.IsNullOrEmpty(SearchText)
            ? SubtitleModel.SubList.ToList()
            : SubtitleModel.SubList.Where(x => x.Contains(SearchText));

        #endregion

        #region public methods
        public void LoadSubtitle()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "srt files (*.srt)|*.srt",
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() == false) return;

            SubtitleModel.IsSubLoaded = false;
            SubtitleModel = new SubtitleModel {FileName = dlg.FileName};
            SubtitleModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs args)
            {
                OnPropertyChanged(args.PropertyName == nameof(SubtitleModel.SubList)
                    ? nameof(FilteredDisplaySubList)
                    : args.PropertyName);
            };

            try
            {
                if (SubtitleModel.FileName == "") throw new Exception("No file has been specified.");

                List<SubtitleItem> subList;
                var parser = new SubtitlesParser.Classes.Parsers.SrtParser();
                using (var stream = File.OpenRead(SubtitleModel.FileName))
                {
                    subList = parser.ParseStream(stream, Encoding.Unicode);
                }

                SubtitleModel.SubList = subList.Select(a => new SubtitleItemExt(a)).ToList();
                SubtitleModel.IsSubLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ApplyAdjustment()
        {
            foreach (var sub in SubtitleModel.SubList)
            {
                sub.StartTime += _adjustMillisecond;
                sub.EndTime += _adjustMillisecond;
            }

            SubtitleModel.IsAdjustmentApplied = true;

            // reselect current line
            SelectedLine = _selectedLine;
            // set adjustment to 0
            Adjustment = "0";
            // update listbox
            OnPropertyChanged(nameof(FilteredDisplaySubList));
        }

        public void SaveSubtitle()
        {
            try
            {
                if (IsBackupBeforeSave)
                    BackupSubtitle();

                using (var stream = new StreamWriter(SubtitleModel.FileName))
                {
                    for (var i = 0; i < SubtitleModel.SubList.Count; i++)
                    {
                        var item = SubtitleModel.SubList[i];
                        stream.WriteLine(
                            $"{i + 1}{Environment.NewLine}{ToTimeString(item.StartTime)} --> {ToTimeString(item.EndTime)}");
                        foreach (var line in item.Lines)
                            stream.WriteLine(line);
                        stream.WriteLine("");
                    }
                }

                MessageBox.Show("File has been saved successfully!", "Saved", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string ToTimeString(int timeInMillisecond)
        {
            var t = TimeSpan.FromMilliseconds(timeInMillisecond);
            var fmt = timeInMillisecond < 0 ? @"\-hh\:mm\:ss\,fff" : @"hh\:mm\:ss\,fff";
            return t.ToString(fmt);
        }
        #endregion

        #region private methods
        private static int? ParseSrtTimecode(string timeString)
        {
            //var match = Regex.Match(s, "(?<a>[0-9]*):*(?<b>[0-9]*):*(?<c>[0-9]*)(?<f>[,\\.][0-9]+)?");
            //check for sign first
            int sign;
            if (timeString.StartsWith("-"))
            {
                sign = -1;
                timeString = timeString.Substring(1, timeString.Length - 1);
            }
            else
                sign = 1;

            timeString = timeString.Replace(',', '.');
            var timeParts = timeString.Split(':');
            if (timeParts.Length == 1)
            {
                if (!double.TryParse(timeString, out var s))
                    return -1;
                return (int)TimeSpan.FromSeconds(s).TotalMilliseconds * sign;
            }
            if (timeParts.Length == 2)
            {
                if (!double.TryParse(timeParts[1], out var s))
                    return -1;
                if (!int.TryParse(timeParts[0], out var m))
                    return -1;
                var ms = s % (int)s * 1000;
                ms = double.IsNaN(ms) ? s % 1 * 1000 : ms;
                return (int)new TimeSpan(0, 0, m, (int)s, (int)ms).TotalMilliseconds * sign;
            }
            if (timeParts.Length == 3)
            {
                if (!double.TryParse(timeParts[2], out var s))
                    return -1;
                if (!int.TryParse(timeParts[1], out var m))
                    return -1;
                if (!int.TryParse(timeParts[0], out var h))
                    return -1;
                var ms = s % (int)s * 1000;
                ms = double.IsNaN(ms) ? s % 1 * 1000 : ms;
                return (int)new TimeSpan(0, h, m, (int)s, (int)ms).TotalMilliseconds * sign;
            }

            if (timeParts.Length == 4)
            {
                if (!double.TryParse(timeParts[3], out var s))
                    return -1;
                if (!int.TryParse(timeParts[2], out var m))
                    return -1;
                if (!int.TryParse(timeParts[1], out var h))
                    return -1;
                if (!int.TryParse(timeParts[0], out var d))
                    return -1;
                var ms = s % (int)s * 1000;
                ms = double.IsNaN(ms) ? s % 1 * 1000 : ms;
                return (int)new TimeSpan(d, h, m, (int)s, (int)ms).TotalMilliseconds * sign;
            }

            return null;
        }

        private void BackupSubtitle()
        {
            if (SubtitleModel.FileName == "") throw new Exception("No file has been specified.");
            var buFile = Path.Combine(Path.GetDirectoryName(SubtitleModel.FileName),
                Path.GetFileNameWithoutExtension(SubtitleModel.FileName) + " (backup).srt");
            var i = 1;
            while (File.Exists(buFile))
            {
                buFile = Path.Combine(Path.GetDirectoryName(SubtitleModel.FileName),
                    Path.GetFileNameWithoutExtension(SubtitleModel.FileName) + " (backup " + i + ").srt");
                i++;
            }

            File.Move(SubtitleModel.FileName, buFile);
        }
        #endregion

        #region property change implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }


}