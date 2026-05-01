const API = '../api/index.php?r=';
let schedulesData = [];

async function api(route, options = {}) {
  const res = await fetch(API + route, {
    headers: { 'Content-Type': 'application/json' },
    ...options
  });

  return res.json();
}

async function login() {
  const email = document.getElementById("email").value.trim();
  const password = document.getElementById("password").value.trim();

  const data = await api('login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  });

  if (data.user) {
    localStorage.setItem("user", JSON.stringify(data.user));

    if (data.user.role_id == 5 || data.user.role === "System Admin") {
      window.location.href = "admin-dashboard.html";
    } else {
      window.location.href = "dashboard.html";
    }
  } else {
    alert(data.error || "Invalid login");
  }
}

function logout() {
  localStorage.removeItem("user");
  window.location.href = "index.html";
}

function confirmLogout() {
  if (confirm("Are you sure you want to log out?")) {
    logout();
  }
}

function showPage(pageId, link) {
  document.querySelectorAll(".page-section").forEach(section => {
    section.style.display = "none";
  });

  const selectedPage = document.getElementById(pageId);

  if (!selectedPage) {
    alert("Page section not found: " + pageId);
    return;
  }

  selectedPage.style.display = "block";

  document.querySelectorAll(".nav-link").forEach(nav => {
    nav.classList.remove("active");
  });

  if (link) {
    link.classList.add("active");
  }

  if (pageId === "profilePage") loadProfile();
  if (pageId === "paymentsPage") loadPayments();
}

async function loadDashboard() {
  const user = JSON.parse(localStorage.getItem("user"));

  if (!user) {
    window.location.href = "index.html";
    return;
  }

  const welcomeText = document.getElementById("welcomeText");
  if (welcomeText) {
    welcomeText.innerText = "Welcome, " + user.full_name;
  }

  await loadSchedules();
  await loadBookings();
  await loadPayments();
}

async function loadSchedules() {
  const table = document.getElementById("schedulesTable");
  if (!table) return;

  const data = await api('schedules');
  schedulesData = Array.isArray(data) ? data : [];

  const tripCount = document.getElementById("tripCount");
  if (tripCount) {
    tripCount.innerText = schedulesData.length;
  }

  renderSchedules(schedulesData);
}

function renderSchedules(data) {
  const table = document.getElementById("schedulesTable");
  if (!table) return;

  if (!data || data.length === 0) {
    table.innerHTML = `
      <tr>
        <td colspan="6" class="text-center text-muted py-4">
          No available schedules found.
        </td>
      </tr>
    `;
    return;
  }

  table.innerHTML = data.map(s => {
    const availableSeats = Math.max(Number(s.available_seats), 0);

    return `
      <tr>
        <td><b>${s.origin}</b> → ${s.destination}</td>
        <td>${s.departure_time}</td>
        <td>${s.vehicle_type}</td>
        <td>${availableSeats}</td>
        <td>₱${s.fare}</td>
        <td>
          ${
            availableSeats <= 0
              ? `<button class="btn btn-secondary btn-sm" disabled>Full</button>`
              : `<button class="btn btn-success btn-sm" onclick="book(${s.id}, ${availableSeats})">Book Now</button>`
          }
        </td>
      </tr>
    `;
  }).join('');
}

function filterSchedules() {
  const searchInput = document.getElementById("searchRoute");
  if (!searchInput) return;

  const search = searchInput.value.toLowerCase();

  const filtered = schedulesData.filter(s => {
    return `${s.origin} ${s.destination}`.toLowerCase().includes(search);
  });

  renderSchedules(filtered);
}

async function book(scheduleId, availableSeats) {
  if (availableSeats <= 0) {
    alert("This schedule is already full.");
    return;
  }

  const seats = prompt("How many seats do you want to book?");
  if (!seats || Number(seats) <= 0) return;

  if (Number(seats) > availableSeats) {
    alert("Not enough available seats.");
    return;
  }

  const user = JSON.parse(localStorage.getItem("user"));

  const result = await api('bookings', {
    method: 'POST',
    body: JSON.stringify({
      schedule_id: scheduleId,
      user_id: user.id,
      seats: Number(seats)
    })
  });

  if (result.error) {
    alert(result.error);
    return;
  }

  alert("Booking successful!");
  await loadDashboard();

  const bookingLink = document.querySelectorAll(".nav-link")[2];
  showPage("bookingsPage", bookingLink);
}

async function loadBookings() {
  const table = document.getElementById("bookingsTable");
  if (!table) return;

  const user = JSON.parse(localStorage.getItem("user"));
  const data = await api('bookings&user_id=' + user.id);
  const bookings = Array.isArray(data) ? data : [];

  const bookingCount = document.getElementById("bookingCount");
  if (bookingCount) {
    bookingCount.innerText = bookings.length;
  }

  const completedCount = document.getElementById("completedCount");
  if (completedCount) {
    completedCount.innerText = bookings.filter(b =>
      (b.status || '').toLowerCase() === 'completed'
    ).length;
  }

  if (bookings.length === 0) {
    table.innerHTML = `
      <tr>
        <td colspan="6" class="text-center text-muted py-4">
          No recent bookings yet.
        </td>
      </tr>
    `;
    return;
  }

  table.innerHTML = bookings.map(b => `
    <tr>
      <td>${b.id}</td>
      <td>${b.full_name}</td>
      <td><b>${b.origin}</b> → ${b.destination}</td>
      <td>${b.seats}</td>
      <td>₱${b.total_amount}</td>
      <td>
        <span class="badge-status ${getStatusClass(b.status)}">
          ${b.status}
        </span>
        ${
          (b.status || '').toLowerCase() !== 'paid'
            ? `<button class="btn btn-primary btn-sm ms-2" onclick="payBooking(${b.id}, ${b.total_amount})">Pay Now</button>`
            : ''
        }
      </td>
    </tr>
  `).join('');
}

function getStatusClass(status) {
  status = (status || '').toLowerCase();

  if (status === 'confirmed') return 'status-confirmed';
  if (status === 'cancelled') return 'status-cancelled';
  if (status === 'completed') return 'status-confirmed';
  if (status === 'paid') return 'status-confirmed';

  return 'status-pending';
}

async function loadPayments() {
  const user = JSON.parse(localStorage.getItem("user"));
  if (!user) return;

  const data = await api("payments&user_id=" + user.id);
  const payments = Array.isArray(data) ? data : [];

  const paymentCount = document.getElementById("paymentCount");
  if (paymentCount) {
    paymentCount.innerText = payments.length;
  }

  const table = document.getElementById("paymentsTable");
  if (!table) return;

  if (payments.length === 0) {
    table.innerHTML = `
      <tr>
        <td colspan="6" class="text-center text-muted py-4">
          No payment records yet.
        </td>
      </tr>
    `;
    return;
  }

  table.innerHTML = payments.map(p => `
    <tr>
      <td>${p.id}</td>
      <td>${p.booking_id}</td>
      <td><b>${p.origin}</b> → ${p.destination}</td>
      <td>₱${p.amount}</td>
      <td>${p.method}</td>
      <td><span class="badge-status status-confirmed">${p.status}</span></td>
    </tr>
  `).join('');
}

async function payBooking(bookingId, amount) {
  const method = prompt("Enter payment method: Cash, GCash, or Card");

  if (!method) return;

  const user = JSON.parse(localStorage.getItem("user"));

  const result = await api("payments", {
    method: "POST",
    body: JSON.stringify({
      booking_id: bookingId,
      user_id: user.id,
      method: method
    })
  });

  if (result.error) {
    alert(result.error);
    return;
  }

  alert("Payment successful!");
  await loadDashboard();

  const paymentsLink = document.querySelectorAll(".nav-link")[3];
  showPage("paymentsPage", paymentsLink);
}

function loadProfile() {
  const user = JSON.parse(localStorage.getItem("user"));

  const profileName = document.getElementById("profileName");
  const profileEmail = document.getElementById("profileEmail");

  if (profileName) profileName.value = user.full_name || "";
  if (profileEmail) profileEmail.value = user.email || "";
}

function saveProfile() {
  const user = JSON.parse(localStorage.getItem("user"));

  user.full_name = document.getElementById("profileName").value;

  localStorage.setItem("user", JSON.stringify(user));

  const welcomeText = document.getElementById("welcomeText");
  if (welcomeText) {
    welcomeText.innerText = "Welcome, " + user.full_name;
  }

  alert("Profile updated successfully.");
}

function showRegister() {
  document.querySelector(".login-box").style.display = "none";
  document.getElementById("registerBox").style.display = "block";
}

function showLogin() {
  document.querySelector(".login-box").style.display = "block";
  document.getElementById("registerBox").style.display = "none";
}

async function register() {
  const full_name = document.getElementById("regName").value.trim();
  const email = document.getElementById("regEmail").value.trim();
  const password = document.getElementById("regPassword").value.trim();

  if (!full_name || !email || !password) {
    alert("Please fill in all fields.");
    return;
  }

  const data = await api("register", {
    method: "POST",
    body: JSON.stringify({
      full_name: full_name,
      email: email,
      password: password
    })
  });

  if (data.id || data.message) {
    alert("Account created successfully. You can now login.");
    showLogin();

    document.getElementById("email").value = email;
    document.getElementById("password").value = password;
  } else {
    alert(data.error || "Registration failed.");
  }
}