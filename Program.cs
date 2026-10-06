var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Create the SQLite database (rms.db) and sample data on first run
Db.Initialize();

// Serve the HTML/CSS/JS files from the wwwroot folder (index.html = login page)
app.UseDefaultFiles();
app.UseStaticFiles();

string[] validStatuses = { "Pending", "Processing", "Done", "Cancelled" };

// ---------------- Login ----------------
app.MapPost("/api/login", (LoginRequest req) =>
{
    var user = Db.Login(req.Username, req.Password, req.Role);
    return user is null
        ? Results.Json(new { error = "Invalid username, password or role." }, statusCode: 401)
        : Results.Ok(user);
});

// ---------------- Menu ----------------
app.MapGet("/api/menu", () => Db.GetMenu());

// ---------------- Orders ----------------

// All orders (staff/admin) or only one customer's orders when ?userId= is given
app.MapGet("/api/orders", (int? userId) => Db.GetOrders(userId));

app.MapGet("/api/orders/{id:int}", (int id) =>
{
    var order = Db.GetOrder(id);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

// Customer confirms the cart -> new order with status "Pending"
app.MapPost("/api/orders", (PlaceOrderRequest req) =>
{
    if (req.Items == null || !req.Items.Any(i => i.Quantity > 0))
        return Results.BadRequest(new { error = "Your cart is empty." });

    int orderId = Db.CreateOrder(req.UserId, req.Items);
    return Results.Ok(new { id = orderId });
});

// Staff changes the status: Processing / Done / Cancelled
app.MapPut("/api/orders/{id:int}/status", (int id, StatusRequest req) =>
{
    if (!validStatuses.Contains(req.Status))
        return Results.BadRequest(new { error = "Invalid status." });
    if (Db.GetOrder(id) is null)
        return Results.NotFound();

    Db.UpdateStatus(id, req.Status);
    return Results.Ok();
});

// Staff edits the items of an order (only while Pending or Processing)
app.MapPut("/api/orders/{id:int}/items", (int id, EditOrderRequest req) =>
{
    var order = Db.GetOrder(id);
    if (order is null)
        return Results.NotFound();
    if (order.Status == "Done" || order.Status == "Cancelled")
        return Results.BadRequest(new { error = "Finished or cancelled orders cannot be edited." });
    if (req.Items == null || !req.Items.Any(i => i.Quantity > 0))
        return Results.BadRequest(new { error = "An order needs at least one item. Cancel it instead." });

    Db.ReplaceOrderItems(id, req.Items);
    return Results.Ok(Db.GetOrder(id));
});

// ---------------- Admin stats ----------------
app.MapGet("/api/stats", () => Db.GetStats());

app.Run();
