using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//public static class UtilityCommands
//{
//    public static async Task<string> SetPin(SocketCommandContext Args, string Text)
//    {

//        CommandState State = new CommandState(Args, Text);

//        Match result = Regex.Match(Text, @"(add|append|create|clear|delete) ([#]?\S*)( .*)?");
//        //parts[0] = subcommand: append/create/clear
//        //parts[1] = #keyword
//        //parts[2] = content

//        if (!result.Success)
//            return $"{State.UsernameForHeader}, syntax must be !pin create/append/clear #keyword [content]";

//        string keyword = result.Groups[2].Value.ToLower();
//        if (!keyword.Contains("#"))
//        {
//            keyword = "#" + keyword;
//        }
//        RestUserMessage UserMessage;
//        switch (result.Groups[1].Value.ToLower())
//        {

//            case "add":
//            case "append":
//                if (result.Groups.Count == 4)
//                {
//                    UserMessage = await State.GetPinnedMessage(keyword, State.UserID);
//                    await UserMessage.ModifyAsync((mp) => mp.Content = UserMessage.Content + result.Groups[3].Value);
//                    return $"{State.UsernameForHeader}, {keyword} was appended with {result.Groups[3].Value}";
//                }
//                break;
//            case "create":
//                RestUserMessage sendResult = await State.Args.Channel.SendMessageAsync($"{keyword} {(result.Groups.Count == 4 ? result.Groups[3].Value : "")}");
//                await sendResult.PinAsync();
//                return $"{State.UsernameForHeader}, {keyword} was created.";
//            case "clear":
//                UserMessage = await State.GetPinnedMessage(keyword, State.UserID);
//                await UserMessage.ModifyAsync((mp) => mp.Content = keyword);
//                return $"{State.UsernameForHeader}, {keyword} was reset.";
//            case "delete":
//                UserMessage = await State.GetPinnedMessage(keyword, State.UserID);
//                await UserMessage.DeleteAsync();
//                return $"{State.UsernameForHeader}, {keyword} was removed.";
//            default:
//                break;
//        }

//        //await MyMessage.ModifyAsync((mp) => mp.Content = newMessage);
//        return $"{State.UsernameForHeader}, syntax must be !pin create/append/clear #keyword [content]";
//    }

//    private static async Task<RestUserMessage> GetPinnedMessage(this CommandState State, string Keyword, ulong UserID)
//    {
//        IReadOnlyCollection<RestMessage> result = await State.Args.Channel.GetPinnedMessagesAsync();
//        RestMessage Message = result.FirstOrDefault((m) => m.Content.ToLower().Contains(Keyword) && m.Author.Id == State.Args.Client.CurrentUser.Id);
//        return Message as RestUserMessage;
//    }

//    //public static Action<MessageProperties> DoModifyMessage = ModifyMessage;
//    private static void ModifyMessage(MessageProperties Properties)
//    {
//        //Properties.Content = NewContent;
//        Properties.Content = "I WUZ HERE";
//    }

//    public static string SanitizeSqlParameter(this string input)
//    {
//        return input.Replace("'", "''");
//    }
//}
