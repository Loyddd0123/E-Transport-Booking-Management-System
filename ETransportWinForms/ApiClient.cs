using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ETransportWinForms
{
    public class ApiClient
    {
        private readonly HttpClient _http = new HttpClient();
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();
        public string BaseUrl { get; set; }

        public ApiClient(string baseUrl)
        {
            BaseUrl = baseUrl.TrimEnd('?');
        }

        private string Url(string route)
        {
            return BaseUrl + route;
        }

        public async Task<object> GetAsync(string route)
        {
            string text = await _http.GetStringAsync(Url(route));
            return _json.DeserializeObject(text);
        }

        public async Task<Dictionary<string, object>> SendAsync(string route, string method, Dictionary<string, object> body)
        {
            string data = _json.Serialize(body ?? new Dictionary<string, object>());
            var req = new HttpRequestMessage(new HttpMethod(method), Url(route));
            req.Content = new StringContent(data, Encoding.UTF8, "application/json");
            var res = await _http.SendAsync(req);
            string text = await res.Content.ReadAsStringAsync();
            var obj = _json.DeserializeObject(text) as Dictionary<string, object>;
            return obj ?? new Dictionary<string, object>();
        }

        public static List<Dictionary<string, object>> AsList(object obj)
        {
            var list = new List<Dictionary<string, object>>();
            var arr = obj as object[];
            if (arr == null) return list;
            foreach (var item in arr)
            {
                var d = item as Dictionary<string, object>;
                if (d != null) list.Add(d);
            }
            return list;
        }

        public static string S(Dictionary<string, object> row, string key)
        {
            return row != null && row.ContainsKey(key) && row[key] != null ? Convert.ToString(row[key]) : "";
        }

        public static int I(Dictionary<string, object> row, string key)
        {
            int v;
            return int.TryParse(S(row, key), out v) ? v : 0;
        }

        public static decimal D(Dictionary<string, object> row, string key)
        {
            decimal v;
            return decimal.TryParse(S(row, key), out v) ? v : 0;
        }
    }
}
