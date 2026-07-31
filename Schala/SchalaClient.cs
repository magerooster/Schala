#define MAINZEAL
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Swordfish.NET.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Channels;
using DSharpPlus.Extensions;
using System.Collections.Concurrent;

namespace Schala
{
    public class SchalaClient : ObservableBackgroundService
    {
        #region Internal Data
        #region Session
        public static SchalaClient Current { get; private set; }
        public DiscordClient Client { get; private set; }
        //public DiscordSlashClient SlashClient { get; private set; }
        //private CommandService _commands = new CommandService();
        private IServiceCollection _map = new ServiceCollection();
        //private IServiceProvider _services;

        public string activeBotName = "";
        #endregion
        #region Logging Stuff
        public enum LogEventType
        {
            Error = 0100,
            System = 1000,
            Message = 2000,
            Emoji = 2001,
            Command = 2002,
        }

        public static IReadOnlyDictionary<LogEventType, EventId> EventIds = new Dictionary<LogEventType, EventId>()
        {
            {LogEventType.System, new EventId((int)LogEventType.System, "System") },
            {LogEventType.Emoji, new EventId((int)LogEventType.Emoji, "Emoji Modified") },
            {LogEventType.Message, new EventId((int)LogEventType.Message, "Message-Based Command") },
            {LogEventType.Command, new EventId((int)LogEventType.Command, "Slash Command") },
            {LogEventType.Error, new EventId((int)LogEventType.Error, "Error") },
        };

        #endregion
        #region Reactions
        public Dictionary<ulong, Dictionary<string, string>> GuildEmojiLookup = new Dictionary<ulong, Dictionary<string, string>>(); //This is a list for our own internal commands.
        private List<ulong> ScrapedEmojis = new List<ulong>(); //This is a list for the scraper.

        public const ulong OldZealGuildID = 163384044149538816; //Kingdom of Zeal
        public const ulong OldEmojiChannelID = 691448909889011833; //Kingdom of Zeal Emoji Channel
        public const ulong SchalaUserID = 179324659483934720;
        public const ulong OldSkygateWelcomeMessage = 534129476444094504;
        public const ulong OldSkygateRoleReacts = 604960242199035924;
        public const ulong OldZealReportChannelID = 167392785647927297; //Zeal Palace
        public const ulong OldZealAnnouncementChannelID = 163384044149538816; //Mountain of Woe
        //private Timer UpdateClock;

        public const ulong ZealGuildID = 899878415375667210;
        public const ulong ZealEmojiChannelID = 904537801729671178;
        public const ulong ZealReportChannelID = 904536577437466664;
        public const ulong ZealUserUpdateChannelID = 904537801729671178;
        public const ulong ZealAnnouncementChannelID = 899880133580714004;

        public DiscordChannel? EmojiChannel = null;
        public DiscordChannel? UserUpdateChannel = null;

        #endregion
        #region Persistent Data
        #endregion
        public List<IWeightedList> WeightedLists = new List<IWeightedList>();
        #endregion
        #region Properties
        private string _ConnectionStatus;
        public string ConnectionStatus
        {
            get { return GetField(ref _ConnectionStatus); }
            set { SetField(ref _ConnectionStatus, value); }
        }

        //Previous inputs of commands for editing

        //Tuple<userID, channelID, SupportedCommandEnum>, Previous Command Info
        public Dictionary<Tuple<ulong, ulong, SupportedCommandToEdit>, PreviousCommand> PreviousMessages = new Dictionary<Tuple<ulong, ulong, SupportedCommandToEdit>, PreviousCommand>();
        #endregion
        #region Constructor and Startup
        public SchalaClient(ILogger<SchalaClient> logger, IHostApplicationLifetime applicationLifetime, DiscordClient client) : base(logger)
        {
            Current = this;
            this.Client = client;

            // Token resolution, command registration, and event wiring are now handled
            // by the Host/DI pipeline in Program.cs (AddDiscordClient, AddCommandsExtension,
            // ConfigureEventHandlers) rather than here.

            ConnectionStatus = "Disconnected";

            StartLogin();
            LoadWeightedLists();
        }

        // Momentary connection drops make a client appear to leave and immediately rejoin the
        // same voice channel. Delay "left" announcements by this long so a quick reconnect to
        // the same channel can cancel it out instead of spamming the log with blips.
        private static readonly TimeSpan VoiceLeaveDebounce = TimeSpan.FromSeconds(10);
        private readonly ConcurrentDictionary<ulong, (ulong ChannelId, CancellationTokenSource Cts)> PendingVoiceLeaves = new();

        public async Task VoiceStateUpdated(VoiceStateUpdatedEventArgs args)
        {
            if (args.GuildId != ZealGuildID || UserUpdateChannel is null)
                return;

            ulong? beforeChannelId = args.Before?.ChannelId;
            ulong? afterChannelId = args.After?.ChannelId;

            if (beforeChannelId == afterChannelId)
                return;

            // Reconnecting to the same channel we appeared to just leave is a connection blip, not a real event.
            if (afterChannelId != null
                && PendingVoiceLeaves.TryGetValue(args.UserId, out var pending)
                && pending.ChannelId == afterChannelId)
            {
                PendingVoiceLeaves.TryRemove(args.UserId, out _);
                pending.Cts.Cancel();
                pending.Cts.Dispose();
                return;
            }

            if (afterChannelId != null)
            {
                var channel = await args.After.GetChannelAsync();
                if (channel is not null)
                {
                    var name = await GetVoiceEventDisplayNameAsync(args);
                    await UserUpdateChannel.SendMessageAsync($"{name} joined voice channel {channel.Name}");
                }
            }

            if (beforeChannelId != null && afterChannelId == null)
            {
                var channel = await args.Before.GetChannelAsync();
                if (channel is not null)
                {
                    var name = await GetVoiceEventDisplayNameAsync(args);
                    var cts = new CancellationTokenSource();
                    PendingVoiceLeaves[args.UserId] = (beforeChannelId.Value, cts);
                    _ = AnnounceVoiceLeaveAfterDelay(args.UserId, name, channel.Name, cts);
                }
            }
            else if (beforeChannelId != null && afterChannelId != null)
            {
                // A genuine move between channels, not a disconnect - announce immediately.
                var channel = await args.Before.GetChannelAsync();
                if (channel is not null)
                {
                    var name = await GetVoiceEventDisplayNameAsync(args);
                    await UserUpdateChannel.SendMessageAsync($"{name} left voice channel {channel.Name}");
                }
            }
        }

        private async Task AnnounceVoiceLeaveAfterDelay(ulong userId, string displayName, string channelName, CancellationTokenSource cts)
        {
            try
            {
                await Task.Delay(VoiceLeaveDebounce, cts.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            finally
            {
                PendingVoiceLeaves.TryRemove(userId, out _);
                cts.Dispose();
            }

            if (UserUpdateChannel is not null)
                await UserUpdateChannel.SendMessageAsync($"{displayName} left voice channel {channelName}");
        }

        private static async Task<string> GetVoiceEventDisplayNameAsync(VoiceStateUpdatedEventArgs args)
        {
            var user = await args.GetUserAsync();
            return (user as DiscordMember)?.DisplayName ?? user?.Username ?? args.UserId.ToString();
        }

        public void StartLogin()
        {
            SchalaClient.Log(LogEventType.System, "Connecting to Discord...");

            Client.ConnectAsync().GetAwaiter().GetResult();

            SchalaClient.Log(LogEventType.System, "Connected!");
            ConnectionStatus = "Connected";
        }

        public void LoadWeightedLists()
        {
            if (!Directory.Exists(".\\WeightedLists\\"))
            {
                Directory.CreateDirectory(".\\WeightedLists\\");
            }

            foreach (string filename in Directory.GetFiles(".\\WeightedLists\\", "*.json"))
            {
                WeightedLists.Add(WeightedList.Load(filename));
            }
        }

        #endregion
        #region Discord.NET Event Callbacks
        public async Task OnConnected(SocketEventArgs e)
        {
            if (Client.CurrentUser.Username != activeBotName)
            {
                await SetUsername(activeBotName);
            }
        }

        public async Task GuildAvailable(GuildAvailableEventArgs e)
        {
            await Task.Run(async () =>
            {
                Log(LogEventType.System, "Connecting to " + e.Guild.Name);
                LoadEmojis(e.Guild);
                //DownloadUsersAsync(e.Guild);

                if (e.Guild.Id == ZealGuildID)
                {
                    //Grab the channel ID for user updates.
                    DiscordChannel channel = e.Guild.Channels[ZealUserUpdateChannelID];
                    UserUpdateChannel = channel;

                    await StartClock(e.Guild);
                    SpecialDiceRollers.SetupDice();
                }
            });
        }

        //private async Task DownloadUsersAsync(SocketGuild Guild)
        //{
        //    await Guild.DownloadUsersAsync().ConfigureAwait(false);
        //    Log(LogEventType.System, "Users cached for " + Guild.Name);
        //}

        private async Task SetUsername(string Name)
        {
            await Client.ModifyCurrentUserAsync(Name);
            await Client.UpdateStatusAsync(new DiscordActivity("Use /schala for commands", DiscordActivityType.Custom));
        }

        public async Task MessageReactionAdded(MessageReactionAddedEventArgs e)
        {
            if (e.Guild is null)
                return;

            await HandleReaction(e.Message, e.Channel, e.User, e.Guild, e.Emoji, ReactionChange.Added);
        }
        public async Task MessageReactionRemoved(MessageReactionRemovedEventArgs e)
        {
            if (e.Guild is null)
                return;

            await HandleReaction(e.Message, e.Channel, e.User, e.Guild, e.Emoji, ReactionChange.Removed);
        }

        public async Task HandleReaction(DiscordMessage Message, DiscordChannel Channel, DiscordUser User, DiscordGuild Guild, DiscordEmoji Emoji, ReactionChange Change)
        {
            await ScrapeEmoji(User.Id, Channel, Emoji);
            DiscordMember member = await Guild.GetMemberAsync(User.Id);

            switch (Message.Id)
            {
                case ulong Id when Message.Id == OldSkygateWelcomeMessage:
                    DiscordRole earthboundRole = Guild.Roles.First(r => r.Value.Name == "Earthbound").Value;
                    if (Change == ReactionChange.Added)
                    {
                        await member.GrantRoleAsync(earthboundRole);
                    }
                    break;
                case ulong Id when Message.Id == OldSkygateRoleReacts:
                    DiscordRole? kozrole = null;
                    switch (Emoji.Name)
                    {
                        case "minecraft":
                            kozrole = Guild.Roles.First(r => r.Value.Name == "Plays Minecraft").Value;
                            break;
                        case "ffxiv":
                            kozrole = Guild.Roles.First(r => r.Value.Name == "Plays FFXIV").Value;
                            break;
                        case "world":
                            kozrole = Guild.Roles.First(r => r.Value.Name == "Plays Grand Strategy").Value;
                            break;
                        case "warframe":
                            kozrole = Guild.Roles.First(r => r.Value.Name == "Plays Warframe").Value;
                            break;
                        case "wiggler":
                            kozrole = Guild.Roles.First(r => r.Value.Name == "Plays Monster Hunter").Value;
                            break;
                        case "🎲":
                            kozrole = Guild.Roles.First(r => r.Value.Name == "Plays Tabletop").Value;
                            break;
                        case "sus":
                            kozrole = Guild.Roles.First(r => r.Value.Name == "Plays Among Us").Value;
                            break;
                        case "🌎":
                            kozrole = Guild.Roles.First(r => r.Value.Name == "Plays New World").Value;
                            break;
                        default:
                            return;
                    }
                    if (Change == ReactionChange.Added)
                    {
                        await member.GrantRoleAsync(kozrole);
                    }
                    else
                    {
                        await member.RevokeRoleAsync(kozrole);
                    }

                    string kozDisplayName = member.Nickname;
                    if (member.Nickname == null)
                        kozDisplayName = member.Username;
                    var kozresult = await Channel.SendMessageAsync(kozDisplayName + ", your roles have been updated.");
                    DeleteMessage(Channel, kozresult, new TimeSpan(0, 0, 10));
                    break;
                default:
                    break;
            }


            return;
        }

        public async Task ClientMessageReceived(MessageCreatedEventArgs e)
        {
            MessageState state = new MessageState(e);

            if (state.Channel == "emojis")
                return;
            Regex regex = new Regex(@"<a?:(.*?):(\d+)>");

            foreach (Match match in regex.Matches(e.Message.Content))
            {
                if (ulong.TryParse(match.Groups[2].Value, out ulong emojiID) && e.Message.Channel is not null)
                {
                    await ScrapeEmoji(state.UserID, e.Message.Channel, emojiID);
                }
            }
        }

        public async Task OnMessageUpdated(MessageUpdatedEventArgs e)
        {
            await Task.Delay(0);
            return;
        }

        public async Task GuildMemberRemoved(GuildMemberRemovedEventArgs args)
        {
            if (UserUpdateChannel is not null)
                await UserUpdateChannel.SendMessageAsync($"<@{args.Member.Id}> left or was kicked from {args.Guild.Name}.");
        }

        public async Task UserUpdated(UserUpdatedEventArgs args)
        {
            if (args.UserBefore.Username != args.UserAfter.Username && UserUpdateChannel is not null)
            {
                await UserUpdateChannel.SendMessageAsync($"<@{args.UserBefore.Id}> changed their nickname from {args.UserBefore.Username} to {args.UserAfter.Username}.");
            }
        }
        #endregion
        #region Utility
        public static void Log(LogLevel Severity, EventId Id, string Message)
        {
            if (Current == null)
            {
                Debug.WriteLine($"[{Severity.ToString()}] [{Id.Name}] {Message}");
            }
            else
            {
                Current.Client.Logger.Log(Severity, Id, Message);
            }
        }

        public static void Log(LogEventType EventType, string Message, Exception? Error = null)
        {
            LogLevel level = EventType != LogEventType.Error ? LogLevel.Information : LogLevel.Error;
            if (Current == null)
            {
                Debug.WriteLine($"[{level}] [{EventIds[EventType].Name}] {Message}; {(Error != null ? Error.Message : "(No Exception)")}");
            }
            else
            {
                if (Error == null)
                    Current.Client.Logger.Log(level, EventIds[EventType], Message);
                else
                    Current.Client.Logger.Log(level, EventIds[EventType], Message, Error);
            }
        }

        public async Task Run(string ActionName, Action action, TimeSpan period, CancellationToken cancelToken)
        {
            Log(LogEventType.System, $"{ActionName} scheduled in {period}.");
            while (!cancelToken.IsCancellationRequested)
            {
                await Task.Delay(period, cancelToken);

                if (!cancelToken.IsCancellationRequested)
                {
                    Log(LogEventType.System, $"{ActionName} executed a delayed task scheduled after {period}.");
                    action();
                    return;
                }
            }
        }

        public Task Run(string ActionName, Action action, TimeSpan period)
        {
            return Run(ActionName, action, period, CancellationToken.None);
        }

        public void DeleteMessage(DiscordChannel Channel, DiscordMessage Message, TimeSpan DeleteTime)
        {
            Run("Delete Message " + Message.Content, () => Channel.DeleteMessageAsync(Message), DeleteTime);
        }

        public void LoadEmojis(DiscordGuild Guild)
        {
            //Special logic for our home server.
            if (Guild.Id == ZealGuildID)
            {
                DiscordChannel channel = Guild.Channels[ZealEmojiChannelID];

                EmojiChannel = channel;

                foreach (var emoji in Guild.Emojis)
                {
                    ScrapedEmojis.Add(emoji.Key);
                }
            }

            //Read all the emojis of all the servers, though.
            Dictionary<string, string> guildEmojis = new Dictionary<string, string>();
            if (Guild.Name != null && !GuildEmojiLookup.ContainsKey(Guild.Id))
            {
                GuildEmojiLookup.Add(Guild.Id, guildEmojis);

                foreach (var emoji in Guild.Emojis)
                {
                    if (emoji.Value is not null && emoji.Value.Name != null && !guildEmojis.ContainsKey(emoji.Value.Name))
                    {
                        guildEmojis.Add(emoji.Value.Name, $" <:{emoji.Value.Name}:{emoji.Key}>");
                    }
                }
            }

            Log(LogEventType.System, "Emojis loaded for " + Guild.Name);
            return;
        }

        public string ResolveEmoji(ulong guildID, string emojiName)
        {
            if (GuildEmojiLookup.ContainsKey(guildID))
            {
                if (GuildEmojiLookup[guildID].ContainsKey(emojiName))
                {
                    return GuildEmojiLookup[guildID][emojiName];
                }

                return $"[Guild {guildID} does not have an emoji named {emojiName}]";
            }

            return $"[Not currently connected to {guildID}]";
        }

        public async Task ScrapeEmoji(ulong userID, DiscordChannel Channel, DiscordEmoji Emoji)
        {
            if (userID != SchalaUserID && !ScrapedEmojis.Contains(Emoji.Id) && EmojiChannel is not null)
            {
                ScrapedEmojis.Add(Emoji.Id);
                await EmojiChannel.SendMessageAsync($"<@{userID}> posted a message in <#{Channel.Id}> with the following emoji: <:{Emoji.Name}:{Emoji.Id}>\n{Emoji.Url}");
            }

           return;
        }

        public async Task ScrapeEmoji(ulong userID, DiscordChannel Channel, ulong emojiID)
        {
            if (userID != SchalaUserID && !ScrapedEmojis.Contains(emojiID) && EmojiChannel is not null)
            {
                if (DiscordEmoji.TryFromGuildEmote(Client, emojiID, out DiscordEmoji Emoji))
                {
                    ScrapedEmojis.Add(emojiID);
                    await EmojiChannel.SendMessageAsync($"<@{userID}> posted a message in <#{Channel.Id}> with the following emoji: <:{Emoji.Name}:{Emoji.Id}>\n{Emoji.Url}");
                }
            }

            return;
        }

        public async Task StartClock(DiscordGuild Guild) //This should only be called for Zeal.
        {
            await Task.Delay(0);
            //SeattleClockChannel = Guild.GetVoiceChannel(SeattleClockChannelId);
            //EorzeaClockChannel = Guild.GetVoiceChannel(EorzeaClockChannelId);

            //TimerCallback updateClock = (obj) =>
            //{
            //    SeattleClockChannel.ModifyAsync((prop) => prop.Name = "⏲ " + DateTime.Now.ToShortTimeString() + " ZST");

            //    DateTimeOffset final = DateTimeOffset.FromUnixTimeSeconds((long)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 20.571428571428573d));

            //    EorzeaClockChannel.ModifyAsync((prop) => prop.Name = "⏲ " + final.DateTime.ToShortTimeString() + " ET");
            //};

            ////This has been increased to a 5 minute interval to comply with Discord spam rules.
            //UpdateClock = new Timer(updateClock, null, new TimeSpan(0, 0, 301 - DateTime.Now.Second), new TimeSpan(0, 0, 300));
        }

//        async Task<List<EmbedBuilder>> ReadEmbeds(string Path)
//#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
//        {
//            List<EmbedBuilder> embeds = new List<EmbedBuilder>();

//            if (!File.Exists(Path))
//            {
//                return embeds;
//            }

//            using (FileStream fs = new FileStream(Path, FileMode.Open))
//            {
//                string currentLine, currentKey, currentValue;
//                //EmbedBuilder currentEmbed = null;
//                using (StreamReader reader = new StreamReader(fs))
//                {
//                    currentLine = reader.ReadLine();
//                    if (!currentLine.Contains("="))
//                    {
//                        currentKey = currentLine;
//                        currentValue = "";
//                    }
//                    else
//                    {
//                        string[] pieces = currentLine.Split(new char[] { '=' }, 2);
//                        currentKey = pieces[0];
//                        currentValue = pieces[1];
//                    }

//                    switch (currentKey)
//                    {
//                        case "Name":
//                            break;
//                        case "Description":
//                            break;
//                        case "Thumbnail":
//                            break;
//                        case "Image":
//                            break;
//                        case "Footer":
//                            break;
//                        case "Inline":
//                            break;
//                        default:
//                            break;
//                    }
//                }
//            }

//            return new List<EmbedBuilder>();
//        }

#endregion
    }
}
