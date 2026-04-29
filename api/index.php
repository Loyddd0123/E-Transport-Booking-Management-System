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

        $stmt = db()->prepare("INSERT INTO users (role_id, full_name, email, password_hash) VALUES (1,?,?,?)");
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

    if ($resource === 'schedules' && $method === 'GET') {
        $sql = "SELECT s.id, r.origin, r.destination, r.fare,
                v.vehicle_type, v.seat_capacity,
                s.departure_time,
                (v.seat_capacity - COALESCE(SUM(b.seats),0)) AS available_seats
                FROM schedules s
                JOIN routes r ON r.id=s.route_id
                JOIN vehicles v ON v.id=s.vehicle_id
                LEFT JOIN bookings b ON b.schedule_id=s.id
                GROUP BY s.id
                ORDER BY s.departure_time";

        json_response(db()->query($sql)->fetchAll());
    }

    if ($resource === 'bookings' && $method === 'GET') {
        $sql = "SELECT b.*, u.full_name,
                r.origin, r.destination
                FROM bookings b
                JOIN users u ON u.id=b.user_id
                JOIN schedules s ON s.id=b.schedule_id
                JOIN routes r ON r.id=s.route_id
                ORDER BY b.id DESC";

        json_response(db()->query($sql)->fetchAll());
    }

    if ($resource === 'bookings' && $method === 'POST') {
        $b = body();

        $stmt = db()->prepare("
            SELECT r.fare 
            FROM schedules s
            JOIN routes r ON r.id=s.route_id
            WHERE s.id=?
        ");
        $stmt->execute([$b['schedule_id']]);
        $fare = $stmt->fetchColumn();

        $total = $fare * (int)$b['seats'];

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
            'total' => $total
        ]);
    }

    json_response(['error' => 'Endpoint not found'], 404);

} catch (Throwable $e) {
    json_response(['error' => $e->getMessage()], 500);
}