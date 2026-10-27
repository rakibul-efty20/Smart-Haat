namespace SmartHaat.Domain.Orders;

public sealed class DeliveryZone
{
    private DeliveryZone() { }
    public DeliveryZone(string code, string name, decimal fee)
    { Update(code, name, fee); }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Fee { get; private set; }
    public bool IsActive { get; private set; } = true;
    public void Update(string code, string name, decimal fee)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 40) throw new ArgumentException("A zone code of at most 40 characters is required.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120) throw new ArgumentException("A zone name of at most 120 characters is required.");
        if (fee < 0) throw new ArgumentOutOfRangeException(nameof(fee));
        Code = code.Trim().ToUpperInvariant(); Name = name.Trim(); Fee = decimal.Round(fee, 2, MidpointRounding.AwayFromZero);
    }
    public void SetActive(bool active) => IsActive = active;
}
