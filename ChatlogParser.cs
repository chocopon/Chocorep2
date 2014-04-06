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
        TimeSpan DefaultSpan = new TimeSpan(0, 0, 0, 0, 600);

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

        public void Parse(DateTime time, FFXIVLog log, EntitiesSnap[] entitysnaps, out object res)
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

            Entity[] entities = GetEntitiesFromSnaps(entitysnaps,time,DefaultSpan);

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
                    res = AddMyHitDamage(time, dest, num, numrate, crit, entitysnaps);
                    //res = AddPTMemberHitDamage(time, dest, num, numrate, crit, entitysnaps);//検証用
                }
                else if(ptmember&&(log.LogType==0x12||log.LogType==0x13))
                {
                    res = AddPTMemberHitDamage(time, dest, num, numrate, crit, entitysnaps);
                }
                else if (pets)
                {
                    res = AddPetHitDamage(time, dest, num, numrate, crit, entitysnaps);
                }
                if (res == null)
                {
                    System.Diagnostics.Debug.WriteLine("{0}:{1}", log.LogTypeHexString, log.LogBodyReplaceTabCode);
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
                    if((currenttarget==null||selfae.Count(obj=>obj==action)>0))
                    {
                        x = src_ent.X;
                        y = src_ent.Y;
                        z = src_ent.Z;
                    }
                    else{
                        x = currenttarget.X;
                        y = currenttarget.Y;
                        z = currenttarget.Z;
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
        private object AddMyHitDamage(DateTime time, string dest, int dmg, int dmgrate, bool crit, EntitiesSnap[] entitysnaps)
        {
           Entity[] entities = GetEntitiesFromSnaps(entitysnaps, time, DefaultSpan);
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
                string action = MySelf.lastDDAction.ActionName;
                if (action == "ファイア" || action == "ファイラ" || action == "ファイガ" || action == "フレア" ||
                    action == "ブリザド" || action == "ブリザラ" || action == "ブリザガ" || action == "フリーズ")
                {//1.8秒
                    entities = GetEntitiesFromSnaps(entitysnaps, time, new TimeSpan(0, 0, 0, 1, 800));
                    srcEnt = Helper.FindEntityByName(MySelf.Name, entities);
                }
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
        private object AddPTMemberHitDamage(DateTime time, string dest, int dmg, int dmgrate, bool crit,EntitiesSnap[] entitysnaps)
        {
            List<DDPerson> personlist = new List<DDPerson>();
            Entity[] entities = GetEntitiesFromSnaps(entitysnaps, time,DefaultSpan);

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
            foreach (DDPerson person in ddpersonList.Where(obj => obj.lastDDAction != null &&obj.PersonType== PersonType.PTMember))
            {
                ActionDone[] dones = person.GetActionDones();
                if (dones[dones.Length - 1].timestamp.AddSeconds(1) < time)
                {//時間切れ
                    person.lastDDAction = null;
                    continue;
                }
                if (dmgrate > 0)
                {//ダメージRATEが0以上（条件で威力が変化するアクションのみ）
                    if (person.lastDDAction.PowerMax == person.lastDDAction.PowerMin)
                    {
                        continue;
                    }
                }
                //if (person.lastDDAction.Area && person.DestEntList.Count(obj => obj.Name == dest) == 0)
                //{//範囲で対象の名前のもぶがない場合
                //    continue;
                //}
                personlist.Add(person);
            }
            //ソートsssss
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
                string action = person.lastDDAction.ActionName;
                if (action == "ファイア" || action == "ファイラ" || action == "ファイガ" || action == "フレア" ||
                    action == "ブリザド" || action == "ブリザラ" || action == "ブリザガ" || action == "フリーズ")
                {//1.8秒
                    entities = GetEntitiesFromSnaps(entitysnaps, time, new TimeSpan(0, 0, 0, 1, 800));
                }

                Entity srcEnt = Helper.FindEntityByName(person.Name, entities);
                Entity destEnt = null;

                if (person.lastDDAction.Area)
                {
                    foreach (Entity _ent in person.DestEntList.Where(obj => obj.Name == dest))
                    {
                        destEnt = _ent;
                        break;
                    }
                    if (destEnt == null)
                    {//範囲内のモブ取得ミスというか少しのずれはあるかも
                        System.Diagnostics.Debug.WriteLine("範囲攻撃の対象モブを使い切っています。名前から取得します。");
                        destEnt = Helper.FindEntityByName(dest, entities);
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

                System.Diagnostics.Debug.WriteLine("In 複数");
                DDPerson person = null;
                Entity srcEnt = null;
                Entity destEnt = null;
                //それぞれのダメージ予測を計算する
                List<object[]> dmgsetlist = new List<object[]>();

                foreach (DDPerson _person in personlist)
                {
                    string action = _person.lastDDAction.ActionName;
                    if (action == "ファイア" || action == "ファイラ" || action == "ファイガ" || action == "フレア" ||
                        action == "ブリザド" || action == "ブリザラ" || action == "ブリザガ" || action == "フリーズ")
                    {//1.8秒
                        entities = GetEntitiesFromSnaps(entitysnaps, time, new TimeSpan(0, 0, 0, 1, 800));
                    }
                    Entity _srcEnt = Helper.FindEntityByName(_person.Name, entities);

                    Entity _destEnt = null;

                    float? predmg = null;
                    if (_person.lastDDAction.Area)
                    {
                        foreach (Entity _ent in _person.DestEntList.Where(obj => obj.Name == dest))
                        {
                            _destEnt = _ent;
                        }
                        //この範囲攻撃で与えたダメージを取得する
                        foreach (ActionDD _dd in _person.GetActionDDs().Where(obj => obj.actionName == _person.lastDDAction.ActionName && obj.timestamp >= time.AddSeconds(-1)))
                        {
                            System.Diagnostics.Debug.WriteLine("既存範囲攻撃ダメージあり {0} {1} > {2} {3}", _person.Name, _dd.actionName, _dd.Dest.Name,_dd.damage);
                            predmg = _dd.damage/(_dd.IsCritical?1.5F:1);
                        }
                    }
                    else
                    {
                        _destEnt = Helper.FindEntityByID(_srcEnt.TargetId, entities);
                    }
                    if (_destEnt == null)
                    {
                        _destEnt = Helper.FindEntityByName(dest, entities);
                    }
                    float _dmg = 0;
                    if (predmg != null)
                    {  //この範囲攻撃で与えた既存ダメージ
                        _dmg = (float)((crit?1.5F:1)* predmg);
                    }
                    else
                    {
                        //予測ダメージ
                        float dmgbase = _person.CalcDamageBase();
                        ActionDD dd = new ActionDD(time, _person.lastDDAction.ActionName, _destEnt, _srcEnt, dmg, dmgrate, crit);
                        float critrate = crit ? 1.5F : 1.0F;
                        float buffeffect = dd.GetBuffsEffectRate();
                        _dmg = critrate * (dmgrate > 0 ? _person.lastDDAction.PowerMax : _person.lastDDAction.PowerMin) * dmgbase * buffeffect;
                    }
                    float zure = Math.Abs(dmg - _dmg) / _dmg;
                    System.Diagnostics.Debug.WriteLine("{0}「{1}」dmg{2} _dmg{3} zure{4}", _person.Name, _person.lastDDAction.ActionName, dmg, _dmg, zure);
                    dmgsetlist.Add(new object[] { _person, _dmg ,zure,_srcEnt,_destEnt});
                }
                //誤差が許容範囲のもの
                List<object[]> allowreslist = new List<object[]>();
                //誤差が最少のもの
                object[] minres = null;
                float minzure = float.MaxValue;
                foreach (object[] res in dmgsetlist)
                {
                    float zure = (float)res[2];
                    if (zure< 0.06)
                    {
                        allowreslist.Add(res);
                    }
                    if (zure < minzure)
                    {
                        minres = res;
                        minzure = zure;
                    }
                }
                if (allowreslist.Count > 0)
                {//許容範囲のが2つ以上
                    System.Diagnostics.Debug.WriteLine("許容範囲が{0}個", allowreslist.Count);
                    foreach (object[] res in allowreslist)
                    {
                        person = (DDPerson)res[0];
                        destEnt = (Entity)res[4];
                        srcEnt = (Entity)res[3];
                        if (person.lastDDAction.Area)
                        {
                            if (person.DestEntList.Count(obj => obj.Name == dest) > 0)
                            {//採用
                                System.Diagnostics.Debug.WriteLine("範囲攻撃で対象が残っているものを採用");
                                break;
                            }
                        }
                        else
                        {//単体攻撃を採用
                            System.Diagnostics.Debug.WriteLine("単体攻撃を採用");
                            break;
                        }
                    }
                }
                else
                {//許容範囲の攻撃がないか１つ
                    System.Diagnostics.Debug.WriteLine("許容範囲の攻撃がないか１つなので誤差最小を採用");
                    person =(DDPerson) minres[0];
                    destEnt =(Entity) minres[4];
                    srcEnt = (Entity)minres[3];
                }

                if (person.lastDDAction.Area)
                {//範囲処理の後処理
                    person.DestEntList.Remove(destEnt);
                }
                if (destEnt == null)
                {
                    System.Diagnostics.Debug.WriteLine("対象のEntityがnull");
                    return null;
                }
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
                System.Diagnostics.Debug.WriteLine("ミスした対象がありません。");
                System.Diagnostics.Debug.WriteLine("  PTMEMBER ⇒ {0}にミス",dest);
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
                {//a
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

#region Helper Functions
        private  Entity[] GetEntitiesFromSnaps(EntitiesSnap[] entitysnaps, DateTime time, TimeSpan span)
        {
            EntitiesSnap entsnap = entitysnaps[0];
            //for (int i = 0; i < entitysnaps.Length; i++)
            //{
            //    System.Diagnostics.Debug.WriteLine(entitysnaps[i].timestamp.ToString("o"));
            //}

           // System.Diagnostics.Debug.WriteLine("{0}", (time - entsnap.timestamp).TotalMilliseconds);

            for (int i = 0; i < entitysnaps.Length; i++)
            {
                if (entitysnaps[i].timestamp.Add(span) < time)
                {
                    entsnap = entitysnaps[i];
                    break;
                }
            }
            return entsnap.Entities;
        }
#endregion

        string[] selfae = new string[] { "ホーリー", "サークル・オブ・ドゥーム", "シュトルムヴィント", "リング・オブ・ソーン","ブリザラ"};
    }
}
