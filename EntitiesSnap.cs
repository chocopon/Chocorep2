using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ffxivlib;

namespace PrecisionRep
{
    public class EntitiesSnap
    {
        public DateTime timestamp;
        public Entity[] Entities;

        public EntitiesSnap(DateTime time, Entity[] ents)
        {
            timestamp = time;
            Entities = ents;
        }
    }
}
