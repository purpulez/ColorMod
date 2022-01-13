using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocColor.Config
{
    public class Integer
    {
       public uint value { get; set; }
        
        public Integer(uint value) {
         
            this.value = value;
        }
        
    }
    public class CharInfo
    {
        public string name { get; set; }

        public bool isHero { get; set; }

        public CharInfo(string name, bool isHero)
        {
            this.name = name;
            this.isHero = isHero;

        }
    }

    public class Map<TKey, TValue> : ConcurrentDictionary<TKey, TValue> where TValue : class
        {
            public new TValue this[TKey key]
            {
                get
                {
                    if (key == null || !ContainsKey(key))
                        return null;
                    else
                        return base[key];
                }
                set
                {   if (key == null) return;
                    if (!ContainsKey(key))
                        TryAdd(key, value);
                    else
                        base[key] = value;
                }
            }
        }
    }
