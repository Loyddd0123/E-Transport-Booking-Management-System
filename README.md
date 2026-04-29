# E-Transport Booking and Management System

A downloadable starter system for a multi-platform E-Transport project.

## Included
- `api/` - PHP REST API. Both systems connect here first.
- `web/` - Bootstrap web booking interface for passengers.
- `desktop/ETransportAdmin/` - C# Windows Forms admin system.
- `database/schema.sql` - MySQL database tables and sample data.
- `docs/` - Architecture and user guide.

## Architecture

Web Application -> REST API -> Centralized MySQL Database

Windows C# Admin Application -> REST API -> Centralized MySQL Database

The web system and desktop system do not directly access the database. Updating bookings, schedules, vehicles, or reports through one system will reflect in the other because both use the same API and database.

## Quick Setup using XAMPP
1. Install XAMPP and start Apache and MySQL.
2. Copy the whole `e_transport_system` folder into `xampp/htdocs/`.
3. Open phpMyAdmin and import `database/schema.sql`.
4. Check `api/config.php` database username and password.
5. Open the web app: `http://localhost/e_transport_system/web/`
6. Test API health: `http://localhost/e_transport_system/api/index.php?r=health`
7. Open the C# project in Visual Studio: `desktop/ETransportAdmin/ETransportAdmin.csproj`.
8. Run the C# admin app.

## Sample Accounts
- Admin: `admin@etransport.local`
- Passenger: `passenger@etransport.local`
- Password for both: `password`

## Notes
This is a functional starter/prototype. Add production security before real deployment, including HTTPS, proper token-based authentication, input validation, audit logging, and authorization middleware.
