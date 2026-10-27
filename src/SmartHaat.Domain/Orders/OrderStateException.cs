namespace SmartHaat.Domain.Orders;

public sealed class OrderStateException(string message) : Exception(message);
