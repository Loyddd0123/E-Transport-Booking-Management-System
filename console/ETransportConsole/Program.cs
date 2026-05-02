using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string API = "http://localhost/e_transport_system/api/index.php?r=";

        using HttpClient client = new HttpClient();

        try
        {
            Console.WriteLine("Fetching schedules...\n");

            var response = await client.GetStringAsync(API + "schedules");

            Console.WriteLine("=== Available Schedules ===");
            Console.WriteLine(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}