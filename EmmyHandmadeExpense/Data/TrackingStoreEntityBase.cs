namespace AssetManager.Data
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class TrackingStoreEntityBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
