using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamBot
{
    /// <summary>
    /// A class that manages SteamBot processes.
    /// </summary>
    public class BotManager
    {
        private readonly List<RunningBot> botThreads;
        private Log mainLog;
        private List<int> collectingBotIndexes;
        private List<int> givingBotIndexes;

        private Bot collectingBot = null;
        private Bot givingBot = null;
        public List<ulong> approvedIDs;

        public BotManager()
        {
            botThreads = new List<RunningBot>();
            givingBotIndexes = new List<int>();
            collectingBotIndexes = new List<int>();
            approvedIDs = new List<ulong>();
        }

        public Configuration ConfigObject { get; private set; }

        /// <summary>
        /// Loads a configuration file to use when creating bots.
        /// </summary>
        /// <param name="configFile"><c>false</c> if there was problems loading the config file.</param>
        public bool LoadConfiguration(string configFile)
        {
            if (!File.Exists(configFile))
                return false;

            try
            {
                ConfigObject = Configuration.LoadConfiguration(configFile);
            }
            catch (JsonReaderException)
            {
                // handle basic json formatting screwups
                ConfigObject = null;
            }

            if (ConfigObject == null)
                return false;

            mainLog = new Log(ConfigObject.MainLog, null, Log.LogLevel.Debug);

            for (int i = 0; i < ConfigObject.Bots.Length; i++)
            {
                Configuration.BotInfo info = ConfigObject.Bots[i];
                if (info.BotControlClass == "SteamBot.ItemGivingUserHandler")
                {
                    givingBotIndexes.Add(i);
                }
                else if (info.BotControlClass == "SteamBot.ItemCollectingUserHandler")
                {
                    collectingBotIndexes.Add(i);
                }

                var v = new RunningBot(i, ConfigObject, this);
                botThreads.Add(v);
            }

            if (collectingBotIndexes.Count() == 0)
                throw new ArgumentException("Configuration file did not contain any bots listed with ItemCollectingUserHandler", "configFile");

            return true;
        }

        /// <summary>
        /// Starts the bots that have been configured.
        /// </summary>
        /// <returns><c>false</c> if there was something wrong with the configuration or logging.</returns>
        public bool StartBots()
        {
            if (ConfigObject == null || mainLog == null)
                return false;

            foreach (var runningBot in botThreads)
            {
                runningBot.Start();

                Thread.Sleep(2000);
            }

            return true;
        }

        /// <summary>
        /// Kills all running bot processes.
        /// </summary>
        public void StopBots()
        {
            mainLog.Debug("Shutting down all bot processes.");
            foreach (var botProc in botThreads)
            {
                botProc.Stop();
            }
        }

        /// <summary>
        /// Kills a single bot process given that bots index in the configuration.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        public void StopBot(int index)
        {
            mainLog.Debug(String.Format("Killing bot process {0}.", index));
            if (index < botThreads.Count)
            {
                botThreads[index].Stop();
            }
        }

        /// <summary>
        /// Stops a bot given that bots configured username.
        /// </summary>
        /// <param name="botUserName">The bot's username.</param>
        public void StopBot(string botUserName)
        {
            mainLog.Debug(String.Format("Killing bot with username {0}.", botUserName));

            var res = from b in botThreads
                      where b.BotConfig.Username.Equals(botUserName, StringComparison.CurrentCultureIgnoreCase)
                      select b;

            foreach (var bot in res)
            {
                bot.Stop();
            }
        }

        /// <summary>
        /// Starts a bot in a new process given that bot's index in the configuration.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        public void StartBot(int index)
        {
            mainLog.Debug(String.Format("Starting bot at index {0}.", index));

            if (index < ConfigObject.Bots.Length)
            {
                botThreads[index].Start();
            }
        }

        /// <summary>
        /// Starts a bot given that bots configured username.
        /// </summary>
        /// <param name="botUserName">The bot's username.</param>
        public void StartBot(string botUserName)
        {
            mainLog.Debug(String.Format("Starting bot with username {0}.", botUserName));

            var res = from b in botThreads
                      where b.BotConfig.Username.Equals(botUserName, StringComparison.CurrentCultureIgnoreCase)
                      select b;

            foreach (var bot in res)
            {
                bot.Start();
            }

        }

        /// <summary>
        /// Sets the SteamGuard auth code on the given bot
        /// </summary>
        /// <param name="index">The bot's index</param>
        /// <param name="AuthCode">The auth code</param>
        public void AuthBot(int index, string AuthCode)
        {
            if (index < botThreads.Count)
            {
                botThreads[index].TheBot.AuthCode = AuthCode;
            }
        }

        /// <summary>
        /// Sends the BotManager command to the target Bot
        /// </summary>
        /// <param name="index">The target bot's index</param>
        /// <param name="command">The command to be executed</param>
        public void SendCommand(int index, string command)
        {
            mainLog.Debug(String.Format("Sending command \"{0}\" to Bot at index {1}", command, index));
            if (index < botThreads.Count)
            {
                if (botThreads[index].IsRunning)
                {
                    botThreads[index].TheBot.HandleBotCommand(command);
                }
                else
                {
                    mainLog.Warn(String.Format("Bot at index {0} is not running. Use the 'Start' command first", index));
                }
            }
            else
            {
                mainLog.Warn(String.Format("Invalid Bot index: {0}", index));
            }
        }

        /// <summary>
        /// A method to return an instance of the <c>bot.BotControlClass</c>.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <param name="sid">The steamId.</param>
        /// <returns>A <see cref="UserHandler"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown if the control class type does not exist.</exception>
        public static UserHandler UserHandlerCreator(Bot bot, SteamID sid)
        {
            Type controlClass = Type.GetType(bot.BotControlClass);

            if (controlClass == null)
                throw new ArgumentException("Configured control class type was null. You probably named it wrong in your configuration file.", "bot");

            return (UserHandler)Activator.CreateInstance(
                    controlClass, new object[] { bot, sid });
        }

        #region Nested RunningBot class

        /// <summary>
        /// Nested class that holds the information about a spawned bot.
        /// </summary>
        private class RunningBot
        {
            private const string BotExecutable = "SteamBot.exe";
            private readonly Configuration config;
            private BotManager Manager;

            /// <summary>
            /// Creates a new instance of <see cref="RunningBot"/> class.
            /// </summary>
            /// <param name="index">The index of the bot in the configuration.</param>
            /// <param name="config">The bots configuration object.</param>
            public RunningBot(int index, Configuration config, BotManager m)
            {
                this.config = config;
                BotConfigIndex = index;
                BotConfig = config.Bots[BotConfigIndex];
                Manager = m;
            }

            public int BotConfigIndex { get; private set; }

            public Configuration.BotInfo BotConfig { get; private set; }

            public Bot TheBot { get; set; }

            public bool IsRunning = false;

            public void Stop()
            {
                if (TheBot != null && TheBot.IsRunning)
                {
                    TheBot.StopBot();
                    IsRunning = false;
                }
            }

            public void Start()
            {
                if (TheBot == null)
                {
                    SpawnBotThread(BotConfig);
                    IsRunning = true;
                }
                else if (!TheBot.IsRunning)
                {
                    SpawnBotThread(BotConfig);
                    IsRunning = true;
                }
            }

            private void SpawnBotThread(Configuration.BotInfo botConfig)
            {
                // the bot object itself is threaded so we just build it and start it.
                Bot b = new Bot(botConfig,
                                config.ApiKey,
                                UserHandlerCreator,
                                Manager,
                                true);

                TheBot = b;
                TheBot.StartBot();
            }

            //private static void BotStdOutHandler(object sender, DataReceivedEventArgs e)
            //{
            //    if (!String.IsNullOrEmpty(e.Data))
            //    {
            //        Console.WriteLine(e.Data);
            //    }
            //}
        }

        #endregion Nested RunningBot class

        #region Automatic Item Collection

        public void InitiateAutomaticCollection() 
        {
            if (collectingBotIndexes.Count() != 0)
            {
                StartBot(collectingBotIndexes.ElementAt(0));

                if (givingBotIndexes.Count() != 0)
                {
                    StartBot(givingBotIndexes.ElementAt(0));
                }
                else
                {
                    StopBots();
                    Console.WriteLine("There are no more giving bots available!");
                }
            }
        }

        public void ReportReady(Bot reportingBot)
        {
            ulong id = reportingBot.SteamUser.SteamID.ConvertToUInt64();
            if (!approvedIDs.Contains(id))
            {
                approvedIDs.Add(id);
            }

            if (reportingBot.BotControlClass == "SteamBot.ItemCollectingUserHandler")
            {
                collectingBot = reportingBot;

            }
            else if (reportingBot.BotControlClass == "SteamBot.ItemGivingUserHandler")
            {
                givingBot = reportingBot;
            }

            // If both bots have 'checked-in' we can get them to trade each other
            if (collectingBot != null && givingBot != null)
            {
                if (givingBot.SteamFriends.GetFriendRelationship(collectingBot.SteamUser.SteamID) != EFriendRelationship.Friend)
                {
                    collectingBot.SteamFriends.AddFriend(givingBot.SteamUser.SteamID);
                }

                int numSlotsAvail = (int)collectingBot.MyInventory.NumSlots - collectingBot.MyInventory.Items.Count();

                // If collecting bot has no inventory space available OR giving bot has no items to trade
                if (numSlotsAvail == 0 || givingBot.MyInventory.GetNumberOfTradableItems() == 0)
                {
                    ReportTradeSuccess();
                }
                else
                {
                    // MUST NOT trigger a trade until the bots are friends.
                    while (givingBot.SteamFriends.GetFriendRelationship(collectingBot.SteamUser.SteamID) != EFriendRelationship.Friend)
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                    givingBot.tradeItems(numSlotsAvail, collectingBot.SteamUser.SteamID);
                }
            }
        }

        public void ReportTradeSuccess()
        {
            collectingBot.GetInventory();
            givingBot.GetInventory();

            if (givingBot.MyInventory.GetNumberOfTradableItems() == 0)
            {
                givingBot = null;
                StopBot(givingBotIndexes.ElementAt(0));
                givingBotIndexes.RemoveAt(0);
            }

            if (collectingBot.MyInventory.NumSlots == collectingBot.MyInventory.Items.Count())
            {
                collectingBot = null;
                StopBot(collectingBotIndexes.ElementAt(0));
                collectingBotIndexes.RemoveAt(0);
            }

            if (collectingBot == null)
            {
                if (collectingBotIndexes.Count() != 0)
                {
                    StartBot(collectingBotIndexes.ElementAt(0));
                }
                else
                {
                    StopBots();
                    Console.WriteLine("There are no more collecting bots available!");
                }  
            }
            else
            {
                collectingBot.PleaseReport();
            }

            if (givingBot == null)
            {
                if (givingBotIndexes.Count() != 0)
                {
                    StartBot(givingBotIndexes.ElementAt(0));
                }
                else
                {
                    StopBots();
                    Console.WriteLine("There are no more giving bots available!");
                }
            }
            else
            {
                givingBot.PleaseReport();
            }
        }

        #endregion Automatic Item Collection

    }
}
