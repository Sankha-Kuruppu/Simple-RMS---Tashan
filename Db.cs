using Microsoft.Data.Sqlite;

// All SQLite database code lives here.
public static class Db
{
    const string ConnectionString = "Data Source=rms.db";

    static SqliteConnection Open()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    // Small helper: build a command with parameters, e.g. Command(conn, "... WHERE Id=$id", ("$id", 5))
    static SqliteCommand Command(SqliteConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var p in parameters)
            cmd.Parameters.AddWithValue(p.Name, p.Value);
        return cmd;
    }

    // ---------------- Setup ----------------

    public static void Initialize()
    {
        using var conn = Open();

        Command(conn, @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL,
                Password TEXT NOT NULL,
                Role TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS MenuItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                Category TEXT NOT NULL,
                Price REAL NOT NULL,
                ImageUrl TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Status TEXT NOT NULL,
                Total REAL NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS OrderItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId INTEGER NOT NULL,
                MenuItemId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Price REAL NOT NULL,
                Quantity INTEGER NOT NULL
            );").ExecuteNonQuery();

        // Sample users (only added when the table is empty)
        if (Convert.ToInt32(Command(conn, "SELECT COUNT(*) FROM Users").ExecuteScalar()) == 0)
        {
            var users = new[]
            {
                ("admin", "admin123", "admin"),
                ("staff", "staff123", "staff"),
                ("john", "john123", "customer"),
                ("mary", "mary123", "customer"),
            };
            foreach (var (username, password, role) in users)
                Command(conn, "INSERT INTO Users (Username, Password, Role) VALUES ($u, $p, $r)",
                    ("$u", username), ("$p", password), ("$r", role)).ExecuteNonQuery();
        }

        // Sample menu (only added when the table is empty)
        if (Convert.ToInt32(Command(conn, "SELECT COUNT(*) FROM MenuItems").ExecuteScalar()) == 0)
        {
            const string img = "https://images.unsplash.com/";
            const string size = "?auto=format&fit=crop&w=600&q=70";
            var menu = new[]
            {
                ("Classic Beef Burger", "Grilled beef patty, cheddar, lettuce, tomato and house sauce.", "Mains", 9.50, img + "photo-1568901346375-23c9450c58cd" + size),
                ("Margherita Pizza", "Tomato, fresh mozzarella and basil on a thin crust.", "Mains", 11.00, img + "photo-1565299624946-b28f40a0ae38" + size),
                ("Creamy Pasta", "Penne in a creamy garlic and parmesan sauce.", "Mains", 10.50, img + "photo-1621996346565-e3dbc646d9a9" + size),
                ("Grilled Steak", "Ribeye steak with roasted vegetables and pepper sauce.", "Mains", 18.90, img + "photo-1600891964092-4316c288032e" + size),
                ("Sushi Platter", "Chef's selection of nigiri and maki rolls.", "Mains", 16.00, img + "photo-1579584425555-c3ce17fd4351" + size),
                ("Garden Salad", "Mixed greens, avocado, cherry tomatoes and vinaigrette.", "Starters", 7.00, img + "photo-1512621776951-a57141f2eefd" + size),
                ("French Fries", "Crispy golden fries with sea salt.", "Starters", 3.90, img + "photo-1573080496219-bb080dd4f877" + size),
                ("Pancake Stack", "Fluffy pancakes with berries and maple syrup.", "Desserts", 6.50, img + "photo-1567620905732-2d1ec7ab7445" + size),
                ("Glazed Donuts", "Two freshly made donuts with sweet glaze.", "Desserts", 4.20, img + "photo-1551024601-bec78aea704b" + size),
                ("Fresh Mojito", "Lime, mint and soda over crushed ice (non-alcoholic).", "Drinks", 4.50, img + "photo-1544145945-f90425340c7e" + size),
            };
            foreach (var (name, desc, category, price, image) in menu)
                Command(conn, "INSERT INTO MenuItems (Name, Description, Category, Price, ImageUrl) VALUES ($n, $d, $c, $p, $i)",
                    ("$n", name), ("$d", desc), ("$c", category), ("$p", price), ("$i", image)).ExecuteNonQuery();
        }
    }

    // ---------------- Users ----------------

    public static User? Login(string username, string password, string role)
    {
        using var conn = Open();
        using var reader = Command(conn,
            "SELECT Id, Username, Role FROM Users WHERE Username = $u AND Password = $p AND Role = $r",
            ("$u", username ?? ""), ("$p", password ?? ""), ("$r", role ?? "")).ExecuteReader();

        if (!reader.Read()) return null;
        return new User { Id = reader.GetInt32(0), Username = reader.GetString(1), Role = reader.GetString(2) };
    }

    // ---------------- Menu ----------------

    public static List<MenuItem> GetMenu()
    {
        var items = new List<MenuItem>();
        using var conn = Open();
        using var reader = Command(conn,
            "SELECT Id, Name, Description, Category, Price, ImageUrl FROM MenuItems ORDER BY Id").ExecuteReader();

        while (reader.Read())
        {
            items.Add(new MenuItem
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                Category = reader.GetString(3),
                Price = reader.GetDouble(4),
                ImageUrl = reader.GetString(5)
            });
        }
        return items;
    }

    // ---------------- Orders ----------------

    public static List<Order> GetOrders(int? userId = null)
    {
        var orders = new List<Order>();
        using var conn = Open();

        string sql = @"SELECT o.Id, o.UserId, u.Username, o.Status, o.Total, o.CreatedAt
                       FROM Orders o JOIN Users u ON u.Id = o.UserId";
        if (userId != null) sql += " WHERE o.UserId = $userId";
        sql += " ORDER BY o.Id DESC";

        using (var reader = Command(conn, sql, ("$userId", userId ?? 0)).ExecuteReader())
        {
            while (reader.Read())
            {
                orders.Add(new Order
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Customer = reader.GetString(2),
                    Status = reader.GetString(3),
                    Total = reader.GetDouble(4),
                    CreatedAt = reader.GetString(5)
                });
            }
        }

        foreach (var order in orders)
            order.Items = GetOrderItems(conn, order.Id);

        return orders;
    }

    public static Order? GetOrder(int id)
    {
        return GetOrders().FirstOrDefault(o => o.Id == id);
    }

    static List<OrderItem> GetOrderItems(SqliteConnection conn, int orderId)
    {
        var items = new List<OrderItem>();
        using var reader = Command(conn,
            "SELECT MenuItemId, Name, Price, Quantity FROM OrderItems WHERE OrderId = $id ORDER BY Id",
            ("$id", orderId)).ExecuteReader();

        while (reader.Read())
        {
            items.Add(new OrderItem
            {
                MenuItemId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Price = reader.GetDouble(2),
                Quantity = reader.GetInt32(3)
            });
        }
        return items;
    }

    public static int CreateOrder(int userId, List<CartItem> items)
    {
        using var conn = Open();
        Command(conn, "INSERT INTO Orders (UserId, Status, Total, CreatedAt) VALUES ($u, 'Pending', 0, $t)",
            ("$u", userId), ("$t", DateTime.Now.ToString("yyyy-MM-dd HH:mm"))).ExecuteNonQuery();

        int orderId = Convert.ToInt32(Command(conn, "SELECT last_insert_rowid()").ExecuteScalar());
        SaveItems(conn, orderId, items);
        return orderId;
    }

    public static void ReplaceOrderItems(int orderId, List<CartItem> items)
    {
        using var conn = Open();
        Command(conn, "DELETE FROM OrderItems WHERE OrderId = $id", ("$id", orderId)).ExecuteNonQuery();
        SaveItems(conn, orderId, items);
    }

    // Saves the items using the current menu name/price, then recalculates the order total
    static void SaveItems(SqliteConnection conn, int orderId, List<CartItem> items)
    {
        var menu = GetMenu().ToDictionary(m => m.Id);

        foreach (var item in items)
        {
            if (item.Quantity <= 0 || !menu.ContainsKey(item.MenuItemId)) continue;
            var menuItem = menu[item.MenuItemId];

            Command(conn, @"INSERT INTO OrderItems (OrderId, MenuItemId, Name, Price, Quantity)
                            VALUES ($o, $m, $n, $p, $q)",
                ("$o", orderId), ("$m", menuItem.Id), ("$n", menuItem.Name),
                ("$p", menuItem.Price), ("$q", item.Quantity)).ExecuteNonQuery();
        }

        Command(conn, @"UPDATE Orders SET Total =
                            (SELECT IFNULL(SUM(Price * Quantity), 0) FROM OrderItems WHERE OrderId = $id)
                        WHERE Id = $id", ("$id", orderId)).ExecuteNonQuery();
    }

    public static void UpdateStatus(int orderId, string status)
    {
        using var conn = Open();
        Command(conn, "UPDATE Orders SET Status = $s WHERE Id = $id", ("$s", status), ("$id", orderId)).ExecuteNonQuery();
    }

    // ---------------- Admin stats ----------------

    public static object GetStats()
    {
        using var conn = Open();

        double Scalar(string sql) => Convert.ToDouble(Command(conn, sql).ExecuteScalar());

        // Top 5 selling items (cancelled orders are not counted)
        var topItems = new List<object>();
        using (var reader = Command(conn, @"
            SELECT i.Name, SUM(i.Quantity) AS Qty, SUM(i.Price * i.Quantity) AS Amount
            FROM OrderItems i JOIN Orders o ON o.Id = i.OrderId
            WHERE o.Status <> 'Cancelled'
            GROUP BY i.Name ORDER BY Qty DESC LIMIT 5").ExecuteReader())
        {
            while (reader.Read())
                topItems.Add(new { name = reader.GetString(0), quantity = reader.GetInt32(1), amount = reader.GetDouble(2) });
        }

        string today = DateTime.Now.ToString("yyyy-MM-dd");

        return new
        {
            totalOrders = (int)Scalar("SELECT COUNT(*) FROM Orders"),
            pending = (int)Scalar("SELECT COUNT(*) FROM Orders WHERE Status = 'Pending'"),
            processing = (int)Scalar("SELECT COUNT(*) FROM Orders WHERE Status = 'Processing'"),
            done = (int)Scalar("SELECT COUNT(*) FROM Orders WHERE Status = 'Done'"),
            cancelled = (int)Scalar("SELECT COUNT(*) FROM Orders WHERE Status = 'Cancelled'"),
            // Revenue = money from completed (Done) orders
            revenue = Scalar("SELECT IFNULL(SUM(Total), 0) FROM Orders WHERE Status = 'Done'"),
            todayRevenue = Scalar($"SELECT IFNULL(SUM(Total), 0) FROM Orders WHERE Status = 'Done' AND CreatedAt LIKE '{today}%'"),
            todayOrders = (int)Scalar($"SELECT COUNT(*) FROM Orders WHERE CreatedAt LIKE '{today}%'"),
            topItems,
            recentOrders = GetOrders().Take(8)
        };
    }
}
