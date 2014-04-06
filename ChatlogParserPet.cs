using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ffxivlib;

namespace PrecisionRep
{
    public partial class ChatlogParser
    {
        #region Action Hit MISS
        /// <summary>
        /// ダメージ
        /// </summary>
        /// <param name="src"></param>
        /// <param name="action"></param>
        private object AddPetHitDamage(DateTime time, string dest, int dmg, int dmgrate, bool crit, EntitiesSnap[] entitisnaps)
        {
            Entity[] entities = GetEntitiesFromSnaps(entitisnaps, time, DefaultSpan);
            List<DDPerson> personlist = new List<DDPerson>();
            foreach (DDPerson person in ddpersonList.Where(obj => obj.lastDDAction != null && obj.PersonType == PersonType.Pet))
            {
                ActionDone[] dones = person.GetActionDones();
                if (dones[dones.Length - 1].timestamp.AddSeconds(1) < time)
                {//時間切れ
                    person.lastDDAction = null;
                    continue;
                }
                if (person.lastDDAction.Area && person.DestEntList.Count(obj => obj.Name == dest) == 0)
                {//範囲でリストがないものは除外
                    continue;
                }
                personlist.Add(person);
            }

            if (personlist.Count == 0)
            {//ない
                //光輝の盾
                foreach (DDPerson p in ddpersonList)
                {
                    Entity srcEnt = Helper.FindEntityByName(p.Name, entities);
                    if (srcEnt == null)
                        continue;
                    Entity destEnt = Helper.FindEntityByID(srcEnt.TargetId, entities);
                    if (srcEnt.Buffs.Count(obj => obj.BuffID == 89) > 0)
                    {
                        AddDamage adddmg = p.AddAddDamage(time, "光輝の盾", destEnt, srcEnt, dmg, crit);
                        return adddmg;
                    }
                }
                return null;
            }
            else if (personlist.Count == 1)
            {//ひとり
                DDPerson person = personlist[0];
                Entity srcEnt = Helper.FindEntityByName(person.Name, entities);
                Entity destEnt = null;

                if (person.lastDDAction.Area)
                {

                    foreach (Entity _ent in person.DestEntList.Where(obj => obj.Name == dest))
                    {
                        destEnt = _ent;
                        person.DestEntList.Remove(destEnt);
                        break;
                    }
                }
                else
                {
                    destEnt = Helper.FindEntityByID(srcEnt.TargetId, entities);
                    if (destEnt == null)
                    {
                        destEnt = Helper.FindEntityByName(dest, entities);
                    }
                }
                if (destEnt == null) return null;
                ActionDD dd = person.AddActionDD(time, person.lastDDAction.ActionName, destEnt, srcEnt, dmg, dmgrate, crit);
                if (!person.lastDDAction.Area)
                {
                    person.lastDDAction = null;

                }
                return dd;
            }
            else
            {//複数
                double minZure = double.MaxValue;
                DDPerson person = null;
                Entity srcEnt = null;
                Entity destEnt = null;
                foreach (DDPerson p in personlist)
                {
                    Entity _srcEnt = Helper.FindEntityByName(p.Name, entities);

                    Entity _destEnt = null;
                    if (p.lastDDAction.Area)
                    {

                        foreach (Entity _ent in p.DestEntList.Where(obj => obj.Name == dest))
                        {
                            _destEnt = _ent;
                        }
                    }
                    else
                    {
                        _destEnt = Helper.FindEntityByID(_srcEnt.TargetId, entities);
                        if (_destEnt == null)
                        {
                            _destEnt = Helper.FindEntityByName(dest, entities);
                        }
                    }
                    if (_destEnt == null) continue;

                    float dmgbase = p.CalcDamageBase();
                    ActionDD dd = new ActionDD(time, p.lastDDAction.ActionName, _destEnt, _srcEnt, dmg, dmgrate, crit);
                    float critrate = crit ? 1.5F : 1.0F;
                    float buffeffect = dd.GetBuffsEffectRate();
                    float _dmg = critrate * p.lastDDAction.PowerMin * dmgbase * buffeffect;
                    float zure = Math.Abs(dmg - _dmg) / _dmg;

                    if (zure < minZure)
                    {
                        minZure = zure;
                        person = p;
                        destEnt = _destEnt;
                        srcEnt = _srcEnt;
                        if (zure < 0.2) break;
                    }
                }
                if (person == null)
                {
                    Console.WriteLine("エラー");
                    return null;
                }
                if (person.lastDDAction.Area)
                {
                    person.DestEntList.Remove(destEnt);
                }
                if (destEnt == null) return null;
                ActionDD actiondd = person.AddActionDD(time, person.lastDDAction.ActionName, destEnt, srcEnt, dmg, dmgrate, crit);
                if (!person.lastDDAction.Area)
                {
                    person.lastDDAction = null;
                }
                return actiondd;
            }
        }

        /// <summary>
        /// ミス
        /// </summary>
        /// <param name="src"></param>
        /// <param name="action"></param>
        private object AddPetActionMiss(DateTime time, string dest, bool ineffective, Entity[] entities)
        {
            List<DDPerson> personlist = new List<DDPerson>();
            foreach (DDPerson person in ddpersonList.Where(obj => obj.lastDDAction != null && obj.PersonType == PersonType.Pet))
            {
                if (person.lastDDAction.Area && person.DestEntList.Count(obj => obj.Name == dest) == 0)
                {//範囲でリストがないものは除外
                    continue;
                }
                personlist.Add(person);
            }
            personlist.Sort(delegate(DDPerson a, DDPerson b) { return a.lastDDAction.Area.CompareTo(b.lastDDAction.Area); });
            if (personlist.Count == 0)
            {//ない
                Console.WriteLine("ミスした対象がありません。エラー");
                return null;
            }
            else if (personlist.Count == 1)
            {//ひとり
                DDPerson person = personlist[0];
                Entity srcEnt = Helper.FindEntityByName(person.Name, entities);
                Entity destEnt = null;

                if (person.lastDDAction.Area)
                {
                    foreach (Entity _ent in person.DestEntList.Where(obj => obj.Name == dest))
                    {
                        destEnt = _ent;
                        person.DestEntList.Remove(destEnt);
                        break;
                    }
                }
                else
                {
                    destEnt = Helper.FindEntityByID(srcEnt.TargetId, entities);
                    if (destEnt == null)
                    {
                        destEnt = Helper.FindEntityByName(dest, entities);
                    }
                }
                ActionMiss miss = person.AddActioMiss(time, destEnt, srcEnt,person.lastDDAction==null?"":person.lastDDAction.ActionName, ineffective);
                if (!person.lastDDAction.Area)
                {
                    person.lastDDAction = null;

                }
                return miss;
            }
            else
            {//複数
                double minspan = double.MaxValue;
                DDPerson person = null;
                Entity srcEnt = null;
                Entity destEnt = null;
                foreach (DDPerson p in personlist)
                {
                    Entity _srcEnt = Helper.FindEntityByName(p.Name, entities);

                    Entity _destEnt = null;
                    if (p.lastDDAction.Area)
                    {

                        foreach (Entity _ent in p.DestEntList.Where(obj => obj.Name == dest))
                        {
                            _destEnt = _ent;
                        }
                    }
                    else
                    {
                        _destEnt = Helper.FindEntityByID(_srcEnt.TargetId, entities);
                        if (_destEnt == null)
                        {
                            _destEnt = Helper.FindEntityByName(dest, entities);
                        }
                    }
                    if (_destEnt == null) continue;

                    ActionDone[] actions = p.GetActionDones();
                    ActionDone lastactiondone = actions[actions.Length - 1];
                    DateTime actiontime = lastactiondone.timestamp;
                    double span = (time - actiontime).TotalSeconds;


                    if (span < minspan)
                    {
                        minspan = span;
                        person = p;
                        destEnt = _destEnt;
                        srcEnt = _srcEnt;
                    }
                }
                if (person == null)
                {
                    Console.WriteLine("エラー");
                    return null;
                }
                if (person.lastDDAction.Area)
                {
                    person.DestEntList.Remove(destEnt);
                }
                ActionMiss actionmiss = person.AddActioMiss(time, destEnt, srcEnt, person.lastDDAction == null ? "" : person.lastDDAction.ActionName, ineffective);
                if (!person.lastDDAction.Area)
                {
                    person.lastDDAction = null;
                }
                return actionmiss;
            }
        }

        #endregion
    }
}
