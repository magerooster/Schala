using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus.Commands;
using DSharpPlus.Entities;

using Rei.Random;

namespace Schala;

public class PreviousCommand(CommandContext Context)
{
    public CommandContext OriginalContext { get; } = Context;
}

public class PreviousGenesysCommand(CommandContext OriginalContext, long profiency_dice, long ability_dice, long boost_dice, long setback_dice, long difficulty_dice, long challenge_dice, long force_dice, long characteristic_dice, long skill_dice, SFMT RandomizerState) : PreviousCommand(OriginalContext)
{
    public SFMT RandomizerState { get; } = RandomizerState;

    public long Proficiency_Dice { get; } = profiency_dice;
    public long Abiity_Dice { get; } = ability_dice;
    public long Boost_Dice { get; } = boost_dice;
    public long Setback_Dice { get; } = setback_dice;
    public long Difficulty_Dice { get; } = difficulty_dice;
    public long Challenge_Dice { get; } = challenge_dice;
    public long Force_Dice { get; } = force_dice;
    public long Characteristic_Dice { get; } = characteristic_dice;
    public long Skill_Dice { get; } = skill_dice;
}
