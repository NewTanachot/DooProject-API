namespace DooProject.CustomExceptions
{
    public class DuplicateException : Exception
    {
        public DuplicateException() { }

        public DuplicateException(string message) : base(message) { }

        public DuplicateException(string message, Exception inner) : base(message, inner) { }
    }
}
