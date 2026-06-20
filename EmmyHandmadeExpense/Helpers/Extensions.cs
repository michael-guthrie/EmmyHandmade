namespace AssetManager.Helpers
{
    using System.Windows;
    using System.Windows.Media;

    public static class Extensions
    {
        public static T GetParentOfType<T>(this DependencyObject element) where T : DependencyObject
        {
            while (true)
            {
                if (element == null) return null;
                var parent = VisualTreeHelper.GetParent(element);
                if (parent is T target) return target;
                element = parent;
            }
        }
    }
}
