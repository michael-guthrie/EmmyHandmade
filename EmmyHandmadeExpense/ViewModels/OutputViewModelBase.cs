namespace AssetManager.ViewModels
{
    using AssetManager.Data;
    using GalaSoft.MvvmLight.Command;
    using System;
    using System.Collections.Generic;

    public abstract class OutputViewModelBase : ExpenseViewModelBase
    {
        public OutRecord OutRecord
        {
            get => _OutRecord;
            protected set
            {
                if (value != _OutRecord)
                {
                    _OutRecord = value;
                    RaisePropertyChanged(nameof(OutRecord));
                }
            }
        }
        private OutRecord _OutRecord;

        public RelayCommand SaveCommand { get; }

        public event EventHandler SaveCompleted;

        protected OutputViewModelBase()
        {
            SaveCommand = new RelayCommand(OnSave, CanSave);
        }

        protected virtual async void OnSave()
        {
            try
            {
                await DataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                MessengerInstance.Send<Exception>(ex);
                return;
            }
            MessengerInstance.Send<IEnumerable<DateTime>>(new[] { OutRecord.Date }, Helpers.MessengerToken.Expense);
            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        protected virtual bool CanSave() => true;
    }
}
