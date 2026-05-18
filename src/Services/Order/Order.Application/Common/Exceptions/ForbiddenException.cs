namespace Order.Application.Common.Exceptions;

public class ForbiddenException(string message) : ApplicationExceptionBase(message);
