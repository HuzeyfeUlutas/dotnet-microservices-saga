namespace Order.Application.Common.Exceptions;

public class ConflictException(string message) : ApplicationExceptionBase(message);
