#define MAINZEAL

using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Extensions;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using Swordfish.NET.Collections;
using System.Diagnostics;
using System.Reactive.Linq;

namespace Schala
{
    public class Program
    {
        public static ConcurrentObservableCollection<LogEvent> MainLog { get; private set; } = [];
        private static Dictionary<string, string> knownTokens = new Dictionary<string, string>();


        public static async Task Main(string[] args)
        {
            //ILoggingBuilder builder;
            //ILoggerFactory factory;

            //Create our logger.
            Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Observers(events => events.Do(evt =>
                {
                    MainLog.Add(evt);
                })
                .Subscribe())
                .CreateLogger();

            string buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "unknown";
            string gitSha = Environment.GetEnvironmentVariable("GIT_SHA") ?? "unknown";
            Serilog.Log.Logger.Information($"Schala build {buildNumber} ({gitSha})");

            Serilog.Log.Logger.Information($"Command-line args: [{string.Join(", ", args)}]");

            //Find our token.
            string[] tokenFileLocations = ["./KnownServices.json", "/app/config/KnownServices.json"];
            string? tokenFileLocation = null;
            foreach (string candidate in tokenFileLocations)
            {
                bool readable = TryProbeFile(candidate, out string status);
                Serilog.Log.Logger.Information($"Checking config path {candidate}: {status}");
                tokenFileLocation ??= readable ? candidate : null;
            }
            tokenFileLocation ??= tokenFileLocations[0];
            Serilog.Log.Logger.Information($"Using config file path: {tokenFileLocation}");
            ReadBotTokenFile(tokenFileLocation);

            string token = string.Empty;
            string name = string.Empty;
            foreach (var rawArg in args)
            {
                var arg = rawArg.StartsWith("--", StringComparison.Ordinal) ? rawArg.Substring(2) : rawArg;

                if (arg.StartsWith("token=", StringComparison.OrdinalIgnoreCase))
                {
                    token = arg.Substring("token=".Length);
                    break;
                }
                if (arg.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
                {
                    name = arg.Substring("name=".Length);
                    if (knownTokens.ContainsKey(name))
                        token = knownTokens[name];
                }
            }

            Serilog.Log.Logger.Information($"Parsed name=\"{name}\"; found matching token in {tokenFileLocation}? {knownTokens.ContainsKey(name)}");

            while (string.IsNullOrEmpty(token))
            {
                Serilog.Log.Logger.Warning($"No token resolved for name=\"{name}\". Not connecting to Discord; waiting for changes to {tokenFileLocation} before retrying.");

                DateTime lastWriteTime = GetSafeWriteTime(tokenFileLocation);
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));

                    string currentLocation = tokenFileLocations.FirstOrDefault(p => TryProbeFile(p, out _)) ?? tokenFileLocation;
                    DateTime currentWriteTime = GetSafeWriteTime(currentLocation);

                    if (currentLocation != tokenFileLocation || currentWriteTime != lastWriteTime)
                    {
                        tokenFileLocation = currentLocation;
                        break;
                    }
                }

                Serilog.Log.Logger.Information($"Detected change to {tokenFileLocation}; re-reading token information.");
                ReadBotTokenFile(tokenFileLocation);
                if (knownTokens.TryGetValue(name, out string? updatedToken))
                    token = updatedToken;

                Serilog.Log.Logger.Information($"Parsed name=\"{name}\"; found matching token in {tokenFileLocation}? {knownTokens.ContainsKey(name)}");
            }

            IHost host = Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddCommandLine(args);
                    config.Build();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<SchalaClient>();
                    services.AddHostedService(sp => sp.GetRequiredService<SchalaClient>());
                    services.AddDiscordClient(token, DiscordIntents.AllUnprivileged);

                    //Register commands
                    services.AddCommandsExtension((serviceProvider, commandsExtension) =>
                    {
                        // 1. Add Command Processors (e.g., Slash, Text)
                        // The SlashCommandProcessor is registered by default.
                        // If you want text commands (with "!" prefix), add a TextCommandProcessor:
                        commandsExtension.AddProcessor(new TextCommandProcessor(new TextCommandConfiguration()));

                        // 2. Register your command classes
                        // Global Commands
                        commandsExtension.AddCommands<SchalaGlobalCommands>();
                        commandsExtension.AddCommands<SchalaGlobalCommandGroups>();
                        commandsExtension.AddCommands<AdminModule>();

                        // Guild-Specific Commands (using your preprocessor directives)
#if MAINZEAL
                        commandsExtension.AddCommands<SchalaZealCommands>(SchalaClient.ZealGuildID);
#else
                        commandsExtension.AddCommands<SchalaZealCommands>(SchalaClient.BackupZealGuildID);
#endif
                    });

                    //Register events
                    services.ConfigureEventHandlers(b =>
                    {
                        b.HandleSocketOpened(async (client, e) =>
                        {
                            var schala = client.ServiceProvider.GetRequiredService<SchalaClient>();
                            await schala.OnConnected(e);
                        });

                        b.HandleGuildAvailable(async (client, e) =>
                        {
                            var schala = client.ServiceProvider.GetRequiredService<SchalaClient>();
                            await schala.GuildAvailable(e);
                        });

                        b.HandleMessageCreated(async (client, e) =>
                        {
                            var schala = client.ServiceProvider.GetRequiredService<SchalaClient>();
                            await schala.ClientMessageReceived(e);
                        });

                        b.HandleMessageUpdated(async (client, e) =>
                        {
                            var schala = client.ServiceProvider.GetRequiredService<SchalaClient>();
                            await schala.OnMessageUpdated(e);
                        });

                        b.HandleMessageReactionAdded(async (client, e) =>
                        {
                            var schala = client.ServiceProvider.GetRequiredService<SchalaClient>();
                            await schala.MessageReactionAdded(e);
                        });

                        b.HandleMessageReactionRemoved(async (client, e) =>
                        {
                            var schala = client.ServiceProvider.GetRequiredService<SchalaClient>();
                            await schala.MessageReactionRemoved(e);
                        });

                        b.HandleUserUpdated(async (client, e) =>
                        {
                            var schala = client.ServiceProvider.GetRequiredService<SchalaClient>();
                            await schala.UserUpdated(e);
                        });

                        b.HandleGuildMemberRemoved(async (client, e) =>
                        {
                            var schala = client.ServiceProvider.GetRequiredService<SchalaClient>();
                            await schala.GuildMemberRemoved(e);
                        });

                        b.HandleVoiceStateUpdated(async (client, e) =>
                        {
                            var schala = client.ServiceProvider.GetRequiredService<SchalaClient>();
                            await schala.VoiceStateUpdated(e);
                        });
                    });
                })
                .UseSerilog()
                .Build();

            host.Services.GetRequiredService<SchalaClient>().activeBotName = name;

            await host.RunAsync();
        }

        private static void ReadBotTokenFile(string Path)
        {
            //This will be a json file that looks like this:
            /*
              {
                "SomeDisplayableBotName": "__secret_token__________.______.__________________________",
                "AnotherDisplayableBotName": "__secret_token__________.______.__________________________"
              }
}           */

            Serilog.Log.Logger.Information("Attempting to read token information from " + Path);

            bool readable = TryProbeFile(Path, out string status);
            Serilog.Log.Logger.Information($"Config file status for {Path}: {status}");
            if (!readable)
                return;

            Dictionary<string, string>? tokens = Data.Load<Dictionary<string, string>>(Path, false);

            if (tokens == null)
            {
                Serilog.Log.Logger.Error("Could not read token information.");
                knownTokens = new Dictionary<string, string>();
            }
            else
            {
                Serilog.Log.Logger.Information($"Read {tokens.Count} tokens from file.");
                knownTokens = tokens;
            }
        }

        // Distinguishes "file doesn't exist" from "exists but can't be opened" (permissions,
        // locked, etc.) - File.Exists() silently swallows access errors and just returns false,
        // which hides permission problems on mounted volumes.
        private static bool TryProbeFile(string path, out string status)
        {
            try
            {
                using FileStream stream = File.OpenRead(path);
                status = "ok";
                return true;
            }
            catch (FileNotFoundException)
            {
                status = "not found";
            }
            catch (DirectoryNotFoundException)
            {
                status = "not found (containing directory missing)";
            }
            catch (UnauthorizedAccessException ex)
            {
                status = $"permission denied ({ex.Message})";
            }
            catch (IOException ex)
            {
                status = $"IO error ({ex.Message})";
            }
            return false;
        }

        private static DateTime GetSafeWriteTime(string path)
        {
            try
            {
                return File.GetLastWriteTimeUtc(path);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}