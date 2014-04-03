using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ffxivlib;

namespace PrecisionRep
{
    public class DDPerson
    {
        Regex destRegex = new Regex(@"([^\s⇒！]+\s?[^\s]+)に");
        Regex srcRegex = new Regex(@"(\w.+)の");
        Regex numRegex = new Regex(@"(?<num>\d+)ダメージ|(?<num>\d+)\((?<rate>[+-]\d+)%\)ダメージ");
        Regex actionRegex = new Regex(@"「(.+)」");

        public string Name;
        public PersonType PersonType;
        public int Job;

        public List<AutoAttackDD> _AAList = new List<AutoAttackDD>();
        public List<ActionDone> _ActionList = new List<ActionDone>();
        public List<ActionDD> _ActionDDList = new List<ActionDD>();
        public List<AutoAttackMiss> _AAMissList = new List<AutoAttackMiss>();
        public List<ActionMiss> _ActionMissList = new List<ActionMiss>();
        public List<AddDamage> _AddedDmgList = new List<AddDamage>();

        /// <summary>
        /// ひとつのアクションのログ
        /// </summary>
        public List<string> actionLogList = new List<string>();

        public DDAction lastDDAction;
        /// <summary>
        /// 範囲の敵ｓ
        /// </summary>
        public List<Entity> DestEntList = new List<Entity>();
        List<ActionDD> lastActionDDList = new List<ActionDD>();
        public bool addedopen;

        private void OpenAction(string action)
        {
            int id = ResourceParser.GetActionID(action);
            lastDDAction = DDAction.GetDDAction(id);
            DestEntList.Clear();
        }

        //private void CloseAction()
        //{
        //    lastDDAction = null;
        //}

        DateTime _LastTime;

        public DateTime GetLastTime()
        {
            return _LastTime;
        }

        public AutoAttackDD[] GetAutoAttackDDs()
        {
            return _AAList.ToArray();
        }

        public ActionDone[] GetActionDones()
        {
            return _ActionList.ToArray();
        }


        public ActionDD[] GetActionDDs()
        {
            return _ActionDDList.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AutoAttackMiss[] GetAutoAttackMissies()
        {
            return _AAMissList.ToArray();
        }

        public ActionMiss[] GetActionMissies()
        {
            return _ActionMissList.ToArray();
        }

        /// <summary>
        /// ダメージ総計
        /// </summary>
        /// <returns></returns>
        public int GetTotalDamage()
        {
            return GetTotalActionDD() + GetTotalAutoAttackD() + GetTotalAddDamages();
        }


        #region AddDamage（追加ダメージ)
        /// <summary>
        /// 全ての追加効果ダメージを取得する
        /// </summary>
        /// <returns></returns>
        public AddDamage[] GetAddDamages()
        {
            return _AddedDmgList.ToArray();
        }

        /// <summary>
        /// 追加ダメージ累計
        /// </summary>
        /// <returns></returns>
        public int GetTotalAddDamages()
        {
            int sum = 0;
            foreach (AddDamage add in GetAddDamages())
            {
                sum += add.damage;
            }
            return sum;
        }

        /// <summary>
        /// 追加ダメージ累計 対象にたいする
        /// </summary>
        /// <param name="destname"></param>
        /// <returns></returns>
        public int GetTotalAddDamagesByDest(string destname)
        {
            int sum = 0;
            foreach (AddDamage add in GetAddDamages().Where(obj=>obj.Dest!=null && obj.Dest.Name==destname))
            {
                sum += add.damage;
            }
            return sum;
        }

        /// <summary>
        /// 追加ダメージカウント
        /// </summary>
        /// <returns></returns>
        public int GetAddDamageCount()
        {
            return _AddedDmgList.Count;
        }
        /// <summary>
        /// 追加ダメージカウント 対象に対する
        /// </summary>
        /// <param name="destname"></param>
        /// <returns></returns>
        public int GetAddDamageCountByDest(string destname)
        {
            return _AddedDmgList.ToArray().Count(obj=>obj.Dest.Name==destname);
        }

        #endregion




        #region AutoAttack
        /// <summary>
        /// AutoAttackのダメージ総計
        /// </summary>
        /// <returns></returns>
        public int GetTotalAutoAttackD()
        {
            int sum = 0;
            foreach (AutoAttackDD aa in GetAutoAttackDDs())
            {
                sum += aa.damage;
            }
            return sum;
        }

        /// <summary>
        /// AutoAttackのダメージ総計
        /// </summary>
        /// <param name="destname">対象の名前</param>
        /// <returns></returns>
        public int GetTotalAutoAttackDByDest(string destname)
        {
            int sum = 0;
            foreach (AutoAttackDD aa in GetAutoAttackDDs().Where(obj=>obj.Dest!=null &&obj.Dest.Name == destname))
            {
                sum += aa.damage;
            }
            return sum;
        }


        /// <summary>
        /// AutoAttack回数
        /// </summary>
        /// <returns></returns>
        public int GetAACount()
        {
            return _AAList.Count;
        }

        /// <summary>
        /// AutoAttack回数 対象に対する
        /// </summary>
        /// <param name="destname"></param>
        /// <returns></returns>
        public int GetAACountByDest(string destname)
        {
            return _AAList.ToArray().Count(obj=>obj.Dest.Name==destname);
        }
        #endregion

        /// <summary>
        /// アクションダメージ回数
        /// </summary>
        /// <returns></returns>
        public int GetActionDDCount()
        {
            return _ActionDDList.Count;
        }

        /// <summary>
        /// アクションダメージ回数　対象に対する
        /// </summary>
        /// <param name="destname"></param>
        /// <returns></returns>
        public int GetActionDDCountByDest(string destname)
        {
            return _ActionDDList.ToArray().Count(obj=>obj.Dest.Name==destname);
        }

        /// <summary>
        /// アクションの回数
        /// </summary>
        /// <returns></returns>
        public int GetActionCount()
        {
            return _ActionList.Count;
        }

        #region アクション

        /// <summary>
        /// アクションによるダメージ総計
        /// </summary>
        /// <returns></returns>
        public int GetTotalActionDD()
        {
            int sum = 0;
            foreach (ActionDD dd in GetActionDDs())
            {
                sum += dd.damage;
            }
            return sum;
        }
        /// <summary>
        /// アクションによる対象へのダメージ総計
        /// </summary>
        /// <returns></returns>
        public int GetTotalActionDDByDest(string destname)
        {
            int sum = 0;
            foreach (ActionDD dd in GetActionDDs().Where(obj=>obj.Dest!=null && obj.Dest.Name==destname))
            {
                sum += dd.damage;
            }
            return sum;
        }
        #endregion

        public DDPerson(string name, PersonType ptype, int job)
        {
            Name = name;
            PersonType = ptype;
            Job = job;
        }

        #region addlist
        public AutoAttackDD AddAutoAttack(DateTime time, Entity dest,Entity src, int dmg, bool crit)
        {
            var res = new AutoAttackDD(time, dest, src, dmg, crit);
            _AAList.Add(res);
            _LastTime = time;
            return res;
        }

        public ActionDone AddActionDone(DateTime time, Entity src, string action)
        {
            var res = new ActionDone(time, src, action);
            _ActionList.Add(res);
            _LastTime = time;
            return res;
        }

        public ActionDD AddActionDD(DateTime time, string action,Entity dest,Entity src, int dmg, int dmgrate,bool crit)
        {
            if (dest == null)
            {
                throw new Exception("Dest がnull");
            }
            var res = new ActionDD(time, action, dest, src, dmg,dmgrate, crit);
            _ActionDDList.Add(res);
            _LastTime = time;
            return res;
        }
        public AddDamage AddAddDamage(DateTime time, string buffname, Entity dest, Entity src, int dmg, bool crit)
        {
            var res = new AddDamage(time, buffname, dest, src, dmg, crit);
            _AddedDmgList.Add(res);
            _LastTime = time;
            return res;
        }


        public AutoAttackMiss AddAAMiss(DateTime time,Entity dest,Entity src, bool ineffective)
        {
            var res = new AutoAttackMiss(time, dest, src, ineffective);
            _AAMissList.Add(res);
            _LastTime = time;
            return res;
        }
        public ActionMiss AddActioMiss(DateTime time, Entity dest, Entity src,string action, bool ineffective)
        {
            var res = new ActionMiss(time, dest, src,action, ineffective);
            _ActionMissList.Add(res);
            _LastTime = time;
            return res;
        }
        #endregion


        private Entity[] FindEntityAt(float x, float y,float range, Entity[] entities)
        {
            List<Entity> list = new List<Entity>();
            foreach (Entity ent in entities.Where(obj=>obj.CurrentHP>0))
            {
                double dist = Math.Sqrt((x - ent.X) * (x - ent.X) + (y - ent.Y) * (y - ent.Y));
                if (dist < range)
                {
                    list.Add(ent);
                }
            }
            return list.ToArray();
        }

        private Entity FindEntityByName(string name, Entity[] entities,int count=0)
        {
            if (String.IsNullOrEmpty(name))
                return null;
            int c = 0;
            Entity entity = null;
            foreach (Entity ent in entities.Where(ent => ent.Name == name))
            {
                entity = ent;
                if (count == c++)
                {
                    return entity;
                }
            }
            return entity;
        }

        private Entity FindEntityByID(int id, Entity[] entities)
        {
            Entity entity = null;
            foreach (Entity ent in entities.Where(ent => ent.NPCId == id||ent.PCId==id))
            {
                entity = ent;
                return entity;
            }
            return entity;
        }

   
        /// <summary>
        /// 威力１バフなし状態でのダメージを算出
        /// </summary>
        /// <returns></returns>
        public float CalcDamageBase()
        {
            List<float> blist = new List<float>();
            foreach (ActionDD dd in GetActionDDs())
            {
                DDAction ddaction = DDAction.GetDDAction(dd.actionName);
                if (ddaction != null && ddaction.actionID!=29)//ウィズイン除外
                {
                    float critrate = dd.IsCritical ? 1.5F : 1.0F;
                    float buffeffect = dd.GetBuffsEffectRate();
                    float power = dd.damagerate > 0 ? ddaction.PowerMax : ddaction.PowerMin;
                    float rate = dd.damage / power / buffeffect / critrate;
                    blist.Add(rate);
                }
            }

            if (blist.Count == 0)
            {
                if (Job == (int)JOB.SMN || Job == (int)JOB.THM || Job == (int)JOB.BLM)
                {
                    return 2.0F;
                }
                if (Job == (int)JOB.GLD || Job==(int)JOB.PLD ||Job== (int)JOB.MRD ||Job== (int)JOB.WAR)
                {
                    return 1.0F;
                }
                return 1.5F;
            }
            blist.Sort();
            return blist[blist.Count / 2];

        }

        /// <summary>
        /// 威力１バフなし状態でのダメージを算出
        /// </summary>
        /// <returns></returns>
        public float CalcDamageBaseByDest(string destname)
        {
            List<float> blist = new List<float>();
            foreach (ActionDD dd in GetActionDDs().Where(obj=>obj.Dest!=null && obj.Dest.Name==destname))
            {
                DDAction ddaction = DDAction.GetDDAction(dd.actionName);
                if (ddaction != null && ddaction.actionID != 29)//ウィズイン除外
                {
                    float critrate = dd.IsCritical ? 1.5F : 1.0F;
                    float buffeffect = dd.GetBuffsEffectRate();
                    float power = dd.damagerate > 0 ? ddaction.PowerMax : ddaction.PowerMin;
                    float rate = dd.damage / power / buffeffect / critrate;
                    blist.Add(rate);
                }
            }

            if (blist.Count == 0)
            {
                if (Job == (int)JOB.SMN || Job == (int)JOB.THM || Job == (int)JOB.BLM)
                {
                    return 2.0F;
                }
                if (Job == (int)JOB.GLD || Job == (int)JOB.PLD || Job == (int)JOB.MRD || Job == (int)JOB.WAR)
                {
                    return 1.0F;
                }
                return 1.5F;
            }
            blist.Sort();
            return blist[blist.Count / 2];

        }
    }

    public class AutoAttackDD
    {
        public DateTime timestamp;
        public Entity Dest;
        public Entity Src;
        public int damage;
        public bool IsCritical;

        public AutoAttackDD(DateTime time, Entity dest,Entity src, int dmg, bool crit)
        {
            timestamp = time;
            damage = dmg;
            Dest = dest;
            Src = src;
            IsCritical = crit;
        }

        public float GetBuffsEffectRate()
        {
            return (1.0F+GetSrcBuffsEffectRateAdd() + GetDestBuffsEffectRateAdd())
                *GetSrcBuffsEffectRateMul()*GetDestBuffsEffectRateMul();
        }

        public float GetSrcBuffsEffectRateAdd()
        {
            float rateadd = 0;
            foreach (BUFF b in Src.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                rateadd += be.GetSrcAddRate((int)Src.Job, Src.Level);
            }
            return rateadd;
        }

        public float GetDestBuffsEffectRateAdd()
        {
            float rateadd = 0;
            foreach (BUFF b in Dest.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                rateadd +=  be.GetDestAddRate((int)Src.Job, Src.Level);
            }
            return rateadd;
        }

        public float GetSrcBuffsEffectRateMul()
        {
            float ratemul = 1.0F;
            foreach (BUFF b in Src.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                ratemul*= be.GetSrcMulRate((int)Src.Job, Src.Level);
            }
            return ratemul;
        }

        public float GetDestBuffsEffectRateMul()
        {
            float ratemul = 1.0F;
            foreach (BUFF b in Dest.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                ratemul *= be.GetDestMulRate((int)Src.Job, Src.Level);
            }
            return ratemul;
        }
    }

    public class ActionDone
    {
        public DateTime timestamp;
        public Entity Src;
        public string actionname;

        public ActionDone(DateTime time, Entity src,string action)
        {
            timestamp = time;
            Src = src;
            actionname = action;
        }
    }

    public class ActionDD
    {
        public string actionName;
        public DateTime timestamp;
        public Entity Dest;
        public Entity Src;
        public int damage;
        public int damagerate;
        public bool IsCritical;
        public ActionDD(DateTime time, string action,Entity dest,Entity src, int dmg,int dmgrate, bool crit)
        {
            if (dest == null)
            {
            }
            timestamp = time;
            actionName = action;
            Dest = dest;
            Src = src;
            damage = dmg;
            damagerate = dmgrate;
            IsCritical = crit;
        }

        public float GetBuffsEffectRate()
        {
            return (1.0F+ GetSrcBuffsEffectRateAdd() + GetDestBuffsEffectRateAdd())
                * GetSrcBuffsEffectRateMul() * GetDestBuffsEffectRateMul();
        }

        public float GetSrcBuffsEffectRateAdd()
        {
            float rateadd = 0;
            float sprate = 0;
            #region 黒魔道士
            if (actionName == "ファイア" || actionName == "ファイラ" || actionName == "ファイガ" || actionName == "フレア")
            {
                foreach (BUFF b in Src.Buffs.Where(obj => obj.BuffID >= 173 && obj.BuffID <= 178))
                {
                    if (b.BuffID == 173)
                    {//アストラルファイアI
                        sprate = 0.4F;
                    }
                    else if (b.BuffID == 174)
                    {//アストラルファイアII
                        sprate = 0.6F;
                    }
                    else if (b.BuffID == 175)
                    {//アストラルファイアIII
                        sprate = 0.8F;
                    }
                    else if (b.BuffID == 176)
                    {//アンブラルブリザードI
                        sprate = -0.1F;
                    }
                    else if (b.BuffID == 177)
                    {//アンブラルブリザードII
                        sprate = -0.2F;
                    }
                    else if (b.BuffID == 178)
                    {//アンブラルブリザードIII
                        sprate = -0.3F;
                    }
                }
            }
            if (actionName == "サンダー" || actionName == "サンダラ" || actionName == "サンダガ")
            {
                if (Src.Buffs.Count(obj => obj.BuffID == 164) > 0)
                {
                    sprate = 4.5F;
                }
            }
            #endregion
            #region 竜騎士
            if (actionName == "ジャンプ" || actionName == "スパンダイブ")
            {
                foreach (BUFF b in Src.Buffs.Where(obj => obj.BuffID >= 120))
                {//竜槍
                    sprate = 0.5F;
                }
            }
            #endregion
            foreach (BUFF b in Src.Buffs.Where(obj=>obj.BuffID>0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                if (actionName == "スチールサイクロン")
                {//ディフェンダー分無視
                    rateadd += 0.25F;
                }
                rateadd += be.GetSrcAddRate((int)Src.Job, Src.Level);
            }
            return rateadd+sprate;
        }

        public float GetDestBuffsEffectRateAdd()
        {
            float rateadd = 0;
            foreach (BUFF b in Dest.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                rateadd += be.GetDestAddRate((int)Src.Job, Src.Level);
            }
            return rateadd;
        }

        public float GetSrcBuffsEffectRateMul()
        {
            float ratemul = 1.0F;
            foreach (BUFF b in Src.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                ratemul *= be.GetSrcMulRate((int)Src.Job, Src.Level);
            }
            return ratemul;
        }

        public float GetDestBuffsEffectRateMul()
        {
            float ratemul = 1.0F;
            foreach (BUFF b in Dest.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                ratemul *= be.GetDestMulRate((int)Src.Job, Src.Level);
            }
            return ratemul;
        }
    }

    /// <summary>
    /// 追加効果
    /// </summary>
    public class AddDamage
    {
        public DateTime timestamp;
        public Entity Dest;
        public Entity Src;
        public string Buffname;
        public int damage;
        public bool IsCritical;

        public AddDamage(DateTime time,string buffname, Entity dest, Entity src, int dmg, bool crit)
        {
            timestamp = time;
            Buffname = buffname;
            damage = dmg;
            Dest = dest;
            Src = src;
            IsCritical = crit;
        }

        public float GetBuffsEffectRate()
        {
            return (1.0F + GetSrcBuffsEffectRateAdd() + GetDestBuffsEffectRateAdd())
                * GetSrcBuffsEffectRateMul() * GetDestBuffsEffectRateMul();
        }

        public float GetSrcBuffsEffectRateAdd()
        {
            float rateadd = 0;
            foreach (BUFF b in Src.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                rateadd += be.GetSrcAddRate((int)Src.Job, Src.Level);
            }
            return rateadd;
        }

        public float GetDestBuffsEffectRateAdd()
        {
            float rateadd = 0;
            foreach (BUFF b in Dest.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                rateadd += be.GetDestAddRate((int)Src.Job, Src.Level);
            }
            return rateadd;
        }

        public float GetSrcBuffsEffectRateMul()
        {
            float ratemul = 1.0F;
            foreach (BUFF b in Src.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                ratemul *= be.GetSrcMulRate((int)Src.Job, Src.Level);
            }
            return ratemul;
        }

        public float GetDestBuffsEffectRateMul()
        {
            float ratemul = 1.0F;
            foreach (BUFF b in Dest.Buffs.Where(obj => obj.BuffID > 0))
            {
                BuffEffect be = BuffEffectProvider.GetBuffEffect(b.BuffID);
                ratemul *= be.GetDestMulRate((int)Src.Job, Src.Level);
            }
            return ratemul;
        }
    }
    public class AutoAttackMiss
    {
        public DateTime timestamp;
        public Entity Dest;
        public Entity Src;
        public bool Invulnerable;

        public AutoAttackMiss(DateTime time, Entity dest,Entity src, bool ineffective)
        {
            timestamp = time;
            Dest = dest;
            Src = src;
            Invulnerable = ineffective;
        }
    }

    public class ActionMiss
    {
        public bool Invulnerable;
        public DateTime timestamp;
        public string ActionName;
        public Entity Dest;
        public Entity Src;

        public ActionMiss(DateTime time, Entity dest,Entity src,string action, bool ineffective)
        {
            timestamp = time;
            Dest = dest;
            Src = src;
            ActionName = action;
            Invulnerable = ineffective;
        }
    }

    public enum PersonType
    {
        MySelf,
        PTMember,
        Ally,
        Pet,
        Other,
        Enemy
    }

}
