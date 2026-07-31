using DSharpPlus.Entities;
using DSharpPlus.Commands;
using Rei.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schala
{
    public class PreviousCommand
    {
        public CommandContext OriginalContext { get; private set; }

        public PreviousCommand(CommandContext Context)
        {
            this.OriginalContext = Context;
        }
    }

    public class PreviousGenesysCommand : PreviousCommand
    {
        public SFMT RandomizerState { get; private set; }

        public long Proficiency_Dice { get; private set; }
        public long Abiity_Dice { get; private set; }
        public long Boost_Dice { get; private set; }
        public long Setback_Dice { get; private set; }
        public long Difficulty_Dice { get; private set; }
        public long Challenge_Dice { get; private set; }
        public long Force_Dice { get; private set; }
        public long Characteristic_Dice { get; private set; }
        public long Skill_Dice { get; private set; }

        public PreviousGenesysCommand(CommandContext OriginalContext, long profiency_dice, long ability_dice, long boost_dice, long setback_dice, long difficulty_dice, long challenge_dice, long force_dice, long characteristic_dice, long skill_dice, SFMT RandomizerState) : base(OriginalContext)
        {
            this.RandomizerState = RandomizerState;
        }
    }
}
