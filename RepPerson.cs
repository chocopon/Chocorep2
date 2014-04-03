using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ffxivlib;

namespace PrecisionRep
{
    public class RepPerson
    {
        public string name;
        public DDPerson ddperson;
        public BuffPerson buffperson;

        List<EntitiesSnap> EntitiesSnapList;

        /// <summary>
        /// 最後にしようしたダメージの発生するアクション
        /// </summary>
        public DDAction LastDDAction
        {
            get
            {
                return ddperson.lastDDAction;
            }
        }

        private void AddEntitisSnap(DateTime time, Entity[] ents)
        {
            EntitiesSnapList.Insert(0, (new EntitiesSnap(time, ents)));
            for(int i=EntitiesSnapList.Count-1;i>=0;i--)
            {
                if (EntitiesSnapList[i].timestamp < time.AddSeconds(-5))
                {
                    EntitiesSnapList.RemoveAt(i);
                }
            }
        }

        public EntitiesSnap FindEntitiesSnap(DateTime time)
        {
            foreach (EntitiesSnap snap in EntitiesSnapList)
            {
                if (snap.timestamp < time.AddSeconds(-1))
                {
                    return snap;
                }
            }
            return null;
        }


       
        //public RepPerson(Entity ent, PersonType persontype)
        //{
        //    this.name = ent.Name;
        //    PersonType t = persontype;
        //    if(t!= PersonType.MySelf&& t!= PersonType.PTMember)
        //    {
        //        throw new Exception("自身かパーティメンバーしか実装してないっす");
        //    }
        //    ddperson = new DDPerson(name, t,(int)ent.Job);
        //    buffperson = new BuffPerson(name);
        //    EntitiesSnapList = new List<EntitiesSnap>();
        //}

        public RepPerson(string name, PersonType persontype,JOB job)
        {
            if (persontype != PersonType.MySelf && persontype != PersonType.PTMember && persontype!= PersonType.Pet)
            {
                throw new Exception("自身かパーティメンバーかペットで実装してあるっす");
            }
            this.name = name;
            ddperson = new DDPerson(name, persontype, (int)job);
            buffperson = new BuffPerson(name);
            EntitiesSnapList = new List<EntitiesSnap>();
        }

        /// <summary>
        /// バフ状態からDoTを更新
        /// </summary>
        /// <param name="time"></param>
        /// <param name="entities"></param>
        public void UpdateBuff(DateTime time,Entity[] entities)
        {
            AddEntitisSnap(time, entities);
            buffperson.UpdateBuffList(entities, time);
            buffperson.UpdateAoE(entities, time);
        }

        #region AutoAttack
        /// <summary>
        /// AutoAttackのダメージ
        /// </summary>
        /// <returns></returns>
        public int GetAADamage()
        {
            return ddperson.GetTotalAutoAttackD();
        }
        /// <summary>
        /// AutoAttackのダメージ
        /// </summary>
        /// <param name="destname">対象の名前</param>
        /// <returns></returns>
        public int GetAADamageByDest(string destname)
        {
            return ddperson.GetTotalAutoAttackDByDest(destname);
        }
        /// <summary>
        /// AutoAttackの回数
        /// </summary>
        /// <returns></returns>
        public int GetAACount()
        {
            return ddperson.GetAACount();
        }

        /// <summary>
        /// AutoAttackの回数
        /// </summary>
        /// <returns></returns>
        public int GetAACountByDest(string destname)
        {
            return ddperson.GetAACountByDest(destname);
        }

        #endregion

        #region ActionDamage

        /// <summary>
        /// アクションによるダメージ
        /// </summary>
        /// <returns></returns>
        public int GetActionDamage()
        {
            return ddperson.GetTotalActionDD();
        }

        /// <summary>
        /// アクションによるダメージ
        /// </summary>
        /// <param name="destname">対称の名前</param>
        /// <returns></returns>
        public int GetActionDamageByDest(string destname)
        {
            return ddperson.GetTotalActionDDByDest(destname);
        }

        /// <summary>
        /// アクションによるダメージ回数
        /// </summary>
        /// <returns></returns>
        public int GetActionDDCount()
        {
            return ddperson.GetActionDDCount();
        }

        /// <summary>
        /// アクションによるダメージ回数　対象に対する
        /// </summary>
        /// <returns></returns>
        public int GetActionDDCountByDest(string destname)
        {
            return ddperson.GetActionDDCountByDest(destname);
        }

        #endregion

        #region AddDamage（追加ダメージ）
        /// <summary>
        /// 追加ダメージ累計
        /// </summary>
        /// <returns></returns>
        public int GetAddDamage()
        {
            return ddperson.GetTotalAddDamages();
        }

        /// <summary>
        /// 追加ダメージ　対象に対する
        /// </summary>
        /// <param name="destname"></param>
        /// <returns></returns>
        public int GetAddDamageByDest(string destname)
        {
            return ddperson.GetTotalAddDamagesByDest(destname);
        }

        /// <summary>
        /// 追加ダメージカウント
        /// </summary>
        /// <returns></returns>
        public int GetAddDamageCount()
        {
            return ddperson.GetAddDamageCount();
        }

        /// <summary>
        /// 追加ダメージカウント　対象に対する
        /// </summary>
        /// <param name="destname"></param>
        /// <returns></returns>
        public int GetAddDamageCountByDest(string destname)
        {
            return ddperson.GetAddDamageCountByDest(destname);
        }

        #endregion

        #region リミットブレーク
        /// <summary>
        /// リミットブレークによるダメージ
        /// </summary>
        /// <returns></returns>
        public int GetLimitBreakDamage()
        {
            ActionDD[] lbs = ddperson.GetActionDDs().Where(obj =>
                obj.actionName == "ブレイバー" ||
                obj.actionName == "ブレードダンス" ||
                obj.actionName == "ファイナルヘヴン" ||
                obj.actionName == "スカイシャード" ||
                obj.actionName == "プチメテオ" ||
                obj.actionName == "メテオ").ToArray();
            int sum = 0;
            foreach (ActionDD lb in lbs)
            {
                sum += lb.damage;
            }
            return sum;
        }

        /// <summary>
        /// リミットブレークによるダメージ　対象
        /// </summary>
        /// <param name="destname"></param>
        /// <returns></returns>
        public int GetLimitBreakDamageByDest(string destname)
        {
            ActionDD[] lbs = ddperson.GetActionDDs().Where(obj => obj.Dest.Name==destname&&(
                obj.actionName == "ブレイバー" ||
                obj.actionName == "ブレードダンス" ||
                obj.actionName == "ファイナルヘヴン" ||
                obj.actionName == "スカイシャード" ||
                obj.actionName == "プチメテオ" ||
                obj.actionName == "メテオ")).ToArray();
            int sum = 0;
            foreach (ActionDD lb in lbs)
            {
                sum += lb.damage;
            }
            return sum;
        }
        #endregion

        /// <summary>
        /// ミス回数
        /// </summary>
        /// <returns></returns>
        public int GetMissCount()
        {
            int aamiss = ddperson.GetAutoAttackMissies().Count(obj => obj.Invulnerable == false);
            int ddmiss = ddperson.GetActionMissies().Count(obj => obj.Invulnerable == false);
            return aamiss + ddmiss; 
        }

        /// <summary>
        /// ミス回数 対象に対する
        /// </summary>
        /// <returns></returns>
        public int GetMissCountByDest(string dest)
        {
            int aamiss = ddperson.GetAutoAttackMissies().Count(obj => obj.Invulnerable == false && obj.Dest.Name==dest);
            int ddmiss = ddperson.GetActionMissies().Count(obj => obj.Invulnerable == false&&obj.Dest!=null&& obj.Dest.Name==dest);
            return aamiss + ddmiss;
        }




        /// <summary>
        /// 攻撃回数
        /// </summary>
        /// <returns></returns>
        public int GetHitCount()
        {
            return ddperson.GetAACount() + ddperson.GetActionDDCount();
        }

        /// <summary>
        /// 攻撃回数　対象に対する
        /// </summary>
        /// <returns></returns>
        public int GetHitCountByDest(string dest)
        {
            return ddperson.GetAACountByDest(dest) + ddperson.GetActionDDCountByDest(dest);
        }


        /// <summary>
        /// クリティカル数
        /// </summary>
        /// <returns></returns>
        public int GetCritCount()
        {
            int aacrit = ddperson.GetAutoAttackDDs().Count(obj => obj.IsCritical);
            int ddcrit = ddperson.GetActionDDs().Count(obj => obj.IsCritical);
            int addcrit = ddperson.GetAddDamages().Count(obj => obj.IsCritical);
            return aacrit + ddcrit + addcrit;
        }

        /// <summary>
        /// クリティカル数 対象に対する
        /// </summary>
        /// <returns></returns>
        public int GetCritCountByDest(string dest)
        {
            int aacrit = ddperson.GetAutoAttackDDs().Count(obj => obj.IsCritical&&obj.Dest.Name == dest);
            int ddcrit = ddperson.GetActionDDs().Count(obj => obj.IsCritical && obj.Dest.Name == dest);
            int addcrit = ddperson.GetAddDamages().Count(obj => obj.IsCritical && obj.Dest.Name == dest);
            return aacrit + ddcrit + addcrit;
        }


        /// <summary>
        /// クリティカル率　追加効果をふくむ
        /// </summary>
        /// <returns></returns>
        public float GetCritRate()
        {
            float hitcount = GetAACount() + GetActionDDCount() + GetAddDamageCount();
            float critcount = GetCritCount();
            if (hitcount == 0) return 0;
            return critcount / hitcount;
        }

        /// <summary>
        /// クリティカル率　追加効果をふくむ　対象に対する
        /// </summary>
        /// <returns></returns>
        public float GetCritRateByDest(string dest)
        {
            float hitcount = GetAACountByDest(dest) + GetActionDDCountByDest(dest) + GetAddDamageCountByDest(dest);
            float critcount = GetCritCountByDest(dest);
            if (hitcount == 0) return 0;
            return critcount / hitcount;
        }

        public float GetTotalDmg(DateTime time)
        {
            int aadmg = GetAADamage();
            int dddmg = GetActionDamage();
            int adddmg = GetAddDamage();
            int dotdmg = GetDoTDamage(time);
            return aadmg + dddmg + dotdmg;
        }

        public float GetDPS(DateTime _time)
        {
            DateTime firstTime = _time;
            DateTime lastTime = DateTime.MinValue;
            
            foreach(AutoAttackDD aa in ddperson.GetAutoAttackDDs())
            {
                if (aa.timestamp < firstTime)
                {
                    firstTime = aa.timestamp;
                }
                if (aa.timestamp > lastTime)
                {
                    lastTime = aa.timestamp;
                }
            }
            foreach (ActionDD aa in ddperson.GetActionDDs())
            {
                if (aa.timestamp < firstTime)
                {
                    firstTime = aa.timestamp;
                }
                if (aa.timestamp > lastTime)
                {
                    lastTime = aa.timestamp;
                }
            }
            foreach (BUFFSnap aa in buffperson.GetBuffSnaps().Where(obj=>obj.DotBuff!=null))
            {
                if (aa.startTime < firstTime)
                {
                    firstTime = aa.startTime;
                }
                if (aa.endTime > lastTime)
                {
                    lastTime = aa.endTime;
                }
            }
            if (buffperson.GetBuffSnaps().Count(obj => obj.DotBuff != null && !obj.IsFinalized) > 0)
            {
                lastTime = _time;
            }
            float secs = (float)(lastTime - firstTime).TotalSeconds;
            if (secs <= 0)
            {
                return 0;
            }
            float totaldmg = GetTotalDmg(_time);
            return totaldmg / secs;
        }

        public float GetDPSByDest(DateTime _time,string destname)
        {
            DateTime firstTime = _time;
            DateTime lastTime = DateTime.MinValue;

            foreach (AutoAttackDD aa in ddperson.GetAutoAttackDDs().Where(obj=>obj.Dest.Name==destname))
            {
                if (aa.timestamp < firstTime)
                {
                    firstTime = aa.timestamp;
                }
                if (aa.timestamp > lastTime)
                {
                    lastTime = aa.timestamp;
                }
            }
            foreach (ActionDD aa in ddperson.GetActionDDs().Where(obj => obj.Dest.Name == destname))
            {
                if (aa.timestamp < firstTime)
                {
                    firstTime = aa.timestamp;
                }
                if (aa.timestamp > lastTime)
                {
                    lastTime = aa.timestamp;
                }
            }
            foreach (BUFFSnap aa in buffperson.GetBuffSnaps().Where(obj => obj.DotBuff != null &&obj.DestEnt.Name==destname))
            {
                if (aa.startTime < firstTime)
                {
                    firstTime = aa.startTime;
                }
                if (aa.endTime > lastTime)
                {
                    lastTime = aa.endTime;
                }
            }
            if (buffperson.GetBuffSnaps().Count(obj => obj.DotBuff != null && !obj.IsFinalized && obj.DestEnt.Name==destname) > 0)
            {
                lastTime = _time;
            }
            float secs = (float)(lastTime - firstTime).TotalSeconds;
            if (secs <= 0)
            {
                return 0;
            }
            float totaldmg = GetTotalDmg(_time);
            return totaldmg / secs;
        }

        /// <summary>
        /// 直接攻撃のHIT率
        /// </summary>
        /// <returns></returns>
        public float GetHitRate()
        {
            float hitcount = GetHitCount();
            float misscount = GetMissCount();
            if(hitcount+misscount==0)return 0;
            return hitcount / (hitcount + misscount);
        }

        /// <summary>
        /// 直接攻撃のHIT率 対象に対する
        /// </summary>
        /// <returns></returns>
        public float GetHitRateByDest(string dest)
        {
            float hitcount = GetHitCountByDest(dest);
            float misscount = GetMissCountByDest(dest);
            if (hitcount + misscount == 0) return 0;
            return hitcount / (hitcount + misscount);
        }

        #region DoT
        /// <summary>
        /// DOTダメージ ダメージベース（中間値）を算出してDOT威力との掛け算
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public int GetDoTDamage(DateTime time)
        {
            float _critrate = GetCritRate();
            float critrate = _critrate>0.1F?_critrate:0.1F;//10%以下の場合は10%に
            float noncritdmg = CalcDamageBase()*(buffperson.GetTotalDoTPower(time)+buffperson.GetTotalAoEPower()) ;
            float critdmg = 1.5F * noncritdmg * critrate;
            float normaldmg = noncritdmg * (1 - critrate);
            return (int)(critdmg + normaldmg);
        }
        /// <summary>
        /// DOTダメージ ダメージベース（中間値）を算出してDOT威力との掛け算
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public int GetDoTDamageByDest(DateTime time,string destname)
        {
            float _critrate = GetCritRateByDest(destname);
            float critrate = _critrate > 0.1F ? _critrate : 0.1F;//10%以下の場合は10%に
            float noncritdmg = CalcDamageBaseByDest(destname) * (buffperson.GetTotalDoTPowerByDest(time, destname) + buffperson.GetTotalAoEPowerByDest(destname));
            float critdmg = 1.5F * noncritdmg * critrate;
            float normaldmg = noncritdmg * (1 - critrate);
            return (int)(critdmg + normaldmg);
        }
        /// <summary>
        /// DoTの回数
        /// </summary>
        /// <returns></returns>
        public int GetDoTCount()
        {
            return buffperson.bufflist.ToArray().Count(obj => obj.DotBuff != null);
        }

        /// <summary>
        /// DoTの回数　対象に対する
        /// </summary>
        /// <returns></returns>
        public int GetDoTCountByDest(string dest)
        {
            return buffperson.bufflist.ToArray().Count(obj => obj.DotBuff != null && obj.DestEnt.Name == dest);
        }

        #endregion
        /// <summary>
        /// 威力１バフなし状態でのダメージを算出
        /// </summary>
        /// <returns></returns>
        public float CalcDamageBase()
        {
            return ddperson.CalcDamageBase();
        }

        /// <summary>
        /// 威力１バフなし状態でのダメージを算出
        /// </summary>
        /// <returns></returns>
        public float CalcDamageBaseByDest(string destname)
        {
            return ddperson.CalcDamageBaseByDest(destname);
        }
    }

}
