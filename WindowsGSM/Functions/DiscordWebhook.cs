﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web;

namespace WindowsGSM.Functions
{
    class DiscordWebhook 
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _webhookUrl;
        private readonly string _customMessage;
        private readonly string _donorType;

        public DiscordWebhook(string webhookurl, string customMessage, string donorType = "")
        {
            _webhookUrl = webhookurl ?? "";
            _customMessage = customMessage ?? "";
            _donorType = donorType ?? "";
        }

        public async Task<bool> Send(string serverid, string servergame, string serverstatus, string servername, string serverip, string serverport)
        {
            if (string.IsNullOrWhiteSpace(_webhookUrl))
            {
                return false;
            }

            string avatarUrl = GetAvatarUrl();
            string json = @"
            {
                ""username"": ""WindowsGSM"",
                ""avatar_url"": """ + avatarUrl  + @""",
                ""content"": """ + HttpUtility.JavaScriptStringEncode(_customMessage) + @""",
                ""embeds"": [
                {
                    ""type"": ""rich"",
                    ""color"": " + GetColor(serverstatus) + @",
                    ""fields"": [
                    {
                        ""name"": ""Status"",
                        ""value"": """ + GetStatusWithEmoji(serverstatus) + @""",
                        ""inline"": true
                    },
                    {
                        ""name"": ""Game Server"",
                        ""value"": """ + servergame + @""",
                        ""inline"": true
                    },
                    {
                        ""name"": ""Server IP:Port"",
                        ""value"": """ + serverip + ":"+ serverport + @""",
                        ""inline"": true
                    }],
                    ""author"": {
                        ""name"": """ + HttpUtility.JavaScriptStringEncode(servername) + @""",
                        ""icon_url"": """ + GetServerGameIcon(servergame) + @"""
                    },
                    ""footer"": {
                        ""text"": """ + MainWindow.WGSM_VERSION + @" - Discord Alert"",
                        ""icon_url"": """ + avatarUrl + @"""
                    },
                    ""timestamp"": """ + DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.mssZ") + @""",
                    ""thumbnail"": {
                        ""url"": """ + GetThumbnail(serverstatus) + @"""
                    }
                }]
            }";

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_webhookUrl, content);
                if (response.Content != null)
                {
                    return true;
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"Fail to send webhook ({_webhookUrl})");
            }

            return false;
        }

        private static string GetColor(string serverStatus)
        {
            if (serverStatus.Contains("Started"))
            {
                return "65280"; //Green
            }
            else if (serverStatus.Contains("Restarted"))
            {
                return "65535"; //Cyan
            }
            else if (serverStatus.Contains("Crashed"))
            {
                return "16711680"; //Red
            }
            else if (serverStatus.Contains("Updated"))
            {
                return "16564292"; //Gold
            }

            return "16711679";
        }

        private static string GetStatusWithEmoji(string serverStatus)
        {
            if (serverStatus.Contains("Started"))
            {
                return ":green_circle: " + serverStatus;
            }
            else if (serverStatus.Contains("Restarted"))
            {
                return ":blue_circle: " + serverStatus;
            }
            else if (serverStatus.Contains("Crashed"))
            {
                return ":red_circle: " + serverStatus;
            }
            else if (serverStatus.Contains("Updated"))
            {
                return ":orange_circle: " + serverStatus;
            }

            return serverStatus;
        }

        private static string GetThumbnail(string serverStatus)
        {
            string url = "https://github.com/WindowsGSM/Discord-Alert-Icons/raw/master/";
            if (serverStatus.Contains("Started"))
            {
                return $"{url}Started.png";
            }
            else if (serverStatus.Contains("Restarted"))
            {
                return $"{url}Restarted.png";
            }
            else if (serverStatus.Contains("Crashed"))
            {
                return $"{url}Crashed.png";
            }
            else if (serverStatus.Contains("Updated"))
            {
                return $"{url}Updated.png";
            }

            return $"{url}Test.png";
        }

        private static string GetServerGameIcon(string serverGame)
        {
            try
            {
                return @"https://github.com/WindowsGSM/WindowsGSM/raw/master/WindowsGSM/" + GameServer.Data.Icon.ResourceManager.GetString(serverGame);
            }
            catch
            {
                return @"https://github.com/WindowsGSM/WindowsGSM/raw/master/WindowsGSM/Images/WindowsGSM.png";
            }
        }

        private string GetAvatarUrl()
        {
            return "https://github.com/WindowsGSM/WindowsGSM/raw/master/WindowsGSM/Images/WindowsGSM" + (string.IsNullOrWhiteSpace(_donorType) ? "" : $"-{_donorType}") + ".png";
        }
    }
}
