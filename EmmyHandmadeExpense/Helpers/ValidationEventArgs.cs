namespace AssetManager.Helpers
{
    public class ValidationEventArgs : System.EventArgs
    {
        public string ErrorMessage { get; }

        public ValidationEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}
