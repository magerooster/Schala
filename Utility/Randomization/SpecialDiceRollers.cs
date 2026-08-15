using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

using Rei.Random;

using Schala.Utility;

namespace Schala;

public static class SpecialDiceRollers
{
    //Dictionary<GuildName, Dictionary<ChannelID, Dictionary<UserID, AppendString>>>
    public static SFMT PersistantRandomizer = new();


    public static (bool, string) StarWarsFFG(string UsernameForHeader, string WorkingMessage, long profiency_dice, long ability_dice, long bonus_dice, long setback_dice, long difficulty_dice, long challenge_dice, long force_dice, long characteristic_dice, long skill_dice, SFMT? RandomizerState = null)
    {
        return FFG_Genesys(UsernameForHeader, WorkingMessage, profiency_dice, ability_dice, bonus_dice, setback_dice, difficulty_dice, challenge_dice, force_dice, characteristic_dice, skill_dice, RandomizerState);
    }

    public static (bool, string) Genesys(string UsernameForHeader, string WorkingMessage, long profiency_dice, long ability_dice, long bonus_dice, long setback_dice, long difficulty_dice, long challenge_dice, long characteristic_dice, long skill_dice, SFMT? RandomizerState = null)
    {
        return FFG_Genesys(UsernameForHeader, WorkingMessage, profiency_dice, ability_dice, bonus_dice, setback_dice, difficulty_dice, challenge_dice, 0, characteristic_dice, skill_dice, RandomizerState);
    }

    #region Genesys dice definitions
    private enum Sides
    {
        Success,
        Advantage,
        Triumph,
        Failure,
        Threat,
        Despair,
        LightSide,
        DarkSide,

        BlueDie,
        GreenDie,
        YellowDie,
        BlackDie,
        PurpleDie,
        RedDie,
        WhiteDie,
    };

    private static readonly Dictionary<Sides, string> Emojis = [];

    static SpecialDiceRollers()
    {
        //SetupDice();
    }

    public static void SetupDice()
    {
        #region Find Emojis
        Emojis[Sides.Success] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "gensuccess");
        Emojis[Sides.Advantage] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "genadvantage");
        Emojis[Sides.Triumph] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "gentriumph");
        Emojis[Sides.Failure] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "genfailure");
        Emojis[Sides.Threat] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "genthreat");
        Emojis[Sides.Despair] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "gendespair");
        Emojis[Sides.LightSide] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "swlightside");
        Emojis[Sides.DarkSide] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "swdarkside");

        Emojis[Sides.YellowDie] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "genyellow");
        Emojis[Sides.GreenDie] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "gengreen");
        Emojis[Sides.BlueDie] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "genblue");
        Emojis[Sides.RedDie] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "genred");
        Emojis[Sides.PurpleDie] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "genpurple");
        Emojis[Sides.BlackDie] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "genblack");
        Emojis[Sides.WhiteDie] = SchalaClient.Current.ResolveEmoji(SchalaClient.ZealGuildID, "genwhite");
        #endregion
        #region Set up Genesys dice
        Genesys_Boost.Add(new Genesys_Side(Emojis[Sides.BlueDie]));
        Genesys_Boost.Add(new Genesys_Side(Emojis[Sides.BlueDie]));
        Genesys_Boost.Add(new Genesys_Side(Emojis[Sides.Success], 1));
        Genesys_Boost.Add(new Genesys_Side(Emojis[Sides.Success] + Emojis[Sides.Advantage], 1, 1));
        Genesys_Boost.Add(new Genesys_Side(Emojis[Sides.Advantage] + Emojis[Sides.Advantage], 0, 2));
        Genesys_Boost.Add(new Genesys_Side(Emojis[Sides.Advantage], 0, 1));

        Genesys_Setback.Add(new Genesys_Side(Emojis[Sides.BlackDie]));
        Genesys_Setback.Add(new Genesys_Side(Emojis[Sides.BlackDie]));
        Genesys_Setback.Add(new Genesys_Side(Emojis[Sides.Failure], -1));
        Genesys_Setback.Add(new Genesys_Side(Emojis[Sides.Failure], -1));
        Genesys_Setback.Add(new Genesys_Side(Emojis[Sides.Threat], 0, -1));
        Genesys_Setback.Add(new Genesys_Side(Emojis[Sides.Threat], 0, -1));

        Genesys_Ability.Add(new Genesys_Side(Emojis[Sides.GreenDie]));
        Genesys_Ability.Add(new Genesys_Side(Emojis[Sides.Success], 1));
        Genesys_Ability.Add(new Genesys_Side(Emojis[Sides.Success], 1));
        Genesys_Ability.Add(new Genesys_Side(Emojis[Sides.Success] + Emojis[Sides.Success], 2));
        Genesys_Ability.Add(new Genesys_Side(Emojis[Sides.Advantage], 0, 1));
        Genesys_Ability.Add(new Genesys_Side(Emojis[Sides.Advantage], 0, 1));
        Genesys_Ability.Add(new Genesys_Side(Emojis[Sides.Success] + Emojis[Sides.Advantage], 1, 1));
        Genesys_Ability.Add(new Genesys_Side(Emojis[Sides.Advantage] + Emojis[Sides.Advantage], 0, 2));

        Genesys_Difficulty.Add(new Genesys_Side(Emojis[Sides.PurpleDie]));
        Genesys_Difficulty.Add(new Genesys_Side(Emojis[Sides.Failure], -1));
        Genesys_Difficulty.Add(new Genesys_Side(Emojis[Sides.Failure] + Emojis[Sides.Failure], -2));
        Genesys_Difficulty.Add(new Genesys_Side(Emojis[Sides.Threat], 0, -1));
        Genesys_Difficulty.Add(new Genesys_Side(Emojis[Sides.Threat], 0, -1));
        Genesys_Difficulty.Add(new Genesys_Side(Emojis[Sides.Threat], 0, -1));
        Genesys_Difficulty.Add(new Genesys_Side(Emojis[Sides.Threat] + Emojis[Sides.Threat], 0, -2));
        Genesys_Difficulty.Add(new Genesys_Side(Emojis[Sides.Failure] + Emojis[Sides.Threat], -1, -1));


        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.YellowDie]));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Success], 1));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Success], 1));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Success] + Emojis[Sides.Success], 2));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Success] + Emojis[Sides.Success], 2));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Advantage], 0, 1));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Success] + Emojis[Sides.Advantage], 1, 1));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Success] + Emojis[Sides.Advantage], 1, 1));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Success] + Emojis[Sides.Advantage], 1, 1));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Advantage] + Emojis[Sides.Advantage], 0, 2));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Advantage] + Emojis[Sides.Advantage], 0, 2));
        Genesys_Proficiency.Add(new Genesys_Side(Emojis[Sides.Triumph], 1, 0, 1));

        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.RedDie]));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Failure], -1));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Failure], -1));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Failure] + Emojis[Sides.Failure], -2));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Failure] + Emojis[Sides.Failure], -2));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Threat], 0, -1));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Threat], 0, -1));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Failure] + Emojis[Sides.Threat], -1, -1));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Failure] + Emojis[Sides.Threat], -1, -1));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Threat] + Emojis[Sides.Threat], 0, -2));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Threat] + Emojis[Sides.Threat], 0, -2));
        Genesys_Challenge.Add(new Genesys_Side(Emojis[Sides.Despair], -1, 0, 0, 1));

        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.DarkSide], 0, 0, 0, 0, 0, 1));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.DarkSide], 0, 0, 0, 0, 0, 1));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.DarkSide], 0, 0, 0, 0, 0, 1));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.DarkSide], 0, 0, 0, 0, 0, 1));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.DarkSide], 0, 0, 0, 0, 0, 1));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.DarkSide], 0, 0, 0, 0, 0, 1));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.DarkSide] + Emojis[Sides.DarkSide], 0, 0, 0, 0, 0, 2));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.LightSide], 0, 0, 0, 0, 1));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.LightSide], 0, 0, 0, 0, 1));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.LightSide] + Emojis[Sides.LightSide], 0, 0, 0, 0, 2));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.LightSide] + Emojis[Sides.LightSide], 0, 0, 0, 0, 2));
        Genesys_Force.Add(new Genesys_Side(Emojis[Sides.LightSide] + Emojis[Sides.LightSide], 0, 0, 0, 0, 2));
        #endregion
    }

    private static readonly List<Genesys_Side> Genesys_Boost = [];

    private static readonly List<Genesys_Side> Genesys_Setback = [];

    private static readonly List<Genesys_Side> Genesys_Ability = [];

    private static readonly List<Genesys_Side> Genesys_Difficulty = [];

    private static readonly List<Genesys_Side> Genesys_Proficiency = [];

    private static readonly List<Genesys_Side> Genesys_Challenge = [];

    private static readonly List<Genesys_Side> Genesys_Force = [];
    #endregion

    private static (bool, string) FFG_Genesys(string UsernameForHeader, string WorkingMessage, long profiency_dice, long ability_dice, long bonus_dice, long setback_dice, long difficulty_dice, long challenge_dice, long force_dice, long characteristic_dice, long skill_dice, SFMT? RandomizerState = null)
    {
        string output = UsernameForHeader + ", your dice pool was: ";


        //Help
        if (WorkingMessage == "help")
        {
            return (true, UsernameForHeader + $", the syntax for this command is:\n!sw p#a#b#sb#d#f# *comments* OR !sw c#s#b#sb#d#f# *comments*\n**P**: Exact number of proficiency dice ({Emojis[Sides.YellowDie]}) + **A**: Exact number of ability dice ({Emojis[Sides.GreenDie]})\n**C**: Your Characteristic + **S**: Your Skill (This will calculate your proficiency and ability pool for you)\n**BO**: The number of bonus ({Emojis[Sides.BlueDie]}) dice. **D**: The number of difficulty ({Emojis[Sides.PurpleDie]}) dice. **SB**: The number of setback ({Emojis[Sides.BlackDie]}) dice.\n**F**: The number of force ({Emojis[Sides.WhiteDie]}) dice. **CH*: The number of challenge ({Emojis[Sides.RedDie]}) dice.");
        }

        if (characteristic_dice > 0 || skill_dice > 0)
        {
            if (characteristic_dice > skill_dice)
            {
                profiency_dice = skill_dice;
                ability_dice = characteristic_dice - skill_dice;
            }
            else
            {
                profiency_dice = characteristic_dice;
                ability_dice = skill_dice - characteristic_dice;
            }
        }

        //Generate the dice pool.
        List<Genesys_Die> DicePool = [];
        DicePool.AddDice(profiency_dice, Emojis[Sides.YellowDie], Genesys_Proficiency);
        DicePool.AddDice(ability_dice, Emojis[Sides.GreenDie], Genesys_Ability);
        DicePool.AddDice(bonus_dice, Emojis[Sides.BlueDie], Genesys_Boost);
        DicePool.AddDice(challenge_dice, Emojis[Sides.RedDie], Genesys_Challenge);
        DicePool.AddDice(difficulty_dice, Emojis[Sides.PurpleDie], Genesys_Difficulty);
        DicePool.AddDice(setback_dice, Emojis[Sides.BlackDie], Genesys_Setback);
        DicePool.AddDice(force_dice, Emojis[Sides.WhiteDie], Genesys_Force);

        //Add the dice pool emojis to the output.
        foreach (var die in DicePool)
        {
            output += die.PoolEmoji;
        }

        if (WorkingMessage?.Length > 0)
        {
            output += $"\n**Re:** {WorkingMessage}";
        }

        output += "\n**Results:** ";

        //Roll each die and grab a side.
        int hits = 0, advantages = 0, triumphs = 0,
            despairs = 0, light = 0, dark = 0;


        RandomizerState ??= PersistantRandomizer;

        foreach (var die in DicePool)
        {
            Genesys_Side side = die.Sides[Math.Abs(RandomizerState.NextInt32()) % die.Sides.Count];
            hits += side.hits;
            advantages += side.advantages;
            triumphs += side.triumphs;
            despairs += side.despairs;
            light += side.light;
            dark += side.dark;

            output += side.Display + "|";
        }

        string finalSection = " = ";
        if (hits > 0)
            finalSection += $"**{hits}** Success{(hits > 1 ? "es" : "")} ";
        if (hits < 0)
            finalSection += $"**{-hits}** Failure{(hits < -1 ? "s" : "")} ";

        if (advantages > 0)
            finalSection += $"**{advantages}** Advantage{(advantages > 1 ? "s" : "")} ";
        if (advantages < 0)
            finalSection += $"**{-advantages}** Threat{(advantages < -1 ? "s" : "")} ";

        if (triumphs > 0)
            finalSection += $"**{triumphs}** Triumph{(triumphs > 1 ? "s" : "")} ";

        if (despairs > 0)
            finalSection += $"**{despairs}** Despair{(despairs > 1 ? "s" : "")} ";

        if (light > 0)
            finalSection += $"**{light}** light side symbol{(light > 1 ? "s" : "")} ";

        if (dark > 0)
            finalSection += $"**{dark}** dark side symbol{(dark > 1 ? "s" : "")} ";

        if (finalSection.Length > 3)
            output += finalSection;
        else
            output += "**__Nothing Happens__**";

        return (false, output);
    }

    private static List<Genesys_Die> AddDice(this List<Genesys_Die> Pool, long Count, string Emoji, List<Genesys_Side> Sides)
    {
        for (int n = 0; n < Count; n++)
        {
            Genesys_Die die = new(Emoji, Sides);
            Pool.Add(die);
        }
        return Pool;
    }

    public static string GenesysCritTable(MessageState State, long modifier)
    {
        Random r = new();

        int value = (r.Next() % 100) + 1;

        string output = $"{State.UsernameForHeader}, your critical injury rolled {value}{(modifier >= 0 ? "+" : "")}{modifier}:\n";

        output += (value + modifier) switch
        {
            long n when (n <= 40) => $"**Severity**: Easy ({Emojis[Sides.PurpleDie]})\n",
            long n when (n <= 90) => $"**Severity**: Average ({Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]})\n",
            long n when (n <= 125) => $"**Severity:** Hard ({Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]})\n",
            _ => $"**Severity:** Daunting ({Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]})\n",
        };
        output += (value + modifier) switch
        {
            long n when (n <= 5) => "**Minor Nick:** The target suffers 1 strain.",
            long n when (n <= 10) => "**Slowed Down:** The target can only act during the last allied Initiative slot on their next turn.",
            long n when (n <= 15) => "**Sudden Jolt:** The target drops whatever is in hand.",
            long n when (n <= 20) => "**Distracted:** The target cannot perform a free maneuver during their next turn.",
            long n when (n <= 25) => $"**Off-Balance:** Add {Emojis[Sides.BlackDie]} to the target’s next skill check.",
            long n when (n <= 30) => "**Discouraging Wound:** Move one player pool Story Point to the Game Master pool (reverse if NPC).",
            long n when (n <= 35) => "**Stunned:** The target is staggered until the end of their next turn.",
            long n when (n <= 40) => "**Stinger:** Increase the difficulty of the target’s next check by one.",
            long n when (n <= 45) => "**Bowled Over:** The target is knocked prone and suffers 1 strain.",
            long n when (n <= 50) => "**Head Ringer:** The target increases the difficulty of all Intellect and Cunning checks by one until this Critical Injury is healed.",
            long n when (n <= 55) => "**Fearsome Wound:** The target increases the difficulty of all Presence and Willpower checks by one until this Critical Injury is healed.",
            long n when (n <= 60) => "**Agonizing Wound:** The target increases the difficulty of all Brawn and Agility checks by one until this Critical Injury is healed.",
            long n when (n <= 65) => "**Slightly Dazed:** The target is disoriented until this Critical Injury is healed.",
            long n when (n <= 70) => $"**Scattered Senses:** The target removes all {Emojis[Sides.BlueDie]} from skill checks until this Critical Injury is healed.",
            long n when (n <= 75) => "**Hamstrung:** The target loses their free maneuver until this Critical Injury is healed.",
            long n when (n <= 80) => "**Overpowered:** The target leaves themself open, and the attacker may immediately attempt another attack against them as an incidental, using the exact same pool as the original attack.",
            long n when (n <= 85) => "**Winded:** The target cannot voluntarily suffer strain to activate any abilities or gain additional maneuvers until this Critical Injury is healed.",
            long n when (n <= 90) => "**Compromised:** Increase difficulty of all skill checks by one until this Critical Injury is healed.",
            long n when (n <= 95) => "**At the Brink:** The target suffers 2 strain each time they perform an action until this Critical Injury is healed.",
            long n when (n <= 100) => "**Crippled:** One of the target’s limbs (selected by the GM) is impaired until this Critical Injury is healed. Increase difficulty of all checks that require use of that limb by one.",
            long n when (n <= 105) => $"**Maimed:** One of the target’s limbs (selected by the GM) is permanently lost. Unless the target has a cybernetic or prosthetic replacement, the target cannot perform actions that would require the use of that limb.All other actions gain {Emojis[Sides.BlackDie]} until this Critical Injury is healed.",
            long n when (n <= 110) => "**Horrific Injury:** Roll 1d10 to determine which of the target’s characteristics is affected: 1–3 for Brawn, 4–6 for Agility, 7 for Intellect, 8 for Cunning, 9 for Presence, 10 for Willpower.Until this Critical Injury is healed, treat that characteristic as one point lower.",
            long n when (n <= 115) => "**Temporarily Disabled:** The target is immobilized until this Critical Injury is healed.",
            long n when (n <= 120) => "**Blinded:** The target can no longer see. Upgrade the difficulty of all checks twice, and upgrade the difficulty of Perception and Vigilance checks three times, until this Critical Injury is healed.",
            long n when (n <= 125) => "**Knocked Senseless:** The target is staggered until this Critical Injury is healed.",
            long n when (n <= 130) => "**Gruesome Injury:** Roll 1d10 to determine which of the target’s characteristics is affected: 1–3 for Brawn, 4–6 for Agility, 7 for Intellect, 8 for Cunning, 9 for Presence, 10 for Willpower.That characteristic is permanently reduced by one, to a minimum of 1.",
            long n when (n <= 140) => "**Bleeding Out:** Until this Critical Injury is healed, every round, the target suffers 1 wound and 1 strain at the beginning of their turn. For every 5 wounds they suffer beyond their wound threshold, they suffer one additional Critical Injury. Roll on the chart, suffering the injury(if they suffer this result a second time due to this, roll again).",
            long n when (n <= 150) => "**The End Is Nigh:** The target dies after the last Initiative slot during the next round unless this Critical Injury is healed.",
            _ => "**Dead:** Complete, obliterated death.",
        };
        return output;
    }

    public static string StarWarsVehicleCritTable(MessageState State, long modifier)
    {
        Random r = new();

        int value = (r.Next() % 100) + 1;

        string output = $"{State.UsernameForHeader}, your critical vehicle damage rolled {value}{(modifier >= 0 ? "+" : "")}{modifier}:\n";

        output += (value + modifier) switch
        {
            long n when (n <= 54) => $"**Severity**: Easy ({Emojis[Sides.PurpleDie]})\n",
            long n when (n <= 81) => $"**Severity**: Average ({Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]})\n",
            long n when (n <= 126) => $"**Severity:** Hard ({Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]})\n",
            _ => $"**Severity:** Daunting ({Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]}{Emojis[Sides.PurpleDie]})\n",
        };
        output += (value + modifier) switch
        {
            long n when (n <= 9) => "**Mechanical Stress:** The ship or vehicle suffers 1 point of system strain.",
            long n when (n <= 18) => "**Jostled:** A small explosion or impact rocks the vehicle. All crew members suffer 1 strain and are disoriented for one round.",
            long n when (n <= 27) => "**Losing Power to Shields:** Decrease defense in affected defense zone by 1 until the Critical Hit is repaired. If the ship or vehicle has no defense, suffer 1 point of system strain.",
            long n when (n <= 36) => "**Knocked Off Course:** A particularly strong blast or impact sends the ship or vehicle careening off in a new direction. On his next turn, the pilot cannot execute any maneuvers and must make a Piloting check to regain control. The difficulty of this check depends on his current speed.",
            long n when (n <= 45) => $"**Tailspin:** All firing from the ship or vehicle suffers {Emojis[Sides.BlackDie]}{Emojis[Sides.BlackDie]} until the end of the pilot's next turn. All crew members are immobilized until the end of the pilot's next turn.",
            long n when (n <= 54) => "**Component Hit:** One component of the attacker's choice is knocked offline and is rendered inoperable until the end of the following round. For a list of ship components, see **Table 7-10: Small Ship or Vehicle Components** or **Table 7-11: Large Ship or Vehicle Components** depending on target ship silhouette.",
            long n when (n <= 63) => "**Shields Failing:** Reduce defense in all zones by 1 point until the Critical Hit is repaired. If the ship or vehicle has no defense, suffer 2 points of system strain",
            long n when (n <= 72) => "**Navicomputer Failure:** The navicomputer (or in the case of a ship without a navicomputer, its R2 unit) fails, and the ship cannot make the jump to hyperspace until the Critical Hit is repaired. If the ship or vehicle is without a hyperdrive, the vehicle or ship's navigation systems fail, leaving it flying or driving blind, unable to tell where it's going.",
            long n when (n <= 81) => "**Power Fluctuations:** The ship or vehicle is beset by random power surges and outages. The pilot cannot voluntarily inflict system strain on the ship (to gain an extra starship maneuver, for example) until this Critical Hit is repaired.",
            long n when (n <= 90) => "**Shields Down:** Decrease defense in afflicted defense zone to 0, and decrease defense in all other defense zones by 1 until this Critical Hit is repaired. While the defense of the afflicted defense zone cannot be restored until the Critical Hit is repaired, defense for other zones can be assigned to protect that defense zone as usual. If the ship or vehicle has no defense, sufer 4 points of system strain.",
            long n when (n <= 99) => "**Engine Damaged:** The ship or vehicle's maximum speed is reduced by 1 point, to a minimum of 1, until the Critical Hit is repaired.",
            long n when (n <= 108) => "**Shield Overload:** The ship's shields completely fail. Decrease the defense of all defensive zones to 0. This Critical Hit cannot be repaired until the end of the encounter, and the ship suffers 2 points of system strain. If the ship or vehicle has no defense, reduce armor by 1 until the Critical Hit is repaired.",
            long n when (n <= 117) => "**Engines Down:** The ship or vehicle's maximum speed is reduced to 0 until the Critical Hit is repaired, although it ocntinues on its present course thanks to momentum. In addition, the ship cannot execute any maneuvers until the Critical Hit table is repaired. ",
            long n when (n <= 126) => "**Major System Failure:** One component of the attacker's choice is heavily damaged and is inoperable until the Critical Hit is repaired. For a list of ship components, see **Table 7-10: Small Ship or Vehicle Components** or **Table 7-11: Large Ship or Vehicle Components** depending on target ship silhouette.",
            long n when (n <= 133) => "**Major Hull Breach:** A huge, gaping tear is torn in the ship's hull, and the ship depressurizes. For ships and vehicles of silhouette 4 and smaller, the entire ship depressurizes in a number of rounds equal to the ship's silouette. Ships and vehicles of silhoutte 5 and larger tend to be highly compartimentalized and have many safeguards against depressurization. These ships don't completely depressurize, but parts do (the specifics regarding which parts depressurize is up to the GM; however, each section of the ship or vehicle that does lose air does so in a number of rounds equal to the vehicles silhouette). Vehicles and ships operating in an atmopshere can better handle this Critical Hit. However, the huge tear still inflicts penalties, causing the vehicle to suffer the Destablized Critical Hit instead. (Destabilized: Halve hull trauma and system strain thresholds until repaired.)",
            long n when (n <= 138) => "**Destabilized:** The ship or vehicle's structural integrity is seriously damaged. Reduce the ship or vehicle's hull trauma threshold and system strain threshold to half their original values until repaired.",
            long n when (n <= 144) => "**Fire!:** Fire rages through the ship. The ship or vehicle immediately takes 2 points of system strain, and anyone caught in the fire takes fire damage as discussed on page 220. A fire can be put out with some quick thinking and appropriate skill or Vigilance and/or Cool checks at the Game Master's discretion. Once going, a fire takes one round per 2 of the ship's silhouette to put out.",
            long n when (n <= 153) => "**Breaking Up:** The vehicle or ship has suffered so much damage that it begins to come apart at its seams, breaking up and disintegrating around the crew. At the end of the following round, the ship is completely destroyed, and the surrounding environment is littered with debris. Anyone aboard the ship or vehicle has one round to get to an escape pod, bail out, or dive for the nearest hatch before they are lost.",
            _ => "**Vaporized:** The ship or vehicle is completely destroyed, consumed in a particularly large and dramatic fireball. Nothing survives.",
        };
        return output;
    }
}

internal class Genesys_Die(string Emoji, List<Genesys_Side> Sides)
{
    public string PoolEmoji = Emoji;
    public List<Genesys_Side> Sides = Sides;
}

internal class Genesys_Side(string display = "", int hits = 0, int advantages = 0, int triumphs = 0, int despairs = 0, int light = 0, int dark = 0)
{
    public int hits = hits;
    public int advantages = advantages;
    public int triumphs = triumphs;
    public int despairs = despairs;
    public int light = light;
    public int dark = dark;
    public string Display = display;
}
