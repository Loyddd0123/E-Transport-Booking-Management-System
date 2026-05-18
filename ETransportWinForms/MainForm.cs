using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ETransportWinForms
{
    public class MainForm : Form
    {
        ApiClient api => LoginForm.Api;
        Dictionary<string, object> user => LoginForm.CurrentUser;
        Panel content, menu;
        Label title;
        DataGridView grid;
        bool IsAdmin => ApiClient.S(user, "role_id") == "5";
        List<Dictionary<string, object>> currentRows = new List<Dictionary<string, object>>();

        public MainForm()
        {
            Text = "E-Transport System - Windows Forms";
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(245, 247, 251);

            menu = new Panel { Dock = DockStyle.Left, Width = 235, BackColor = Color.FromArgb(22, 36, 71) };
            Controls.Add(menu);
            menu.Controls.Add(new Label { Text = "E-Transport", ForeColor = Color.White, Font = new Font("Segoe UI", 20, FontStyle.Bold), Left = 20, Top = 24, Width = 190, Height = 45 });
            menu.Controls.Add(new Label { Text = ApiClient.S(user, "full_name"), ForeColor = Color.FromArgb(198,208,235), Left = 22, Top = 70, Width = 190, Height = 35 });

            int y = 125;
            AddMenu("Dashboard", y, async () => await Dashboard()); y += 50;
            AddMenu("Schedules", y, async () => await Schedules()); y += 50;
            AddMenu("My Bookings", y, async () => await MyBookings()); y += 50;
            AddMenu("Payments", y, async () => await Payments()); y += 50;
            if (IsAdmin)
            {
                AddMenu("Admin Bookings", y, async () => await AdminBookings()); y += 50;
                AddMenu("Routes", y, async () => await Routes()); y += 50;
                AddMenu("Vehicles / Seats", y, async () => await Vehicles()); y += 50;
                AddMenu("Add Schedule", y, async () => await AddSchedulePage()); y += 50;
            }
            AddMenu("Logout", y + 20, () => { Close(); return Task.CompletedTask; });

            content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24), BackColor = Color.FromArgb(245,247,251) };
            Controls.Add(content);
            _ = Dashboard();
        }

        void AddMenu(string text, int top, Func<Task> action)
        {
            var b = new Button { Text = text, Left = 18, Top = top, Width = 195, Height = 38, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.White, BackColor = Color.FromArgb(34, 51, 94) };
            b.FlatAppearance.BorderSize = 0;
            b.Click += async (s, e) => await action();
            menu.Controls.Add(b);
        }

        void Clear(string t)
        {
            content.Controls.Clear();
            title = new Label { Text = t, Font = new Font("Segoe UI", 22, FontStyle.Bold), Left = 24, Top = 20, Width = 800, Height = 45 };
            content.Controls.Add(title);
        }

        DataGridView MakeGrid(int top = 95)
        {
            grid = new DataGridView { Left = 24, Top = top, Width = content.Width - 70, Height = content.Height - top - 55, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            content.Controls.Add(grid);
            return grid;
        }

        Button ActionButton(string text, int left, int top, EventHandler click)
        {
            var b = new Button { Text = text, Left = left, Top = top, Width = 150, Height = 38, BackColor = Color.FromArgb(34,92,255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            b.Click += click;
            content.Controls.Add(b); return b;
        }

        async Task Dashboard()
        {
            Clear("Dashboard");
            var hello = new Label { Text = "Welcome, " + ApiClient.S(user, "full_name"), Left = 28, Top = 78, Width = 700, Height = 30, ForeColor = Color.FromArgb(70,70,85) };
            content.Controls.Add(hello);
            try
            {
                var schedules = ApiClient.AsList(await api.GetAsync("schedules"));
                var bookings = ApiClient.AsList(await api.GetAsync("bookings&user_id=" + ApiClient.S(user, "id")));
                int completed = 0; foreach (var b in bookings) if (ApiClient.S(b, "status").ToLower() == "completed") completed++;
                if (IsAdmin)
                {
                    var report = await api.GetAsync("admin-reports") as Dictionary<string, object>;
                    Card("Total Bookings", ApiClient.S(report, "total_bookings"), 28, 135);
                    Card("Total Payments", ApiClient.S(report, "total_payments"), 265, 135);
                    Card("Revenue", "₱" + ApiClient.S(report, "total_revenue"), 502, 135);
                    Card("Users", ApiClient.S(report, "total_users"), 739, 135);
                }
                else
                {
                    Card("Available Trips", schedules.Count.ToString(), 28, 135);
                    Card("My Bookings", bookings.Count.ToString(), 265, 135);
                    Card("Completed Trips", completed.ToString(), 502, 135);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "API Error"); }
        }

        void Card(string label, string value, int x, int y)
        {
            var p = new Panel { Left = x, Top = y, Width = 210, Height = 120, BackColor = Color.White };
            p.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle, Color.FromArgb(225,225,235), ButtonBorderStyle.Solid);
            p.Controls.Add(new Label { Text = label, Left = 18, Top = 18, Width = 170, ForeColor = Color.FromArgb(100,105,120) });
            p.Controls.Add(new Label { Text = value, Left = 18, Top = 48, Width = 170, Height = 45, Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Color.FromArgb(22,36,71) });
            content.Controls.Add(p);
        }

        async Task Schedules()
        {
            Clear("Schedules"); ActionButton("Refresh", 24, 72, async (s,e)=> await Schedules());
            var g = MakeGrid(125); currentRows = ApiClient.AsList(await api.GetAsync("schedules"));
            g.Columns.Clear(); foreach (var c in new[] { "id", "origin", "destination", "vehicle_type", "seat_capacity", "available_seats", "fare", "departure_time" }) g.Columns.Add(c, c.Replace("_", " ").ToUpper());
            foreach (var r in currentRows) g.Rows.Add(ApiClient.S(r,"id"),ApiClient.S(r,"origin"),ApiClient.S(r,"destination"),ApiClient.S(r,"vehicle_type"),ApiClient.S(r,"seat_capacity"),ApiClient.S(r,"available_seats"),ApiClient.S(r,"fare"),ApiClient.S(r,"departure_time"));
            if (!IsAdmin) ActionButton("Book Selected", 185, 72, async (s,e)=> await BookSelected());
        }

        async Task BookSelected()
        {
            if (grid.CurrentRow == null) return;
            int idx = grid.CurrentRow.Index; var r = currentRows[idx];
            int available = ApiClient.I(r, "available_seats"); if (available <= 0) { MessageBox.Show("This schedule is full."); return; }
            string seatsText = Prompt("How many seats?", "1"); int seats; if (!int.TryParse(seatsText, out seats) || seats <= 0) return;
            if (seats > available) { MessageBox.Show("Not enough available seats."); return; }
            var res = await api.SendAsync("bookings", "POST", new Dictionary<string, object>{{"schedule_id", ApiClient.S(r,"id")},{"user_id",ApiClient.S(user,"id")},{"seats",seats}});
            MessageBox.Show(res.ContainsKey("message") ? ApiClient.S(res,"message") : ApiClient.S(res,"error"));
            await MyBookings();
        }

        async Task MyBookings()
        {
            Clear("My Bookings"); ActionButton("Refresh", 24, 72, async (s,e)=> await MyBookings()); ActionButton("Pay Selected", 185, 72, async (s,e)=> await PaySelected());
            var g = MakeGrid(125); currentRows = ApiClient.AsList(await api.GetAsync("bookings&user_id=" + ApiClient.S(user,"id")));
            g.Columns.Clear(); foreach (var c in new[] { "id", "origin", "destination", "seats", "total_amount", "status", "created_at" }) g.Columns.Add(c, c.Replace("_", " ").ToUpper());
            foreach (var r in currentRows) g.Rows.Add(ApiClient.S(r,"id"),ApiClient.S(r,"origin"),ApiClient.S(r,"destination"),ApiClient.S(r,"seats"),ApiClient.S(r,"total_amount"),ApiClient.S(r,"status"),ApiClient.S(r,"created_at"));
        }

        async Task PaySelected()
        {
            if (grid.CurrentRow == null) return; var r = currentRows[grid.CurrentRow.Index];
            if (ApiClient.S(r,"status").ToLower() != "pending") { MessageBox.Show("Only pending bookings can be paid."); return; }
            string method = Prompt("Payment method: Cash, GCash, or Card", "Cash"); if (string.IsNullOrWhiteSpace(method)) return;
            var res = await api.SendAsync("payments", "POST", new Dictionary<string, object>{{"booking_id",ApiClient.S(r,"id")},{"user_id",ApiClient.S(user,"id")},{"method",method}});
            MessageBox.Show(res.ContainsKey("message") ? ApiClient.S(res,"message") : ApiClient.S(res,"error"));
            await Payments();
        }

        async Task Payments()
        {
            Clear("Payments"); var g = MakeGrid(95); currentRows = ApiClient.AsList(await api.GetAsync("payments&user_id=" + ApiClient.S(user,"id")));
            g.Columns.Clear(); foreach (var c in new[] { "id", "booking_id", "origin", "destination", "amount", "method", "status", "paid_at" }) g.Columns.Add(c, c.Replace("_", " ").ToUpper());
            foreach (var r in currentRows) g.Rows.Add(ApiClient.S(r,"id"),ApiClient.S(r,"booking_id"),ApiClient.S(r,"origin"),ApiClient.S(r,"destination"),ApiClient.S(r,"amount"),ApiClient.S(r,"method"),ApiClient.S(r,"status"),ApiClient.S(r,"paid_at"));
        }

        async Task AdminBookings()
        {
            Clear("Admin Bookings"); ActionButton("Complete Trip", 24, 72, async (s,e)=> await CompleteSelected());
            var g = MakeGrid(125); currentRows = ApiClient.AsList(await api.GetAsync("admin-bookings"));
            g.Columns.Clear(); foreach (var c in new[] { "id", "full_name", "origin", "destination", "seats", "total_amount", "status" }) g.Columns.Add(c, c.Replace("_", " ").ToUpper());
            foreach (var r in currentRows) g.Rows.Add(ApiClient.S(r,"id"),ApiClient.S(r,"full_name"),ApiClient.S(r,"origin"),ApiClient.S(r,"destination"),ApiClient.S(r,"seats"),ApiClient.S(r,"total_amount"),ApiClient.S(r,"status"));
        }
        async Task CompleteSelected()
        {
            if (grid.CurrentRow == null) return; var r = currentRows[grid.CurrentRow.Index];
            var res = await api.SendAsync("booking-status", "PUT", new Dictionary<string, object>{{"booking_id",ApiClient.S(r,"id")},{"status","Completed"}});
            MessageBox.Show(res.ContainsKey("message") ? ApiClient.S(res,"message") : ApiClient.S(res,"error")); await AdminBookings();
        }

        async Task Routes()
        {
            Clear("Routes"); ActionButton("Add Route", 24, 72, async (s,e)=> await AddRoute());
            var g = MakeGrid(125); currentRows = ApiClient.AsList(await api.GetAsync("admin-routes"));
            g.Columns.Clear(); foreach (var c in new[] { "id", "origin", "destination", "fare" }) g.Columns.Add(c, c.ToUpper());
            foreach (var r in currentRows) g.Rows.Add(ApiClient.S(r,"id"),ApiClient.S(r,"origin"),ApiClient.S(r,"destination"),ApiClient.S(r,"fare"));
        }
        async Task AddRoute()
        {
            string origin = Prompt("Origin", "City Terminal"); string dest = Prompt("Destination", "Airport"); string fare = Prompt("Fare", "100");
            var res = await api.SendAsync("admin-routes", "POST", new Dictionary<string, object>{{"origin",origin},{"destination",dest},{"fare",fare}});
            MessageBox.Show(res.ContainsKey("message") ? ApiClient.S(res,"message") : ApiClient.S(res,"error")); await Routes();
        }

        async Task Vehicles()
        {
            Clear("Vehicles / Add Seats"); ActionButton("Add Vehicle", 24, 72, async (s,e)=> await AddVehicle()); ActionButton("Edit Seats", 185, 72, async (s,e)=> await EditVehicleSeats());
            var g = MakeGrid(125); currentRows = ApiClient.AsList(await api.GetAsync("admin-vehicles"));
            g.Columns.Clear(); foreach (var c in new[] { "id", "plate_number", "vehicle_type", "seat_capacity", "status", "maintenance_status" }) g.Columns.Add(c, c.Replace("_", " ").ToUpper());
            foreach (var r in currentRows) g.Rows.Add(ApiClient.S(r,"id"),ApiClient.S(r,"plate_number"),ApiClient.S(r,"vehicle_type"),ApiClient.S(r,"seat_capacity"),ApiClient.S(r,"status"),ApiClient.S(r,"maintenance_status"));
        }
        async Task AddVehicle()
        {
            string plate = Prompt("Plate number", "NEW-123"); string type = Prompt("Vehicle type", "Van"); string seats = Prompt("Seat capacity", "14");
            var res = await api.SendAsync("admin-vehicles", "POST", new Dictionary<string, object>{{"plate_number",plate},{"vehicle_type",type},{"seat_capacity",seats},{"status","Available"},{"maintenance_status","Good"}});
            MessageBox.Show(res.ContainsKey("message") ? ApiClient.S(res,"message") : ApiClient.S(res,"error")); await Vehicles();
        }
        async Task EditVehicleSeats()
        {
            if (grid.CurrentRow == null) return; var r = currentRows[grid.CurrentRow.Index];
            string seats = Prompt("New seat capacity", ApiClient.S(r,"seat_capacity"));
            var res = await api.SendAsync("admin-vehicles", "PUT", new Dictionary<string, object>{{"id",ApiClient.S(r,"id")},{"plate_number",ApiClient.S(r,"plate_number")},{"vehicle_type",ApiClient.S(r,"vehicle_type")},{"seat_capacity",seats},{"status",ApiClient.S(r,"status")},{"maintenance_status",ApiClient.S(r,"maintenance_status")}});
            MessageBox.Show(res.ContainsKey("message") ? ApiClient.S(res,"message") : ApiClient.S(res,"error")); await Vehicles();
        }

        async Task AddSchedulePage()
        {
            Clear("Add Schedule");
            ActionButton("Add Schedule", 24, 72, async (s,e)=> await AddSchedule());
            var info = new Label { Text = "Use existing Route ID and Vehicle ID from Routes/Vehicles pages. This uses admin-schedules API.", Left = 190, Top = 80, Width = 650, Height = 30 };
            content.Controls.Add(info); await Schedules(); title.Text = "Add Schedule / Schedule List";
        }
        async Task AddSchedule()
        {
            string route = Prompt("Route ID", "1"); string vehicle = Prompt("Vehicle ID", "1"); string depart = Prompt("Departure time YYYY-MM-DD HH:MM:SS", DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"));
            var res = await api.SendAsync("admin-schedules", "POST", new Dictionary<string, object>{{"route_id",route},{"vehicle_id",vehicle},{"departure_time",depart},{"arrival_time",depart},{"status","Open"}});
            MessageBox.Show(res.ContainsKey("message") ? ApiClient.S(res,"message") : ApiClient.S(res,"error")); await Schedules();
        }

        public static string Prompt(string text, string defaultValue)
        {
            var f = new Form { Width = 430, Height = 165, StartPosition = FormStartPosition.CenterParent, Text = text, Font = new Font("Segoe UI", 10) };
            var box = new TextBox { Left = 18, Top = 42, Width = 380, Text = defaultValue };
            var ok = new Button { Text = "OK", Left = 240, Top = 82, Width = 75, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancel", Left = 323, Top = 82, Width = 75, DialogResult = DialogResult.Cancel };
            f.Controls.Add(new Label { Text = text, Left = 18, Top = 15, Width = 380 }); f.Controls.Add(box); f.Controls.Add(ok); f.Controls.Add(cancel);
            f.AcceptButton = ok; f.CancelButton = cancel;
            return f.ShowDialog() == DialogResult.OK ? box.Text : null;
        }
    }
}
