using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrecisionRep
{
    public class DDAction
    {
        public int actionID;
        public string ActionName;
        public int PowerMax;
        public int PowerMin;
        public bool Area;
        public int[] Jobs;

        public DDAction(int id, string name, int pMax, int pMin, bool area, params int[] jobs)
        {
            actionID = id;
            ActionName = name;
            PowerMax = pMax;
            PowerMin = pMin;
            Area = area;
            Jobs = jobs;
        }

        public static DDAction[] DDActions
        {
            get
            {
                if (_DDActions == null)
                {
                    _DDActions = GetDDActions();
                }
                return _DDActions;
            }
        }
        public static DDAction GetDDAction(int id)
        {
            foreach (DDAction dd in DDActions.Where(obj => obj.actionID == id))
            {
                return dd;
            }
            return null;
        }
        public static DDAction GetDDAction(string actionname)
        {
            foreach (DDAction dd in DDActions.Where(obj => obj.ActionName == actionname))
            {
                return dd;
            }
            return null;
        }
        private static DDAction[] _DDActions;
        private static DDAction[] GetDDActions()
        {
            List<DDAction> list = new List<DDAction>();

            //剣・ナイト
            list.Add(new DDAction(9, "ファストブレード", 150, 150, false, 1, 19));
            list.Add(new DDAction(11, "サベッジブレード", 200, 100,false, 1, 19));
            list.Add(new DDAction(16, "シールドバッシュ", 110, 110, false, 1, 19));
            list.Add(new DDAction(21, "レイジ・オブ・ハルオーネ", 260, 100, false, 1, 19));
            list.Add(new DDAction(23, "サークル・オブ・ドゥーム", 100, 100, true, 1, 19));
            list.Add(new DDAction(24, "シールドロブ", 120, 120, false, 1, 19));
            list.Add(new DDAction(25, "シールドスワイプ", 210, 210, false, 1, 19));
            list.Add(new DDAction(29, "スピリッツウィズイン", 300, 300, false, 19));
            //斧･戦士
            list.Add(new DDAction(31, "ヘヴィスウィング", 150, 150, false, 3, 21));
            list.Add(new DDAction(33, "フラクチャー", 100, 100, false, 1, 2, 3, 5, 19, 20, 21));
            list.Add(new DDAction(35, "スカルサンダー", 200, 100, false, 1, 2, 3, 5, 19, 20, 21));
            list.Add(new DDAction(36, "マーシーストローク", 200, 200, false, 1, 2, 3, 5, 6, 7, 26));
            list.Add(new DDAction(37, "メイム", 190, 100, false, 3, 21));
            list.Add(new DDAction(38, "ブルータルスウィング", 100, 100, false, 3, 21));
            list.Add(new DDAction(41, "オーバーパワー", 120, 120, false, 3, 21));
            list.Add(new DDAction(42, "シュトルムヴィント", 250, 100, false, 3, 21));
            list.Add(new DDAction(45, "シュトルムブレハ", 270, 100, false, 3, 21));
            list.Add(new DDAction(46, "トマホーク", 130, 130, false, 3, 21));
            list.Add(new DDAction(47, "ボーラアクス", 270, 100, false, 3, 21));
            list.Add(new DDAction(48, "原初の魂", 300, 300, false, 3, 21));
            list.Add(new DDAction(51, "スチールサイクロン", 200, 200, false, 3, 21));
            //闘・モンク
            list.Add(new DDAction(53, "連撃", 130, 130, false, 2, 20));
            list.Add(new DDAction(54, "正拳突き", 190, 150, false, 2, 20));
            list.Add(new DDAction(56, "崩拳", 180, 140, false, 2, 20));
            list.Add(new DDAction(58, "カウンター", 170, 170, false, 1, 2, 3, 5, 19, 20, 21));
            list.Add(new DDAction(61, "双掌打", 140, 100, false, 2, 20));
            list.Add(new DDAction(62, "壊神衝", 50, 50, true, 2, 20));
            list.Add(new DDAction(64, "鉄山靠", 150, 150, false, 2, 20));
            list.Add(new DDAction(66, "破砕拳", 70, 30, false, 2, 20));
            list.Add(new DDAction(67, "空鳴拳", 170, 170, true, 2, 20));
            list.Add(new DDAction(68, "秘孔拳", 20, 20, false, 2, 20));
            list.Add(new DDAction(70, "地烈斬", 130, 130, true, 20));
            list.Add(new DDAction(71, "羅刹衝", 100, 100, false, 20));
            list.Add(new DDAction(72, "短勁", 120, 120, false, 20));
            list.Add(new DDAction(74, "双竜脚", 150, 100, false, 20));
            //槍・竜
            list.Add(new DDAction(75, "トゥルースラスト", 150, 150, false, 4, 22));
            list.Add(new DDAction(76, "フェイント", 120, 120, false, 1, 2, 3, 5, 20, 22, 23));
            list.Add(new DDAction(78, "ボーパルスラスト", 200, 100, false, 4, 22));
            list.Add(new DDAction(79, "ヘヴィスラスト", 170, 100, false, 4, 22));
            list.Add(new DDAction(81, "インパルスドライヴ", 180, 100, false, 1, 2, 3, 20, 22));
            list.Add(new DDAction(82, "足払い", 130, 130, false, 4, 22));
            list.Add(new DDAction(84, "フルスラスト", 330, 100, false, 4, 22));
            list.Add(new DDAction(86, "ドゥームスパイク", 160, 160, true, 4, 22));
            list.Add(new DDAction(87, "ディセムボウル", 220, 100, false, 4, 22));
            list.Add(new DDAction(88, "桜華狂咲", 200, 100, false, 4, 22));
            list.Add(new DDAction(89, "リング・オブ・ソーン", 150, 100, true, 4, 22));
            list.Add(new DDAction(90, "ピアシングタロン", 120, 120, false, 4, 22));
            list.Add(new DDAction(91, "二段突き", 170, 170, false, 4, 22));
            list.Add(new DDAction(92, "ジャンプ", 200, 200, false, 22));
            list.Add(new DDAction(95, "スパインダイブ", 170, 170, false, 22));
            list.Add(new DDAction(96, "ドラゴンダイブ", 250, 250, true, 22));
            //弓・詩人
            list.Add(new DDAction(97, "ヘヴィショット", 150, 150, false, 5, 23));
            list.Add(new DDAction(98, "ストレートショット", 140, 140, false, 1, 2, 3, 5, 23));
            list.Add(new DDAction(100, "ベノムバイト", 100, 100, false, 1, 2, 3, 5, 23));
            list.Add(new DDAction(103, "ミザリーエンド", 190, 190, false, 5, 23));
            list.Add(new DDAction(106, "クイックノック", 110, 110, true, 5, 23));
            list.Add(new DDAction(109, "ブラントアロー", 50, 50, false, 5, 23));
            list.Add(new DDAction(110, "ブラッドレッター", 150, 150, false, 5, 23));
            list.Add(new DDAction(111, "ワイドボレー", 110, 110, true, 5, 23));
            list.Add(new DDAction(112, "リペリングショット", 80, 80, false, 5, 23));
            list.Add(new DDAction(113, "ウィンドバイト", 60, 60, false, 5, 23));
            list.Add(new DDAction(117, "レイン・オブ・デス", 100, 100, true, 23));
            //幻・白
            list.Add(new DDAction(119, "ストーン", 140, 140, false, 6, 24));
            list.Add(new DDAction(121, "エアロ", 50, 50, false, 6, 7, 27, 28));
            list.Add(new DDAction(127, "ストンラ", 170, 170, false, 6, 24));
            list.Add(new DDAction(132, "エアロラ", 50, 50, false, 6, 24));
            list.Add(new DDAction(134, "アクアオーラ", 150, 150, false, 6, 24));
            list.Add(new DDAction(139, "ホーリー", 200, 200, true, 24));
            //呪・黒
            list.Add(new DDAction(141, "ファイア", 150, 150, false, 7, 25));
            list.Add(new DDAction(142, "ブリザド", 150, 150, false, 7, 25));
            list.Add(new DDAction(144, "サンダー", 30, 30, false, 7, 25));
            list.Add(new DDAction(146, "ブリザラ", 50, 50, true, 7, 25));
            list.Add(new DDAction(147, "ファイラ", 100, 100, true, 7, 25));
            list.Add(new DDAction(148, "サンダラ", 50, 50, false, 7, 25));
            list.Add(new DDAction(152, "ファイガ", 220, 220, false, 7, 25));
            list.Add(new DDAction(153, "サンダガ", 60, 60, false, 7, 25));
            list.Add(new DDAction(154, "ブリザガ", 220, 220, false, 7, 25));
            list.Add(new DDAction(156, "コラプス", 200, 100, false, 7, 25));
            list.Add(new DDAction(159, "フリーズ", 20, 20, false, 7, 25));
            list.Add(new DDAction(162, "フレア", 260, 260, true, 25));
            //巴・召喚・学者
            list.Add(new DDAction(163, "ルイン", 80, 80, false, 6, 7, 26, 25, 27, 28));
            list.Add(new DDAction(167, "エナジードレイン", 150, 150, false, 26, 27, 28));
            list.Add(new DDAction(168, "ミアズマ", 20, 20, false, 26, 27, 28));
            list.Add(new DDAction(172, "ルインラ", 80, 80, false, 26, 27, 28));
            list.Add(new DDAction(177, "ミアズラ", 20, 20, false, 26, 27, 28));
            list.Add(new DDAction(181, "ミアズマバースト", 300, 100, false, 28));
            list.Add(new DDAction(182, "トライディザスター", 30, 30, true, 28));
            //リミットブレイク
            list.Add(new DDAction(200, "ブレイバー", 2400, 2400, false, 2, 4, 20));
            list.Add(new DDAction(201, "ブレードダンス", 5250, 5250, false, 2, 4, 20));
            list.Add(new DDAction(202, "ファイナルヘヴン", 9000, 9000, false, 2, 4, 20));

            list.Add(new DDAction(203, "スカイシャード", 1650, 1650, true, 7, 26, 27));
            list.Add(new DDAction(204, "プチメテオ", 3600, 3600, true, 7, 26, 27));
            list.Add(new DDAction(205, "メテオ", 6150, 6150, true, 7, 26, 27));
            //ペット
            list.Add(new DDAction(633, "ひっかき", 70, 70, false, 26, 27, 28));
            list.Add(new DDAction(634, "トパーズの輝き", 60, 60, false, 26, 27, 28));
            list.Add(new DDAction(636, "体当たり", 60, 60, false, 26, 27, 28));
            list.Add(new DDAction(637, "ガスト", 90, 90, false, 26, 27, 28));
            list.Add(new DDAction(638, "ふきとばし", 80, 80, false, 26, 27, 28));
            list.Add(new DDAction(639, "ダウンバースト", 80, 80, false, 26, 27, 28));

            list.Add(new DDAction(787, "ロックバスター", 85, 85, false, 26, 27, 28));
            list.Add(new DDAction(788, "マウンテンバスター", 70, 70, true, 26, 27, 28));
            list.Add(new DDAction(790, "ランドスライド", 70, 70, false, 26, 27, 28));
            list.Add(new DDAction(791, "大地の怒り", 200, 200, false, 26, 27, 28));

            list.Add(new DDAction(792, "ウィンドブレード", 100, 100, false, 26, 27, 28));
            list.Add(new DDAction(793, "ショックウェーブ", 90, 90, false, 26, 27, 28));
            list.Add(new DDAction(794, "エリアルスラッシュ", 90, 90, true, 26, 27, 28));
            list.Add(new DDAction(796, "エリアルブラスト", 250, 250, false, 26, 27, 28));

            list.Add(new DDAction(797, "クリムゾンサイクロン", 105, 105, false, 26, 27, 28));
            list.Add(new DDAction(798, "バーニングストライク", 120, 120, false, 26, 27, 28));
            list.Add(new DDAction(800, "フレイミングクラッシュ", 105, 105, true, 26, 27, 28));
            list.Add(new DDAction(801, "地獄の火炎", 200, 200, true, 26, 27, 28));


            return list.ToArray();
        }
    }
}
