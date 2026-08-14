using DSharpPlus.EventArgs;
using DSharpPlus.Commands;
using Rei.Random;
using Schala.PRNG;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Schala
{
    public class ParserState
    {
        #region Properties
        public IReadOnlyDictionary<Regex, Solver.RegexEvaluator> CustomLogic { get; }

        public SFMT Randomizer { get; }
        public int Seed { get; }

        public MessageState State { get; }
        public string Message { get; set; }

        #endregion
        #region Constructor
        public ParserState(CommandContext Context, IReadOnlyDictionary<Regex, Solver.RegexEvaluator>? CustomLogic = null, int Seed = 0) : this(CustomLogic, Seed)
        {
            State = new CommandState(Context);
            Message = string.Empty;
        }

        public ParserState(CommandContext Context, string Message, IReadOnlyDictionary<Regex, Solver.RegexEvaluator>? CustomLogic = null, int Seed = 0) : this(CustomLogic, Seed)
        {
            this.Message = Message;
            State = new CommandState(Context);
        }

        private ParserState(IReadOnlyDictionary<Regex, Solver.RegexEvaluator>? CustomLogic = null, int Seed = 0)
        {
            this.CustomLogic = CustomLogic ?? Solver.ParsingOrder;
            if (Seed == 0)
            {
                Seed = Environment.TickCount;
            }
            this.Randomizer = new SFMT(Seed);
            this.Seed = Seed;
            // State and Message are always set immediately by whichever public constructor
            // chains into this one; this constructor is never used on its own.
            State = null!;
            Message = null!;
        }
        #endregion
        #region Utility
        public string Parse()
        {
            return Solver.Parse(this, CustomLogic);
        }
        #endregion
    }
}
