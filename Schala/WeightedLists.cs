using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Schala
{
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
        private Dictionary<string, long> _Values { get; set; } = new Dictionary<string, long>();

        public string Name { get; private set; }
        public WeightedList(string name)
        {
            Name = name;
            Save();
        }

        public void Set(string Value, long Weight)
        {
            if (Weight == 0 && _Values.ContainsKey(Value))
            {
                _Values.Remove(Value);
            }
            else
            {
                _Values[Value] = Weight;
            }
            Save();
        }

        public string ConvertToCsv()
        {
            return string.Join(',', _Values);
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

            foreach (var item in _Values)
            {
                total += item.Value;
            }

            return total;
        }

        public string GetElementFromWeightedPosition(long position)
        {
            long cursor = 0;

            foreach (var item in _Values)
            {
                cursor += item.Value;
                if (position < cursor)
                    return item.Key;
            }

            return "";
        }

        public IEnumerable<KeyValuePair<string, long>> GetSection(int start, int length)
        {
            return _Values.Skip(start).Take(length);
        }

        public int Count()
        {
            return _Values.Count;
        }
    }
}
