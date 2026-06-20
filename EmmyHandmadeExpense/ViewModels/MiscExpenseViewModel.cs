namespace AssetManager.ViewModels
{
    using AssetManager.Data;
    using GalaSoft.MvvmLight.Command;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class MiscExpenseViewModel : ExpenseViewModelBase
    {
        public ObservableCollection<MiscellaneousExpense> Expenses { get; private set; }

        public RelayCommand SaveCommand { get; }

        private bool CanSave
        {
            get => _CanSave;
            set
            {
                if (value != _CanSave)
                {
                    _CanSave = value;
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private bool _CanSave = true;

        public event EventHandler SaveStarted;
        public event EventHandler SaveCompleted;
        public event EventHandler<Helpers.ValidationEventArgs> ValidationFailed;

        public MiscExpenseViewModel()
        {
            SaveCommand = new RelayCommand(OnSave, () => CanSave);
        }

        protected override async Task InitializeAsync()
        {
            Expenses = new ObservableCollection<MiscellaneousExpense>(
                await Task.Run(() => DataContext.MiscellaneousExpenses.ToList()));
            Expenses.CollectionChanged += Items_CollectionChanged;
            RaisePropertyChanged(nameof(Expenses));
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var i in e.NewItems)
                    {
                        var exp = (MiscellaneousExpense)i;
                        if (exp.DateOfExpense == new DateTime())
                        {
                            exp.DateOfExpense = DateTime.Today;
                        }
                        DataContext.MiscellaneousExpenses.Add(exp);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var i in e.OldItems)
                    {
                        DataContext.MiscellaneousExpenses.Remove((MiscellaneousExpense)i);
                    }
                    break;
            }
        }

        private async void OnSave()
        {
            CanSave = false;
            SaveStarted?.Invoke(this, EventArgs.Empty);

            try
            {
                // Check that all items are valid.
                string valErrors = await Task.Run(() =>
                    {
                        var sb = new System.Text.StringBuilder();
                        if (Expenses.Any(exp => string.IsNullOrEmpty(exp.Description)))
                        {
                            sb.AppendLine("You have one or more expenses missing a description.");
                        }
                        return sb.ToString();
                    });
                if (valErrors.Length > 0)
                {
                    ValidationFailed?.Invoke(this, new Helpers.ValidationEventArgs(valErrors));
                    return;
                }

                try
                {
                    await DataContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    MessengerInstance.Send<Exception>(ex);
                    return;
                }
                MessengerInstance.Send<IEnumerable<DateTime>>(Expenses.Select(exp => exp.DateOfExpense), Helpers.MessengerToken.Expense);
                SaveCompleted?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                CanSave = true;
            }
        }
    }
}
