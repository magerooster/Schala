using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Schala;

public static class Data
{
    public static GlobalMetadata Global = new();
    public static Dictionary<ulong, ServerMetadata> Server = [];
    public static Dictionary<ulong, UserMetadata> User = [];
    //public static Dictionary<ulong, Dictionary<string, string>> ServerVars = new Dictionary<ulong, Dictionary<string, string>>(); //Collection of key/value pairs that only work on a particular server.

    public const string GlobalFileLocation = ".\\Data\\Global.json";
    public const string ServerFolderLocation = ".\\Data\\Server.json";
    public const string UserFileLocation = ".\\Data\\User.json";

    public static void LoadAll()
    {
        Console.WriteLine("Loading global variables...");
        Global = Load<GlobalMetadata>(GlobalFileLocation, true) ?? new GlobalMetadata();

        Console.WriteLine("Loading server variables...");
        Server = Load<Dictionary<ulong, ServerMetadata>>(ServerFolderLocation, true) ?? [];

        Console.WriteLine("Loading user variables...");
        User = Load<Dictionary<ulong, UserMetadata>>(UserFileLocation, true) ?? [];
    }
    public static T? Load<T>(string Location, bool CreateIfMissing)
    {
        if (string.IsNullOrEmpty(Location))
        {
            Console.WriteLine("No location provided...");
            return default;
        }

        if (!File.Exists(Location) && CreateIfMissing)
        {
            Console.WriteLine(Location + " did not exist, creating...");
            Save(Global, Location);
        }
        try
        {
            T? thisObject = JsonConvert.DeserializeObject<T>(File.ReadAllText(Location));
            return thisObject;
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error(ex, $"Failed to deserialize {typeof(T).Name} from {Location}");
            return default;
        }


    }

    public static void Save(object ThisObject, string Location)
    {
        if (!Directory.Exists(".\\Data\\"))
        {
            Console.WriteLine("Making Data directory in " + Path.GetFullPath(Location));
            Directory.CreateDirectory(".\\Data\\");
        }

        try
        {
            Console.WriteLine("Saving file to " + Location);
            File.WriteAllText(Location, JsonConvert.SerializeObject(ThisObject));
        }
        catch (IOException)
        {

        }

    }
}

public class GlobalMetadata
{
    public Dictionary<string, string> Variables { get; set; } = []; //Collection of key/value pairs not associated with any particular entity.
}

public class ServerMetadata
{
    public Dictionary<ulong, ChannelMetadata> Channel { get; set; } = [];
}

public class ChannelMetadata
{
    public Dictionary<string, string> Variables { get; set; } = []; //Collection of key/value pairs that only work in a particular channel on a particular server.
    public string DefaultRollSystem { get; set; } = "d20";
}

public class UserMetadata
{
    public Dictionary<string, string> Variables { get; set; } = []; //Collection of key/value pairs that only work for a particular user.
}
