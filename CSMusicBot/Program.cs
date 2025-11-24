using CSMusicBot.Commands;
using CSMusicBot.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;

namespace CSMusicBot
{
    internal class Program
    {
        private static ILogger<DiscordSocketClient> logger;
        public static async Task Main()
        {
            var builder = new HostApplicationBuilder();
            var token = builder.Configuration.GetValue<string>("Token");
            var logSeverity = builder.Configuration.GetValue<string>("Logging.LogLevel.Default");

            builder.Services.Configure<Command>(builder.Configuration.GetSection("Command"));
            builder.Services.Configure<IdleInactivityTrackerOptions>(config =>
            {
                config.IdleStates = [PlayerState.Destroyed, PlayerState.NotPlaying];
                config.Timeout = TimeSpan.FromSeconds(1);
            });
            builder.Services.ConfigureLavalink(config =>
            {
                config.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Lavalink.Lavalink") ?? "https://lavalink.jirayu.net");
                config.Passphrase = builder.Configuration.GetValue<string>("Lavalink.Lavapass") ?? "youshallnotpass";
            });
            if (!Enum.TryParse(logSeverity, out LogSeverity result))
            {
                result = LogSeverity.Info;
            }
            var _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.GuildVoiceStates | GatewayIntents.MessageContent | GatewayIntents.Guilds,
                LogLevel = result
            });

            _client.Log += _client_Log;

            builder.Services.AddHostedService<DiscordClientHost>();
            builder.Services.AddSingleton(_client);
            builder.Services.AddSingleton<ITrackQueue, TrackQueue>();
            builder.Services.AddSingleton<CommandHandler>();
            builder.Services.AddSingleton<CommandService>();
            builder.Services.AddLogging();
            builder.Services.AddLavalink();

            var app = builder.Build();
            app.UseSponsorBlock();
            logger = app.Services.GetRequiredService<ILogger<DiscordSocketClient>>();
            await app.RunAsync();
        }


        private static Task _client_Log(LogMessage arg)
        {
            switch(arg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    logger.LogError($"{arg.Source}: {arg.Message ?? arg.Exception.ToString()}");
                    break;
                case LogSeverity.Warning:
                    logger.LogWarning($"{arg.Source}: {arg.Message}");
                    break;
                case LogSeverity.Info:
                    logger.LogInformation($"{arg.Source}: {arg.Message}");
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    logger.LogDebug($"{arg.Source}: {arg.Message}");
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
