namespace PetImages.Exceptions
{
    using System;

    public class DatabaseException : Exception
    {
        public DatabaseException()
        {
        }
        
        public DatabaseException(string message) : base(message)
        {
        }
    }
}
