using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WoofVsSneakersRaffle
{
    internal class TwoCaptcha
    {
        public static HttpClient _solveClient = new HttpClient();

        public static async Task<string> GetAnswer(string apiKey, string captchaId)
        {
            var answer = await _solveClient
                .GetAsync("http://2captcha.com/res.php?key=" + apiKey + "&action=get&id=" + captchaId).Result.Content
                .ReadAsStringAsync();

            if (answer.Length < 3) return answer;

            if (answer.Contains("OK|"))
            {
                Program.CaptchaPool.Add(answer.Split('|')[1]);
                return answer.Split('|')[1];
            }

            if (answer != "CAPTCHA_NOT_READY")
                return "";
            return "";
        }

        public static async Task<string> SolveAsync(string siteKey, string twoCapKey, string pageUrl)
        {
            var twoCapUrl = "http://2captcha.com/in.php?key=" + twoCapKey + "&method=userrecaptcha&googlekey=" + siteKey + "&pageurl=" + pageUrl;

            try
            {
                var apiResponse = _solveClient.GetAsync(twoCapUrl).Result.Content.ReadAsStringAsync().Result;

                Debug.WriteLine(apiResponse);

                if (apiResponse.Length > 3)
                    if (apiResponse.Contains("OK|"))
                    {
                        // captcha id
                        var captchaId = apiResponse.Split('|')[1];
                        var solvedCappa = "";

                        while (solvedCappa == "")
                        {
                            solvedCappa = await GetAnswer(twoCapKey, captchaId);
                            Thread.Sleep(5000);
                        }

                        return solvedCappa;
                    }

                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}