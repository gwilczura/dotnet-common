namespace Wilczura.Common.Exceptions;
public class CustomException(string message, Exception? innerException = null) : Exception(message, innerException)
{
}

