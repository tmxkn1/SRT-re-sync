using System;
using System.Windows.Input;

namespace SRT_resync
{
    internal class ApplyCommand:ICommand
    {
        private readonly SubtitleViewModel _subtitleViewModel;

        public ApplyCommand(SubtitleViewModel subtitleObj)
        {
            _subtitleViewModel = subtitleObj;
        }

        public bool CanExecute(object parameter)
        {
            return _subtitleViewModel.CanApplyAdjustment;
        }

        public void Execute(object parameter)
        {
            _subtitleViewModel.ApplyAdjustment();
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    internal class LoadFileCommand : ICommand
    {
        private readonly SubtitleViewModel _subtitleViewModel;

        public LoadFileCommand(SubtitleViewModel subtitleObj)
        {
            _subtitleViewModel = subtitleObj;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            _subtitleViewModel.LoadSubtitle();
        }

        public event EventHandler CanExecuteChanged;
    }

    internal class SaveFileCommand : ICommand
    {
        private readonly SubtitleViewModel _subtitleViewModel;

        public SaveFileCommand(SubtitleViewModel subtitleObj)
        {
            _subtitleViewModel = subtitleObj;
        }

        public bool CanExecute(object parameter)
        {
            return _subtitleViewModel.IsAdjustmentApplied;
        }

        public void Execute(object parameter)
        {
            _subtitleViewModel.SaveSubtitle();
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
