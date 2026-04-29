using System;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ETransportAdmin;
public class MainForm : Form {
  readonly HttpClient http = new();
  readonly string apiBase = "http://localhost/e_transport_system/api/index.php?r=";
  readonly DataGridView grid = new(){Dock=DockStyle.Fill, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
  readonly ComboBox module = new(){DropDownStyle=ComboBoxStyle.DropDownList, Width=180};
  public MainForm(){
    Text="E-Transport Admin - Windows C# System"; Width=1050; Height=650;
    var top=new FlowLayoutPanel(){Dock=DockStyle.Top, Height=45};
    module.Items.AddRange(new object[]{"bookings","schedules","vehicles","routes","payments","reports"}); module.SelectedIndex=0;
    var refresh=new Button(){Text="Refresh"}; refresh.Click+=async (_,__)=>await LoadData();
    var paid=new Button(){Text="Mark Booking Paid"}; paid.Click+=async (_,__)=>await UpdateBooking("Paid");
    var cancel=new Button(){Text="Cancel Booking"}; cancel.Click+=async (_,__)=>await UpdateBooking("Cancelled");
    top.Controls.AddRange(new Control[]{new Label(){Text="Module:",AutoSize=true,Padding=new Padding(8)},module,refresh,paid,cancel});
    Controls.Add(grid); Controls.Add(top); Shown+=async (_,__)=>await LoadData();
  }
  async Task LoadData(){
    var json=await http.GetStringAsync(apiBase+module.SelectedItem);
    grid.DataSource = ToTable(json);
  }
  static DataTable ToTable(string json){
    var table=new DataTable();
    using var doc=JsonDocument.Parse(json);
    var root=doc.RootElement;
    if(root.ValueKind==JsonValueKind.Object){
      table.Columns.Add("name"); table.Columns.Add("value");
      foreach(var p in root.EnumerateObject()) table.Rows.Add(p.Name, p.Value.ToString());
      return table;
    }
    foreach(var item in root.EnumerateArray()){
      foreach(var p in item.EnumerateObject()) if(!table.Columns.Contains(p.Name)) table.Columns.Add(p.Name);
      var row=table.NewRow();
      foreach(var p in item.EnumerateObject()) row[p.Name]=p.Value.ToString();
      table.Rows.Add(row);
    }
    return table;
  }
  async Task UpdateBooking(string status){
    if(module.SelectedItem?.ToString()!="bookings" || grid.CurrentRow==null || !grid.Columns.Contains("id")){ MessageBox.Show("Open bookings and select a booking row first."); return; }
    var id=grid.CurrentRow.Cells["id"].Value?.ToString();
    var body=new StringContent(JsonSerializer.Serialize(new{booking_id=id,status}), Encoding.UTF8, "application/json");
    await http.PostAsync(apiBase+"booking-status", body);
    await LoadData();
  }
}
