using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace StatRandomizer
{
    public abstract class RandomStat
    {
        public object Value;
        public Vector2 Range = Vector2.zero;

        public FieldInfo FieldInfo;

        public abstract void Randomize();
        public abstract void SetValue(ItemStats stats);
        public abstract void RemoveValue(ItemStats stats);

        public abstract override string ToString();
        public abstract void Deserialize(string _data);
    }

    public class RandomFloat : RandomStat
    {
        public override void Deserialize(string _data)
        {
            if (float.TryParse(_data, out float f))
            {
                Value = f;
            }
        }

        public override void Randomize()
        {
            float f = UnityEngine.Random.Range(Range.x, Range.y);
            f = (float)Math.Round(Convert.ToDecimal(f), 2);

            Value = f;
        }

        public override void RemoveValue(ItemStats stats)
        {
            if (FieldInfo.DeclaringType.IsAssignableFrom(stats.GetType()))
            {
                var orig = FieldInfo.GetValue(stats);
                FieldInfo.SetValue(stats, (float)orig - (float)Value);
            }
        }

        public override void SetValue(ItemStats stats)
        {
            if (FieldInfo.DeclaringType.IsAssignableFrom(stats.GetType()))
            {
                var orig = FieldInfo.GetValue(stats);
                FieldInfo.SetValue(stats, (float)orig + (float)Value);
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class RandomInt : RandomStat
    {
        public override void Deserialize(string _data)
        {
            if (int.TryParse(_data, out int i))
            {
                Value = i;
            }
        }

        public override void Randomize()
        {
            Value = UnityEngine.Random.Range((int)Range.x, (int)Range.y);
        }

        public override void RemoveValue(ItemStats stats)
        {
            if (FieldInfo.DeclaringType.IsAssignableFrom(stats.GetType()))
            {
                var orig = FieldInfo.GetValue(stats);
                FieldInfo.SetValue(stats, (int)orig - (int)Value);
            }
        }

        public override void SetValue(ItemStats stats)
        {
            if (FieldInfo.DeclaringType.IsAssignableFrom(stats.GetType()))
            {
                var orig = FieldInfo.GetValue(stats);
                FieldInfo.SetValue(stats, (int)orig + (int)Value);
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class RandomFloatArray : RandomStat
    {
        public override string ToString()
        {
            string s = "";
            foreach (var f in (float[])Value)
            {
                if (s != "") { s += "/"; }
                s += f.ToString();
            }
            return s;
        }

        public override void Deserialize(string _data)
        {
            var data2 = _data.Split(new char[] { '/' });

            var list = new List<float>();
            foreach (var s in data2)
            {
                if (float.TryParse(s, out float f))
                {
                    list.Add(f);
                }
            }

            Value = list.ToArray();
        }

        public override void Randomize()
        {
            var list = new List<float>();
            for (int i = 0; i < 6; i++)
            {
                float f = UnityEngine.Random.Range(Range.x, Range.y);
                f = (float)Math.Round(Convert.ToDecimal(f), 2);
                list.Add(f);
            }

            list.AddRange(new List<float>() { 0f, 0f, 0f }); // add the unused types

            Value = list.ToArray();
        }

        public override void SetValue(ItemStats stats)
        {
            if (FieldInfo.DeclaringType.IsAssignableFrom(stats.GetType()))
            {
                var orig = (float[])FieldInfo.GetValue(stats);

                var list = new List<float>();
                for (int i = 0; i < 6; i++)
                {
                    list.Add(orig[i] + ((float[])Value)[i]);
                }

                list.AddRange(new List<float>() { 0f, 0f, 0f });

                FieldInfo.SetValue(stats, list.ToArray());
            }
        }

        public override void RemoveValue(ItemStats stats)
        {
            if (FieldInfo.DeclaringType.IsAssignableFrom(stats.GetType()))
            {
                var orig = (float[])FieldInfo.GetValue(stats);

                var list = new List<float>();
                for (int i = 0; i < 6; i++)
                {
                    list.Add(orig[i] - ((float[])Value)[i]);
                }

                list.AddRange(new List<float>() { 0f, 0f, 0f });

                FieldInfo.SetValue(stats, list.ToArray());
            }
        }
    }
}
