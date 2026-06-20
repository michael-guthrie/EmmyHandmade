namespace AssetManager.Helpers
{
    using System;

    public class NotifyEventArgs<T> : EventArgs
    {
        public T Item { get; }

        public NotifyEventArgs(T item)
        {
            Item = item;
        }
    }
}
