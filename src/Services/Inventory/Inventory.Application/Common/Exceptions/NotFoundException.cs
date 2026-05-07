namespace Inventory.Application.Common.Exceptions;

public class NotFoundException(string message) : ApplicationExceptionBase(message);
