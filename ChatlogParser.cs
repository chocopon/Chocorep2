using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using ffxivlib;
namespace PrecisionRep
{
    public partial class ChatlogParser
    {
        Regex destRegex = new Regex(@"([^\s⇒！]+\s?[^\s]+)に");
        Regex srcRegex = new Regex(@"(\w.+?)の");
        Regex numRegex = new Regex(@"(?<num>\d+)ダメージ|(?<num>\d+)\((?<rate>[+-]\d+)%\)ダメージ");
        Regex actionRegex = new Regex(@"「(.+)」");

        List<DDPerson> ddpersonList;
        DDPerson TuikaDmgPerson;
        DDPerson MySelf;

        public ChatlogParser()
        {
            ddpersonList = new List<DDPerson>();
        }

        public void AddDDPerson(DDPerson person)
        {
            ddpersonList.Add(person);
            if (person.PersonType == PersonType.MySelf)
            {
                MySelf = person;
            }
        }

        public void Parse(DateTime time, FFXIVLog log, Entity[] entities, out object res)
        {
            res = null;
            //自分のログか
            bool myself = log.LogType >= 0x08 && log.LogType <= 0x0B;
            //メンバーのログか
            bool ptmember = log.LogType >= 0x10 && log.LogType <= 0x13;
            //ペットか
            bool pets = log.LogType >= 0x40 && log.LogType <= 0x4C;

            if (!ptmember && !myself && !pets)
            {
                TuikaDmgPerson = null;
                return;
            }
            #region パーシング
            string logbody = log.LogBodyReplaceTabCode;
            Match srcMatch = srcRegex.Match(logbody);
            Match destMatch = destRegex.Match(logbody);
            Match numMatch = numRegex.Match(logbody);
            Match actionMatch = actionRegex.Match(logbody);

            string src = srcMatch.Groups[1].Value;
            string dest = destMatch.Groups[1].Value;


            int num = 0;
            int numrate = 0;
            if (numMatch.Success)
            {
                num = Convert.ToInt32(numMatch.Groups["num"].Value);
                if (numMatch.Groups["rate"].Value != "")
                {
                    numrate = Convert.ToInt32(numMatch.Groups["rate"].Value.Replace("+", ""));
                }
            }
            string action = actionMatch.Groups[1].Value;

            bool crit = logbody.Contains("クリティカル");
            bool ineffective = !logbody.Contains("ミス");

            #endregion

            if (TuikaDmgPerson != null)
                goto actiondmg;

            #region 攻撃(AA)
            if (logbody.Contains("攻撃"))
            {//Auto attack

                if (log.ActionType == 0x29 || log.ActionType == 0xA9)//ダメージ
                {
                    res = AddAAHit(time, src, dest, num, crit, entities);
                    return;
                }
                else if (log.ActionType == 0xAA)//ミス
                {
                    res = AddAAMiss(time, src, dest, ineffective, entities);
                    return;
                }
            }
            #endregion

            #region アクションダメージ
        actiondmg:

            if (log.ActionType == 0x29 || log.ActionType == 0xA9)//ダメージ
            {
                if (!numMatch.Success)
                {//数字がない(敵視アップ等)
                    return;
                }

                if (myself&&(log.LogType==0x0A||log.LogType==0x0B))
                {
                    res = AddMyHitDamage(time, dest, num, numrate, crit, entities);
                    if (res == null)
                    {
                    }
                }
                else if(ptmember&&(log.LogType==0x12||log.LogType==0x13))
                {
                    res = AddPTMemberHitDamage(time, dest, num, numrate, crit, entities);
                }
                else if (pets)
                {
                    res = AddPetHitDamage(time, dest, num, numrate, crit, entities);
                }
                if (res == null)
                {
                    System.Diagnostics.Debug.WriteLine("unable parse:{0}", log.LogBodyReplaceTabCode);
                }
                TuikaDmgPerson = null;
                return;
            }
            #endregion

            #region アクション実行
            if (log.ActionType == 0x2B && logbody.EndsWith("」"))
            {//アクション DONE
                res = AddActionDone(time, src, action, entities);
                return;
            }
            #endregion

            #region アクションミス

            if (log.ActionType == 0xAA||log.ActionType==0x2A)//ミス
            {
                if (logbody.Contains("効果なし") || logbody.Contains("レジスト"))
                {
                    return;
                }
                if (myself)
                {
                    res = AddMyActionMiss(time,dest,ineffective,entities);
                }
                else if (ptmember)
                {
                    res = AddPTMemberActionMiss(time, dest, ineffective, entities);
                }
                else if (pets)
                {
                    res = AddPetActionMiss(time, dest, ineffective, entities);
                }
                return;
            }
            #endregion
        }

        #region Auto Attack
        /// <summary>
        /// AutoAttackHIT
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="dmg"></param>
        /// <param name="crit"></param>
        private AutoAttackDD AddAAHit(DateTime time, string src, string dest, int dmg, bool crit, Entity[] entities)
        {
            foreach (DDPerson person in ddpersonList.Where(obj=>obj.Name == src))
            {
                Entity srcEnt = Helper.FindEntityByName(person.Name, entities);
                Entity destEnt = Helper.FindEntityByID(srcEnt.TargetId, entities);
                if (destEnt == null)
                {
                    Console.WriteLine("対象のEntityがnull");
                    return null;
                }
                var res = person.AddAutoAttack(time, destEnt, srcEnt, dmg, crit);
                //追加効果　忠義の剣状態ならOPEN
                if (srcEnt.Buffs.Count(obj => obj.BuffID == 78) > 0)
                {
                    person.addedopen = true;
                    TuikaDmgPerson = person;
                }
                else
                {
                    person.addedopen = false;
                    TuikaDmgPerson = null;
                }
                return res;
            }
            return null;
        }

        /// <summary>
        /// AutoAttackHIT
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="dmg"></param>
        /// <param name="crit"></param>
        private AutoAttackMiss AddAAMiss(DateTime time, string src, string dest,bool ineffective,Entity[] entities)
        {
            foreach (DDPerson person in ddpersonList.Where(obj=>obj.Name==src))
            {
                Entity srcEnt = Helper.FindEntityByName(person.Name, entities);
                Entity destEnt = Helper.FindEntityByID(srcEnt.TargetId, entities);
                if (destEnt == null)
                {
                    Console.WriteLine("対象のEntityがnull");
                    return null;
                }
                return person.AddAAMiss(time, destEnt, srcEnt, ineffective);
            }
            return null;
        }
        #endregion

        /// <summary>
        /// アクション実行
        /// </summary>
        /// <param name="src"></param>
        /// <param name="action"></param>
        private ActionDone AddActionDone(DateTime time, string src, string action,Entity[] entities)
        {
            Entity src_ent = Helper.FindEntityByName(src, entities);
            if (src_ent == null) return null;

            ActionDone actiondone = new ActionDone(time, src_ent, action);
            foreach (DDPerson person in ddpersonList.Where(obj => obj.Name == src))
            {
                person.AddActionDone(time, src_ent, action);
                person.lastDDAction = DDAction.GetDDAction(action);
                if (person.lastDDAction!=null&& person.lastDDAction.Area)
                {//範囲攻撃なら対象となる敵をピックアップ
                    Entity currenttarget = Helper.FindEntityByID(src_ent.TargetId,entities);
                    float x, y, z;
                    if((currenttarget==null||selfae.Count(obj=>obj==action)==0))
                    {
                        x=currenttarget.X;
                        y=currenttarget.Y;
                        z=currenttarget.Z;
                    }
                    else{
                        x=src_ent.X;
                        y=src_ent.Y;
                        z=src_ent.Z;
                    }
                    person.DestEntList.Clear();
                    float range = 5;
                    if (person.PersonType == PersonType.Pet) range = 4;
                    if (action == "ホーリー") range = 8;
                    person.DestEntList.AddRange(Helper.FindEntityAt(x, y,z,range, entities));
                }
            }
            return actiondone;
        }

        /// <summary>
        /// 自分の出したダメージ
        /// </summary>
        /// <param name="src"></param>
        /// <param name="action"></param>
        private object AddMyHitDamage(DateTime time, string dest, int dmg, int dmgrate, bool crit, Entity[] entities)
        {
            Entity srcEnt = Helper.FindEntityByName(MySelf.Name, entities);
            if (srcEnt == null) return null;

            if (TuikaDmgPerson != null)
            {//忠義の剣
                Entity destEnt = Helper.FindEntityByID(srcEnt.TargetId, entities);
                AddDamage adddmg = TuikaDmgPerson.AddAddDamage(time, "忠義の剣", destEnt, srcEnt, dmg, crit);
                TuikaDmgPerson = null;
                return adddmg;
            }

            if (MySelf.lastDDAction != null)
            {
                Entity destEnt = Helper.FindEntityByID(srcEnt.TargetId, entities);
                if (destEnt == null)
                {
                    destEnt = Helper.FindEntityByName(dest, entities);
                }
                if(destEnt==null) return null;
                ActionDD dd = MySelf.AddActionDD(time, MySelf.lastDDAction.ActionName, destEnt, srcEnt, dmg, dmgrate, crit);
                if (!MySelf.lastDDAction.Area)
                {
                    MySelf.lastDDAction = null;
                }
                return dd;
            }
            else
            {
                //ヴェンジェンス
                Entity destEnt = Helper.FindEntityByID(srcEnt.TargetId, entities);
                if (srcEnt.Buffs.Count(obj => obj.BuffID == 89) > 0)
                {
                    AddDamage adddmg = MySelf.AddAddDamage(time, "ヴェンジェンス", destEnt, srcEnt, dmg, crit);
                    return adddmg;
                }

            }
            //throw new Exception("自分のダメージなのに・・・");
            return null;
        }


        /// <summary>
        /// ダメージ
        /// </summary>
        /// <param name="src"></param>
        /// <param name="action"></param>
        private object AddPTMemberHitDamage(DateTime time, string dest, int dmg, int dmgrate, bool crit, Entity[] entities)
        {
            List<DDPerson> personlist = new List<DDPerson>();
            if (TuikaDmgPerson != null)
            {//忠義の剣
                Entity srcEnt = Helper.FindEntityByName(TuikaDmgPerson.Name, entities);
                if (srcEnt == null) 
                    return null;
                Entity destEnt = Helper.FindEntityByID(srcEnt.TargetId, entities);
                if (destEnt == null)
                    return null;
                AddDamage adddmg = TuikaDmgPerson.AddAddDamage(time, "忠義の剣", destEnt, srcEnt, dmg, crit);
                TuikaDmgPerson = null;
                return adddmg;
            }
            foreach (DDPerson person in ddpersonList.Where(obj => obj.lastDDAction != null))
            {
                ActionDone[] dones = person.GetActionDones();
                if (dones[dones.Length - 1].timestamp.AddSeconds(1) < time)
                {//時間切れ
                    person.lastDDAction = null;
                    continue;
                }
                personlist.Add(person);
            }
            //ソート
            personlist.Sort(delegate(DDPerson a, DDPerson b) { return a.lastDDAction.Area.CompareTo(b.lastDDAction.Area); });

            if (personlist.Count == 0)
            {//ない
                //ヴェンジェンス
                foreach (DDPerson p in ddpersonList)
                {
                    Entity srcEnt = Helper.FindEntityByName(p.Name, entities);
                    if (srcEnt == null)
                        continue;
                    Entity destEnt = Helper.FindEntityByID(srcEnt.TargetId, entities);
                    if (srcEnt.Buffs.Count(obj => obj.BuffID == 89) > 0)
                    {
                        AddDamage adddmg = p.AddAddDamage(time, "ヴェンジェンス", destEnt, srcEnt, dmg, crit);
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
        /// PTメンバーのミス
        /// </summary>
        /// <param name="src"></param>
        /// <param name="action"></param>
        private object AddPTMemberActionMiss(DateTime time, string dest, bool ineffective, Entity[] entities)
        {
            List<DDPerson> personlist = new List<DDPerson>();
            foreach (DDPerson person in ddpersonList.Where(obj => obj.lastDDAction != null && obj.PersonType == PersonType.PTMember))
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
                ActionMiss miss = person.AddActioMiss(time, destEnt, srcEnt, person.lastDDAction == null ? "" : person.lastDDAction.ActionName, ineffective);
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
                    ActionDone lastactiondone = actions[actions.Length-1];
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
                ActionMiss actionmiss = person.AddActioMiss(time, destEnt, srcEnt,person.lastDDAction==null?"":person.lastDDAction.ActionName, ineffective);
                if (!person.lastDDAction.Area)
                {
                    person.lastDDAction = null;
                }
                return actionmiss;
            }
        }

        /// <summary>
        /// 自身のミス
        /// </summary>
        /// <param name="src"></param>
        /// <param name="action"></param>
        private object AddMyActionMiss(DateTime time, string dest, bool ineffective, Entity[] entities)
        {
            Entity srcEnt = Helper.FindEntityByName(MySelf.Name, entities);
            if (srcEnt == null)
            {
                return null;
            }

            Entity destEnt = null;
            if (MySelf.lastDDAction == null)
                return null;

            if (MySelf.lastDDAction.Area)
            {
                foreach (Entity _ent in MySelf.DestEntList.Where(obj => obj.Name == dest))
                {
                    destEnt = _ent;
                    MySelf.DestEntList.Remove(destEnt);
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
            ActionMiss miss = MySelf.AddActioMiss(time, destEnt, srcEnt, MySelf.lastDDAction == null ? "" : MySelf.lastDDAction.ActionName, ineffective);
            if (!MySelf.lastDDAction.Area)
            {
                MySelf.lastDDAction = null;

            }
            return miss;
        }

        string[] selfae = new string[] { "ホーリー", "サークル・オブ・ドゥーム", "シュトルムヴィント", "リング・オブ・ソーン","ブリザラ"};
    }
}
