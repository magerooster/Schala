using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rei.Random;

namespace Schala.PRNG;

public class FudgeDiceRandEx(int Dice = 1) : BaseRandEx
{
    protected static CustomDie FudgeDie = new(
        new DieFace(-1, "[-]"),
        new DieFace(-1, "[-]"),
        new DieFace(0, "[ ]"),
        new DieFace(0, "[ ]"),
        new DieFace(1, "[+]"),
        new DieFace(1, "[+]"));

    protected int Dice = Dice > 0 ? Dice : 1;

    public override void Roll()
    {
        int Limit = GetRandLimit(6); //Hard coded for Fudge dice.
        for (int n = 0; n < Dice; n++)
        {
            int i = RollDie(6, Limit);
            Sum += FudgeDie.Faces![i].Value;

            if (Dice < 100)
            {
                _blockText.Append(FudgeDie.Faces[i].Output);
                if (n < Dice - 1)
                    _blockText.Append(' ');
            }
        }

        _header.Append(Sum);
    }
}

public class GenericWithTargetRandEx : GenericRandEx
{
    #region Pool checks for WoD
    public static Func<GenericRandEx, string> WoDResult = (rngState) =>
    {
        string nonSpec = "";
        string spec = "";

        if (rngState is GenericWithTargetRandEx state)
        {

            nonSpec = state.Hits == 0 && state.Botches > 0
                ? "**Botch**"
                : state.Botches > state.Hits ? "**Failure**" : $"{state.Hits - state.Botches} Successes";

            spec = state.Hits + state.MaxHits == 0 && state.Botches > 0
                ? "**Botch**"
                : state.Botches > state.Hits + state.MaxHits ? "**Failure**" : $"{state.MaxHits + state.Hits - state.Botches} Successes";

        }

        return $"{nonSpec} ({spec} if specialized)";
    };
    #endregion

    protected long _target = 0;
    protected long _botchThreshold = 0;
    protected long _hits = 0;
    protected long _maxHits = 0;
    public long MaxHits { get { return _maxHits; } protected set { _maxHits = value; } }
    public long Hits { get { return _hits; } protected set { _hits = value; } }
    protected long _botches = 0;
    public long Botches { get { return _botches; } protected set { _botches = value; } }
    protected Func<GenericRandEx, string> DiceResult;

    public GenericWithTargetRandEx(long Dice, long Sides, long Target, long BotchThreshold, long Autos, Func<GenericRandEx, string>  DiceRollResult) : base(Dice, Sides)
    {
        if (Target > Sides)
            Target = Sides;
        if (Target < 0)
            Target = 0;
        this._target = Target;
        this._botchThreshold = BotchThreshold;
        this.DiceResult = DiceRollResult;
    }

    public override void Roll()
    {
        int Limit = GetRandLimit(Sides);

        for (int n = 0; n < Dice; n++)
        {
            int i = RollDie((int)Sides, Limit);

            Sum += i;

            if (Sides < 100)
            {
                if (i == Sides)
                {
                    _blockText.Append("**");
                    _maxHits++;
                    _hits++;
                }
                else if (i >= _target)
                {
                    _blockText.Append("__");
                    _hits++;
                }
                else if (i <= _botchThreshold)
                {
                    _blockText.Append("~~");
                    _botches++;
                }


                _blockText.Append(i);
                if (i == Sides)
                    _blockText.Append("**");
                else if (i >= _target)
                    _blockText.Append("__");
                else if (i <= _botchThreshold)
                    _blockText.Append("~~");

                if (n < Dice - 1)
                    _blockText.Append(' ');
            }
        }

        _header.Append(DiceResult(this));
    }
}

public class GenericRandEx : BaseRandEx
{
    protected long Dice = 0;
    protected long Sides = 0;

    public GenericRandEx(long Dice, long Sides)
    {
        if (Dice > 0)
            this.Dice = Dice;
        if (Sides > 0)
            this.Sides = Sides;
    }

    public override void Roll()
    {
        int Limit = GetRandLimit(Sides);

        for (int n = 0; n < Dice; n++)
        {
            int i = RollDie((int)Sides, Limit);

            Sum += i;

            if (Dice < 100)
            {
                _blockText.Append(i);
                if (n < Dice - 1)
                    _blockText.Append(' ');
            }
        }

        _header.Append(Sum);
    }
}

public abstract class BaseRandEx
{
    #region Basic Data
    protected static SFMT Rnd = new();

    //private long _sum = 0;
    public long Sum
    {
        get;
        protected set;
    }

    protected StringBuilder _header = new();
    public string Header
    {
        get
        {
            return _header.ToString();
        }
    }

    protected StringBuilder _blockText = new();
    public string BlockText
    {
        get
        {
            return _blockText.ToString();
        }
    }
    #endregion

    public abstract void Roll();

    protected static int GetRandLimit(long Sides)
    {
        int s = (int)Sides;
        return int.MaxValue - (((int.MaxValue % s) + 1) % s);
    }

    protected static int RollDie(int Sides, int Limit)
    {
        int r;
        while ((r = Math.Abs(Rnd.NextInt32())) > Limit) { }
        return (r % Sides) + 1;
    }

    //    //Base stuff (Inputs)
    //    public int Dice = 1; //Number of dice to roll.
    //    public int Sides = 1; //Number of sides to roll.
    //    public int Modifier = 0; //Added to the final result, whether that's calculated in successes or totalled.
    //    public bool ApplyPreviousCriticalBonus = false; //Is this the follow-up roll for a critical? Important mainly for damage rolls.

    //    //Target numbers
    //    public int FumbleThreshold = 1; //This number or less contributes to fumble calculation.
    //    public int SuccessThreshold; //This number or more counts as 1 hit.
    //    public int CriticalThreshold; //This number or more counts as 1 hit + does something extra as defined by PerformOnExtra

    //    //Current result state
    //    public long Sum = 0;
    //    public int Hits = 0;

    //    public string ReinsertionString = "";
    //    public string Header = "";
    //    public string BlockText = "";

    //    //For really weird stuff
    //    public SpecialRules Rules;
    //    public Dictionary<int, string> SpecialDie;

    //    //Called when a die rolls CriticalThreshold or higher. Implemented versions perform actions like 10 again, criticals, exploding dice...
    //    public abstract void PerformOnCritical(ref int RolledValue); 

    //    public abstract void PerformOnFumble(ref int RolledValue); //Any special logic for when bad numbers (generally this means 1s) roll.

    //    #region Known Weird Dice
    //    public static Dictionary<int, DieFace> SpecialDie_Fudge = new Dictionary<int, DieFace>()
    //    {
    //        {1, new DieFace(-1, "[-]") },
    //        {2, new DieFace(-1, "[-]") },
    //        {3, new DieFace(0, "[ ]") },
    //        {4, new DieFace(0, "[ ]") },
    //        {5, new DieFace(1, "[+]") },
    //        {6, new DieFace(1, "[+]") },
    //    };
    //    #endregion
}

public enum SpecialRules
{
    ResultIsSum = 0x01, //Return Sum + Modifier for total.
    ResultIsHits = 0x02, //Return Hits + Modifier for total.

    UseSpecialDice = 0x10, //Use the SpecialDie definition, otherwise dice are numerical, from 1 to number of sides.
}

public class CustomDie(params DieFace[] Faces)
{
    public int Sides { get; set; } = Faces.Length;
    public DieFace[]? Faces { get; set; } = Faces;
}

public class DieFace(int Value, string Output)
{
    public string Output = Output;
    public int Value = Value;
}
