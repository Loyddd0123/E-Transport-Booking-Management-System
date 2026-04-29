# System Architecture

## Goal
Create two separate systems that synchronize through one REST API and one centralized database.

## Components
1. Passenger Web Booking System - HTML, CSS, JavaScript, Bootstrap, PHP API calls.
2. Windows Admin Management System - C# Windows Forms.
3. Shared REST API - PHP using PDO.
4. Centralized Database - MySQL.

## Data Flow
Customer or Staff UI -> Web Application -> API -> Database

Administrator UI -> Windows C# Application -> API -> Database

## Main Modules
- User management
- Role-based login
- Route and schedule viewing
- Booking creation and status updates
- Vehicle records
- Payment records
- Reports

## Synchronization
The web app and Windows app read and write using the same REST endpoints. This means new bookings from the web app become visible in the C# admin app after refresh. Admin changes in C# are saved by the API and then reflected in the web app.
