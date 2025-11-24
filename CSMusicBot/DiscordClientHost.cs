
using CSMusicBot.Commands;
using Discord;
using Discord.WebSocket;

namespace CSMusicBot
{
    public class DiscordClientHost : IHostedService
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IConfiguration _configuration;
        private readonly CommandHandler _commandHandler;

        public DiscordClientHost(DiscordSocketClient discordSocketClient, CommandHandler commandHandler, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(discordSocketClient);
            ArgumentNullException.ThrowIfNull(commandHandler);
            ArgumentNullException.ThrowIfNull(configuration);

            _discordSocketClient = discordSocketClient;
            _commandHandler = commandHandler;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _commandHandler.InstallCommandsAsync();
            
            await _discordSocketClient
                .LoginAsync(TokenType.Bot, _configuration.GetValue<string>("Token"))
                .ConfigureAwait(false);

            await _discordSocketClient
                .StartAsync()
                .ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discordSocketClient
                .StopAsync()
                .ConfigureAwait(false);
        }
    }
}
