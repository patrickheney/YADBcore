﻿using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YADB.Services
{
    /// <summary>
    /// 2017-8-18
    /// Ref: https://www.cleverbot.com/api/
    /// </summary>
    public static class Chat
    {
        private static ConsoleColor userColor = ConsoleColor.Green;
        private static ConsoleColor botColor = ConsoleColor.Magenta;
        private static ConsoleColor systemColor = ConsoleColor.White;
        private static ConsoleColor warningColor = ConsoleColor.Yellow;

        private static string csFile = "ConversationToken.txt";
        private static string cs;

        /// <summary>
        /// 2017-8-18
        /// </summary>
        private static Task LoadConversationToken(string fileName)
        {
            string result = "";

            try
            {
                result = System.IO.File.ReadAllText(fileName);
                //await Program.AsyncConsoleMessage("Previous conversation loaded. Welcome back.", fg : systemColor);
            }
            catch (Exception)
            {
                //  ignore
                Program.AsyncConsoleMessage("Unable to load conversation token. This will be a new conversation.", fg: warningColor);
            }

            cs = result;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 2017-8-18
        /// </summary>
        private static Task StoreConversationToken(string fileName, string token)
        {
            try
            {
                System.IO.File.WriteAllText(fileName, token);
                //await Program.AsyncConsoleMessage("Conversation saved", fg: systemColor);
            }
            catch (Exception)
            {
                Program.AsyncConsoleMessage("Unabled to save conversation token: " + token, fg: warningColor);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Max calls per month, for free account
        /// </summary>
        private static int maxAPIcalls = 5000;
        private static int callCount;

        static string baseUrl = "https://www.cleverbot.com/getreply?";
        static string keyTemplate = "key={key}";
        static string stateTemplate = "cs={cs}";
        static string inputTempalte = "input={input}";

        static string key = "kv85jCC3ypppmqcLrWHip1cdFZrloGIQ";
        static string de = "5";
        static bool chatEnabled = true;
        static DateTime lastMessageReceived;

        public static bool ChatStatus { get { return chatEnabled; } }

        /// <summary>
        /// 2017-8-18
        /// This allows the cycle to be distrupted when the bot
        /// starts a conversation with itself.
        /// </summary>
        public static Task EnableChat(bool enabled)
        {
            Chat.chatEnabled = enabled;
            return Task.CompletedTask;
        }

        public static async Task Reply(ICommandContext context, string message, bool addressUser = true)
        {
            if (!chatEnabled) return;

            //  get the conversation history
            await LoadConversationToken(csFile);

            //  get the response to the user's message
            string response = await GetReply(message);

            //  name and user id for the person talking to the bot
            string userName = context.User.Username;
            var id = context.User.Id;

            //  Determine the channel to send the reply.
            ISocketMessageChannel messageChannel = context.Channel as ISocketMessageChannel;

            //  Public channel conversation should be addressed back to the user,
            //  using their nickname in the guild / server.
            SocketGuild guild = context.Guild as SocketGuild;

            if (guild != null)
            {
                SocketGuildUser guildUser = guild.GetUser(id);
                string nickname = guildUser.Nickname;
                if (!string.IsNullOrWhiteSpace(nickname)) userName = nickname;
            }

            await messageChannel.SendMessageAsync((addressUser ? userName + ", " : "") + response);
            await StoreConversationToken(csFile, cs);
        }

        /// <summary>
        /// Sample response:
        ///     {
        ///         "cs":"76nxdxIJO2...AAA",
        ///         "interaction_count":"1",
        ///         "input":"",
        ///         "output":"Good afternoon.",
        ///         "conversation_id":"AYAR1E3ITW",
        ///         ...
        ///     }
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        //public static async Task<string> GetReply(string input, out string response)
        public static async Task<string> GetReply(string input)
        {
            string response = "";

            string url = baseUrl
                + keyTemplate.Replace("{key}", decrypt(key, de))
                + "&" + inputTempalte.Replace("{input}", input);

            if (!string.IsNullOrWhiteSpace(cs)) url += "&" + stateTemplate.Replace("{cs}", cs);

            //  Build the web request
            WebRequest webRequest = WebRequest.Create(url);
            webRequest.ContentType = "application/json";

            //  Create an empty response
            WebResponse webResp = null;

            try
            {
                //  Execute the request and put the result into response
                webResp = await webRequest.GetResponseAsync();
                var encoding = ASCIIEncoding.ASCII;
                using (var reader = new System.IO.StreamReader(webResp.GetResponseStream(), encoding))
                {
                    //  Convert the json string to a json object
                    JObject json = (JObject)JsonConvert.DeserializeObject(reader.ReadToEnd());

                    //  store conversation state
                    cs = (string)json["cs"];

                    //  Get conversation reply
                    response = (string)json["output"];
                }
            }
            catch (WebException e)
            {
                //401: unauthorised due to missing or invalid API key or POST request, the Cleverbot API only accepts GET requests
                //404: API not found
                //413: request too large if you send a request over 16Kb
                //502 or 504: unable to get reply from API server, please contact us
                //503: too many requests from a single IP address or API key
                response = "There are reasons for me not to respond to that.";
            }
            lastMessageReceived = DateTime.Now;
            //return Task.CompletedTask;
            return response;
        }

        /// <summary>
        /// Returns the time span since the last message received.
        /// </summary>
        public static TimeSpan LastMessageInterval
        {
            get
            {
                return DateTime.Now - lastMessageReceived;
            }
        }

        /// <summary>
        /// 2017-8-17
        /// </summary>
        private static string decrypt(string source, string key)
        {
            return source.Substring(int.Parse(key));
        }
    }
}
