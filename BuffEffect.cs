using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrecisionRep
{
    public class BuffEffectProvider
    {
        private static BuffEffect[] _buffs;

        public static BuffEffect GetBuffEffect(int buffid)
        {
            foreach (BuffEffect be in BuffEffects.Where(b => b.BuffID == buffid))
            {
                return be;
            }
            return new BuffEffect();
        }

        public static BuffEffect[] BuffEffects
        {
            get
            {
                if (_buffs == null)
                {
                    _buffs = GetBuffs();
                }
                return _buffs;
            }
        }

        private static BuffEffect[] GetBuffs()
        {
            List<BuffEffect> bufflist = new List<BuffEffect>();
            bufflist.Add(new FightorFlight());//ファイト・オア・フライト
            bufflist.Add(new ShieldOath());//忠義の盾

            bufflist.Add(new Maim());//メイム
            bufflist.Add(new Berserk());//バーサク
            bufflist.Add(new StormsEye());//シュトルムブレハ
            bufflist.Add(new Defiance());//ディフェンダー 
            bufflist.Add(new Unchained());//ディフェンダー 
            bufflist.Add(new Defiance());//アンチェインド

            bufflist.Add(new FistsofFire());//紅蓮の構え
            bufflist.Add(new TwinSneaks());//双掌打
            bufflist.Add(new GreasedLightningI());//疾風迅雷1
            bufflist.Add(new GreasedLightningII());//疾風迅雷2
            bufflist.Add(new GreasedLightningIII());//疾風迅雷3

            bufflist.Add(new Sutemi());//捨身
            bufflist.Add(new Disembowel());//ディセムボウル

            bufflist.Add(new Mosanogeki());//猛者の撃
            bufflist.Add(new HawkEye());//ホークアイ
            bufflist.Add(new Ballad());//バラード
            bufflist.Add(new Paeon());//パイオン
            bufflist.Add(new FoeRequiem());//レクイエム

            bufflist.Add(new Weak());//衰弱
            bufflist.Add(new Weak2());//衰弱[強]

            return bufflist.ToArray();
        }
    }

    public class BuffEffect
    {
        public int BuffID=0;
        /// <summary>
        /// 自身のバフによるダメージ補正足し算
        /// </summary>
        /// <param name="job">自身のジョブ</param>
        /// <param name="level">自身のレベル</param>
        public virtual float GetSrcAddRate(int job, int level)
        {
            return 0;
        }
        /// <summary>
        /// 対象のバフによるダメージ補正足し算
        /// </summary>
        /// <param name="job">自身のジョブ</param>
        /// <param name="level">自身のレベル</param>
        public virtual float GetDestAddRate(int job, int level)
        {
            return 0;
        }
        /// <summary>
        /// 自身のバフによるダメージ補正掛け算
        /// </summary>
        /// <param name="job">自身のジョブ</param>
        /// <param name="level">自身のレベル</param>
        public virtual float GetSrcMulRate(int job, int level)
        {
            return 1;
        }
        /// <summary>
        /// 対象のバフによるダメージ補正掛け算
        /// </summary>
        /// <param name="job">自身のジョブ</param>
        /// <param name="level">自身のレベル</param>
        public virtual float GetDestMulRate(int job, int level)
        {
            return 1;
        }
    }

    #region 剣・ナイト
    /// <summary>
    /// ファイト・オア・フライト　-20
    /// </summary>
    public class FightorFlight : BuffEffect
    {
        public FightorFlight()
        {
            BuffID = 76;
        }
        /// <summary>
        /// 30%上昇させる
        /// </summary>
        /// <param name="job"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public override float GetSrcAddRate(int job, int level)
        {
            return 0.3F;
        }
    }
    /// <summary>
    /// 忠義の盾 -20%
    /// </summary>
    public class ShieldOath : BuffEffect
    {
        public ShieldOath()
        {
            BuffID = 79;
        }
        /// <summary>
        /// -20%
        /// </summary>
        /// <param name="job"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public override float GetSrcAddRate(int job, int level)
        {
            return -0.2F;
        }
    }
    #endregion

    #region 斧・戦士
    /// <summary>
    /// メイム 20%アップ
    /// </summary>
    public class Maim : BuffEffect
    {
        public Maim()
        {
            BuffID = 85;
        }
        public override float GetSrcAddRate(int job, int level)
        {
            return 0.2F;
        }

    }
    /// <summary>
    /// バーサク 50%アップ
    /// </summary>
    public class Berserk : BuffEffect
    {
        public Berserk()
        {
            BuffID = 86;
        }
        /// <summary>
        /// 50%アップ
        /// </summary>
        /// <param name="job"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public override float GetSrcAddRate(int job, int level)
        {
            return 0.5F;
        }

    }

    /// <summary>
    /// シュトルムブレハ　斬耐性 -10%
    /// </summary>
    public class StormsEye:BuffEffect
    {
        public StormsEye()
        {
            BuffID = 90;
        }

        public override float GetDestMulRate(int job, int level)
        {
            if (job == 1 || job == 3 || job == 19 || job == 21)
            {
                return 1 / 0.9F;
            }
            return 1;
        }
    }

    /// <summary>
    /// ディフェンダー 25%ダウン
    /// </summary>
    public class Defiance : BuffEffect
    {
        public Defiance()
        {
            BuffID = 91;
        }
        /// <summary>
        /// 25%ダウン
        /// </summary>
        /// <param name="job"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public override float GetSrcAddRate(int job, int level)
        {
            return -0.25F;
        }

    }
    /// <summary>
    /// アンチェインド 25%アップ（ディフェンダー分無効）
    /// </summary>
    public class Unchained : BuffEffect
    {
        public Unchained()
        {
            BuffID = 92;
        }

        /// <summary>
        /// 25%アップ（ディフェンダー時だが同時にしかありえない）
        /// </summary>
        /// <param name="job"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public override float GetSrcAddRate(int job, int level)
        {
            return 0.25F;
        }
    }

    #endregion

    #region 格・モンク

    /// <summary>
    /// 紅蓮の構え
    /// </summary>
    public class FistsofFire : BuffEffect
    {
        public FistsofFire()
        {
            BuffID = 103;
        }

        public override float GetSrcAddRate(int job, int level)
        {
            return 0.05F;
        }
    }

    /// <summary>
    /// 双掌打
    /// </summary>
    public class TwinSneaks:BuffEffect
    {
        public TwinSneaks()
        {
            BuffID = 101;
        }

        public override float GetSrcAddRate(int job, int level)
        {
            return 0.1F;
        }
    }

    /// <summary>
    /// 疾風迅雷1 +9%
    /// </summary>
    public class GreasedLightningI:BuffEffect
    {
        public GreasedLightningI()
        {
            BuffID = 111;
        }

        public override float GetSrcAddRate(int job, int level)
        {
            return 0.09F;
        }
    }
    /// <summary>
    /// 疾風迅雷2 +18%
    /// </summary>
    public class GreasedLightningII : BuffEffect
    {
        public GreasedLightningII()
        {
            BuffID = 112;
        }

        public override float GetSrcAddRate(int job, int level)
        {
            return 0.18F;
        }
    }
    /// <summary>
    /// 疾風迅雷３ +27%
    /// </summary>
    public class GreasedLightningIII : BuffEffect
    {
        public GreasedLightningIII()
        {
            BuffID = 113;
        }

        public override float GetSrcAddRate(int job, int level)
        {
            return 0.27F;
        }
    }
    #endregion

    #region 槍・竜
    /// <summary>
    /// 捨身
    /// </summary>
    public class Sutemi : BuffEffect
    {
        public Sutemi()
        {
            BuffID = 50;
        }
        public override float GetSrcAddRate(int job, int level)
        {
            if ((job == 4 || job == 22) && level >= 44)
            {
                return 0.3F;
            }
            return 0.1F;
        }
        public override float GetDestAddRate(int job, int level)
        {
            return 0.25F;
        }
    }

    /// <summary>
    /// ディセムボウル
    /// </summary>
    public class Disembowel : BuffEffect
    {
        public Disembowel()
        {
            BuffID = 121;
        }

        public override float GetDestMulRate(int job, int level)
        {
            if (job == 4 || job == 5 || job == 22 || job == 23)
            {
                return 1 / 0.9F;
            }
            return 1;
        }
    }
    #endregion

    #region 弓・詩人
    /// <summary>
    /// 猛者の撃
    /// </summary>
    public class Mosanogeki:BuffEffect
    {
        public Mosanogeki()
        {
            BuffID = 125;
        }
        public override float GetSrcAddRate(int job, int level)
        {
            return 0.2F;
        }
    }

    /// <summary>
    /// バラード
    /// </summary>
    public class Ballad : BuffEffect
    {
        public Ballad()
        {
            BuffID = 135;
        }
        
        public override float GetSrcAddRate(int job, int level)
        {
            return -0.2F;
        }
    }
    /// <summary>
    /// パイオン
    /// </summary>
    public class Paeon : Ballad
    {
        public Paeon()
        {
            BuffID = 137;
        }
    }

    /// <summary>
    /// レクイエム 10%ダウン
    /// </summary>
    public class FoeRequiem : BuffEffect
    {
        public FoeRequiem()
        {
            BuffID = 140;
        }

        public override float GetDestMulRate(int job, int level)
        {
            if (job == 6 || job == 7 || job == 24 || job == 25 || job == 26 || job == 27 || job == 28)
            {
                return 1 / 0.9F;
            }
            return 1;
        }

    }
    /// <summary>
    /// ホークアイ
    /// </summary>
    public class HawkEye : BuffEffect
    {
        public HawkEye()
        {
            BuffID = 123;
        }
        public override float GetSrcMulRate(int job, int level)
        {
            if (job == 5 || job == 23)
            {
                return 1.15F;
            }
            return 1;
        }
    }

    #endregion


    public class Weak : BuffEffect
    {
        public Weak()
        {
            BuffID = 43;
        }
        public override float GetSrcMulRate(int job, int level)
        {
            return 0.85F;
        }
    }
    public class Weak2 : BuffEffect
    {
        public Weak2()
        {
            BuffID = 44;
        }
        public override float GetSrcMulRate(int job, int level)
        {
            return 0.7F;
        }
    }
}
