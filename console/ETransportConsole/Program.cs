using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

class Program
{
    static readonly string API = "http://localhost/e_transport_system/api/index.php?r=";
    static readonly HttpClient client = new HttpClient();
    static JsonObject? currentUser;

    static async Task Main()
    {
        Console.Title = "E-Transport Console System";

        await Login();

        string roleId = currentUser?["role_id"]?.ToString() ?? "";

        if (roleId == "5")
            await AdminMenu();
        else
            await PassengerMenu();
    }

    static async Task Login()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== E-Transport Login ===");

            Console.Write("Email: ");
            string email = Console.ReadLine() ?? "";

            Console.Write("Password: ");
            string password = Console.ReadLine() ?? "";

            var result = await PostJson("login", new { email, password });

            if (result["user"] != null)
            {
                currentUser = result["user"]!.AsObject();
                Console.WriteLine("\nLogin successful!");
                await Task.Delay(800);
                return;
            }

            Console.WriteLine("\nInvalid login.");
            Pause();
        }
    }

    static async Task PassengerMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Passenger Console ===");
            Console.WriteLine($"Logged in as: {currentUser?["full_name"]}");
            Console.WriteLine();
            Console.WriteLine("1. View Available Schedules");
            Console.WriteLine("2. Book a Schedule");
            Console.WriteLine("3. View My Bookings");
            Console.WriteLine("4. Pay Booking");
            Console.WriteLine("5. View My Payments");
            Console.WriteLine("6. View Profile");
            Console.WriteLine("7. Update Profile");
            Console.WriteLine("0. Logout");
            Console.Write("\nChoose: ");

            string choice = Console.ReadLine() ?? "";
            Console.Clear();

            if (choice == "1") await ViewSchedules();
            else if (choice == "2") await BookSchedule();
            else if (choice == "3") await ViewMyBookings();
            else if (choice == "4") await PayBooking();
            else if (choice == "5") await ViewMyPayments();
            else if (choice == "6") ViewProfile();
            else if (choice == "7") await UpdateProfile();
            else if (choice == "0") return;
            else Console.WriteLine("Invalid choice.");

            Pause();
        }
    }

    static async Task AdminMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Admin Console ===");
            Console.WriteLine($"Logged in as: {currentUser?["full_name"]}");
            Console.WriteLine();
            Console.WriteLine("1. View Dashboard Reports");
            Console.WriteLine("2. View All Bookings");
            Console.WriteLine("3. View All Payments");
            Console.WriteLine("4. View All Users");
            Console.WriteLine("5. View Schedules");
            Console.WriteLine("6. Add Route");
            Console.WriteLine("7. Update Route");
            Console.WriteLine("8. Delete Route");
            Console.WriteLine("9. Add Vehicle");
            Console.WriteLine("10. Update Vehicle");
            Console.WriteLine("11. Delete Vehicle");
            Console.WriteLine("12. Add Schedule");
            Console.WriteLine("13. Update Schedule");
            Console.WriteLine("14. Delete Schedule");
            Console.WriteLine("0. Logout");
            Console.Write("\nChoose: ");

            string choice = Console.ReadLine() ?? "";
            Console.Clear();

            if (choice == "1") await ViewReports();
            else if (choice == "2") await ViewAllBookings();
            else if (choice == "3") await ViewAllPayments();
            else if (choice == "4") await ViewAllUsers();
            else if (choice == "5") await ViewSchedules();
            else if (choice == "6") await AddRoute();
            else if (choice == "7") await UpdateRoute();
            else if (choice == "8") await DeleteRoute();
            else if (choice == "9") await AddVehicle();
            else if (choice == "10") await UpdateVehicle();
            else if (choice == "11") await DeleteVehicle();
            else if (choice == "12") await AddSchedule();
            else if (choice == "13") await UpdateSchedule();
            else if (choice == "14") await DeleteSchedule();
            else if (choice == "0") return;
            else Console.WriteLine("Invalid choice.");

            Pause();
        }
    }

    static async Task ViewSchedules()
    {
        Console.WriteLine("Fetching schedules...\n");

        JsonArray data = await GetArray("schedules");

        Console.WriteLine("=== Available Schedules ===\n");
        Console.WriteLine($"{"ID",-5} {"Route",-30} {"Departure",-22} {"Vehicle",-15} {"Seats",-8} {"Fare",-10}");
        Console.WriteLine(new string('-', 95));

        foreach (var item in data)
        {
            var s = item!.AsObject();
            string route = $"{s["origin"]} -> {s["destination"]}";

            Console.WriteLine($"{s["id"],-5} {route,-30} {s["departure_time"],-22} {s["vehicle_type"],-15} {s["available_seats"],-8} ₱{s["fare"],-10}");
        }
    }

    static async Task BookSchedule()
    {
        await ViewSchedules();

        Console.Write("\nSchedule ID: ");
        string scheduleId = Console.ReadLine() ?? "";

        Console.Write("Number of seats: ");
        string seats = Console.ReadLine() ?? "";

        if (!NotEmpty(scheduleId, seats)) return;

        var result = await PostJson("bookings", new
        {
            user_id = currentUser?["id"]?.ToString(),
            schedule_id = scheduleId,
            seats = seats
        });

        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task ViewMyBookings()
    {
        string userId = currentUser?["id"]?.ToString() ?? "0";
        JsonArray data = await GetArray("bookings&user_id=" + userId);

        Console.WriteLine("=== My Bookings ===\n");
        Console.WriteLine($"{"ID",-5} {"Route",-30} {"Seats",-8} {"Total",-10} {"Status",-12}");
        Console.WriteLine(new string('-', 75));

        foreach (var item in data)
        {
            var b = item!.AsObject();
            string route = $"{b["origin"]} -> {b["destination"]}";

            Console.WriteLine($"{b["id"],-5} {route,-30} {b["seats"],-8} ₱{b["total_amount"],-10} {b["status"],-12}");
        }
    }

    static async Task PayBooking()
    {
        await ViewMyBookings();

        Console.Write("\nBooking ID to pay: ");
        string bookingId = Console.ReadLine() ?? "";

        Console.Write("Payment method (Cash/GCash/Card): ");
        string method = Console.ReadLine() ?? "";

        if (!NotEmpty(bookingId, method)) return;

        var result = await PostJson("payments", new
        {
            booking_id = bookingId,
            user_id = currentUser?["id"]?.ToString(),
            method = method
        });

        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task ViewMyPayments()
    {
        string userId = currentUser?["id"]?.ToString() ?? "0";
        JsonArray data = await GetArray("payments&user_id=" + userId);

        Console.WriteLine("=== My Payments ===\n");
        Console.WriteLine($"{"ID",-5} {"Booking",-10} {"Route",-30} {"Amount",-10} {"Method",-12} {"Status",-10}");
        Console.WriteLine(new string('-', 90));

        foreach (var item in data)
        {
            var p = item!.AsObject();
            string route = $"{p["origin"]} -> {p["destination"]}";

            Console.WriteLine($"{p["id"],-5} {p["booking_id"],-10} {route,-30} ₱{p["amount"],-10} {p["method"],-12} {p["status"],-10}");
        }
    }

    static void ViewProfile()
    {
        Console.WriteLine("=== My Profile ===\n");
        Console.WriteLine($"ID        : {currentUser?["id"]}");
        Console.WriteLine($"Name      : {currentUser?["full_name"]}");
        Console.WriteLine($"Email     : {currentUser?["email"]}");
        Console.WriteLine($"Role ID   : {currentUser?["role_id"]}");
    }

    static async Task UpdateProfile()
    {
        Console.Write("New full name: ");
        string fullName = Console.ReadLine() ?? "";

        if (!NotEmpty(fullName)) return;

        var result = await PutJson("update-profile", new
        {
            id = currentUser?["id"]?.ToString(),
            full_name = fullName
        });

        if (result["error"] != null)
        {
            Console.WriteLine("Profile update API is not yet available.");
            return;
        }

        currentUser!["full_name"] = fullName;
        Console.WriteLine(result["message"]?.ToString() ?? "Profile updated.");
    }

    static async Task ViewReports()
    {
        JsonObject r = await GetObject("admin-reports");

        Console.WriteLine("=== Admin Reports ===\n");
        Console.WriteLine($"Total Bookings : {r["total_bookings"]}");
        Console.WriteLine($"Total Payments : {r["total_payments"]}");
        Console.WriteLine($"Total Revenue  : ₱{r["total_revenue"]}");
        Console.WriteLine($"Total Users    : {r["total_users"]}");
    }

    static async Task ViewAllBookings()
    {
        JsonArray data = await GetArray("admin-bookings");

        Console.WriteLine("=== All Bookings ===\n");
        Console.WriteLine($"{"ID",-5} {"Passenger",-22} {"Route",-30} {"Seats",-8} {"Total",-10} {"Status",-12}");
        Console.WriteLine(new string('-', 95));

        foreach (var item in data)
        {
            var b = item!.AsObject();
            string route = $"{b["origin"]} -> {b["destination"]}";

            Console.WriteLine($"{b["id"],-5} {b["full_name"],-22} {route,-30} {b["seats"],-8} ₱{b["total_amount"],-10} {b["status"],-12}");
        }
    }

    static async Task ViewAllPayments()
    {
        JsonArray data = await GetArray("admin-payments");

        Console.WriteLine("=== All Payments ===\n");
        Console.WriteLine($"{"ID",-5} {"Booking",-10} {"Passenger",-22} {"Amount",-10} {"Method",-12} {"Status",-10}");
        Console.WriteLine(new string('-', 85));

        foreach (var item in data)
        {
            var p = item!.AsObject();

            Console.WriteLine($"{p["id"],-5} {p["booking_id"],-10} {p["full_name"],-22} ₱{p["amount"],-10} {p["method"],-12} {p["status"],-10}");
        }
    }

    static async Task ViewAllUsers()
    {
        JsonArray data = await GetArray("admin-users");

        Console.WriteLine("=== All Users ===\n");
        Console.WriteLine($"{"ID",-5} {"Name",-25} {"Email",-35} {"Role",-8}");
        Console.WriteLine(new string('-', 80));

        foreach (var item in data)
        {
            var u = item!.AsObject();
            Console.WriteLine($"{u["id"],-5} {u["full_name"],-25} {u["email"],-35} {u["role_id"],-8}");
        }
    }

    static async Task AddRoute()
    {
        Console.Write("Origin: ");
        string origin = Console.ReadLine() ?? "";

        Console.Write("Destination: ");
        string destination = Console.ReadLine() ?? "";

        Console.Write("Fare: ");
        string fare = Console.ReadLine() ?? "";

        if (!NotEmpty(origin, destination, fare)) return;

        var result = await PostJson("admin-routes", new { origin, destination, fare });
        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task UpdateRoute()
    {
        Console.Write("Route ID: ");
        string id = Console.ReadLine() ?? "";

        Console.Write("New Origin: ");
        string origin = Console.ReadLine() ?? "";

        Console.Write("New Destination: ");
        string destination = Console.ReadLine() ?? "";

        Console.Write("New Fare: ");
        string fare = Console.ReadLine() ?? "";

        if (!NotEmpty(id, origin, destination, fare)) return;

        var result = await PutJson("admin-routes", new { id, origin, destination, fare });
        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task DeleteRoute()
    {
        Console.Write("Route ID: ");
        string id = Console.ReadLine() ?? "";

        if (!Confirm("Delete this route?")) return;

        var result = await DeleteJson("admin-routes&id=" + id);
        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task AddVehicle()
    {
        Console.Write("Plate Number: ");
        string plate = Console.ReadLine() ?? "";

        Console.Write("Vehicle Type: ");
        string type = Console.ReadLine() ?? "";

        Console.Write("Seat Capacity: ");
        string seats = Console.ReadLine() ?? "";

        if (!NotEmpty(plate, type, seats)) return;

        var result = await PostJson("admin-vehicles", new
        {
            plate_number = plate,
            vehicle_type = type,
            seat_capacity = seats,
            status = "Available",
            maintenance_status = "Good"
        });

        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task UpdateVehicle()
    {
        Console.Write("Vehicle ID: ");
        string id = Console.ReadLine() ?? "";

        Console.Write("New Plate Number: ");
        string plate = Console.ReadLine() ?? "";

        Console.Write("New Vehicle Type: ");
        string type = Console.ReadLine() ?? "";

        Console.Write("New Seat Capacity: ");
        string seats = Console.ReadLine() ?? "";

        Console.Write("Status: ");
        string status = Console.ReadLine() ?? "Available";

        Console.Write("Maintenance Status: ");
        string maintenance = Console.ReadLine() ?? "Good";

        if (!NotEmpty(id, plate, type, seats, status, maintenance)) return;

        var result = await PutJson("admin-vehicles", new
        {
            id,
            plate_number = plate,
            vehicle_type = type,
            seat_capacity = seats,
            status,
            maintenance_status = maintenance
        });

        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task DeleteVehicle()
    {
        Console.Write("Vehicle ID: ");
        string id = Console.ReadLine() ?? "";

        if (!Confirm("Delete this vehicle?")) return;

        var result = await DeleteJson("admin-vehicles&id=" + id);
        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task AddSchedule()
    {
        Console.Write("Route ID: ");
        string routeId = Console.ReadLine() ?? "";

        Console.Write("Vehicle ID: ");
        string vehicleId = Console.ReadLine() ?? "";

        Console.Write("Departure Time (YYYY-MM-DD HH:MM:SS): ");
        string departure = Console.ReadLine() ?? "";

        if (!NotEmpty(routeId, vehicleId, departure)) return;

        var result = await PostJson("admin-schedules", new
        {
            route_id = routeId,
            vehicle_id = vehicleId,
            departure_time = departure,
            arrival_time = departure,
            status = "Active"
        });

        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task UpdateSchedule()
    {
        Console.Write("Schedule ID: ");
        string id = Console.ReadLine() ?? "";

        Console.Write("New Route ID: ");
        string routeId = Console.ReadLine() ?? "";

        Console.Write("New Vehicle ID: ");
        string vehicleId = Console.ReadLine() ?? "";

        Console.Write("New Departure Time (YYYY-MM-DD HH:MM:SS): ");
        string departure = Console.ReadLine() ?? "";

        Console.Write("Status: ");
        string status = Console.ReadLine() ?? "Active";

        if (!NotEmpty(id, routeId, vehicleId, departure, status)) return;

        var result = await PutJson("admin-schedules", new
        {
            id,
            route_id = routeId,
            vehicle_id = vehicleId,
            departure_time = departure,
            arrival_time = departure,
            status
        });

        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task DeleteSchedule()
    {
        Console.Write("Schedule ID: ");
        string id = Console.ReadLine() ?? "";

        if (!Confirm("Delete this schedule?")) return;

        var result = await DeleteJson("admin-schedules&id=" + id);
        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static bool NotEmpty(params string[] values)
    {
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine("Please fill in all fields.");
                return false;
            }
        }

        return true;
    }

    static bool Confirm(string message)
    {
        Console.Write(message + " (y/n): ");
        string answer = Console.ReadLine() ?? "";
        return answer.ToLower() == "y";
    }

    static void Pause()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    static async Task<JsonArray> GetArray(string route)
    {
        string json = await client.GetStringAsync(API + route);
        return JsonNode.Parse(json)!.AsArray();
    }

    static async Task<JsonObject> GetObject(string route)
    {
        string json = await client.GetStringAsync(API + route);
        return JsonNode.Parse(json)!.AsObject();
    }

    static async Task<JsonObject> PostJson(string route, object body)
    {
        string jsonBody = JsonSerializer.Serialize(body);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(API + route, content);
        string json = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(json)!.AsObject();
    }

    static async Task<JsonObject> PutJson(string route, object body)
    {
        string jsonBody = JsonSerializer.Serialize(body);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var response = await client.PutAsync(API + route, content);
        string json = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(json)!.AsObject();
    }

    static async Task<JsonObject> DeleteJson(string route)
    {
        var response = await client.DeleteAsync(API + route);
        string json = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(json)!.AsObject();
    }
}