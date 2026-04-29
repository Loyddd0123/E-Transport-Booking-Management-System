# API Endpoints

Base URL example:
`http://localhost/e_transport_system/api/index.php?r=`

## GET
- `health` - API status
- `schedules` - available trip schedules with remaining seats
- `bookings` - booking list
- `vehicles` - vehicle list
- `routes` - route list
- `payments` - payment list
- `reports` - booking, payment, and vehicle summary

## POST
- `login` - body: `{ "email":"...", "password":"..." }`
- `register` - body: `{ "full_name":"...", "email":"...", "password":"..." }`
- `bookings` - body: `{ "user_id":2, "schedule_id":1, "seats":1 }`
- `booking-status` - body: `{ "booking_id":1, "status":"Paid" }`
- `vehicles` - body: `{ "plate_number":"XYZ-123", "vehicle_type":"Van", "seat_capacity":14 }`
