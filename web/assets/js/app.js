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
    window.location.href = "dashboard.html";
  } else {
    alert(data.error || "Invalid login");
  }
}

function logout() {
  localStorage.removeItem("user");
  window.location.href = "index.html";
}

async function loadDashboard() {
  const user = JSON.parse(localStorage.getItem("user"));

  if (!user) {
    window.location.href = "index.html";
    return;
  }

  document.getElementById("welcomeText").innerText = `Welcome, ${user.full_name}`;

  await loadSchedules();
  await loadBookings();
}

async function loadSchedules() {
  const table = document.getElementById("schedulesTable");
  if (!table) return;

  const data = await api('schedules');
  schedulesData = data;

  document.getElementById("tripCount").innerText = data.length;

  renderSchedules(data);
}

function renderSchedules(data) {
  const table = document.getElementById("schedulesTable");

  if (!data.length) {
    table.innerHTML = `
      <tr>
        <td colspan="6" class="text-center text-muted py-4">
          No available schedules found.
        </td>
      </tr>
    `;
    return;
  }

  table.innerHTML = data.map(s => `
    <tr>
      <td><b>${s.origin}</b> → ${s.destination}</td>
      <td>${s.departure_time}</td>
      <td>${s.vehicle_type}</td>
      <td>${s.available_seats}</td>
      <td>₱${s.fare}</td>
      <td>
        <button class="btn btn-success btn-sm" onclick="book(${s.id})">
          Book Now
        </button>
      </td>
    </tr>
  `).join('');
}

function filterSchedules() {
  const search = document.getElementById("searchRoute").value.toLowerCase();

  const filtered = schedulesData.filter(s => {
    return `${s.origin} ${s.destination}`.toLowerCase().includes(search);
  });

  renderSchedules(filtered);
}

async function book(scheduleId) {
  const seats = prompt("How many seats do you want to book?");
  if (!seats || seats <= 0) return;

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
  loadDashboard();
}

async function loadBookings() {
  const table = document.getElementById("bookingsTable");
  if (!table) return;

  const data = await api('bookings');

  document.getElementById("bookingCount").innerText = data.length;

  if (!data.length) {
    table.innerHTML = `
      <tr>
        <td colspan="6" class="text-center text-muted py-4">
          No recent bookings yet.
        </td>
      </tr>
    `;
    return;
  }

  table.innerHTML = data.map(b => `
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
      </td>
    </tr>
  `).join('');
}

function getStatusClass(status) {
  status = (status || '').toLowerCase();

  if (status === 'confirmed') return 'status-confirmed';
  if (status === 'cancelled') return 'status-cancelled';

  return 'status-pending';
}