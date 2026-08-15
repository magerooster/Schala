using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Schala;

public interface IWeightedList
{
    string Name { get; }
    public void Set(string Value, long Weight);
    public string ConvertToCsv();
    public long GetTotalWeight();
    public string GetElementFromWeightedPosition(long position);
}

public class WeightedList : IWeightedList
{
    private Dictionary<string, long> Values { get; } = [];

    public string Name { get; }
    public WeightedList(string name)
    {
        Name = name;
        Save();
    }

    public void Set(string Value, long Weight)
    {
        if (Weight == 0 && Values.ContainsKey(Value))
        {
            Values.Remove(Value);
        }
        else
        {
            Values[Value] = Weight;
        }
        Save();
    }

    public string ConvertToCsv()
    {
        return string.Join(',', Values);
    }

    public void Save()
    {
        string json = JsonConvert.SerializeObject(this);
        File.WriteAllText($"D:\\WeightedLists\\{Name}.json", json);
    }

    public static WeightedList Load(string Filename)
    {
        string json = File.ReadAllText(Filename);
        return JsonConvert.DeserializeObject<WeightedList>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize weighted list from {Filename}");
    }

    public long GetTotalWeight()
    {
        long total = 0;

        foreach (var item in Values)
        {
            total += item.Value;
        }

        return total;
    }

    public string GetElementFromWeightedPosition(long position)
    {
        long cursor = 0;

        foreach (var item in Values)
        {
            cursor += item.Value;
            if (position < cursor)
                return item.Key;
        }

        return "";
    }

    public IEnumerable<KeyValuePair<string, long>> GetSection(int start, int length)
    {
        return Values.Skip(start).Take(length);
    }

    public int Count()
    {
        return Values.Count;
    }
}
