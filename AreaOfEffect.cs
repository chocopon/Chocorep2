using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ffxivlib;

namespace PrecisionRep
{

    public class AreaOfEffeceProvider
    {
        public static AreaOfEffectAction[] GetAreaOfEffects()
        {
            List<AreaOfEffectAction> list = new List<AreaOfEffectAction>();
            list.Add(new FlaminngArrow());
            list.Add(new ShadowFlare());
            return list.ToArray();
        }
    }

    public class AreaOfEffect
    {
        public DateTime Timestamp;
        public AreaOfEffectAction AoEAction;
        public BUFFSnap BuffSnap;
        public Entity Dest;
        public AreaOfEffect(DateTime time,AreaOfEffectAction aoeAction,BUFFSnap snap,Entity dest)
        {
            Timestamp = time;
            AoEAction = aoeAction;
            BuffSnap = snap;
            Dest = dest;
        }

        public int GetPower()
        {
            return (int)(BuffSnap.GetRate() * AoEAction.DoTPower);
        }
    }

    public class AreaOfEffectAction
    {
        public int ActionID;
        public int BUFFID;
        public string ActionName;
        /// <summary>
        /// 効果範囲（半径）
        /// </summary>
        public float Range;
        /// <summary>
        /// DoT効果
        /// </summary>
        public int DoTPower;
    }
    public class FlaminngArrow : AreaOfEffectAction
    {
        public FlaminngArrow()
        {
            ActionID = 102;
            BUFFID = 249;
            ActionName = "フレイミングアロー";
            Range = 5.0F;
            DoTPower = 35;
        }
    }
    public class ShadowFlare : AreaOfEffectAction
    {
        public ShadowFlare()
        {
            ActionID = 179;
            BUFFID = 190;
            ActionName = "シャドウフレア";
            Range = 5.0F;
            DoTPower = 25;
        }
    }

   
}
