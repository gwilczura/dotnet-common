﻿namespace Wilczura.Common.Exceptions;
public class CustomException : Exception
{
    public CustomException(string message, Exception? innerException = null) : base(message, innerException) { }
}
