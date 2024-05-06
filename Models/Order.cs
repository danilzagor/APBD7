namespace APBD7.Models;

public class Order
{
    public int IdOrder { get; private set; }
    public int IdProduct { get; private set; }
    public int Amount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime FulfilledAt { get; private set; }
}