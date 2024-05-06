using APBD7.Models;

namespace APBD7.DTOs;

public record OrderDTO(int IdOrder, int IdProduct, int Amount, DateTime CreatedAt, DateTime? FulfilledAt)
{
    public OrderDTO(Order order): this(order.IdOrder, order.IdProduct, order.Amount, order.CreatedAt, order.FulfilledAt){}
}