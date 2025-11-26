using CSMusicBot.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Integrations.SponsorBlock;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;


namespace CSMusicBot.Commands.Music
{
    public class Player : ModuleBase<SocketCommandContext>
    {
        private readonly IAudioService audioService;
        private readonly ITrackQueue trackQueues;
        private readonly ImmutableArray<SegmentCategory> segments;
        private readonly IOptions<Command> options;
        public Player(IAudioService audioService, ITrackQueue tracks, IOptions<Command> options) 
        { 
            this.audioService = audioService;
            this.trackQueues = tracks;
            this.segments = [SegmentCategory.Intro, SegmentCategory.Sponsor, SegmentCategory.SelfPromotion, SegmentCategory.OfftopicMusic, SegmentCategory.Outro, SegmentCategory.Preview];
            this.options = options;
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("plays the provided song")]
        public async Task Play()
        {
            var cmd = options.Value;
            await Context.Message.ReplyAsync($"{cmd.WarningEmoji} Play Commands:\n`{cmd.Prefix}play <song title>` - plays the first result from Youtube\n`{cmd.Prefix}play <URL> - plays the provided song, playlist, or stream`");
        }

        [Command("p", RunMode = RunMode.Async)]
        [Summary("plays the provided song")]
        public async Task P()
        {
            await Play();
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("plays the provided song")]
        [Alias("url")]
        public async Task Play(string url)
        {
            var cmd = options.Value;
            var playerOptions = new LavalinkPlayerOptions
            {
                InitialTrack = new TrackQueueItem(url),
            };
            var msg = await Context.Message.ReplyAsync($"{cmd.LoadingEmoji} Loading... `[{url}]`");
            var player = await GetPlayerAsync().ConfigureAwait(false);
            if (player == null)
            {
                return;
            }
            if (url.Contains("https://www.youtube.com/shorts/"))
            {
                url = url.Replace("https://www.youtube.com/shorts/", "https://www.youtube.com/watch?v=");
            }
            var track = await audioService.Tracks.LoadTrackAsync(url, new TrackLoadOptions
            {
                CacheMode = Lavalink4NET.Rest.Entities.CacheMode.Dynamic,
                SearchBehavior = StrictSearchBehavior.Resolve,
                SearchMode = TrackSearchMode.YouTube
            }).ConfigureAwait(false);

            if (track is null)
            {
                await msg.ModifyAsync((p) =>
                {
                    p.Content = $"{cmd.WarningEmoji} No results found for `{url}`.";
                }).ConfigureAwait(false);
                return;
            }

            var position = await player.PlayAsync(track).ConfigureAwait(false);
            var successString = $"{cmd.SuccessEmoji} Added **{track.Title}** (`{track.Duration}`)";
            if (position is 0)
            {
                await msg.ModifyAsync((p) =>
                {
                    p.Content = successString + " to begin playing.";
                }).ConfigureAwait(false);
            }
            else
            {
                await msg.ModifyAsync((p) =>
                {
                    p.Content = successString + " to the queue at position " + position + ".";
                }).ConfigureAwait(false);
            }
        }

        [Command("p", RunMode = RunMode.Async)]
        [Summary("plays the provided song")]
        [Alias("url")]
        public async Task P(string url)
        {
            await Play(url);
        }


        [Command("stop", RunMode = RunMode.Async)]
        [Summary("stops the current song and clears the queue")]
        public async Task Stop()
        {
            var cmd = options.Value;
            var player = await GetPlayerAsync(connectToVoiceChannel: false);

            if (player is null)
            {
                return;
            }

            if (player.CurrentTrack is null)
            {
                await Context.Message.ReplyAsync($"{cmd.WarningEmoji} The player is not playing any track.").ConfigureAwait(false);
                return;
            }

            await player.StopAsync().ConfigureAwait(false);

            var channel = await Context.Client.GetChannelAsync(player.VoiceChannelId);
            var vc = (SocketVoiceChannel)channel;
            await vc.DisconnectAsync().ConfigureAwait(false);

            await Context.Message.ReplyAsync($"{cmd.SuccessEmoji} The player has stopped and the queue has been cleared.").ConfigureAwait(false);
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Summary("skip the current song")]
        public async Task Skip()
        {
            var cmd = options.Value;
            var player = await GetPlayerAsync(connectToVoiceChannel: true);

            if (player is null)
            {
                return;
            }

            var track = player.CurrentTrack;

            if (track is null)
            {
                await Context.Message.ReplyAsync($"{cmd.WarningEmoji} The player is not playing any track.").ConfigureAwait(false);
                return;
            }

            await player.SkipAsync();
            await Context.Message.ReplyAsync($"{cmd.SuccessEmoji} Skipped **{track.Title}**.");
        }

        [Command("shuffle", RunMode = RunMode.Async)]
        [Summary("shuffles songs you have added")]
        public async Task Shuffle()
        {
            var cmd = options.Value;
            var player = await GetPlayerAsync(false);

            if (player is null)
            {
                return;
            }

            var queueAmount = player.Queue.Count;

            if(queueAmount == 0)
            {
                await Context.Message.ReplyAsync($"You don't have any music in the queue to shuffle!");
            }
            else if(queueAmount == 1)
            {
                await Context.Message.ReplyAsync($"You only have one song in the queue!");
            }
            else
            {
                await player.Queue.ShuffleAsync();

                await Context.Message.ReplyAsync($"You successfully shuffled your {queueAmount} entries.");
            }
        }

        [Command("volume", RunMode = RunMode.Async)]
        [Summary("sets or shows volume [0-150]")]
        [Alias("vol")]
        public async Task Volume(string vol)
        {
            var cmd = options.Value;
            var player = await GetPlayerAsync(false);

            if (player is null)
            {
                return;
            }

            if(int.TryParse(vol, out int realVol))
            {
                if(realVol < 0 || realVol > 150)
                {
                    await Context.Message.ReplyAsync($"{cmd.ErrorEmoji} Volume must be a valid integer between 0 and 150!");
                    return;
                }
                await Context.Message.ReplyAsync($"{cmd.SuccessEmoji} Volume changed from `{player.Volume * 100}` to `{realVol}`");
                await player.SetVolumeAsync(realVol / 100f);
            }
            else
            {
                await Context.Message.ReplyAsync($"{cmd.ErrorEmoji} Volume must be a valid integer between 0 and 150!");
                return;
            }
        }

        [Command("repeat", RunMode = RunMode.Async)]
        [Summary("re-adds music to the queue when finished. Can use with \"loop\" [off|all|single]")]
        public async Task Repeat(string args)
        {
            var cmd = options.Value;
            var player = await GetPlayerAsync(false);

            if (player is null)
            {
                return;
            }

            switch (args.ToLower())
            {
                case "off":
                    player.RepeatMode = TrackRepeatMode.None;
                    break;
                case "all":
                    player.RepeatMode = TrackRepeatMode.Queue;
                    break;
                case "single":
                case "one":
                    player.RepeatMode = TrackRepeatMode.Track;
                    break;
                default:
                    if(player.RepeatMode == TrackRepeatMode.None)
                    {
                        player.RepeatMode = TrackRepeatMode.Queue;
                    }
                    else
                    {
                        player.RepeatMode = TrackRepeatMode.None;
                    }
                    break;
            }

            await Context.Message.ReplyAsync($"Repeat mode is now {player.RepeatMode.ToString()}");
        }

        [Command("repeat", RunMode = RunMode.Async)]
        [Summary("re-adds music to the queue when finished. Can use with \"loop\" [off|all|single]")]
        public async Task Repeat()
        {
            await Repeat(string.Empty);
        }

        [Command("loop", RunMode = RunMode.Async)]
        [Summary("re-adds music to the queue when finished. Can use with \"loop\" [off|all|single]")]
        public async Task Loop(string args)
        {
            await Repeat(args);
        }

        public async Task Loop()
        {
            await Repeat(string.Empty);
        }

        [Command("nowplaying", RunMode = RunMode.Async)]
        [Summary("shows the song that is currently playing")]
        public async Task NowPlaying()
        {
            var cmd = options.Value;
            var player = await GetPlayerAsync(false);

            if (player is null)
            {
                return;
            }

            var track = player.CurrentTrack;
            if (track is null)
            {
                await ReplyAsync($"{cmd.WarningEmoji} **Now Playing...**\nNo music playing");
            }
            else
            {
                var channel = await Context.Client.GetChannelAsync(player.VoiceChannelId);
                var vc = (SocketVoiceChannel)channel;
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.Color = Color.Green;
                embedBuilder.Author = new EmbedAuthorBuilder();
                embedBuilder.Author.Name = track.Author;
                embedBuilder.Title = track.Title;
                embedBuilder.ThumbnailUrl = "https://img.youtube.com/vi/" + track.Identifier + "/mqdefault.jpg";
                embedBuilder.Description = track.Duration.ToString();

                await Context.Message.ReplyAsync($"{cmd.SuccessEmoji} **Now Playing in {vc.Mention}...\n{track.Title}**", embed: embedBuilder.Build());
            }
        }

        [Command("np", RunMode = RunMode.Async)]
        [Summary("shows the song that is currently playing")]
        public async Task NP()
        {
            await NowPlaying();
        }


        /// <summary>
        ///     Gets the guild player asynchronously.
        /// </summary>
        /// <param name="connectToVoiceChannel">
        ///     a value indicating whether to connect to a voice channel
        /// </param>
        /// <returns>
        ///     a task that represents the asynchronous operation. The task result is the lavalink player.
        /// </returns>
        private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(bool connectToVoiceChannel = true)
        {
            var retrieveOptions = new PlayerRetrieveOptions(
                ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

            var guildUser = (Context.User as SocketGuildUser);
            if (guildUser == null) 
            {
                return null;
            }
            var result = await audioService.Players
                .RetrieveAsync(Context.Guild.Id, guildUser.VoiceChannel.Id, playerFactory: PlayerFactory.Queued, Options.Create(new QueuedLavalinkPlayerOptions()
                {
                    TrackQueue = trackQueues
                }), retrieveOptions)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Status switch
                {
                    PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                    PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                    _ => "Unknown error.",
                };

                await Context.Message.ReplyAsync(errorMessage);

                return null;
            }
            var player = result.Player;
            if (player != null)
            {
                try
                {
                    await player.UpdateSponsorBlockCategoriesAsync(segments).ConfigureAwait(false);
                }
                catch
                {

                }
            }
            return player;
        }
    }
}
