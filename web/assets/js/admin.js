const ADMIN_API = '../api/index.php?r=';

async function adminApi(route, options = {}) {
  const res = await fetch(ADMIN_API + route, {
    headers: { 'Content-Type': 'application/json' },
    ...options
  });

  return res.json();
}

function showAdminPage(pageId, link) {
  document.querySelectorAll(".admin-page").forEach(page => {
    page.style.display = "none";
  });

  document.getElementById(pageId).style.display = "block";

  document.querySelectorAll(".admin-link").forEach(nav => {
    nav.classList.remove("active");
  });

  if (link) link.classList.add("active");
}

function adminLogout() {
  if (confirm("Are you sure you want to log out?")) {
    localStorage.removeItem("user");
    window.location.href = "index.html";
  }
}

async function loadAdminDashboard() {
  const user = JSON.parse(localStorage.getItem("user"));

  if (!user) {
    window.location.href = "index.html";
    return;
  }

  await loadReports();
  await loadAdminSchedules();
  await loadAdminBookings();
  await loadAdminPayments();
  await loadAdminVehicles();
  await loadAdminRoutes();
await loadVehicleOptions();
}

async function loadReports() {
  const report = await adminApi("admin-reports");

  document.getElementById("totalBookings").innerText = report.total_bookings || 0;
  document.getElementById("totalPayments").innerText = report.total_payments || 0;
  document.getElementById("totalRevenue").innerText = "₱" + (report.total_revenue || 0);
  document.getElementById("totalUsers").innerText = report.total_users || 0;

  document.getElementById("reportBookings").innerText = report.total_bookings || 0;
  document.getElementById("reportPayments").innerText = report.total_payments || 0;
  document.getElementById("reportRevenue").innerText = report.total_revenue || 0;
  document.getElementById("reportUsers").innerText = report.total_users || 0;
}

async function loadAdminSchedules() {
  const table = document.getElementById("adminSchedulesTable");
  if (!table) return;

  const data = await adminApi("schedules");
  const schedules = Array.isArray(data) ? data : [];

  if (schedules.length === 0) {
    table.innerHTML = `<tr><td colspan="5" class="text-center text-muted">No schedules found.</td></tr>`;
    return;
  }

  table.innerHTML = schedules.map(s => `
    <tr>
      <td><b>${s.origin}</b> → ${s.destination}</td>
      <td>${s.departure_time}</td>
      <td>${s.vehicle_type}</td>
      <td>${s.available_seats}</td>
      <td>₱${s.fare}</td>
    </tr>
  `).join('');
}

async function loadAdminBookings() {
  const table = document.getElementById("adminBookingsTable");
  if (!table) return;

  const data = await adminApi("admin-bookings");
  const bookings = Array.isArray(data) ? data : [];

  if (bookings.length === 0) {
    table.innerHTML = `<tr><td colspan="6" class="text-center text-muted">No bookings found.</td></tr>`;
    return;
  }

  table.innerHTML = bookings.map(b => `
    <tr>
      <td>${b.id}</td>
      <td>${b.full_name}</td>
      <td><b>${b.origin}</b> → ${b.destination}</td>
      <td>${b.seats}</td>
      <td>₱${b.total_amount}</td>
      <td><span class="badge-admin ${getAdminStatusClass(b.status)}">${b.status}</span></td>
    </tr>
  `).join('');
}

async function loadAdminPayments() {
  const table = document.getElementById("adminPaymentsTable");
  if (!table) return;

  const data = await adminApi("admin-payments");
  const payments = Array.isArray(data) ? data : [];

  if (payments.length === 0) {
    table.innerHTML = `<tr><td colspan="5" class="text-center text-muted">No payments found.</td></tr>`;
    return;
  }

  table.innerHTML = payments.map(p => `
    <tr>
      <td>${p.id}</td>
      <td>${p.booking_id}</td>
      <td>₱${p.amount}</td>
      <td>${p.method}</td>
      <td><span class="badge-admin paid">${p.status}</span></td>
    </tr>
  `).join('');
}

function getAdminStatusClass(status) {
  status = (status || "").toLowerCase();

  if (status === "paid" || status === "confirmed" || status === "completed") return "paid";
  if (status === "cancelled") return "cancelled";

  return "pending";
}

async function loadAdminVehicles() {
  const table = document.getElementById("adminVehiclesTable");
  if (!table) return;

  const data = await adminApi("admin-vehicles");
  const vehicles = Array.isArray(data) ? data : [];

  if (vehicles.length === 0) {
    table.innerHTML = `
      <tr>
        <td colspan="6" class="text-center text-muted">
          No vehicles found.
        </td>
      </tr>
    `;
    return;
  }

  table.innerHTML = vehicles.map(v => `
    <tr>
      <td>${v.id}</td>
      <td>${v.plate_number}</td>
      <td>${v.vehicle_type}</td>
      <td>${v.seat_capacity}</td>
      <td>${v.status}</td>
      <td>${v.maintenance_status}</td>
    </tr>
  `).join('');
}

async function addVehicle() {
  const plate_number = document.getElementById("plateNumber").value.trim();
  const vehicle_type = document.getElementById("vehicleType").value.trim();
  const seat_capacity = document.getElementById("seatCapacity").value.trim();
  const status = document.getElementById("vehicleStatus").value;

  if (!plate_number || !vehicle_type || !seat_capacity) {
    alert("Please fill in all vehicle fields.");
    return;
  }

  const result = await adminApi("admin-vehicles", {
    method: "POST",
    body: JSON.stringify({
      plate_number,
      vehicle_type,
      seat_capacity,
      status,
      maintenance_status: "Good"
    })
  });

  if (result.error) {
    alert(result.error);
    return;
  }

  alert("Vehicle added successfully.");

  document.getElementById("plateNumber").value = "";
  document.getElementById("vehicleType").value = "";
  document.getElementById("seatCapacity").value = "";

  loadAdminVehicles();
}

async function loadAdminRoutes() {
  const table = document.getElementById("adminRoutesTable");
  const routeSelect = document.getElementById("scheduleRoute");

  const data = await adminApi("admin-routes");
  const routes = Array.isArray(data) ? data : [];

  if (table) {
    table.innerHTML = routes.map(r => `
      <tr>
        <td>${r.id}</td>
        <td>${r.origin}</td>
        <td>${r.destination}</td>
        <td>₱${r.fare}</td>
      </tr>
    `).join('');
  }

  if (routeSelect) {
    routeSelect.innerHTML = routes.map(r => `
      <option value="${r.id}">
        ${r.origin} → ${r.destination}
      </option>
    `).join('');
  }
}

async function loadVehicleOptions() {
  const vehicleSelect = document.getElementById("scheduleVehicle");
  if (!vehicleSelect) return;

  const data = await adminApi("admin-vehicles");
  const vehicles = Array.isArray(data) ? data : [];

  vehicleSelect.innerHTML = vehicles.map(v => `
    <option value="${v.id}">
      ${v.vehicle_type} - ${v.plate_number}
    </option>
  `).join('');
}

async function addRoute() {
  const origin = document.getElementById("routeOrigin").value.trim();
  const destination = document.getElementById("routeDestination").value.trim();
  const fare = document.getElementById("routeFare").value.trim();

  if (!origin || !destination || !fare) {
    alert("Please fill in all route fields.");
    return;
  }

  const result = await adminApi("admin-routes", {
    method: "POST",
    body: JSON.stringify({ origin, destination, fare })
  });

  if (result.error) {
    alert(result.error);
    return;
  }

  alert("Route added successfully.");

  document.getElementById("routeOrigin").value = "";
  document.getElementById("routeDestination").value = "";
  document.getElementById("routeFare").value = "";

  await loadAdminRoutes();
}

async function addSchedule() {
  const route_id = document.getElementById("scheduleRoute").value;
  const vehicle_id = document.getElementById("scheduleVehicle").value;
  const departure_time = document.getElementById("departureTime").value;

  if (!route_id || !vehicle_id || !departure_time) {
    alert("Please select route, vehicle, and departure date/time.");
    return;
  }

  const result = await adminApi("admin-schedules", {
    method: "POST",
    body: JSON.stringify({
      route_id,
      vehicle_id,
      departure_time,
      arrival_time: departure_time,
      status: "Active"
    })
  });

  if (result.error) {
    alert(result.error);
    return;
  }

  alert("Schedule added successfully.");

  document.getElementById("departureTime").value = "";

  await loadAdminSchedules();
}
