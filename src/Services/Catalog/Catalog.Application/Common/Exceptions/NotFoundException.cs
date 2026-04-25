namespace Catalog.Application.Common.Exceptions;

public class NotFoundException(string message) : ApplicationExceptionBase(message);
