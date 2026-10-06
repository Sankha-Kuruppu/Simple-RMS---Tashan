// ---------------- Request models (JSON sent by the browser) ----------------
public record LoginRequest(string Username, string Password, string Role);
public record CartItem(int MenuItemId, int Quantity);
public record PlaceOrderRequest(int UserId, List<CartItem> Items);
public record StatusRequest(string Status);
public record EditOrderRequest(List<CartItem> Items);

// ---------------- Database models ----------------
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
}

public class MenuItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public double Price { get; set; }
    public string ImageUrl { get; set; } = "";
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Customer { get; set; } = "";
    public string Status { get; set; } = "";
    public double Total { get; set; }
    public string CreatedAt { get; set; } = "";
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public int Quantity { get; set; }
}
