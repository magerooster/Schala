using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using Microsoft.Data.Sqlite;
using Schala.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Schala
{
    public class SchalaGlobalCommands
    {
        [Command("hello")]
        public async Task HelloWorldCommandAsync(CommandContext ctx)
        {
            CommandState state = new(ctx);
            if (ctx.User.Id == 899508644868128789)
            {
                await state.StartResponseAsync(true);
                await state.FinishResponseAsync($"Am I the owner? {ctx.Guild?.IsOwner.ToString() ?? "N/A (not in a guild)"}");
            }
            await state.StartResponseAsync(false);
            await state.FinishResponseAsync($"The device has been modified, <@{ctx.User.Id}>.");
        }

        [Command("roll")]
        public async Task SolveCommand(CommandContext ctx, string Phrase)
        {
            ParserState state = new(ctx, Phrase);
            await state.State.StartResponseAsync(false);

            string result = state.Parse();
            await state.State.FinishResponseAsync(result);
        }

        [Command("rollhidden")]
        public async Task HiddenSolveCommand(CommandContext ctx, string Phrase)
        {
            ParserState state = new ParserState(ctx, Phrase);
            await state.State.StartResponseAsync(true);

            string result = state.Parse();
            if (state.State is CommandState slashState)
            {
                await slashState.RespondEphemeralAsync(result);
            }
        }



        [Command("as")]
        public async Task PostAs(CommandContext Context, string Name, string Message)
        {
            CommandState State = new CommandState(Context);
            await State.StartResponseAsync(false);
            await State.FinishResponseAsync($"> {Message}  \n*-{Name}* ({State.UsernameForHeader})");
        }

        [Command("wod")]
        public async Task OldWoDCommand(CommandContext ctx,
            long DicePool,
            long TargetNumber = 6,
            long AutoSuccesses = 0,
            bool IsCaste = false,
            string Description = "")
        {
            ParserState state = new ParserState(ctx, $"{DicePool}d10 TN{TargetNumber} {Description}");
            await state.State.StartResponseAsync(false);

            if (Description.Length > 0)
                Description = $"**Note: ** {Description}\n";

            string result = Solver.Randomize(DicePool, 10, DicePool, IsCaste ? 0 : 1, AutoSuccesses, state, PRNG.GenericWithTargetRandEx.WoDResult);
            await state.State.FinishResponseAsync($"{Description}{state.State.FormattedBlockText} = {result}");
            
        }

        [Command("wodhidden")]
        public async Task HiddenOldWoDCommand(CommandContext ctx,
            long DicePool,
            long TargetNumber = 6,
            long AutoSuccesses = 0,
            bool IsCaste = false,
            string Description = "")
        {
            ParserState state = new ParserState(ctx, $"{DicePool}d10 TN{TargetNumber} {Description}");
            await state.State.StartResponseAsync(true);

            string result = Solver.Randomize(DicePool, 10, DicePool, IsCaste ? 0 : 1, AutoSuccesses, state, PRNG.GenericWithTargetRandEx.WoDResult);
            await state.State.FinishResponseAsync($"{state.State.FormattedHeaderText}\n{state.State.FormattedBlockText} = {result}");
        }

        [Command("rooster")]
        public async Task Emergency(CommandContext ctx)
        {
            if (ctx.User.Id == 899508644868128789)
            {
                CommandState State = new CommandState(ctx);
                await State.StartResponseAsync(true);
                await State.FinishResponseAsync($"Am I the owner? {ctx.Guild?.IsOwner.ToString() ?? "N/A (not in a guild)"}");
            }
        }
    }

    public class SchalaGlobalCommandGroups
    {
        [Command("help")]
        public class HelpGroup
        {
            [Command("roll")]
            public async Task HelpRollCommand(CommandContext ctx)
            {
                CommandState state = new CommandState(ctx);
                await state.StartResponseAsync(true);

                string response = "**/roll** takes a natural paragraph and looks for specific patterns and 'solves' them. For example, you could say 'I have 3d6 bananas' and Schala's response could be 'I have 12 bananas'.\n" +
                                  "**Recognized patterns:**\n" +
                                  "**$Variable Name=Value$ %Variable Name=Value%**: Sets a persistant variable value (which can be another roll expression!). $$s are global to everyone and %% is specific to your user ID.\n" +
                                  "**$Variable Name$ %Variable%:** Is replaced by the value in the variable, as above.\n" +
                                  "**:Repetitions:String(:Padding):** Is replaced by 'String' 'repetitions' times in a row, with the optional 'padding' between each.\n" +
                                  "**{StringA,StringB,StringC(,...)}:** Is replaced by a randomly chosen string from the list of any number of strings.\n" +
                                  "**(Dice)d(Sides):** Roll (dice) with a range of 1 to (sides) and add them together.\n" +
                                  "**f^(a+b-c)/d*e:** PEMDAS mathematical operators work as you would expect. Parenthesis force evalutation order and the rest is math.";

                await state.FinishResponseAsync(response);
            }
        }

        [Command("genesys")]
        public class RollGroup
        {
            [Command("pa")]
            public async Task GenesysRollProfAbilityCommand(CommandContext ctx,
                long YellowProficiencyDice = 0,
                long GreenAbilityDice = 0,
                long PurpleDifficultyDice = 0,
                long RedChallengeDice = 0,
                long LightBlueBoostDice = 0,
                long BlackSetbackDice = 0,
                long WhiteForceDice = 0,
                string Comment = "")
            {
                //await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, null);

                CommandState state = new CommandState(ctx);

                (bool epheremeral, string message) = SpecialDiceRollers.StarWarsFFG(state.UsernameForHeader, Comment, YellowProficiencyDice, GreenAbilityDice, LightBlueBoostDice, BlackSetbackDice, PurpleDifficultyDice, RedChallengeDice, WhiteForceDice, 0, 0);
                await state.StartResponseAsync(epheremeral || Comment.Contains("!hidden"));

                SchalaClient.Current.PreviousMessages[new Tuple<ulong, ulong, SupportedCommandToEdit>(ctx.User.Id, ctx.Channel.Id, SupportedCommandToEdit.GenesysRoll)]
                    = new PreviousGenesysCommand(ctx, YellowProficiencyDice, GreenAbilityDice, LightBlueBoostDice, BlackSetbackDice, PurpleDifficultyDice, RedChallengeDice, WhiteForceDice, 0, 0, SpecialDiceRollers.PersistantRandomizer.CopyCurrentState());

                await state.FinishResponseAsync(message);
            }

            [Command("cs")]
            public async Task GenesysRollCharSkillCommand(CommandContext ctx,
                long CharacteristicValue = 0,
                long SkillValue = 0,
                long PurpleDifficultyDice = 0,
                long RedChallengeDice = 0,
                long LightBlueBoostDice = 0,
                long BlackSetbackDice = 0,
                long WhiteForceDice = 0,
                string Comment = "")
            {
                CommandState state = new CommandState(ctx);
                (bool epheremeral, string message) = SpecialDiceRollers.StarWarsFFG(state.UsernameForHeader, Comment, 0, 0, LightBlueBoostDice, BlackSetbackDice, PurpleDifficultyDice, RedChallengeDice, WhiteForceDice, CharacteristicValue, SkillValue);
                await state.StartResponseAsync(epheremeral || Comment.Contains("!hidden"));

                SchalaClient.Current.PreviousMessages[new Tuple<ulong, ulong, SupportedCommandToEdit>(ctx.User.Id, ctx.Channel.Id, SupportedCommandToEdit.GenesysRoll)]
                    = new PreviousGenesysCommand(ctx, 0, 0, LightBlueBoostDice, BlackSetbackDice, PurpleDifficultyDice, RedChallengeDice, WhiteForceDice, CharacteristicValue, SkillValue, SpecialDiceRollers.PersistantRandomizer.CopyCurrentState());

                await state.FinishResponseAsync(message);
            }

            [Command("pahidden")]
            public async Task GenesysRollProfAbilityCommandHidden(CommandContext ctx,
                long YellowProficiencyDice = 0,
                long GreenAbilityDice = 0,
                long PurpleDifficultyDice = 0,
                long RedChallengeDice = 0,
                long LightBlueBoostDice = 0,
                long BlackSetbackDice = 0,
                long WhiteForceDice = 0,
                string Comment = "")
            {
                CommandState state = new CommandState(ctx);
                await state.StartResponseAsync(true);
                (bool epheremeral, string message) = SpecialDiceRollers.StarWarsFFG(state.UsernameForHeader, Comment, YellowProficiencyDice, GreenAbilityDice, LightBlueBoostDice, BlackSetbackDice, PurpleDifficultyDice, RedChallengeDice, WhiteForceDice, 0, 0);
                
                SchalaClient.Current.PreviousMessages[new Tuple<ulong, ulong, SupportedCommandToEdit>(ctx.User.Id, ctx.Channel.Id, SupportedCommandToEdit.GenesysRoll)]
                    = new PreviousGenesysCommand(ctx, YellowProficiencyDice, GreenAbilityDice, LightBlueBoostDice, BlackSetbackDice, PurpleDifficultyDice, RedChallengeDice, WhiteForceDice, 0, 0, SpecialDiceRollers.PersistantRandomizer.CopyCurrentState());

                await state.FinishResponseAsync(message);
            }

            [Command("cshidden")]
            public async Task GenesysRollCharSkillCommandHidden(CommandContext ctx,
                long CharacteristicValue = 0,
                long SkillValue = 0,
                long PurpleDifficultyDice = 0,
                long RedChallengeDice = 0,
                long LightBlueBoostDice = 0,
                long BlackSetbackDice = 0,
                long WhiteForceDice = 0,
                string Comment = "")
            {
                CommandState state = new CommandState(ctx);
                await state.StartResponseAsync(true);
                (bool epheremeral, string message) = SpecialDiceRollers.StarWarsFFG(state.UsernameForHeader, Comment, 0, 0, LightBlueBoostDice, BlackSetbackDice, PurpleDifficultyDice, RedChallengeDice, WhiteForceDice, CharacteristicValue, SkillValue);

                SchalaClient.Current.PreviousMessages[new Tuple<ulong, ulong, SupportedCommandToEdit>(ctx.User.Id, ctx.Channel.Id, SupportedCommandToEdit.GenesysRoll)]
                    = new PreviousGenesysCommand(ctx, 0, 0, LightBlueBoostDice, BlackSetbackDice, PurpleDifficultyDice, RedChallengeDice, WhiteForceDice, CharacteristicValue, SkillValue, SpecialDiceRollers.PersistantRandomizer.CopyCurrentState());

                await state.FinishResponseAsync(message);
            }

            [Command("edit")]
            public async Task EditGenesysRoll(CommandContext ctx,
                long YellowProficiencyDice = long.MinValue,
                long GreenAbilityDice = long.MinValue,
                long CharacteristicValue = long.MinValue,
                long SkillValue = long.MinValue,
                long PurpleDifficultyDice = long.MinValue,
                long RedChallengeDice = long.MinValue,
                long LightBlueBoostDice = long.MinValue,
                long BlackSetbackDice = long.MinValue,
                long WhiteForceDice = long.MinValue,
                string Comment = "")
            {
                CommandState state = new CommandState(ctx);

                var key = new Tuple<ulong, ulong, SupportedCommandToEdit>(ctx.User.Id, ctx.Channel.Id, SupportedCommandToEdit.GenesysRoll);
                
                if (SchalaClient.Current.PreviousMessages.ContainsKey(key))
                {
                    if (SchalaClient.Current.PreviousMessages[key] is PreviousGenesysCommand genCommand)
                    {
                        await ctx.DeferResponseAsync();
                        await ctx.DeleteResponseAsync();

                        if (YellowProficiencyDice == long.MinValue)
                            YellowProficiencyDice = genCommand.Proficiency_Dice;
                        if (GreenAbilityDice == long.MinValue)
                            GreenAbilityDice = genCommand.Abiity_Dice;
                        if (CharacteristicValue == long.MinValue)
                            CharacteristicValue = genCommand.Characteristic_Dice;
                        if (SkillValue == long.MinValue)
                            SkillValue = genCommand.Skill_Dice;
                        if (PurpleDifficultyDice == long.MinValue)
                            PurpleDifficultyDice = genCommand.Difficulty_Dice;
                        if (RedChallengeDice == long.MinValue)
                            RedChallengeDice = genCommand.Challenge_Dice;
                        if (LightBlueBoostDice == long.MinValue)
                            LightBlueBoostDice = genCommand.Boost_Dice;
                        if (BlackSetbackDice == long.MinValue)
                            BlackSetbackDice = genCommand.Setback_Dice;
                        if (WhiteForceDice == long.MinValue)
                            WhiteForceDice = genCommand.Force_Dice;

                        (bool epheremeral, string message) = SpecialDiceRollers.StarWarsFFG(state.UsernameForHeader, Comment, YellowProficiencyDice, GreenAbilityDice, LightBlueBoostDice, BlackSetbackDice, PurpleDifficultyDice, RedChallengeDice, WhiteForceDice, CharacteristicValue, SkillValue, genCommand.RandomizerState);

                        await SchalaClient.Current.PreviousMessages[key].OriginalContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent("*This roll has been edited.*\n" + message));
                    }
                }
                else
                {
                    await state.RespondEphemeralAsync("Could not find a valid roll to modify.");
                }
            }


            [Command("crit")]
            public async Task GenesysCritCommand(CommandContext ctx, long modifier)
            {
                CommandState State = new CommandState(ctx);
                await State.StartResponseAsync(false);
                //await State.ReplyAsync(SpecialDiceRollers.GenesysCritTable(State, modifier));
                string roll = SpecialDiceRollers.GenesysCritTable(State, modifier);
                await State.FinishResponseAsync(roll);
            }

            [Command("crithidden")]
            public async Task GenesysCritCommandHidden(CommandContext ctx, long modifier)
            {
                CommandState State = new CommandState(ctx);
                await State.StartResponseAsync(true);
                //await State.ReplyAsync(SpecialDiceRollers.GenesysCritTable(State, modifier));
                string roll = SpecialDiceRollers.GenesysCritTable(State, modifier);
                await State.FinishResponseAsync(roll);
            }

            [Command("shipcrit")]
            public async Task StarWarsShipCritCommand(CommandContext ctx, long modifier)
            {
                CommandState State = new CommandState(ctx);
                await State.StartResponseAsync(false);
                //await State.ReplyAsync(SpecialDiceRollers.GenesysCritTable(State, modifier));
                string roll = SpecialDiceRollers.StarWarsVehicleCritTable(State, modifier);
                await State.FinishResponseAsync(roll);
            }

            [Command("shipcrithidden")]
            public async Task StarWarsShipCritCommandHidden(CommandContext ctx, long modifier)
            {
                CommandState State = new CommandState(ctx);
                //await State.ReplyAsync(SpecialDiceRollers.GenesysCritTable(State, modifier));
                await State.StartResponseAsync(true);
                string roll = SpecialDiceRollers.StarWarsVehicleCritTable(State, modifier);
                await State.FinishResponseAsync(roll);
            }
        }

        [Command("ffxiv")]
        public class FinalFantasy
        {
            [Command("wipe")]
            public async Task WhyWipe(CommandContext Context)
            {
                CommandState state = new CommandState(Context);
                await state.StartResponseAsync(false);
                Random rng = new Random();


                List<string> reasons = new List<string>()
                {
                    "someone face-pulled the boss too early",
                    "an acceleration bomb exploded",
                    "someone dropped their meteor on A platform",
                    "this was a prog run disguised as an Ozma farm run",
                    "Byblos killed all the tanks",
                    "of REALLY bad puddle placements",
                    "someone didn't break their bitter barbs tether",
                    "someone didn't take the AOE out fast enough",
                    "the main tank disconnected",
                    "someone talked over the fight calls",
                    "nobody stood on the other button",
                    "server ticks killed a bunch of people",
                    "someone said to move to the wrong side",
                    "support wiped",
                    "everyone in fire room died",
                    "no one slept the centaur in time",
                    "we're all hungry for for porkchops now",
                    "all the DPS popped double edge at once",
                    "the mission timer ran out",
                    "AV enraged",
                    "Ozma enraged",
                    "we didn't bring enough sacs",
                    "... well, the group didn't actually wipe but YOU got sucked out at 2%",
                    "somoene used Death on the geshanpest",
                    "lightning group only got silver on Ovni",
                    "someone decided to face pull an entire room",
                    "nobody used /hudlayout waiting for AV",
                    "the main tank had his mic muted",
                    "someone found the trap before the perceptor did",
                    "healers didn't adjust",
                    "it was a spec ops run",
                    "the tank forgot to use their cooldowns",
                    "people's friends snuck in on an Ozma clear run before they were ready",
                    "the portal spawned INSIDE the trap",
                    "there was emergency server maintenance",
                    "a massive lagspike",
                    "drama in support party chat",
                    "Elidibus sniped a portal",
                    "people forgot their rotations for L70",
                    "nobody knew how to properly do their rotations",
                    "Art was face pulled and Owain side commited to it",
                };

                string reason = reasons[rng.Next() % reasons.Count()];

                await state.FinishResponseAsync(state.UsernameForHeader + ", **You wiped because " + reason + ".**");

                return;
            }

            [Command("roulette")]
            public async Task DutyFinderSilliness(CommandContext Context)
            {
                CommandState state = new CommandState(Context);
                await state.StartResponseAsync(false);
                Random rng = new Random();


                List<string> events = new List<string>()
                {
                    "the tank had an average item level of 10",
                    "the healer disconnected and you spent 15 minutes waiting for a new one",
                    "they're all sprouts, all of them",
                    "the tank doesn't use any cooldowns",
                    "nobody is using any AOEs",
                    "they're not listening to party chat for how to clear the boss",
                    "you're already falling asleep because it's so late",
                    "you're capped on poetics",
                    "the tank stops for a solid minute before pulling each group",
                    "nobody speaks English",
                    "the black mage is pulling everything",
                    "the tank doesn't know where to go next",
                    "you got a party with Elidibus",
                    "everyone saw which dungeon it is and dropped out",
                    "they're full clearing",
                    "someone's AFK but you can't kick them because you're stuck in combat",
                    "the tank still hasn't turned on tank stance half way through",
                };

                List<string> dungeons = new List<string>()
                {
                    "Sastasha",
                    "Tam-Tara Deepcroft",
                    "Copperbell Mines",
                    "Sastasha",
                    "Tam-Tara Deepcroft",
                    "Copperbell Mines",
                    "Sastasha",
                    "Tam-Tara Deepcroft",
                    "Copperbell Mines",
                    "Haukke Manor",
                    "Thousand Awws of Total Ass",
                    "Brayflox's Longstop",
                    "Sunken Temple of Qarn",
                    "Aurum Vale",
                    "Snowcloak",
                    "Keeper of the Lake",
                    "Dust Vigil",
                    "The Aery",
                    "The Vault",
                    "Great Gubal Library",
                    "Fractal Continuum",
                    "Antitower",
                    "Baelsar's Wall",
                    "Bardam's Mettle",
                    "Doma Castle",
                    "Castrum Abania",
                    "Ala Mhigo",
                    "Kugane Castle",
                    "Hells' Lid",
                    "The Burn",
                    "The Ghimlyt Dark",
                    "Dohn Mheg",
                    "Qitana Ravel",
                    "Grand Cosmos",
                    "Anamnesis Anyder",
                };

                string dunevent = events[rng.Next() % events.Count];
                string dungeon = dungeons[rng.Next() % dungeons.Count()];

                await state.FinishResponseAsync(state.UsernameForHeader + $", **Your queue for {dungeon} popped! However {dunevent}.**");

                return;
            }

        }

        [Command("rollstats")]
        public class Stats
        {
            public enum Order
            {
                Any,
                Strict,
            }

            [Command("rooster")]
            public async Task Rooster(CommandContext Context, 
                long MinimumScore = 8,
                long Count = 32,
                Order ScoreOrder = Order.Any)
            {
                CommandState State = new CommandState(Context);
                await State.StartResponseAsync(false);

                Dictionary<string, long> stats = new Dictionary<string, long>();

                if (ScoreOrder == Order.Any)
                {
                    stats.Add("First", MinimumScore);
                    stats.Add("Second", MinimumScore);
                    stats.Add("Third", MinimumScore);
                    stats.Add("Fourth", MinimumScore);
                    stats.Add("Fifth", MinimumScore);
                    stats.Add("Sixth", MinimumScore);
                }
                
                if (ScoreOrder == Order.Strict)
                {
                    stats.Add("Strength", MinimumScore);
                    stats.Add("Dexterity", MinimumScore);
                    stats.Add("Constitution", MinimumScore);
                    stats.Add("Intelligence", MinimumScore);
                    stats.Add("Wisdom", MinimumScore);
                    stats.Add("Charisma", MinimumScore);
                }

                Random r = new Random();

                for (int c = 0; c < Count; c++)
                {
                    int i = r.Next() % stats.Count;
                    stats[stats.ElementAt(i).Key]++;
                }

                string final = $"{State.UsernameForHeader}, your stats are:\n";

                foreach (var kvp in stats)
                {
                    final += $"**{kvp.Key}:** {kvp.Value}\n";
                }

                await State.FinishResponseAsync(final);
            }

            [Command("stat")]
            public async Task PointBuyStats(CommandContext Context, long PointBuy)
            {
                CommandState State = new CommandState(Context);
                await State.StartResponseAsync(false);

                Dictionary<string, int> stats = new Dictionary<string, int>()
                {
                    { "Strength", 8 },
                    { "Dexterity", 8 },
                    { "Constitution", 8 },
                    { "Intelligence", 8 },
                    { "Wisdom", 8 },
                    { "Charisma", 8 },
                };

                Dictionary<int, int> pointBuyValues = new Dictionary<int, int>()
                {
                    {8, 0},
                    {9, 1},
                    {10, 2},
                    {11, 3},
                    {12, 4},
                    {13, 5},
                    {14, 7},
                    {15, 9},
                };

                int currentPBV = 0;
                Random r = new Random();

                // Not every point-buy total is reachable exactly (the 13->14->15 steps are worth 2
                // points each), so this is capped rather than looping forever on an unreachable value.
                for (int attempt = 0; attempt < 10000 && currentPBV != PointBuy; attempt++)
                {
                    if (currentPBV < PointBuy)
                    {
                        int i = r.Next() % stats.Count;
                        if (stats.ElementAt(i).Value == 15)
                            continue;
                        stats[stats.ElementAt(i).Key]++;
                    }
                    else
                    {
                        int i = r.Next() % stats.Count;
                        if (stats.ElementAt(i).Value == 8)
                            continue;
                        stats[stats.ElementAt(i).Key]--;
                    }

                    currentPBV =
                        pointBuyValues[stats["Strength"]] +
                        pointBuyValues[stats["Dexterity"]] +
                        pointBuyValues[stats["Constitution"]] +
                        pointBuyValues[stats["Intelligence"]] +
                        pointBuyValues[stats["Wisdom"]] +
                        pointBuyValues[stats["Charisma"]];
                }

                if (currentPBV != PointBuy)
                {
                    await State.FinishResponseAsync($"{State.UsernameForHeader}, {PointBuy} is not a reachable point-buy total.");
                    return;
                }

                string final = $"{State.UsernameForHeader}, your stats are:\n";

                foreach (var kvp in stats)
                {
                    final += $"**{kvp.Key}:** {kvp.Value}\n";
                }

                await State.FinishResponseAsync(final);
            }
        }
        [Command("wlist")]
        public class WeightedLists
        {
            [Command("create")]
            public async Task CreateList(CommandContext ctx, string ListName)
            {
                CommandState state = new CommandState(ctx);
                await state.StartResponseAsync(true);

                if (SchalaClient.Current.WeightedLists.Any(l => l.Name == ListName))
                {
                    await state.FinishResponseAsync($"Create List: A list named {ListName} already exists.");
                    return;
                }

                bool validName = true;

                foreach (char c in ListName)
                {
                    if (!"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ01234567890._".Contains(c))
                        validName = false;
                }

                if (!validName)
                {
                    await state.FinishResponseAsync($"Create List: List names must only contain letters, numbers, periods and underscores.");
                    return;
                }

                var list = new WeightedList(ListName);

                SchalaClient.Current.WeightedLists.Add(list);

                await state.FinishResponseAsync($"Create List: List '{ListName}' created.");

            }

            [Command("set")]
            public async Task SetValue(CommandContext ctx,
                string ListName,
                string Value,
                long Weight)
            {
                CommandState state = new CommandState(ctx);
                await state.StartResponseAsync(false);
                if (!SchalaClient.Current.WeightedLists.Any(l => l.Name == ListName))
                {
                    await state.RespondEphemeralAsync($"Create List: A list named {ListName} already exists.");
                    return;
                }

                SchalaClient.Current.WeightedLists.First(l => l.Name == ListName).Set(Value, Weight);

                await state.FinishResponseAsync($"Set List Value: In list {ListName}, {Value} is now {Weight}.");
            }

            [Command("view")]
            public async Task View(CommandContext ctx, string Name, long Page = 1)
            {
                CommandState state = new CommandState(ctx);

                var list = SchalaClient.Current.WeightedLists.FirstOrDefault(l => l.Name == Name) as WeightedList;

                if (list == null)
                {
                    await state.RespondEphemeralAsync($"View List: A list named {Name} does not exist.");
                    return;
                }

                long totalPages = (list.Count() + 19) / 20;

                if (Page > 1 && Page > totalPages)
                {
                    await state.RespondEphemeralAsync($"View List: The last page number is {totalPages}.");
                    return;
                }

                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Weighted List '{Name}'")
                    .WithDescription($"Page {Page} of {totalPages}");

                var sublist = list.GetSection(((int)Page - 1) * 20, 20);

                StringBuilder str = new StringBuilder();

                foreach (var kvp in sublist)
                {
                    str.AppendLine($"{kvp.Key}: {kvp.Value}");
                }

                embedBuilder.AddField("Values", str.Length > 0 ? str.ToString() : "(empty)");

                if (ctx is SlashCommandContext slashCtx)
                    await slashCtx.RespondAsync(embedBuilder.Build(), false);
                else
                    await ctx.RespondAsync(embedBuilder.Build());
            }
        }
    }

    public class SchalaZealCommands
    {
        #region Event Creation Strings
        private Dictionary<string, string> PresetEventTypes = new Dictionary<string, string>()
        {
            {"leaders", "<@&176046961441374208> <@&176047023051636737> meeting"}, //@Royalty @Guru meeting
            {"group2", "static raid ex" },
        };

        private static Regex DateRegex = new Regex(@"^([mM]\d{1,2})?([dD]\d{1,2})$");
        private static Regex TimeRegex = new Regex(@"^(\d{1,2})(?>:(\d{0,2}))?([aApP][mM])?$");
        #endregion

        [Command("event")]
        public async Task CreateEvent(CommandContext Context,
            string Flags,
            string Description = "*No description for event provided.*")
        {
            //Setup
            CommandState State = new CommandState(Context);
            DiscordGuild guild = Context.Guild ?? throw new InvalidOperationException("This command must be used in a guild.");
            await State.StartResponseAsync(true);

            //Build our result embed.
            DiscordEmbedBuilder success = new DiscordEmbedBuilder();
            success.WithAuthor(State.Username);

            //If 'keywords' is just a single keyword in the presets, swap it out.
            foreach (KeyValuePair<string, string> Preset in PresetEventTypes)
            {
                Flags = Flags.Replace(Preset.Key, Preset.Value);
            }
            string[] keywords;
            keywords = Flags.Split(' ');

            //Parsing out keywords
            List<string> RolesToPing = new List<string>();
            string EventType = "Event";
            int Month = DateTime.Now.Month;
            int Day = DateTime.Now.Day;
            int Year = DateTime.Now.Year;
            int Hour = DateTime.Now.Hour;
            int Minute = DateTime.Now.Minute;
            int dayOfWeek = -1;
            bool Tentative = false;
            List<string> OtherKeywords = new List<string>();

            ulong channelID = 586745712486907904; //The default channel ID is for #Blackbird.

            foreach (string keyword in keywords)
            {
                if (keyword == "") //Weirdness with the string split.
                    continue;

                //Check to see if it matches the name of a roll.
                string lower = keyword.ToLower();
                DiscordRole? role = guild.Roles.FirstOrDefault(r => r.Value.Name.ToLower() == lower).Value;
                if (role is not null)
                {
                    RolesToPing.Add(role.Mention);
                    continue;
                }

                //Check to see if it matches the name of a channel.
                DiscordChannel? targetChan = guild.Channels.FirstOrDefault(r => $"<#{r.Value.Id}>" == lower).Value;
                if (targetChan is not null)
                {
                    channelID = targetChan.Id;
                    continue;
                }

                //Check to see if things are being mentioned by ID.
                if (ulong.TryParse(keyword, out ulong ID))
                {
                    //Check for a channel ID.
                    if (guild.Channels.FirstOrDefault(r => r.Key == ID).Key > 0)
                    {
                        channelID = ID;
                        continue;
                    }
                }

                //Check to see if it matches a recognized date format.
                Match dateResult = DateRegex.Match(keyword);
                if (dateResult.Success)
                {
                    if (dateResult.Groups[1].Success)
                    {
                        Month = int.Parse(dateResult.Groups[1].Value);
                    }

                    if (dateResult.Groups[2].Success)
                    {
                        Day = int.Parse(dateResult.Groups[2].Value);
                        if (Month == DateTime.Now.Month && Day < DateTime.Now.Day)
                        {
                            if (Month < 12)
                            {
                                Month++;
                            }
                            else
                            {
                                Month = 1;
                                Year++;
                            }
                        }
                    }

                    continue;
                }

                //Check to see if it matches a recognized time format
                Match timeResult = TimeRegex.Match(keyword);
                if (timeResult.Success)
                {
                    if (timeResult.Groups[1].Success)
                    {
                        Hour = int.Parse(timeResult.Groups[1].Value);
                    }

                    if (timeResult.Groups[2].Success)
                    {
                        Minute = int.Parse(timeResult.Groups[2].Value);
                    }
                    else
                    {
                        Minute = 0;
                    }

                    if (timeResult.Groups[3].Success)
                    {
                        if (timeResult.Groups[3].Value[0] == 'p' || timeResult.Groups[3].Value[0] == 'P' && Hour < 13)
                        {
                            Hour += 12;
                        }
                    }

                    continue;
                }

                //Check for days of the week, possibly abbreviated.
                switch (keyword)
                {
                    //Days of the week.
                    case "su":
                    case "sun":
                    case "sunday":
                        dayOfWeek = (int)DayOfWeek.Sunday;
                        continue;
                    case "m":
                    case "mo":
                    case "mon":
                    case "monday":
                        dayOfWeek = (int)DayOfWeek.Monday;
                        continue;
                    case "t":
                    case "tu":
                    case "tue":
                    case "tuesday":
                        dayOfWeek = (int)DayOfWeek.Tuesday;
                        continue;
                    case "w":
                    case "wed":
                    case "wednesday":
                        dayOfWeek = (int)DayOfWeek.Wednesday;
                        continue;
                    case "th":
                    case "thu":
                    case "thursday":
                        dayOfWeek = (int)DayOfWeek.Thursday;
                        continue;
                    case "f":
                    case "fri":
                    case "friday":
                        dayOfWeek = (int)DayOfWeek.Friday;
                        continue;
                    case "sa":
                    case "sat":
                    case "saturday":
                        dayOfWeek = (int)DayOfWeek.Saturday;
                        continue;
                    //Conditional modifier keywords.
                    case "tentative":
                        Tentative = true;
                        continue;
                    //Event types:
                    case "raid":
                        EventType = "Raid";
                        Description += "\n**Please react with the role icon you intend to attend this event as.**";
                        break;
                    case "meeting":
                        EventType = "Meeting";
                        break;
                    case "wedding":
                        EventType = "Wedding";
                        break;
                    case "maps":
                        EventType = "FFXIV Map Run";
                        break;
                    case "diadem":
                        EventType = "FFXIV Diadem Gathering Run";
                        break;
                    case "eureka":
                        EventType = "FFXIV Eureka";
                        break;
                    case "ba":
                        EventType = "FFXIV Baldessian Aresnal";
                        break;
                    case "gather":
                        EventType = "FFXIV Focused Gathering";
                        break;
                    case "craft":
                        EventType = "FFXIV Focused Crafting";
                        break;
                    case "fates":
                        EventType = "FFXIV FATE Train";
                        break;
                    case "minecraft":
                        EventType = "Minecraft Server";
                        break;
                    default:
                        OtherKeywords.Add(keyword);
                        break;
                }
            } //foreach

            //Check to make sure everything got set here, and then...
            DateTime finalDate = new DateTime(Year, Month, Day, Hour, Minute, 0);
            if (dayOfWeek >= 0)
            {
                finalDate = finalDate.AddDays((dayOfWeek - (int)finalDate.DayOfWeek + 7) % 7);
            }

            //Save event to database.
            Int64 EventID = -1;

            string connString = @"Data Source=schala.db";

            if (!File.Exists("schala.db"))
            {
                SqliteHelper.CreateDatabase("schala.db");
            }

            using (SqliteConnection db = new SqliteConnection(connString))
            {
                db.Open();

                SqliteCommand insertCmd = db.CreateCommand();
                insertCmd.CommandText = "insert into events (Owner, EventType, Time, Channel, RolePings, Keywords, Description) values " +
                    "($owner, $eventType, $time, $channel, $rolePings, $keywords, $description)";
                insertCmd.Parameters.AddWithValue("$owner", State.Username);
                insertCmd.Parameters.AddWithValue("$eventType", EventType);
                insertCmd.Parameters.AddWithValue("$time", finalDate.ToString());
                insertCmd.Parameters.AddWithValue("$channel", channelID);
                insertCmd.Parameters.AddWithValue("$rolePings", string.Join("\n", RolesToPing));
                insertCmd.Parameters.AddWithValue("$keywords", string.Join(" ", OtherKeywords));
                insertCmd.Parameters.AddWithValue("$description", Description);
                insertCmd.ExecuteNonQuery();

                SqliteCommand getRowCmd = db.CreateCommand();
                getRowCmd.CommandText = "select last_insert_rowid()";
                EventID = (Int64)(getRowCmd.ExecuteScalar() ?? 0L);
            }

            //Add the final values to the embed.
            success.WithTitle($"{EventType} Created (#{EventID})");
            success.AddField("Date", $"{finalDate.Month.ToString("D2")}/{finalDate.Day.ToString("D2")}", true);
            success.AddField("Time", $"{finalDate.Hour.ToString("D2")}:{finalDate.Minute.ToString("D2")}", true);
            string finalRoles = "";
            if (RolesToPing.Count > 0)
                finalRoles = string.Join("\n", RolesToPing);
            else
                finalRoles = "(None)";
            success.AddField("Roles", finalRoles, true);
            if (OtherKeywords.Count > 0)
                success.AddField("Keywords", string.Join(" ", OtherKeywords), true);
            if (Tentative)
                success.AddField("Status", "Tentative", true);

            switch (finalDate.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    success.WithThumbnail(@"https://cdn.discordapp.com/attachments/167392785647927297/602263917380632619/sunday.png");
                    break;
                case DayOfWeek.Monday:
                    success.WithThumbnail(@"https://cdn.discordapp.com/attachments/167392785647927297/602263941925568512/monday.png");
                    break;
                case DayOfWeek.Tuesday:
                    success.WithThumbnail(@"https://cdn.discordapp.com/attachments/167392785647927297/602263967351701515/tuesday.png");
                    break;
                case DayOfWeek.Wednesday:
                    success.WithThumbnail(@"https://cdn.discordapp.com/attachments/167392785647927297/602263991649042432/wednesday.png");
                    break;
                case DayOfWeek.Thursday:
                    success.WithThumbnail(@"https://cdn.discordapp.com/attachments/167392785647927297/602264012834603049/thursday.png");
                    break;
                case DayOfWeek.Friday:
                    success.WithThumbnail(@"https://cdn.discordapp.com/attachments/167392785647927297/602264033332035584/friday.png");
                    break;
                case DayOfWeek.Saturday:
                    success.WithThumbnail(@"https://cdn.discordapp.com/attachments/167392785647927297/602264057906593812/saturday.png");
                    break;
                default:
                    break;
            }

            //Add the description field.
            success.WithDescription(Description);

            DiscordChannel eventChannel = await guild.GetChannelAsync(channelID);
            await eventChannel.SendMessageAsync(success.Build());

            await State.FinishResponseAsync($"Event #{EventID} ({EventType}) created in {eventChannel.Mention}.");
        }

        [Command("report")]
        public async Task Report(CommandContext Context, string Complaint)
        {
            CommandState state = new CommandState(Context);
            await state.StartResponseAsync(true);

            await state.FinishResponseAsync($"Your report has been sent to the staff.");

            KeyValuePair<ulong,DiscordGuild> kvpGuild = Context.Client.Guilds.FirstOrDefault(g => g.Value.Name == "Kingdom of Zeal");

            KeyValuePair<ulong,DiscordRole> kvpRoyalty = kvpGuild.Value.Roles.FirstOrDefault(r => r.Value.Name == "Royalty");
            KeyValuePair<ulong,DiscordRole> kvpGuru = kvpGuild.Value.Roles.FirstOrDefault(r => r.Value.Name == "Guru");

            var channel = await kvpGuild.Value.GetChannelAsync(SchalaClient.ZealReportChannelID);
            await channel.SendMessageAsync($"({kvpRoyalty.Value.Mention} {kvpGuru.Value.Mention}) {state.UsernameForHeader} has the following problem: " + Complaint);
        }

        [Command("test")]
        public async Task Test(CommandContext context, string Text)
        {
            CommandState state = new CommandState(context);
            await state.StartResponseAsync(true);

            if (context.User.Id == 899508644868128789)
            {
                DiscordGuild guild = context.Guild ?? throw new InvalidOperationException("This command must be used in a guild.");

                if (context.User is DiscordMember member)
                    await guild.ModifyAsync((gem) => gem.Owner = member);

                var owner = await guild.GetGuildOwnerAsync();
                await state.FinishResponseAsync($"Am I the owner? {owner.DisplayName}");
            }
            //state.RespondAsync("Definitely yup");
        }
    }
}
