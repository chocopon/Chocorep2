using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ffxivlib;

namespace PrecisionRep
{
    public partial class ParsingReportForm : Form
    {
        private PreciRepDataSet dataset;
        private RepPerson[] RepPersons;
        public ParsingReportForm()
        {
            InitializeComponent();
        }

        public void SetDataSet(PreciRepDataSet ds)
        {
            dataset = ds;
        }

        public void SetPersons(RepPerson[] persons)
        {
            RepPersons = persons;
        }

        private void DataUpdate()
        {
            preciRepDataSet1.Clear();
            try
            {
                toolStripStatusLabel1.Text = "データを更新中...";
                Application.DoEvents();

                //データバインド外す
                RepEssentialView.DataSource = null;
                RepEssentialPersonView.DataSource = null;
                ParsingDataView.DataSource = null;
                BuffReportView.DataSource = null;

                for (int i = 0; i < dataset.ParsingReport.Count; i++)
                {
                   object[] items = dataset.ParsingReport[i].ItemArray;
                   var row= preciRepDataSet1.ParsingReport.NewParsingReportRow();
                   row.ItemArray = items;
                   preciRepDataSet1.ParsingReport.AddParsingReportRow(row);
                }
                for (int i = 0; i < dataset.RepEssential.Count; i++)
                {
                    object[] items = dataset.RepEssential[i].ItemArray;
                    var row = preciRepDataSet1.RepEssential.NewRepEssentialRow();
                    row.ItemArray = items;
                    preciRepDataSet1.RepEssential.AddRepEssentialRow(row);
                }

                //対象毎のREP
                UpdateRepEssentialDestBySrc();
                //バフ情報
                UpdateBuffReport();

                //データバインド戻す
                RepEssentialView.DataSource = RepEssentialBindingSource;
                RepEssentialPersonView.DataSource = SrcDestBindingSource;
                ParsingDataView.DataSource = ParsingReportBindingSource;
                BuffReportView.DataSource = BuffReportBingingSource;

                toolStripStatusLabel1.Text = "データを更新しました。";

            }
            catch(Exception _e)
            {
                toolStripStatusLabel1.Text = "データ更新に失敗しました。";
                MessageBox.Show(_e.Message);
            }
        }

        /// <summary>
        /// バフ情報の更新
        /// </summary>
        private void UpdateBuffReport()
        {
            preciRepDataSet1.DoTBuff.Clear();
            foreach (RepPerson person in RepPersons)
            {
                UpdateBuffReport(person);
            }
        }

        private void UpdateBuffReport(RepPerson person)
        {

            foreach(BUFFSnap bs in person.buffperson.bufflist.ToArray())
            {
                PreciRepDataSet.DoTBuffRow row = preciRepDataSet1.DoTBuff.NewDoTBuffRow();
                row.ID = preciRepDataSet1.DoTBuff.Count;
                preciRepDataSet1.DoTBuff.AddDoTBuffRow(row);

                row.BuffID = bs.Buff.BuffID;
                row.BuffName = bs.BuffName;
                row.Src = person.name;
                row.SrcID = bs.Buff.BuffProvider;
                row.Dest = bs.DestEnt.Name;
                row.DestID = bs.DestEnt.NPCId==0 ?bs.DestEnt.PCId  : bs.DestEnt.NPCId;
                row.StartTime = bs.startTime;
                row.FinalTime = bs.endTime;
                row.IsFinal = bs.IsFinalized;
                row.DamageRate = bs.GetRate();
                row.TotalPower = bs.GetTotalDotPowerAddBUFF(DateTime.MaxValue);

                #region src dest buff
                Entity src = bs.SrcEnt;
                Entity dest = bs.DestEnt;

                if (src != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (BUFF b in src.Buffs.Where(obj => obj.BuffID > 0))
                    {
                        string buffname = ResourceParser.GetBuffName(b.BuffID);
                        if (String.IsNullOrEmpty(buffname))
                        {
                            buffname = b.BuffID.ToString();
                        } 
                        sb.AppendFormat("{0},", buffname);
                    }
                    row.SrcBuffs = sb.ToString().TrimEnd(new char[] { ',' });
                }
                if (dest != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (BUFF b in dest.Buffs.Where(obj => obj.BuffID > 0))
                    {
                        string buffname = ResourceParser.GetBuffName(b.BuffID);
                        if (String.IsNullOrEmpty(buffname))
                        {
                            buffname = b.BuffID.ToString();
                        }
                        sb.AppendFormat("{0},", buffname);
                    }
                    row.DestBuffs = sb.ToString().TrimEnd(new char[] { ',' });
                }
                #endregion
            }
        }

        private void UpdateRepEssentialDestBySrc()
        {
            preciRepDataSet1.RepEssentialDestBySrc.Clear();
            foreach (RepPerson person in RepPersons)
            {
                UpdateRepEssentialDestBySrcUnit(person);
            }
        }

        private void UpdateRepEssentialDestBySrcUnit(RepPerson person)
        {
            List<string> DestList = new List<string>();

            #region destname一覧を取得する
            foreach (ActionDD dd in person.ddperson.GetActionDDs())
            {
                if (DestList.Contains(dd.Dest.Name))
                    continue;
                DestList.Add(dd.Dest.Name);
            }
            foreach (AutoAttackDD dd in person.ddperson.GetAutoAttackDDs())
            {
                if (DestList.Contains(dd.Dest.Name))
                    continue;
                DestList.Add(dd.Dest.Name);
            }
            #endregion

            foreach (string dest in DestList)
            {
                PreciRepDataSet.RepEssentialDestBySrcRow row = null;
                foreach (PreciRepDataSet.RepEssentialDestBySrcRow _row in preciRepDataSet1.RepEssentialDestBySrc.Where(obj => obj.SrcName == person.name && obj.DestName == dest))
                {
                    row = _row;
                    break;
                }
                if (row == null)
                {

                    row = preciRepDataSet1.RepEssentialDestBySrc.NewRepEssentialDestBySrcRow();
                    row.SrcName = person.name;
                    row.DestName = dest;
                    preciRepDataSet1.RepEssentialDestBySrc.AddRepEssentialDestBySrcRow(row);
                }

                //RepEssential データ更新
                row.AADamage = person.GetAADamageByDest(dest);
                row.DDamage = person.GetActionDamageByDest(dest);
                row.DotDamage = person.GetDoTDamageByDest(DateTime.MaxValue,dest);
                row.AddDamage = person.GetAddDamageByDest(dest);
                row.LimitBreak = person.GetLimitBreakDamageByDest(dest);
                row.TotalDamage = row.AADamage + row.DDamage + row.DotDamage;
                row.DDCount = person.GetActionDDCountByDest(dest);
                row.AACount = person.GetAACountByDest(dest);
                row.DoTCount = person.GetDoTCountByDest(dest);
                row.HitCount = person.GetHitCountByDest(dest);
                row.CritCount = person.GetCritCountByDest(dest);
                row.MissCount = person.GetMissCountByDest(dest);
                row.HitRate = 100 * person.GetHitRateByDest(dest);
                row.CritRate = 100 * person.GetCritRateByDest(dest);
                row.DamageBase = person.CalcDamageBaseByDest(dest);
                row.DPS = person.GetDPSByDest(DateTime.MaxValue,dest);
            }

        }


        private void データ更新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataUpdate();
        }

        private void このウィンドウを閉じるXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            preciRepDataSet1.Clear();
            preciRepDataSet1.Dispose();
            this.Close();
        }

        private string GetCSV()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ID,DateTime,LogType,LogHex,LogBody,SrcName,DestName,RepType,Num,SrcBuffs,DestBuffs");
            foreach (PreciRepDataSet.ParsingReportRow row in preciRepDataSet1.ParsingReport)
            {//ID{0},DateTime{1},LogType{2},LogHex{3},LogBody{4},SrcName{5},DestName{6},RepType{7},Num{8},SrcBuffs{9},DestBuffs{10}
                sb.AppendLine(String.Format("{0},{1},{2},{3},\"{4}\",\"{5}\",\"{6}\",{7},{8},\"{9}\",\"{10}\"",
                    row.ID,
                    row.DateTime,
                    row.LogType,
                    row.LogHex,
                    row.LogBody.Replace("\r\n", " "),
                    row.IsSrcBuffsNull()?"":row.SrcName,
                    row.IsDestNameNull()?"":row.DestName,
                    row.IsRepTypeNull()?"":row.RepType,
                    row.IsNumNull()?"":row.Num.ToString(),
                    row.IsSrcBuffsNull()?"":row.SrcBuffs,
                    row.IsDestBuffsNull()?"":row.DestBuffs));
            }
            return sb.ToString();
        }

        private void cSV出力EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SaveCSVFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string csv = GetCSV();
                System.IO.File.WriteAllText(SaveCSVFileDialog.FileName, csv,Encoding.GetEncoding("shift-jis"));
            }
        }
        //選択された
        private void RepEssentialView_SelectionChanged(object sender, EventArgs e)
        {
            foreach(DataGridViewCell cell in RepEssentialView.SelectedCells)
            {
                string name = (string)cell.OwningRow.Cells[0].Value;
                SrcDestBindingSource.Filter = String.Format("SrcName = '{0}'", name.Replace("'","''"));
            }
        }

        private void バージョン情報AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 aboutbox = new AboutBox1();
            aboutbox.ShowDialog(this);
        }
    }
}
