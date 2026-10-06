// Shared helpers used by every page

const CURRENCY = "$";

// Call the C# API and return the JSON response (throws on error)
async function api(url, method = "GET", body = null) {
    const options = { method, headers: { "Content-Type": "application/json" } };
    if (body) options.body = JSON.stringify(body);

    const res = await fetch(url, options);
    const text = await res.text();
    const data = text ? JSON.parse(text) : null;

    if (!res.ok) throw new Error((data && data.error) || "Something went wrong.");
    return data;
}

// The logged in user is kept in the browser session
function getUser() {
    return JSON.parse(sessionStorage.getItem("user") || "null");
}

// Send the user back to the login page if they are not allowed on this page
function requireRole(...roles) {
    const user = getUser();
    if (!user || !roles.includes(user.role)) {
        window.location.href = "index.html";
        return null;
    }
    document.querySelectorAll("[data-username]").forEach(el => el.textContent = user.username);
    return user;
}

function logout() {
    sessionStorage.removeItem("user");
    window.location.href = "index.html";
}

function money(value) {
    return CURRENCY + Number(value).toFixed(2);
}

function statusBadge(status) {
    return `<span class="status status-${status}">${status}</span>`;
}

function itemsSummary(items) {
    return items.map(i => `${i.quantity} × ${i.name}`).join(", ");
}

// Escape text before putting it into HTML
function esc(text) {
    const div = document.createElement("div");
    div.textContent = text ?? "";
    return div.innerHTML;
}

function toast(message) {
    const el = document.createElement("div");
    el.className = "toast";
    el.textContent = message;
    document.body.appendChild(el);
    setTimeout(() => el.remove(), 2200);
}

// Shown if a food photo cannot be loaded
const FALLBACK_IMAGE = "data:image/svg+xml;utf8," + encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="600" height="340">
       <rect width="100%" height="100%" fill="#e8eef6"/>
       <text x="50%" y="52%" font-family="Segoe UI, Arial" font-size="28" fill="#1e3a5f" text-anchor="middle">🍽 Image unavailable</text>
     </svg>`);
