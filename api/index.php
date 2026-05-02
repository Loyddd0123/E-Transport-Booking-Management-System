<?php
require __DIR__ . '/config.php';

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    json_response(['ok' => true]);
}

$method = $_SERVER['REQUEST_METHOD'];
$resource = $_GET['r'] ?? 'health';

try {

    if ($resource === 'health') {
        json_response(['status' => 'API running']);
    }

    // LOGIN - PLAIN TEXT PASSWORD FOR TESTING ONLY
    if ($resource === 'login' && $method === 'POST') {
        $b = body();

        $stmt = db()->prepare("SELECT * FROM users WHERE email = ?");
        $stmt->execute([$b['email'] ?? '']);
        $user = $stmt->fetch();

        if (!$user || ($b['password'] ?? '') !== $user['password_hash']) {
            json_response(['error' => 'Invalid login'], 401);
        }

        unset($user['password_hash']);
        json_response(['user' => $user]);
    }

    // REGISTER - PLAIN TEXT PASSWORD FOR TESTING ONLY
    if ($resource === 'register' && $method === 'POST') {
        $b = body();

        $stmt = db()->prepare("
            INSERT INTO users (role_id, full_name, email, password_hash)
            VALUES (1,?,?,?)
        ");

        $stmt->execute([
            $b['full_name'],
            $b['email'],
            $b['password']
        ]);

        json_response([
            'message' => 'Registered successfully',
            'id' => db()->lastInsertId()
        ], 201);
    }

    // GET SCHEDULES
    if ($resource === 'schedules' && $method === 'GET') {
        $sql = "
            SELECT s.id, r.origin, r.destination, r.fare,
            v.vehicle_type, v.seat_capacity,
            s.departure_time,
            GREATEST(
                v.seat_capacity - COALESCE(SUM(CASE WHEN b.status <> 'Cancelled' THEN b.seats ELSE 0 END), 0),
                0
            ) AS available_seats
            FROM schedules s
            JOIN routes r ON r.id = s.route_id
            JOIN vehicles v ON v.id = s.vehicle_id
            LEFT JOIN bookings b ON b.schedule_id = s.id
            GROUP BY s.id
            ORDER BY s.departure_time
        ";

        json_response(db()->query($sql)->fetchAll());
    }

    // GET BOOKINGS BY USER
    if ($resource === 'bookings' && $method === 'GET') {
        $user_id = $_GET['user_id'] ?? null;

        if (!$user_id) {
            json_response(['error' => 'User ID required'], 400);
        }

        $sql = "
            SELECT b.*, u.full_name,
            r.origin, r.destination
            FROM bookings b
            JOIN users u ON u.id = b.user_id
            JOIN schedules s ON s.id = b.schedule_id
            JOIN routes r ON r.id = s.route_id
            WHERE b.user_id = ?
            ORDER BY b.id DESC
        ";

        $stmt = db()->prepare($sql);
        $stmt->execute([$user_id]);

        json_response($stmt->fetchAll());
    }

    // CREATE BOOKING
    if ($resource === 'bookings' && $method === 'POST') {
        $b = body();

        $stmt = db()->prepare("
            SELECT r.fare,
            (v.seat_capacity - COALESCE(SUM(CASE WHEN b.status <> 'Cancelled' THEN b.seats ELSE 0 END), 0)) AS available_seats
            FROM schedules s
            JOIN routes r ON r.id = s.route_id
            JOIN vehicles v ON v.id = s.vehicle_id
            LEFT JOIN bookings b ON b.schedule_id = s.id
            WHERE s.id = ?
            GROUP BY s.id
        ");

        $stmt->execute([$b['schedule_id']]);
        $row = $stmt->fetch();

        if (!$row || (int)$row['available_seats'] <= 0) {
            json_response(['error' => 'No available seats'], 400);
        }

        if ((int)$b['seats'] > (int)$row['available_seats']) {
            json_response(['error' => 'Not enough seats available'], 400);
        }

        $total = (float)$row['fare'] * (int)$b['seats'];

        $stmt = db()->prepare("
            INSERT INTO bookings (user_id, schedule_id, seats, total_amount, status)
            VALUES (?,?,?,?,?)
        ");

        $stmt->execute([
            $b['user_id'],
            $b['schedule_id'],
            $b['seats'],
            $total,
            'Pending'
        ]);

        json_response([
            'message' => 'Booking successful',
            'id' => db()->lastInsertId(),
            'total' => $total
        ], 201);
    }
    // GET PAYMENTS BY USER
if ($resource === 'payments' && $method === 'GET') {
    $user_id = $_GET['user_id'] ?? null;

    if (!$user_id) {
        json_response(['error' => 'User ID required'], 400);
    }

    $sql = "
        SELECT p.*, b.schedule_id, r.origin, r.destination
        FROM payments p
        JOIN bookings b ON b.id = p.booking_id
        JOIN schedules s ON s.id = b.schedule_id
        JOIN routes r ON r.id = s.route_id
        WHERE p.user_id = ?
        ORDER BY p.id DESC
    ";

    $stmt = db()->prepare($sql);
    $stmt->execute([$user_id]);

    json_response($stmt->fetchAll());
}

// CREATE PAYMENT
if ($resource === 'payments' && $method === 'POST') {
    $b = body();

    $stmt = db()->prepare("SELECT * FROM bookings WHERE id = ? AND user_id = ?");
    $stmt->execute([$b['booking_id'], $b['user_id']]);
    $booking = $stmt->fetch();

    if (!$booking) {
        json_response(['error' => 'Booking not found'], 404);
    }

    if ($booking['status'] === 'Paid') {
        json_response(['error' => 'Booking already paid'], 400);
    }

    $stmt = db()->prepare("
        INSERT INTO payments (booking_id, user_id, amount, method, status)
        VALUES (?,?,?,?,?)
    ");

    $stmt->execute([
        $b['booking_id'],
        $b['user_id'],
        $booking['total_amount'],
        $b['method'],
        'Paid'
    ]);

    $update = db()->prepare("UPDATE bookings SET status = 'Paid' WHERE id = ?");
    $update->execute([$b['booking_id']]);

    json_response([
        'message' => 'Payment successful',
        'id' => db()->lastInsertId()
    ], 201);
}

// ADMIN: GET ALL BOOKINGS
if ($resource === 'admin-bookings' && $method === 'GET') {
    $sql = "
        SELECT b.*, u.full_name,
        r.origin, r.destination
        FROM bookings b
        JOIN users u ON u.id = b.user_id
        JOIN schedules s ON s.id = b.schedule_id
        JOIN routes r ON r.id = s.route_id
        ORDER BY b.id DESC
    ";

    json_response(db()->query($sql)->fetchAll());
}

// ADMIN: GET ALL PAYMENTS
if ($resource === 'admin-payments' && $method === 'GET') {
    $sql = "
        SELECT p.*, u.full_name,
        r.origin, r.destination
        FROM payments p
        JOIN users u ON u.id = p.user_id
        JOIN bookings b ON b.id = p.booking_id
        JOIN schedules s ON s.id = b.schedule_id
        JOIN routes r ON r.id = s.route_id
        ORDER BY p.id DESC
    ";

    json_response(db()->query($sql)->fetchAll());
}

// ADMIN: GET USERS
if ($resource === 'admin-users' && $method === 'GET') {
    $sql = "SELECT id, full_name, email, role_id, created_at FROM users ORDER BY id DESC";
    json_response(db()->query($sql)->fetchAll());
}

// ADMIN: REPORTS
if ($resource === 'admin-reports' && $method === 'GET') {
    json_response([
        'total_bookings' => db()->query("SELECT COUNT(*) FROM bookings")->fetchColumn(),
        'total_payments' => db()->query("SELECT COUNT(*) FROM payments")->fetchColumn(),
        'total_revenue' => db()->query("SELECT COALESCE(SUM(amount),0) FROM payments")->fetchColumn(),
        'total_users' => db()->query("SELECT COUNT(*) FROM users")->fetchColumn()
    ]);
}

// ADMIN: GET VEHICLES
if ($resource === 'admin-vehicles' && $method === 'GET') {
    $sql = "SELECT * FROM vehicles ORDER BY id DESC";
    json_response(db()->query($sql)->fetchAll());
}

// ADMIN: ADD VEHICLE
if ($resource === 'admin-vehicles' && $method === 'POST') {
    $b = body();

    $stmt = db()->prepare("
        INSERT INTO vehicles (plate_number, vehicle_type, seat_capacity, status, maintenance_status)
        VALUES (?,?,?,?,?)
    ");

    $stmt->execute([
        $b['plate_number'],
        $b['vehicle_type'],
        $b['seat_capacity'],
        $b['status'] ?? 'Available',
        $b['maintenance_status'] ?? 'Good'
    ]);

    json_response([
        'message' => 'Vehicle added successfully',
        'id' => db()->lastInsertId()
    ], 201);
}

// ADMIN: GET ROUTES
if ($resource === 'admin-routes' && $method === 'GET') {
    $sql = "SELECT * FROM routes ORDER BY id DESC";
    json_response(db()->query($sql)->fetchAll());
}

// ADMIN: ADD ROUTE
if ($resource === 'admin-routes' && $method === 'POST') {
    $b = body();

    $stmt = db()->prepare("
        INSERT INTO routes (origin, destination, fare)
        VALUES (?,?,?)
    ");

    $stmt->execute([
        $b['origin'],
        $b['destination'],
        $b['fare']
    ]);

    json_response([
        'message' => 'Route added successfully',
        'id' => db()->lastInsertId()
    ], 201);
}

// ADMIN: ADD SCHEDULE
if ($resource === 'admin-schedules' && $method === 'POST') {
    $b = body();

    $stmt = db()->prepare("
        INSERT INTO schedules (route_id, vehicle_id, departure_time, arrival_time, status)
        VALUES (?,?,?,?,?)
    ");

    $stmt->execute([
        $b['route_id'],
        $b['vehicle_id'],
        $b['departure_time'],
        $b['arrival_time'],
        $b['status'] ?? 'Active'
    ]);

    json_response([
        'message' => 'Schedule added successfully',
        'id' => db()->lastInsertId()
    ], 201);
}
    json_response(['error' => 'Endpoint not found'], 404);

} catch (Throwable $e) {
    json_response(['error' => $e->getMessage()], 500);
}