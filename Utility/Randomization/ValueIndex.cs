using System;

namespace Schala;

public class ValueIndex<TValue,TIndex>(TValue Value, TIndex Index) : IComparable<ValueIndex<TValue,TIndex>> where TValue : IComparable<TValue>
{
    public TValue Value = Value;
    public TIndex Index = Index;

    public int CompareTo(ValueIndex<TValue, TIndex>? other)
    {
        return other == null ? 1 : Value.CompareTo(other.Value);
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
