# Simple RMS

A small restaurant management system: HTML/CSS/JS frontend, C# (ASP.NET Core minimal API) backend, SQLite database.

## Run

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```
dotnet run
```

Open the URL shown in the console (e.g. http://localhost:5000). The database file `rms.db` is created automatically with sample users and menu items. Delete it to reset everything.

## Demo accounts

| Role     | Username | Password  |
|----------|----------|-----------|
| Customer | john     | john123   |
| Customer | mary     | mary123   |
| Staff    | staff    | staff123  |
| Admin    | admin    | admin123  |

## Structure

```
Program.cs        API endpoints
Db.cs             All SQLite code (tables, sample data, queries)
Models.cs         Data classes
wwwroot/
  index.html      Login (username, password, role)
  customer.html   Menu cards, cart, my orders with status
  staff.html      Orders list, change status, edit order items
  admin.html      Stats dashboard + link to staff screen
  css/style.css   Styles (white + navy + amber)
  js/common.js    Shared helpers
```

## Order statuses

`Pending` (new) → `Processing` → `Done`, or `Cancelled`. Revenue counts only `Done` orders.
Orders can be edited by staff only while Pending or Processing.

## Notes

This is a learning/demo project: passwords are stored as plain text and the API does not check who is calling it. Add hashing and real authentication before using it for anything real.
