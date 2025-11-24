using CSMusicBot.Models;
using Discord.Commands;
using Discord.WebSocket;
using Lavalink4NET;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace CSMusicBot.Commands
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IOptions<Command> _settings;
        private readonly ILogger<CommandHandler> _logger;
        private readonly IAudioService _audioService;
        private readonly IServiceProvider _serviceProvider;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands, IOptions<Command> appSettings, ILogger<CommandHandler> logger, IAudioService audioService, IServiceProvider serviceProvider)
        {
            _commands = commands;
            _client = client;
            _settings = appSettings;
            _logger = logger;
            _audioService = audioService;
            _serviceProvider = serviceProvider;
        }

        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            _client.UnknownDispatchReceived += UnknownDispatchReceived;

            _audioService.TrackEnded += _audioService_TrackEnded;

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: _serviceProvider);
        }

        private async Task _audioService_TrackEnded(object sender, Lavalink4NET.Events.Players.TrackEndedEventArgs eventArgs)
        {
            if (!eventArgs.MayStartNext)
            {
                //leave vc
                var channel = await _client.GetChannelAsync(eventArgs.Player.VoiceChannelId);
                var vc = (SocketVoiceChannel)channel;
                await vc.DisconnectAsync();
            }
        }

        private Task UnknownDispatchReceived(string arg1, Newtonsoft.Json.Linq.JToken arg2)
        {
            Console.WriteLine("Unknown received");
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            if (!(message.HasCharPrefix(_settings.Value.Prefix.First(), ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            var context = new SocketCommandContext(_client, message);

            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _serviceProvider);
        }
    }
}
