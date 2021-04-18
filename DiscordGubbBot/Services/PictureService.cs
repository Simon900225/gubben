using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordGubbBot.Services
{
    public class PictureService
    {
        private Random random = new Random();
        private readonly HttpClient http;

        public PictureService(HttpClient http)
            => this.http = http;

        public async Task<Stream> GetCatPictureAsync()
        {
            var resp = await http.GetAsync("https://cataas.com/cat");
            return await resp.Content.ReadAsStreamAsync();
        }

        private async Task<HttpResponseMessage> GetImage(string url, params string[] jsonPath)
        {
            HttpResponseMessage resp = await http.GetAsync(url);
            var json = await resp.Content.ReadAsStringAsync(); // läs ut url, gör nytt anrop
            dynamic jsonMessage = JObject.Parse(json);
            
            foreach (var path in jsonPath)
                jsonMessage = jsonMessage[path];
            
            return await http.GetAsync(jsonMessage.ToString());
        }

        internal async Task<Stream> GetPictureAsync(string animal)
        {
            if (string.IsNullOrEmpty(animal))
            {
                animal = this.random.Next(1, 9) switch
                {
                    1 => "cat",
                    2 => "dog",
                    3 => "bunny",
                    4 => "duck",
                    5 => "fox",
                    6 => "lizard",
                    7 => "owl",
                    8 => "shiba",
                    _ => "cat",
                };
            }

            HttpResponseMessage resp = null;
            switch (animal)
            {
                case "cat":
                case "katt":
                    resp = await http.GetAsync("https://cataas.com/cat");
                    break;
                case "dog":
                case "hund":
                    resp = await GetImage("https://dog.ceo/api/breeds/image/random", "message");
                    break;
                case "bunny":
                case "kanin":
                    resp = await GetImage("https://api.bunnies.io/v2/loop/random/?media=gif,png", "media", "poster");
                    break;
                case "duck":
                case "anka":
                    resp = await GetImage("https://random-d.uk/api/v1/random?type=png", "url");
                    break;
                case "fox":
                case "räv":
                    resp = await GetImage("https://randomfox.ca/floof/", "image");
                    break;
                case "lizard":
                case "ödla":
                    resp = await GetImage("https://nekos.life/api/v2/img/lizard", "url");
                    break;
                case "owl":
                case "uggla":
                    resp = await GetImage("http://pics.floofybot.moe/owl", "image");
                    break;
                case "shiba":
                    resp = await GetImage("http://shibe.online/api/shibes", "link");
                    break;
                default:
                    resp = await GetImage("http://shibe.online/api/shibes", "link");
                    break;
            }


            return await resp.Content.ReadAsStreamAsync();
        }
    }
}
