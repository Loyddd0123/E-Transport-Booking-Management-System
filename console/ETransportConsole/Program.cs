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

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== E-Transport Console System ===");
            Console.WriteLine($"Logged in as: {currentUser?["full_name"]}");
            Console.WriteLine();
            Console.WriteLine("1. View Schedules");
            Console.WriteLine("2. View All Bookings");
            Console.WriteLine("3. View All Payments");
            Console.WriteLine("4. View Admin Reports");
            Console.WriteLine("5. Add Route");
            Console.WriteLine("6. Add Vehicle");
            Console.WriteLine("7. Add Schedule");
            Console.WriteLine("0. Logout / Exit");
            Console.WriteLine();
            Console.Write("Choose: ");

            string? choice = Console.ReadLine();

            Console.Clear();

            if (choice == "1") await ViewSchedules();
            else if (choice == "2") await ViewBookings();
            else if (choice == "3") await ViewPayments();
            else if (choice == "4") await ViewReports();
            else if (choice == "5") await AddRoute();
            else if (choice == "6") await AddVehicle();
            else if (choice == "7") await AddSchedule();
            else if (choice == "0") break;
            else Console.WriteLine("Invalid choice.");

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    static async Task Login()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Admin Login ===");
            Console.Write("Email: ");
            string? email = Console.ReadLine();

            Console.Write("Password: ");
            string? password = Console.ReadLine();

            var body = new
            {
                email = email,
                password = password
            };

            JsonObject result = await PostJson("login", body);

            if (result["user"] != null)
            {
                currentUser = result["user"]!.AsObject();

                if (currentUser["role_id"]?.ToString() == "5")
                {
                    Console.WriteLine("\nLogin successful!");
                    await Task.Delay(800);
                    return;
                }

                Console.WriteLine("\nAccess denied. Admin account only.");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("\nInvalid login.");
                Console.ReadKey();
            }
        }
    }

    static async Task ViewSchedules()
    {
        JsonArray data = await GetArray("schedules");

        Console.WriteLine("=== Available Schedules ===\n");
        Console.WriteLine($"{"ID",-5} {"Route",-30} {"Departure",-22} {"Vehicle",-12} {"Seats",-8} {"Fare",-10}");
        Console.WriteLine(new string('-', 95));

        foreach (var item in data)
        {
            var s = item!.AsObject();

            string route = $"{s["origin"]} -> {s["destination"]}";

            Console.WriteLine(
                $"{s["id"],-5} {route,-30} {s["departure_time"],-22} {s["vehicle_type"],-12} {s["available_seats"],-8} ₱{s["fare"],-10}"
            );
        }
    }

    static async Task ViewBookings()
    {
        JsonArray data = await GetArray("admin-bookings");

        Console.WriteLine("=== All Bookings ===\n");
        Console.WriteLine($"{"ID",-5} {"Passenger",-22} {"Route",-30} {"Seats",-8} {"Total",-10} {"Status",-12}");
        Console.WriteLine(new string('-', 95));

        foreach (var item in data)
        {
            var b = item!.AsObject();

            string route = $"{b["origin"]} -> {b["destination"]}";

            Console.WriteLine(
                $"{b["id"],-5} {b["full_name"],-22} {route,-30} {b["seats"],-8} ₱{b["total_amount"],-10} {b["status"],-12}"
            );
        }
    }

    static async Task ViewPayments()
    {
        JsonArray data = await GetArray("admin-payments");

        Console.WriteLine("=== All Payments ===\n");
        Console.WriteLine($"{"ID",-5} {"Booking",-10} {"Passenger",-22} {"Amount",-10} {"Method",-12} {"Status",-10}");
        Console.WriteLine(new string('-', 85));

        foreach (var item in data)
        {
            var p = item!.AsObject();

            Console.WriteLine(
                $"{p["id"],-5} {p["booking_id"],-10} {p["full_name"],-22} ₱{p["amount"],-10} {p["method"],-12} {p["status"],-10}"
            );
        }
    }

    static async Task ViewReports()
    {
        JsonObject report = await GetObject("admin-reports");

        Console.WriteLine("=== Admin Reports ===\n");
        Console.WriteLine($"Total Bookings : {report["total_bookings"]}");
        Console.WriteLine($"Total Payments : {report["total_payments"]}");
        Console.WriteLine($"Total Revenue  : ₱{report["total_revenue"]}");
        Console.WriteLine($"Total Users    : {report["total_users"]}");
    }

    static async Task AddRoute()
    {
        Console.WriteLine("=== Add Route ===\n");

        Console.Write("Origin: ");
        string? origin = Console.ReadLine();

        Console.Write("Destination: ");
        string? destination = Console.ReadLine();

        Console.Write("Fare: ");
        string? fare = Console.ReadLine();

        var body = new
        {
            origin = origin,
            destination = destination,
            fare = fare
        };

        JsonObject result = await PostJson("admin-routes", body);

        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task AddVehicle()
    {
        Console.WriteLine("=== Add Vehicle ===\n");

        Console.Write("Plate Number: ");
        string? plate = Console.ReadLine();

        Console.Write("Vehicle Type: ");
        string? type = Console.ReadLine();

        Console.Write("Seat Capacity: ");
        string? seats = Console.ReadLine();

        var body = new
        {
            plate_number = plate,
            vehicle_type = type,
            seat_capacity = seats,
            status = "Available",
            maintenance_status = "Good"
        };

        JsonObject result = await PostJson("admin-vehicles", body);

        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
    }

    static async Task AddSchedule()
    {
        Console.WriteLine("=== Add Schedule ===\n");

        Console.Write("Route ID: ");
        string? routeId = Console.ReadLine();

        Console.Write("Vehicle ID: ");
        string? vehicleId = Console.ReadLine();

        Console.Write("Departure Time (YYYY-MM-DD HH:MM:SS): ");
        string? departure = Console.ReadLine();

        var body = new
        {
            route_id = routeId,
            vehicle_id = vehicleId,
            departure_time = departure,
            arrival_time = departure,
            status = "Active"
        };

        JsonObject result = await PostJson("admin-schedules", body);

        Console.WriteLine(result["message"]?.ToString() ?? result["error"]?.ToString() ?? "Done.");
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

        HttpResponseMessage response = await client.PostAsync(API + route, content);

        string json = await response.Content.ReadAsStringAsync();

        return JsonNode.Parse(json)!.AsObject();
    }
}