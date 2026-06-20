namespace AssetManager.ViewModels
{
    using GalaSoft.MvvmLight;
    using System;
    using System.Threading.Tasks;
    using System.Windows;

    public abstract class ExpenseViewModelBase : ViewModelBase, IDisposable
    {
        private readonly Lazy<Data.TrackingStore> _DataContext =
            new Lazy<Data.TrackingStore>(() => new Data.TrackingStore());

        protected Data.TrackingStore DataContext => _DataContext.Value;

        protected System.Windows.Threading.Dispatcher Dispatcher => Application.Current.Dispatcher;

        public bool HasChanges => DataContext.ChangeTracker.HasChanges();

        public bool IsInitializing
        {
            get => _IsInitializing;
            protected set
            {
                if (value != _IsInitializing)
                {
                    _IsInitializing = value;
                    RaisePropertyChanged(nameof(IsInitializing));
                }
            }
        }
        private bool _IsInitializing;

        public event EventHandler Initialized;

        public async void InitializeBackground()
        {
            IsInitializing = true;
            try
            {
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                MessengerInstance.Send<Exception>(ex);
            }
            IsInitializing = false;
            Initialized?.Invoke(this, EventArgs.Empty);
        }

        protected abstract Task InitializeAsync();

        public override void Cleanup()
        {
            Dispose(true);
            base.Cleanup();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_DataContext.IsValueCreated)
                    {
                        _DataContext.Value.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ExpenseViewModelBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
