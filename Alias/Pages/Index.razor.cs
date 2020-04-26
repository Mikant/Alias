using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace Alias.Pages {
    public partial class Index {
        [Inject]
        public virtual IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject]
        public virtual HttpClient HttpClient { get; set; }
        [Inject]
        public virtual NavigationManager NavigationManager { get; set; }

        private readonly string[] words = new string[5];

        protected override async Task OnInitializedAsync() {
            var words = await GetWords() ?? new string[this.words.Length];
            Array.Copy(words, 0, this.words, 0, Math.Min(words.Length, this.words.Length));
        }

        private async Task<string[]> GetWords() {
            var authToken = HttpContextAccessor.HttpContext.Request.Cookies[".AspNetCore.Cookies"];
            if (authToken == null)
                return null;

            HttpClient.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Cookies=" + authToken);
            var url = NavigationManager.ToAbsoluteUri("/api/User/GetWords");
            return await HttpClient.GetJsonAsync<string[]>(url.ToString());
        }

        private async Task SaveWords() {
            var authToken = HttpContextAccessor.HttpContext.Request.Cookies[".AspNetCore.Cookies"];
            if (authToken == null)
                return;

            HttpClient.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Cookies=" + authToken);
            var url = NavigationManager.ToAbsoluteUri("/api/User/SetWords");

            var content = new StringContent(JsonSerializer.Serialize(words), Encoding.UTF8, "application/json");
            await HttpClient.PostAsync(url.ToString(), content);
        }
    }
}
