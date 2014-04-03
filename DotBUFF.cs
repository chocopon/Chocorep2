using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrecisionRep
{
    public class DotBUFF
    {
        public int BuffID;
        public string Name;
        public int DotIryoku;
        public int[] Jobs;

        public DotBUFF(string name, int buffid, int dotiryoku, params int[] jobs)
        {
            Name = name;
            BuffID = buffid;
            DotIryoku = dotiryoku;
            Jobs = jobs;
        }
        public static DotBUFF[] _DotBUFFs;
        public static DotBUFF[] DotBUFFs
        {
            get
            {
                if (_DotBUFFs == null)
                {
                    _DotBUFFs = GetDotBUFFs();
                }
                return _DotBUFFs;
            }
        }

        public static DotBUFF GetDotBuff(int buffid)
        {
            foreach (DotBUFF buff in DotBUFFs)
            {
                if (buff.BuffID == buffid)
                    return buff;
            }
            return null;
        }

        public static DotBUFF[] GetDotBUFFs()
        {
            List<DotBUFF> list = new List<DotBUFF>();

            list.Add(new DotBUFF("サークル・オブ・ドゥーム", 248, 30, 1));
            list.Add(new DotBUFF("秘孔拳", 106, 25, 2));
            list.Add(new DotBUFF("フラクチャー", 244, 20, 3));
            list.Add(new DotBUFF("二段突き", 119, 25, 4));
            list.Add(new DotBUFF("桜華狂咲", 118, 30, 4));
            list.Add(new DotBUFF("ベノムバイト", 124, 35, 1, 2, 3, 4, 5, 23));
            list.Add(new DotBUFF("ウィンドバイト", 129, 45, 5, 23));
            list.Add(new DotBUFF("エアロ", 143, 25, 6));
            list.Add(new DotBUFF("エアロラ", 144, 40, 6));
            list.Add(new DotBUFF("サンダー", 161, 35, 7));
            list.Add(new DotBUFF("サンダラ", 161, 35, 7));
            list.Add(new DotBUFF("サンダガ", 161, 35, 7));
            list.Add(new DotBUFF("バイオ", 179, 40, 26));
            list.Add(new DotBUFF("ミアズマ", 180, 35, 26));
            list.Add(new DotBUFF("バイオラ", 189, 35, 26));
            list.Add(new DotBUFF("ミアズラ", 188, 10, 26));

            return list.ToArray();
        }
    }
}
