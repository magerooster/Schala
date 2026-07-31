using DSharpPlus.EventArgs;
using DSharpPlus.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Schala
{
    public static class Solver
    {
        #region Lookups and Stuff
        public static Dictionary<string, ParserFunction> CustomFunctions = new Dictionary<string, ParserFunction>();

        public delegate string ParserFunction(string s);
        public delegate string RegexEvaluator(Match Match, ParserState State);

        //The order of this list determines the order of operations. Earlier = Higher priority.
        internal static IReadOnlyDictionary<Regex, RegexEvaluator> ParsingOrder { get; private set; } = new Dictionary<Regex, RegexEvaluator>
        {
            //{new Regex(@"@@"), GetRandomUserFromChannel },
            {new Regex(@"\$(.+?)=(.+)\$"), SetGlobalVariable },             //$varname=value$
            //{new Regex(@"\@(.+?)=(.+)\@"), SetChannelVariable },
            {new Regex(@"\%(.+?)=(.+)\%"), SetPersonalVariable },           //%varname=value%
            {new Regex(@":([\d]+?):(.+?):(.+?):"), RepeatExpression },      //:repeats:string:separator:
            {new Regex(@":([\d]+?):(.+?):"), RepeatExpression },            //:repeats:string:
            {new Regex(@"\$(.+?)\$"), GetGlobalVariable },                  //$varname$
            //{new Regex(@"\@(.+?)\@"), GetChannelVariable },
            {new Regex(@"\%(.+?)\%"), GetPersonalVariable },                //%varname%
            {new Regex(@"{([^!][^{]*?)}"), RandomExpressionFromSet },       //{value,value,value}
            {new Regex(@"([\d]+)[dD]([\d]+)#(\-?[\d]+)"), Randomize},       //DicedSides#Keep
            {new Regex(@"([\d]+)[dD]([\d]+)"), Randomize},                  //DicedSides
            {new Regex(@"\(([\d]+?)\)"), RemoveParenthesis },               //(value)
            {new Regex(@"(\-?[0-9\.]+)\^(\-?[0-9\.]+)"), Exponent },        //base^exponent
            {new Regex(@"(\-?[0-9\.]+)\*(\-?[0-9\.]+)"), Multiply },        //value*value
            {new Regex(@"(\-?[0-9\.]+)/(\-?[0-9\.]+)"),  Divide },          //value/value
            {new Regex(@"(\-?[0-9\.]+)\+(\-?[0-9\.]+)"), Add },             //value+value
            {new Regex(@"(\-?[0-9\.]+)\-(\-?[0-9\.]+)"), Subtract },        //value-value
            {new Regex(@"\[(.+?):(.+?)\]"), ExecuteCustomFunction },        //[functionname:param,param,param]
        };
        #endregion
        #region Constructor
        static Solver()
        {
            CustomFunctions.Add("draw", CustomFunctionDraw);
            CustomFunctions.Add("shuffle", CustomFunctionShuffle);
        }
        #endregion
        #region Parser Functions
        public static string SetGlobalVariable(Match Match, ParserState State)
        {
            if (Data.Global.Variables.ContainsKey(Match.Groups[1].Value))
            {
                Data.Global.Variables[Match.Groups[1].Value] = Match.Groups[2].Value;
            }
            else
            {
                Data.Global.Variables.Add(Match.Groups[1].Value, Match.Groups[2].Value);
            }
            State.State.AppendToBlock("Global variable '" + Match.Groups[1].Value + "' set to '" + Match.Groups[2].Value + "'.\n");
            Data.Save(Data.Global, Data.GlobalFileLocation);
            return "";
        }
        public static string GetGlobalVariable(Match Match, ParserState State)
        {
            if (Data.Global.Variables.ContainsKey(Match.Groups[1].Value))
            {
                return Data.Global.Variables[Match.Groups[1].Value];
            }

            State.State.AppendToBlock("Variable '" + Match.Groups[1].Value + "' not defined.\n");
            return "";
        }

        public static string SetPersonalVariable(Match Match, ParserState State)
        {
            if (!Data.User.ContainsKey(State.State.UserID))
            {
                Data.User.Add(State.State.UserID, new UserMetadata());
            }

            if (Data.User[State.State.UserID].Variables.ContainsKey(Match.Groups[1].Value))
            {
                Data.User[State.State.UserID].Variables[Match.Groups[1].Value] = Match.Groups[2].Value;
            }
            else
            {
                Data.User[State.State.UserID].Variables.Add(Match.Groups[1].Value, Match.Groups[2].Value);
            }
            State.State.AppendToBlock("Personal variable '" + Match.Groups[1].Value + "' set to '" + Match.Groups[2].Value + "'.\n");
            Data.Save(Data.User, Data.UserFileLocation);
            return "";
        }

        public static string GetPersonalVariable(Match Match, ParserState State)
        {
            if (!Data.User.ContainsKey(State.State.UserID))
            {
                Data.User.Add(State.State.UserID, new UserMetadata());
            }

            if (Data.User[State.State.UserID].Variables.ContainsKey(Match.Groups[1].Value))
            {
                return Data.User[State.State.UserID].Variables[Match.Groups[1].Value];
            }

            State.State.AppendToBlock("Variable '" + Data.User[State.State.UserID].Variables[Match.Groups[1].Value] + "' not defined.\n");
            return "";
        }

        //public static string SetChannelVariable(Match Match, ParserState State)
        //{
        //    if (!Data.Server.ContainsKey(State.Args.Server.Id))
        //    {
        //        Data.Server.Add(State.Args.Server.Id, new ServerMetadata());
        //    }

        //    if (!Data.Server[State.Args.Server.Id].Channel.ContainsKey(State.Args.Channel.Id))
        //    {
        //        Data.Server[State.Args.Server.Id].Channel.Add(State.Args.Channel.Id, new ChannelMetadata());
        //    }

        //    if (Data.Server[State.Args.Server.Id].Channel[State.Args.Channel.Id].Variables.ContainsKey(Match.Groups[1].Value))
        //    {
        //        Data.Server[State.Args.Server.Id].Channel[State.Args.Channel.Id].Variables[Match.Groups[1].Value] = Match.Groups[2].Value;
        //    }
        //    else
        //    {
        //        Data.Server[State.Args.Server.Id].Channel[State.Args.Channel.Id].Variables.Add(Match.Groups[1].Value, Match.Groups[2].Value);
        //    }
        //    State.AppendToBlock("Channel variable '" + Match.Groups[1].Value + "' set to '" + Match.Groups[2].Value + "'.\n");
        //    Data.Save(Data.Server, Data.UserFileLocation);
        //    return "";
        //}

        //public static string GetChannelVariable(Match Match, ParserState State)
        //{
        //    if (!Data.Server.ContainsKey(State.Args.Server.Id))
        //    {
        //        Data.Server.Add(State.Args.Server.Id, new ServerMetadata());
        //    }

        //    if (!Data.Server[State.Args.Server.Id].Channel.ContainsKey(State.Args.Channel.Id))
        //    {
        //        Data.Server[State.Args.Server.Id].Channel.Add(State.Args.Channel.Id, new ChannelMetadata());
        //    }

        //    if (Data.Server[State.Args.Server.Id].Channel[State.Args.Channel.Id].Variables.ContainsKey(Match.Groups[1].Value))
        //    {
        //        return Data.Server[State.Args.Server.Id].Channel[State.Args.Channel.Id].Variables[Match.Groups[1].Value];
        //    }

        //    State.AppendToBlock("Variable '" + Data.User[State.UserID].Variables[Match.Groups[1].Value] + "' not defined.\n");
        //    return "";
        //}

        //public static string GetRandomUserFromChannel(Match Match, ParserState State)
        //{
        //    StringBuilder users = new StringBuilder("{");
        //    var channelUsers = State.Args.Channel.GetUsers()
        //    channelUsers.ForEach(u =>
        //    {
        //        users.Append(u. er.Name + ",");
        //    });
        //    return users.Remove(users.Length - 1, 1).Append('}').ToString();
        //}

        public static string RepeatExpression(Match Match, ParserState State)
        {
            string Padding = " ";
            if (Match.Groups.Count == 4)
                Padding = Match.Groups[3].Value;

            int Count;
            if (int.TryParse(Match.Groups[1].Value, out Count))
            {
                string partial = "";
                if (Count > 100)
                    Count = 100;
                if (Count < 2)
                    Count = 2;
                for (int n = 1; n <= Count; n++)
                {
                    partial += Match.Groups[2].Value + Padding;
                }
                return partial;
            }

            State.State.AppendToBlock("Could not repeat expression " + Match.Captures[1].Value + " times.\n");
            return "";
        }
        public static string RandomExpressionFromSet(Match Match, ParserState State)
        {
            string[] split = Match.Groups[1].Value.Split(',');
            if (split.Length > 0)
                return split[State.Randomizer.NextInt32() % split.Length];
            else
                return "";
        }

        public static string RandomExpressionFromWeightedList(Match Match, ParserState State)
        {
            return "(Not yet Implemented)";
        }

        public static string Randomize(long Dice, long Sides, long Keep, long Botch, long Autos, ParserState State, Func<PRNG.GenericRandEx, string>? DiceResult)
        {
            if (Dice < 1)
                Dice = 1;
            if (Sides < 2)
                Sides = 2;
            if (Keep > 100)
                Keep = 100;
            if (Keep < -100)
                Keep = -100;

            int TN = int.MaxValue;
            var TNsub = State.Message.RegexFind(TNRegex);
            if (TNsub.Item1 > 0 && !int.TryParse(State.Message.Substring(TNsub.Item1 + 2, TNsub.Item2 - 2), out TN))
                TN = int.MaxValue;

            long total = 0;
            int succs = 0;
            int n;

            if (Dice < 100)
                State.State.AppendToBlock("{");

            int WorkingDice = (int)Dice;

            //if (WorkingDice > int.MaxValue << 3)
            //    WorkingDice = WorkingDice << 3;

            Console.WriteLine("Starting to generate numbers...");
            if (Keep == Dice)
            {
                PRNG.BaseRandEx Rnd;
                if (TN == int.MaxValue)
                    Rnd = new PRNG.GenericRandEx(Dice, Sides);
                else
                {
                    Rnd = new PRNG.GenericWithTargetRandEx(Dice, Sides, TN, Botch, Autos, DiceRollResult: DiceResult ?? PRNG.GenericWithTargetRandEx.WoDResult);
                }

                Rnd.Roll();

                //total += Rnd.Sum;
                //if (Rnd is PRNG.GenericWithTargetRandEx)
                //    succs += (Rnd as PRNG.GenericWithTargetRandEx).Hits;

                State.State.AppendToBlock(Rnd.BlockText);

                if (Dice < 100)
                    State.State.AppendToBlock("} ");

                return Rnd.Header;
            }
            else
            {
                ExclusiveList<ValueIndex<int, int>> List;
                List<string> Strings;
                if (Keep < 0) //Bottom X
                {
                    List = new ExclusiveList<ValueIndex<int, int>>(ExclusiveList<ValueIndex<int, int>>.LowestComparer, -(int)Keep);
                    Strings = new List<string>(WorkingDice);
                }
                else //Top X
                {
                    List = new ExclusiveList<ValueIndex<int, int>>((int)Keep);
                    Strings = new List<string>();
                }

                //Build the initial data set
                for (int i = 0; i < WorkingDice; i++)
                {
                    n = Math.Abs(State.Randomizer.NextInt32()) % (int)Sides + 1;
                    List.Add(new ValueIndex<int, int>(n, i));
                    Strings.Add(n.ToString());
                }

                //Format the substrings for interesting results.
                for (int i = 0; i < List.Count; i++)
                {
                    total += List[i].Value;
                    if (List[i].Value >= TN)
                    {
                        State.State.AppendToBlock("__" + Strings[List[i].Index] + "__ ");
                        succs++;
                    }
                    else
                    {
                        State.State.AppendToBlock(Strings[List[i].Index] + " ");
                    }
                }

                State.State.AppendToBlock("<- ");

                //Build the final string.
                for (int i = 0; i < Strings.Count; i++)
                {
                    State.State.AppendToBlock(Strings[i]);
                    if (i < Strings.Count - 1)
                        State.State.AppendToBlock(" ");

                }
            }
            Console.WriteLine("Finishing number generation.");
            if (Dice < 100)
                State.State.AppendToBlock("} ");

            if (TN == int.MaxValue)
                return total.ToString();

            return succs.ToString();
            //  function RKH(x, y, z)
            //    if (tonumber(x) > 100000) then
            //      x = 100000
            //      _MSG = _MSG.. " (RND X Capped)"
            //    end
            //    if (tonumber(y) > 100000) then
            //      y = 100000
            //      _MSG = _MSG.. " (RND Y Capped)"
            //    end
            //    if (tonumber(z) > tonumber(x)) then
            //      z = x
            //      _MSG = _MSG.. " (KEEP Capped)"
            //    end
            //    if (x == 0 or y == 0 or z == 0) then return 0 end
            //    a = { }
            //    a["get"] = tonumber(z)
            //    a["size"] = tonumber(x)
            //    for n = 1,tonumber(x) do
            //      a[n] = math.floor (MtRand() * tonumber(y)) + 1
            //    end
            //    return Highest(a)
            //  end
        }
        public static string Randomize(Match Match, ParserState State)
        {
            int Dice, Sides, Keep;
            if (int.TryParse(Match.Groups[1].Value, out Dice) && int.TryParse(Match.Groups[2].Value, out Sides))
            {
                if (int.TryParse(Match.Groups[3].Value, out Keep))
                {
                    return Randomize(Dice, Sides, Keep, 0, 0, State, null);
                }
                else
                {
                    return Randomize(Dice, Sides, Dice, 0, 0, State, null);
                }
            }

            return "0";
        }
        public static string RemoveParenthesis(Match Match, ParserState State)
        {
            return Match.Groups[1].Value;
        }
        public static string Exponent(Match Match, ParserState State)
        {
            int x, y;
            if (!int.TryParse(Match.Groups[1].Value, out x))
            {
                return "0";
            }
            if (!int.TryParse(Match.Groups[2].Value, out y))
            {
                return "0";
            }

            return Math.Pow(x, y).ToString();
        }
        public static string Multiply(Match Match, ParserState State)
        {
            int x, y;
            if (!int.TryParse(Match.Groups[1].Value, out x))
            {
                return "0";
            }
            if (!int.TryParse(Match.Groups[2].Value, out y))
            {
                return "0";
            }

            return (x * y).ToString();
        }
        public static string Divide(Match Match, ParserState State)
        {
            int x, y;
            if (!int.TryParse(Match.Groups[1].Value, out x))
            {
                return "0";
            }
            if (!int.TryParse(Match.Groups[2].Value, out y))
            {
                return "0";
            }

            //BOO HISS
            return (x / y).ToString();
        }
        public static string Add(Match Match, ParserState State)
        {
            int x, y;
            if (!int.TryParse(Match.Groups[1].Value, out x))
            {
                return "0";
            }
            if (!int.TryParse(Match.Groups[2].Value, out y))
            {
                return "0";
            }

            return (x + y).ToString();
        }
        public static string Subtract(Match Match, ParserState State)
        {
            int x, y;
            if (!int.TryParse(Match.Groups[1].Value, out x))
            {
                return "0";
            }
            if (!int.TryParse(Match.Groups[2].Value, out y))
            {
                return "0";
            }

            return (x - y).ToString();
        }
        public static string ExecuteCustomFunction(Match Match, ParserState State)
        {

            if (CustomFunctions.ContainsKey(Match.Groups[1].Value))
            {
                return CustomFunctions[Match.Groups[1].Value](Match.Groups[2].Value);
            }

            return Match.Groups[2].Value;
        }
        #endregion
        #region Custom Functions
        public static string CustomMaxValue(int x, int y)
        {
            return x > y ? x.ToString() : y.ToString();
            //      ["max"] = function() --Takes the higher of two values and returns it.
            //       _,_,ch = string.find(x, ":(.*)")
            //        ch = string.gsub(ch, ":", "")
            //        ch = string.find(ch, "([%D])")
            //        if ch ~= nil then
            //          rv = "[" .. x.. "]"
            //          Note("Fork A: ", rv)
            //        else
            //          rv = params[1]
            //          for i=1,table.maxn(params) do
            //            rv = math.max(rv,params[i])
            //            Note("RV ", rv)
            //          end
            //          Note("Fork B: ", rv)
            //        end
            //      end,
        }
        public static string CustomMinValue(int x, int y)
        {
            return x < y ? x.ToString() : y.ToString();
            //      ["min"] = function()--Takes the lower of two values and returns it.
            //      _,_,ch = string.find(x, ":(.*)")
            //        ch = string.gsub(ch, ":", "")
            //        ch = string.find(ch, "[%D]")
            //        if ch ~= nil then
            //          rv = "[" .. x.. "]"
            //        else
            //          rv = params[1]
            //          for i=1,table.getn(params) do
            //            rv = math.min(rv,params[i])
            //          end
            //        end
            //      end,
        }
        public static string CustomFloorValue(double x)
        {
            return Math.Floor(x).ToString();
            //      ["floor"] = function()   --Rounds a number downwards using math.floor.
            //        _,_,ch = string.find(x, ":(.*)")
            //        if tonumber(ch) == nil then
            //          rv = "[" .. x.. "]"
            //        else
            //          rv = math.floor(tonumber(ch))
            //        end
            //      end,
        }
        //public static string SetLocalVariable(string s)
        //{
        //    //      ["v"] = function()--Sets or returns a temporary(nonpersistant) variable.These variables have the scope of _tv.
        //    //  _,_,ch = string.find(x, ":(.*)")
        //    //        if params[2] == nil then
        //    //          rv = _tv[params[1]]
        //    //        else
        //    //          _tv[params[1]] = params[2]
        //    //rv = ""
        //    //        end
        //    //      end,
        //}
        public static string CustomStringFormat(string Pattern, string Original)
        {
            return String.Format(Pattern, Original);
            //      ["to"] = function()--Formats a number into things.
            //    _,_,ch = string.find(x, ":(.*)")
            //       proc_convert = {
            //         ["imperial"] = function()  --Converts A to X'Y"
            //           if tonumber(params[2]) == nil then
            //             rv = "[" .. x.. "]"
            //           else
            //             _i = math.floor(tonumber(params[2])/12)
            //             _f = math.fmod(tonumber(params[2]),12)
            //             rv = _i.. "'" .. _f.. '"'
            //           end
            //         end,
            //       }
            //       if proc_convert[string.lower(params[1])] ~= nil then proc_convert[string.lower(params[1])]() end
            //      end,
        }
        #endregion
        #region Regex Strings Are Fucking Magic, Okay?
        private static Regex CommentRegex = new Regex(@"[ ]?`.*`");
        private static string TNRegex = @"[tT][nN][0-9]*";
        #endregion

        public static string Parse(ParserState State, IReadOnlyDictionary<Regex, RegexEvaluator>? CustomLogic = null)
        {
            string working = State.Message.RegexGsub(@"^(.*?[sS][cC][hH][aA][lL][aA].*?[ ]?)\b", "").Item2;
            string prevWorking = "";
            Tuple<bool, string> result;

            result = working.RegexGsub(CommentRegex, "");
            //Main parsing loop
            while (working != prevWorking)
            {
                Console.WriteLine(working);
                prevWorking = working;
                foreach (var kvp in State.CustomLogic)
                {
                    result = working.RegexGsub(kvp.Key, kvp.Value, State);
                    if (result.Item1) //Go until we make a change. Item1 is a bool that is true if it changed.
                    {

                        working = result.Item2; //Item2 is the new string.
                        break;
                    }
                }
            }

            working = working.RegexGsub(TNRegex, "").Item2;

            State.State.AppendToHeader(working);

            return $"{State.State.FormattedHeaderText}\n{State.State.FormattedBlockText}";
        }

        private static string CustomFunctionDraw(string input)
        {
            return "";
        }
        private static string CustomFunctionShuffle(string input)
        {
            return "";
        }
    }
}
#region Ignoring for now
//function BetaRoll(name, user, output, wild)
//  -- BEGIN parser functions
//  _append = ""
//  _MSG = ""
//  _tv = {}
//  -- END parser functions



//  working = wild[3]
//  local game_type = GetVariable(string.lower("mode"..vname(wild[2])))

//  -- Scan for and remove flags
//  --Show all randomly generated numbers
//  _, _, flag = string.find(working, "(`show`)")
//  if (flag ~= nil) then _SHOW = true else _SHOW = false end
//  Note(flag)

// -- End Flags
#endregion

//  -- Main loop
//  working = string.gsub(working, "[ ]?`.*`", "")
//  prevworking = ""

//  changes = 0
//  _a = 0
//  while working ~= prevworking do
//    changed = false
//    prevworking = working
//    _a = _a + 1
//    if (changed == false) then                                                 --SET GLOBAL VAR
//      working, changes = string.gsub(working, "%$(.-)=(.*)%$", GVS)
//      changed = check(changes,"GVS")
//    end
//    if (changed == false) then                                                 --SET PERSONAL VARIABLE
//      working, changes = string.gsub(working, "%%(.-)=(.*)%%", PVS)
//      changed = check(changes,"PVS")
//    end
//    if (changed == false) then                                                 --REPEAT EXPRESSION W/ SPECIAL PADDING CHARACTERS
//      working, changes = string.gsub(working, "%#([%d]-):(.-):(.-)%#", RPC)
//      changed = check(changes,"RPC")
//    end
//    if (changed == false) then                                                 --REPEAT EXPRESSION
//      working,changes = string.gsub(working,"%#([%d]-):(.-)%#", RPT)
//      changed = check(changes,"RPT")
//    end
//    if (changed == false) then                                                 --GLOBAL VAR
//      working,changes = string.gsub(working,"%$(.-)%$", GVR)
//      changed = check(changes,"GVR")
//    end
//    if (changed == false) then                                                 --PERSONAL VARIABLE
//      working,changes = string.gsub(working,"%%(.-)%%", PVR)
//      changed = check(changes,"PVR")
//    end
//--    if (changed == false) then                                               --RANDOM USER NAME FROM CHANNEL
//--      working,changes = string.gsub(working,"%_(.)%_", USR)
//--      changed = check(changes,"USR")
//--    end
//    if (changed == false) then                                                 --RANDOMIZE, KEEP HIGHEST
//      working,changes = string.gsub(working,"([%d]+)[dD]([%d]+)[kK]([%d]+)", RKH)
//      changed = check(changes,"RKH")
//    end
//    end
//    Note("Pass " .. _a)
//    Note("State: " .. working)
//    Note("Last: " .. prevworking)
//  end

//  finalparse = "#S#[Solve] #H##N##F#, your result was: #H#"..working.._MSG
//  if (_SHOW) then finalparse = finalparse.. "#F#" .. _append end
//  if (wild[3] ~= working) then Say(user, chan, finalparse) end
//end
