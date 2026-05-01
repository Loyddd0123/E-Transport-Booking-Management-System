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

    json_response(['error' => 'Endpoint not found'], 404);

} catch (Throwable $e) {
    json_response(['error' => $e->getMessage()], 500);
}