using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ffxivlib;

namespace PrecisionRep
{

    public class BUFFSnap
    {
        public Entity DestEnt;
        public Entity SrcEnt;
        public BUFF Buff;
        public DateTime startTime;
        public DateTime endTime;
        public bool IsFinalized;
        public DotBUFF DotBuff;

        public BUFFSnap(Entity dest,Entity src, BUFF buff, DateTime start)
        {
            DestEnt = dest;
            SrcEnt = src;
            Buff = buff;
            startTime = start;
            DotBuff = DotBUFF.GetDotBuff(buff.BuffID);
            BuffName = ResourceParser.GetBuffName(buff.BuffID);
        }

        public string BuffName
        {
            get;
            set;
        }

        /// <summary>
        /// Dot威力×効果時間で算出される威力
        /// </summary>
        /// <param name="_time"></param>
        /// <returns></returns>
        public int GetTotalDotPower(DateTime _time)
        {
            if (DotBuff == null)
                return 0;
            DateTime time = IsFinalized?endTime:_time;
            double sec = Math.Min(Math.Ceiling((time - startTime).TotalSeconds), Math.Ceiling(Buff.TimeLeft));
            return (int)Math.Round(DotBuff.DotIryoku * Math.Round(sec / 3),0);
        }

        /// <summary>
        /// バフによる効果
        /// </summary>
        /// <returns></returns>
        public float GetBuffEffectADDRate()
        {
            float addrate = 0;
            foreach (BUFF b in SrcEnt.Buffs.Where(obj=>obj.BuffID>0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                addrate += be.GetSrcAddRate((int)SrcEnt.Job, SrcEnt.Level);
            }
            return addrate;
        }

        /// <summary>
        /// バフによる効果
        /// </summary>
        /// <returns></returns>
        public float GetBuffEffectMulRate()
        {
            float mulrate = 1.0F;
            foreach (BUFF b in SrcEnt.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                mulrate *= be.GetSrcMulRate((int)SrcEnt.Job, SrcEnt.Level);
            }
            return mulrate;
        }

        /// <summary>
        /// バフ効果を加味したパワー
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public int GetTotalDotPowerAddBUFF(DateTime time)
        {
            int totalPower = GetTotalDotPower(time);
            return (int)(GetRate()* totalPower);
        }

        public float GetRate()
        {
            return (1 + GetBuffEffectADDRate()) * GetBuffEffectMulRate();
        }
    }

    public class BuffPerson
    {
        string pcname;
        public List<BUFFSnap> bufflist = new List<BUFFSnap>();
        public List<AreaOfEffect> aoelist = new List<AreaOfEffect>();
        public DateTime LastAoETime;

        public BuffPerson(string name)
        {
            pcname = name;
        }

        public DateTime GetLastTime(DateTime now)
        {
            if(bufflist.ToArray().Count(obj=>!obj.IsFinalized)>0)
            {
                return now;
            }
            DateTime last = DateTime.MinValue;
            foreach (BUFFSnap snap in bufflist.ToArray())
            {
                if (snap.DotBuff == null) continue;
                if (last < snap.endTime)
                {
                    last = snap.endTime;
                }
            }
            return last;
        }

        /// <summary>
        /// トータル威力
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public int GetTotalDoTPower(DateTime time)
        {
            int power=0;
            foreach (BUFFSnap bs in bufflist.ToArray())
            {
                power += bs.GetTotalDotPowerAddBUFF(time);
            }
            return power;
        }
        /// <summary>
        /// トータル威力　対象への
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public int GetTotalDoTPowerByDest(DateTime time,string destname)
        {
            int power = 0;
            foreach (BUFFSnap bs in bufflist.ToArray().Where(obj=>obj.DestEnt!=null && obj.DestEnt.Name==destname))
            {
                power += bs.GetTotalDotPowerAddBUFF(time);
            }
            return power;
        }

        /// <summary>
        /// AoEのトータル威力　
        /// </summary>
        /// <returns></returns>
        public int GetTotalAoEPower()
        {
            int power = 0;
            foreach (AreaOfEffect aoe in aoelist.ToArray())
            {
                power +=aoe.GetPower();
            }
            return power;
        }

        /// <summary>
        /// AoEのトータル威力　
        /// </summary>
        /// <returns></returns>
        public int GetTotalAoEPowerByDest(string destname)
        {
            int power = 0;
            foreach (AreaOfEffect aoe in aoelist.ToArray().Where(obj=>obj.Dest!=null && obj.Dest.Name==destname))
            {
                power += aoe.GetPower();
            }
            return power;
        }

        public BUFFSnap[] GetBuffSnaps()
        {
            return bufflist.ToArray();
        }

        public void UpdateAoE(Entity[] entities, DateTime time)
        {
            if (LastAoETime.AddSeconds(3) < time)
            {
                if (LastAoETime.AddSeconds(6) < time)
                {
                    LastAoETime = time;
                }
                else
                {
                    LastAoETime = LastAoETime.AddSeconds(3);
                }
                List<Entity> DmgEntList = new List<Entity>();
                BUFFSnap[] buffsnaps = GetBuffSnaps();
                int count = 0;
                foreach (AreaOfEffectAction aoe in AreaOfEffeceProvider.GetAreaOfEffects())
                {
                    foreach (BUFFSnap aoebuffsnap in buffsnaps.Where(obj =>obj.Buff.BuffID == aoe.BUFFID && !obj.IsFinalized))
                    {
                        foreach (Entity ent in FindEntityAt(aoebuffsnap.SrcEnt.AoE_X, aoebuffsnap.SrcEnt.AoE_Y, 5, entities))
                        {
                            if (ent.MobType == TYPE.Mob && ent.CurrentHP > 0 && ent.CurrentHP < ent.MaxHP && ent.Aggro>0)
                            {
                                if (DmgEntList.Count(obj => obj.Address == ent.Address) > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine("AoE　被りID");
                                }
                                else
                                {
                                    aoelist.Add(new AreaOfEffect(time, aoe, aoebuffsnap, ent));
                                    DmgEntList.Add(ent);
                                    System.Diagnostics.Debug.WriteLine("{4}:{3} {0} {1} {2}", time.ToString("hh:mm:ss"), ent.Name, ent.NPCId, this.pcname, count++);
                                }
                            }
                        }
                        double timeleft =Math.Ceiling(aoebuffsnap.Buff.TimeLeft);
                    }
                }
            }
        }


        public void UpdateBuffList(Entity[] entities, DateTime time)
        {
            foreach (Entity ent in entities)
            {
                if (ent.Name == pcname)
                {
                    BUFFSnap[] newsnaps = BuffManager.GetBUFFSnaps(ent, entities, time);
                    foreach (BUFFSnap newbs in newsnaps)
                    {//新規のものを追加
                        foreach (BUFFSnap bs in bufflist.Where(snap => snap.Buff.BuffID == newbs.Buff.BuffID
                            && snap.Buff.BuffProvider == newbs.Buff.BuffProvider
                            && snap.DestEnt.NPCId == newbs.DestEnt.NPCId && !snap.IsFinalized))
                        {
                            //経過時間
                            double secs = (time - bs.startTime).TotalSeconds;
                            double secs2 = bs.Buff.TimeLeft - newbs.Buff.TimeLeft;
                            if (bs.Buff.TimeLeft==0.0F || Math.Abs(secs - secs2) < 3)
                            {//同じもの
                                goto next;
                            }
                            else
                            {//上書きされた
                                bs.IsFinalized = true;
                                bs.endTime = time;
                            }
                        }
                        //新規追加
                        bufflist.Add(newbs);
                    next:
                        continue;
                    }
                    foreach (BUFFSnap bs in bufflist.Where(snap=>snap.IsFinalized==false))
                    {//完了しているものをファイナライズ
                        if (0 < newsnaps.Count(snap => snap.DestEnt.NPCId == bs.DestEnt.NPCId
                            && snap.Buff.BuffID == bs.Buff.BuffID
                            && snap.Buff.BuffProvider == bs.Buff.BuffProvider))
                        {
                            continue;
                        }
                        //時間が経過していないものを省く
                        if ((time - bs.startTime).TotalSeconds < 0.3)
                        {
                            continue;
                        }

                        bs.IsFinalized = true;
                        bs.endTime = time;
                    }
                    break;
                }
            }
        }

        private Entity[] FindEntityAt(float x, float y, float range, Entity[] entities)
        {
            List<Entity> list = new List<Entity>();
            foreach (Entity ent in entities)
            {
                double dist = Math.Sqrt((x - ent.X) * (x - ent.X) + (y - ent.Y) * (y - ent.Y));
                if (dist < range)
                {
                    list.Add(ent);
                }
            }
            return list.ToArray();
        }

    }

    public class BuffManager
    {

        public static BUFFSnap[] GetBUFFSnaps(Entity self , Entity[] entities,DateTime time)
        {
            List<BUFFSnap> bsnaps = new List<BUFFSnap>();

            foreach (Entity ent in entities)
            {
                foreach (BUFF b in ent.Buffs)
                {
                    if (b.BuffProvider == self.PCId)
                        bsnaps.Add(new BUFFSnap(ent,self, b, time));
                }
            }
            return bsnaps.ToArray();
        }
    }
}