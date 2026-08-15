using System.Text;
using System.Threading.Tasks;

using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

using Polly;

namespace Schala;

public class MessageState
{
    protected StringBuilder HeaderText = new();
    protected StringBuilder BlockQuoteText = new();
    internal DiscordMessage? FullMessage;
    internal DiscordInteraction? FullInteraction;

    public MessageState()
    {

    }

    public MessageState(MessageCreatedEventArgs e)
    {
        FullMessage = e.Message;
    }

    #region Cheaty Properties To Format Things For Me
    public virtual string Username => FullMessage?.Author?.Username ?? string.Empty;


    public virtual ulong UserID => FullMessage?.Author?.Id ?? 0;

    public virtual ulong GuildID => Guild?.Id ?? 0; //FullMessage.  Args.Guild.Id;

    public virtual DiscordGuild? Guild => FullMessage?.Channel?.Guild;

    public virtual string Channel => FullMessage?.Channel?.Name ?? string.Empty; // FullMessage.Source;

    public virtual ulong ChannelID => FullMessage?.Channel?.Id ?? 0;

    public virtual string Message => FullMessage?.Content ?? string.Empty;

    public virtual ulong MessageID => FullMessage?.Id ?? 0;

    public virtual string Header => HeaderText.ToString();

    public virtual string BlockQuote => BlockQuoteText.ToString();

    public virtual string UsernameForHeader => "<@" + UserID + ">";

    public virtual string FormattedHeaderText => "** " + HeaderText + " **";

    public virtual string FormattedBlockText => BlockQuoteText.Length > 0 ? "" + BlockQuoteText + "" : "";
    #endregion

    #region Other stuff
    public void AppendToHeader(string s)
    {
        HeaderText.Append(s);
    }

    public void AppendToBlock(string s)
    {
        if (BlockQuoteText.Length < 500)
            BlockQuoteText.Append(s);
    }

    protected virtual async Task RespondAsync(string Message)
    {
        if (FullMessage == null)
            return;

        await FullMessage.RespondAsync(new DiscordMessageBuilder()
                           .WithContent(Message)
                           .WithReply(MessageID, true));
    }

    public virtual Task StartResponseAsync(bool Ephermeral)
    {
        return Task.Delay(0);
    }

    public virtual Task FinishResponseAsync(string Message)
    {
        return Task.Delay(0);
    }
    #endregion 
}
