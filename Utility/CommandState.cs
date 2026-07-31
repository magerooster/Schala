using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Entities;

namespace Schala
{
    public class CommandState : MessageState
    {
        public CommandContext Args { get; protected set; }

        public DiscordUser? SourceUser { get; protected set; }

        public CommandState(CommandContext e) : base()
        {
            Args = e;

            if (e is TextCommandContext textCtx)
            {
                FullMessage = textCtx.Message;
                SourceUser = FullMessage.Author;
            }
            else if (e is SlashCommandContext slashCtx)
            {
                FullInteraction = slashCtx.Interaction;
                SourceUser = slashCtx.User;
                // interaction does not have a Message; it's an interaction
            }

        }

        #region Cheaty Properties To Format Things For Me
        public override string Username => SourceUser?.Username ?? "(No Name)";

        public override ulong UserID => SourceUser?.Id ?? 0;

        public override ulong GuildID => Args?.Guild?.Id ?? 0;

        public override string Channel => Args.Channel.Name;

        //public ulong ChannelID => Args.Channel.Id;

        protected override async Task RespondAsync(string Message)
        {
            if (Args is SlashCommandContext slashCtx)
            {
                await slashCtx.EditResponseAsync(Message);
            }
            else
            {
                await Args.RespondAsync(new DiscordMessageBuilder()
                          .WithContent(Message)
                          .WithReply(MessageID, true));
            }
        }

        public override async Task StartResponseAsync(bool Ephermeral)
        {
            if (Args is SlashCommandContext slashCtx)
            {
                await slashCtx.DeferResponseAsync(Ephermeral);
            }
        }

        public async Task RespondEphemeralAsync(string Message)
        {
            if (Args is SlashCommandContext slashCtx)
            {
                await slashCtx.RespondAsync(Message, true);
            }
            else
            {
                await Args.RespondAsync(new DiscordMessageBuilder()
                          .WithContent(Message)
                          .WithReply(MessageID, true));
            }
        }

        public override async Task FinishResponseAsync(string Message)
        {
            await RespondAsync(Message);
        }

        #endregion
    }

    public class SlashCommandState : MessageState
    {
        private CommandContext Context { get; }

        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
        //public DiscordInteraction? FullInteraction { get; private set; }

        public SlashCommandState(CommandContext ctx)
        {
            Context = ctx;
            if (ctx is SlashCommandContext slashCtx)
            {
                FullInteraction = slashCtx.Interaction;
            }
        }

        private void FillParameters(Dictionary<string, object> Dictionary, DiscordInteractionDataOption Option)
        {
            if (Option.Options == null)
                return;

            foreach (var option in Option.Options)
            {
                if (option.Value != null)
                    Parameters.Add(option.Name, option.Value);
                if (option.Options != null)
                    FillParameters(Dictionary, option);
            }
        }

        public override string Username => FullInteraction?.User.Username ?? "(No Name)";
        public override ulong UserID => FullInteraction?.User.Id ?? 0;
        public override ulong GuildID => FullInteraction?.Guild?.Id ?? 0;
        public override string Channel => FullInteraction?.Channel?.Name ?? "(No Channel)";
        public override ulong ChannelID => FullInteraction?.Channel.Id ?? 0;

        protected override async Task RespondAsync(string Message)
        {
            if (Context is SlashCommandContext slashCtx)
            {
                await slashCtx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent(Message));
            }
            else
            {
                // Fallback for text commands (should not happen in this class)
                await Context.RespondAsync(Message);
            }
        }

        public async Task RespondEphemeralAsync(string Message)
        {
            if (Context is SlashCommandContext slashCtx)
            {
                await slashCtx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent(Message)
                    .AsEphemeral());
            }
            else
            {
                // For text commands, ephemeral doesn't apply; respond normally.
                await Context.RespondAsync(Message);
            }
        }

        public override async Task StartResponseAsync(bool ephemeral)
        {
            if (Context is SlashCommandContext slashCtx)
            {
                await slashCtx.DeferResponseAsync(ephemeral);
            }
            else
            {
                await Context.DeferResponseAsync(); // no ephemeral for text
            }
        }

        public override async Task FinishResponseAsync(string Message)
        {
            if (Context is SlashCommandContext slashCtx)
            {
                await slashCtx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Message));
            }
            else
            {
                await RespondAsync(Message);
            }
        }
    }
}
