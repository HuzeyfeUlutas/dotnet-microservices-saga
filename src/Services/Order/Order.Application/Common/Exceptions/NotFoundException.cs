namespace Order.Application.Common.Exceptions;

public class NotFoundException(string message) : ApplicationExceptionBase(message);
