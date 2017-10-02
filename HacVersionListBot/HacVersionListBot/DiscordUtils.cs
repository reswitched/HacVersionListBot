using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using HacVersionListBot.Properties;

namespace HacVersionListBot
{


    class DiscordUtils
    {
        private static readonly string discord_webhook_url = Resources.DiscordWebhook;

        public static void SendMessage(string message, byte[] version_list, string filename)
        {
            if (!discord_webhook_url.StartsWith("http"))
            {
                Program.Log($"Discord webhook not set up.");
                return;
            }
            try
            {
                using (var form = new MultipartFormDataContent())
                using (var httpC = new HttpClient())
                {

                    form.Add(new StringContent(message), "content");
                    form.Add(new StringContent(filename), "Filename");
                    form.Add(new ByteArrayContent(version_list, 0, version_list.Length), "hac_versionlist", filename);

                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var response = httpC.PostAsync(new Uri(discord_webhook_url), form, cts.Token).Result;
                    Program.Log(response.ToString());
                }
            }
            catch (WebException wex)
            {
                Program.Log($"Failed to post to discord: {wex.Message}");
            }
        }
    }
}
