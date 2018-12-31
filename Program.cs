using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Discord.Net;

namespace YADB
{
    /// <summary>
    /// 2017-8-18
    ///     DONE -- Compile and run this project AS IS. Make NO goddamn changes
    ///             until this thing works as is.
    ///     DONE -- Add command listener that responds to the bot's name, e.g. "Pqq" instead of
    ///             just "!command"
    ///     DONE -- Integrate CleverBot module from my console application.
    ///     DONE -- Integrate Inspirobot
    ///     DONE -- Add verbosity levels to console output, including colors
    ///     DONE -- Redirect !Help output to a private message
    ///     DONE -- Filter !Help results based on user access level
    ///     DONE -- Integrate OED module from my thesaurus console application.
    /// Ref: https://discord.foxbot.me/docs/api/
    /// Ref: https://forum.codingwithstorm.com/index.php?board=29.0
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
            => new Program().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandHandler _commands;

        public async Task StartAsync()
        {
            //  Ensure the configuration file has been created
            Configuration.EnsureExists();

            /*
             * websocket: (For 1.0 users)
             * 
             * When starting your bot, should you encounter a
             * PlatformNotSupportedException, you must target a version 
             * of the .NET runtime supporting .NET Standard 1.3
             * 
             * This should work for .NET Framework 4.6 or higher.
             * 
             * If you are still encountering this issue, or cannot target
             * .NET 4.6, you must install: Discord.Net.Providers.WS4Net.
             * Then, modify your DiscordSocketClient constructor:
             *      _client = new DiscordSocketClient(new DiscordSocketConfig { 
             *          WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
             *      });
             *      
             * Ref: https://discord.foxbot.me/latest/guides/getting_started/installing.html#installing-on-net-standard-11
             */
            /*
             * win7-websockets: As of version 2.0.0-alpha-build-00824, 
             * the regular WS4Net provider should work as intended on Windows 7.
             * Please use that instead of the alternative "WS4NetCore" provider,
             * when possible.
             */
            //  Create a new instance of DiscordSocketClient
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                //  This is not required to set.
                //  This is only there to enable a substitute replacement
                //  for the WebSockets implementation when running on a 
                //  platform on which they're busted (aka Windows 7)
                //WebSocketProvider = WS4NetProvider.Instance,
                //WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance,

                //  Specify console verbose information level
                //LogLevel = LogSeverity.Verbose,
                LogLevel = LogSeverity.Debug,

                //  Tell discord.net how long to store messages (per channel)
                MessageCacheSize = 1000
            });
             
            //  Register the console log event using a custom console writer
            _client.Log += (l) => AsyncConsoleLog(l);
            
            //  Connect to Discord
            await _client.LoginAsync(TokenType.Bot, Configuration.Get.Token);
            await _client.StartAsync();

            //  Initialize the command handler service
            _commands = new CommandHandler();
            await _commands.InstallAsync(_client);
            
            //  Prevent the console window from closing
            await Task.Delay(-1);
        }

        /// <summary>
        /// 2017-8-17
        /// </summary>
        private async Task AsyncConsoleLog(LogMessage logMessage)
        {
            //  default console color
            ConsoleColor fg = ConsoleColor.DarkMagenta;

            switch (logMessage.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    fg = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    fg = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    fg = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                    fg = ConsoleColor.Gray;
                    break;
                case LogSeverity.Debug:
                    fg = ConsoleColor.Cyan;
                    break;
                default:
                    //  do nothing
                    break;
            }

            string message = string.Format("{0} [{1}] {2}: {3}", DateTime.Now, logMessage.Severity.ToString().Substring(0, 1), logMessage.Source, logMessage.Message);
            await AsyncConsoleMessage(message, fg);
            //Console.WriteLine(logMessage.ToString());
        }

        /// <summary>
        /// 2017-8-17
        /// Custom logger allows for changing log level.
        /// Allows color coded console messages.
        /// </summary>
        public static async Task AsyncConsoleMessage(string message, ConsoleColor fg = ConsoleColor.Gray, ConsoleColor bg = ConsoleColor.Black, bool newline = true)
        {
            ConsoleColor ofg, obg;

            //  save original colors
            ofg = Console.ForegroundColor;
            obg = Console.BackgroundColor;

            //  set desired colors
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;

            //  echo message
            await Console.Out.WriteAsync(message);
            if (newline) await Console.Out.WriteLineAsync();

            //  restore original colors
            Console.ForegroundColor = ofg;
            Console.BackgroundColor = obg;
        }

    }
}
