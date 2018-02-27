using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WoofVsSneakersRaffle
{
    class Program
    {

        private static string FirstName = "";
        private static string LastName = "";
        private static string Email = "";
        private static string Phone = "";
        private static string BirthDate = "";
        private static string Size = "";
        private static bool isGmail = false;
        private static bool isCatchAll = false;
        private static int Threads = 0;

        private static string TwoCaptchaKey = "";

        private static HttpClient Client;
        public static List<string> CaptchaPool = new List<string>();
        private static string SettingsPath = Path.Combine(Environment.CurrentDirectory, "settings.json");

        static void Main(string[] args)
        {

            Log("Entry script for OW x Nike AJ1 on Sneakers-Raffle.fr");
            // read settings

            if (!File.Exists(SettingsPath))
            {
                Console.WriteLine("Settings file doesn't exist, generating.");

                JObject settingsObject = new JObject();
                settingsObject["first_name"] = "";
                settingsObject["last_name"] = "";
                settingsObject["email"] = "";
                settingsObject["phone"] = "";
                settingsObject["birthdate"] = "";
                settingsObject["size"] = "";
                settingsObject["twoCaptchaKey"] = "";
                settingsObject["isCatchAll"] = false;
                settingsObject["isGmail"] = false;
                settingsObject["Threads"] = 0;

                File.WriteAllText(SettingsPath, settingsObject.ToString());
            }

            // now we read

            JObject settings = JObject.Parse(File.ReadAllText(SettingsPath));

            FirstName = (string) settings["first_name"];
            LastName = (string) settings["last_name"];
            Email = (string) settings["email"];
            Phone = (string) settings["phone"];
            BirthDate = (string) settings["birthdate"];
            Size = (string) settings["size"];
            TwoCaptchaKey = (string) settings["twoCaptchaKey"];
            isCatchAll = (bool) settings["isCatchAll"];
            isGmail = (bool) settings["isGmail"];
            Threads = (int) settings["Threads"];

            HttpClientHandler handler = new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            };

            Client = new HttpClient(handler);

            Client.DefaultRequestHeaders.TryAddWithoutValidation("Host", "api.sneakers-raffle.fr");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "nl-NL,nl;q=0.9,en-US;q=0.8,en;q=0.7");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://off---white.sneakers-raffle.fr");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://off---white.sneakers-raffle.fr/product/air-jordan-1-white/");

            var sizeId = GetIdBySize().Result;

            for (int i = 0; i < Threads; i++) {
                Thread entryThread = new Thread(() =>
                {
                    Enter:
                    var entry = Program.Enter(sizeId).Result;
                    goto Enter;
                });
                entryThread.Start();
                Log("Started entry thread NR: " + i);
            }

            Console.ReadLine();
        }

        private static async Task<string> RequestTwoCap()
        {
            return await TwoCaptcha.SolveAsync("6LcMxjMUAAAAALhKgWsmmRM2hAFzGSQqYcpmFqHx", TwoCaptchaKey, "https://off---white.sneakers-raffle.fr");
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_rnd.Next(s.Length)]).ToArray());
        }

        private static async Task<string> GetIdBySize()
        {
            var getSizeIds = await Client.GetAsync("https://api.sneakers-raffle.fr/api/shoesizes/8/14");
            var getSizeIdResponse = await getSizeIds.Content.ReadAsStringAsync();

            if (getSizeIds.IsSuccessStatusCode)
            {
                var sizes = JObject.Parse(getSizeIdResponse);

                foreach (var size in sizes["sizes"])
                {
                    if ((string) size["size"] == Size)
                    {
                        return (string) size["id"];
                    }
                }
            }

            return "No matching size found, error?";
        }

        private static void Log(string data)
        {
            Console.Out.WriteLineAsync($"[{DateTime.Now}][WOOFVSSNKRFR] {data}");
        }

        private static Random _rnd = new Random();

        private static async Task<bool> Enter(string sizeId)
        {
            var ourCaptcha = await RequestTwoCap();
            var entryEmail = "";
            if (isGmail)
            {
                entryEmail = Email.Replace("@gmail.com", "") + $"+{RandomString(6)}@gmail.com";
            }else if (isCatchAll)
            {
                entryEmail = RandomString(7) + Email;
            }

            var entryJson = new JObject();
            entryJson["first_name"] = FirstName;
            entryJson["last_name"] = LastName;
            entryJson["email"] = entryEmail;
            entryJson["phone"] = Phone;
            entryJson["birthdate"] = BirthDate;
            entryJson["shoesize_id"] = sizeId;
            entryJson["completed_captcha"] = ourCaptcha;
            entryJson["shoe_id"] = "14";
            entryJson["retailer_id"] = "8";
            entryJson["g-recaptcha-response"] = ourCaptcha;
            entryJson["cc"] = "on";
            entryJson["mail"] = new JObject();
            entryJson["mail"]["key"] = "tEUI-jW_JN_7y1h1B9bNJA";
            entryJson["mail"]["template_name"] = "nike-raffle-confirm-off-white-popup";
            entryJson["mail"]["template_content"] = new JArray(new JObject(new JProperty("name", "example name"), new JProperty("content", "example content")));
            entryJson["mail"]["message"] = new JObject();
            entryJson["mail"]["message"]["subject"] = "Confirmation";
            entryJson["mail"]["message"]["from_email"] = "verify@sneakers-raffle.fr";
            entryJson["mail"]["message"]["from_name"] = "Sneakers Raffle";
            entryJson["mail"]["message"]["to"] = new JArray(new JObject(new JProperty("email", ""), new JProperty("type", "to")));
            entryJson["mail"]["message"]["headers"] = new JObject(new JProperty("Reply-To", "no.reply@sneakers-raffle.fr"));
            entryJson["mail"]["message"]["merge_language"] = "handlebars";
            entryJson["mail"]["message"]["global_merge_vars"] = new JArray(new JObject(new JProperty("name", "shoe name"), new JProperty("content", "The Ten: Air Jordan 1")), new JObject(new JProperty("name", "shoe_image"), new JProperty("content", "https://off---white.sneakers-raffle.fr/app/uploads/2018/02/AirJordan@100cropped.jpg")), new JObject(new JProperty("name", "firstname")), new JObject(new JProperty("name", "pickup date"), new JProperty("content", "11 November")));

            var enterRaffle = await Client.PostAsync("https://api.sneakers-raffle.fr/submit",
                new StringContent(entryJson.ToString(), Encoding.UTF8, "application/json"));

            Console.WriteLine(enterRaffle.StatusCode);

            if (enterRaffle.IsSuccessStatusCode)
            {
                Log("Successfully enterred on raffle!");
            }
            else
            {
                Log("Error enterring raffle!");
            }

            return false;
        }
    }
}
