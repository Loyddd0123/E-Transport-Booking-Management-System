CREATE DATABASE IF NOT EXISTS etransport_db;
USE etransport_db;

CREATE TABLE roles (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE users (
  id INT AUTO_INCREMENT PRIMARY KEY,
  role_id INT NOT NULL,
  full_name VARCHAR(120) NOT NULL,
  email VARCHAR(120) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (role_id) REFERENCES roles(id)
);

CREATE TABLE vehicles (
  id INT AUTO_INCREMENT PRIMARY KEY,
  plate_number VARCHAR(30) NOT NULL UNIQUE,
  vehicle_type VARCHAR(50) NOT NULL,
  seat_capacity INT NOT NULL,
  status ENUM('Available','Assigned','Under Maintenance','Unavailable') DEFAULT 'Available',
  maintenance_status VARCHAR(100) DEFAULT 'Good'
);

CREATE TABLE drivers (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NOT NULL,
  license_no VARCHAR(80),
  status ENUM('Available','Assigned','Off Duty') DEFAULT 'Available',
  FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE TABLE routes (
  id INT AUTO_INCREMENT PRIMARY KEY,
  origin VARCHAR(100) NOT NULL,
  destination VARCHAR(100) NOT NULL,
  fare DECIMAL(10,2) NOT NULL
);

CREATE TABLE schedules (
  id INT AUTO_INCREMENT PRIMARY KEY,
  route_id INT NOT NULL,
  vehicle_id INT NOT NULL,
  driver_id INT NULL,
  departure_time DATETIME NOT NULL,
  arrival_time DATETIME NOT NULL,
  status ENUM('Open','In Transit','Completed','Cancelled') DEFAULT 'Open',
  FOREIGN KEY (route_id) REFERENCES routes(id),
  FOREIGN KEY (vehicle_id) REFERENCES vehicles(id),
  FOREIGN KEY (driver_id) REFERENCES drivers(id)
);

CREATE TABLE bookings (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NOT NULL,
  schedule_id INT NOT NULL,
  seats INT NOT NULL DEFAULT 1,
  total_amount DECIMAL(10,2) NOT NULL,
  status ENUM('Pending','Confirmed','Paid','In Transit','Completed','Cancelled') DEFAULT 'Pending',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES users(id),
  FOREIGN KEY (schedule_id) REFERENCES schedules(id)
);

CREATE TABLE payments (
  id INT AUTO_INCREMENT PRIMARY KEY,
  booking_id INT NOT NULL,
  amount DECIMAL(10,2) NOT NULL,
  method VARCHAR(50) NOT NULL,
  status ENUM('Recorded','Refunded') DEFAULT 'Recorded',
  paid_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (booking_id) REFERENCES bookings(id)
);

CREATE TABLE activity_logs (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NULL,
  action VARCHAR(255) NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO roles(name) VALUES ('Customer'),('Driver'),('Dispatcher'),('Cashier'),('Administrator');
INSERT INTO users(role_id, full_name, email, password_hash) VALUES
(5,'System Admin','admin@etransport.local', '$2y$10$0GhiwvJ.CBlbzBCtrS5oFe8TwysBnaQ.cDlTcImWZlziBP7PMaS4i'),
(1,'Sample Passenger','passenger@etransport.local', '$2y$10$0GhiwvJ.CBlbzBCtrS5oFe8TwysBnaQ.cDlTcImWZlziBP7PMaS4i');
INSERT INTO vehicles(plate_number, vehicle_type, seat_capacity) VALUES ('ABC-1234','Van',14),('BUS-2026','Bus',45);
INSERT INTO routes(origin, destination, fare) VALUES ('City Terminal','Airport',180.00),('Campus','Downtown',75.00);
INSERT INTO schedules(route_id, vehicle_id, departure_time, arrival_time) VALUES
(1,1, DATE_ADD(NOW(), INTERVAL 1 DAY), DATE_ADD(NOW(), INTERVAL 1 DAY + 1 HOUR)),
(2,2, DATE_ADD(NOW(), INTERVAL 2 DAY), DATE_ADD(NOW(), INTERVAL 2 DAY + 45 MINUTE));
