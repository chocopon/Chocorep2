using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

using PrecisionRep;
using System.Windows.Threading;
using ffxivlib;
using System.Diagnostics;

namespace Chocorep2
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        FFXIVLIB lib;
        Chatlog chatlog;
        RepPerson MyRepPerson;
        Entity[] Entities;
        List<RepPerson> RepPersonList;
        ChatlogParser chatlogparser;
        PreciRepDataSet preciRepDataSet1;

        DispatcherTimer timer;

        bool restart = false;

        BackgroundWorker backgroundWorker;
        bool cancel = false;

        DateTime startDateTime;

        public bool versioncheck = true;

        private TargetInfoWindow targetInfoWindow = new TargetInfoWindow();
        private TargetInfoWindow focusTargetInfoWindow = new TargetInfoWindow();

        private int ChoisedProcessID = 0;


        public MainWindow()
        {
            InitializeComponent();
            RepPersonList = new List<RepPerson>();
            preciRepDataSet1 = new PreciRepDataSet();
            //dataGrid1.ItemsSource = preciRepDataSet1.RepEssential.DefaultView;
            RepPersonList = new List<RepPerson>();
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            backgroundWorker.WorkerSupportsCancellation = true;
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            #region 設定読み込み
            if (Properties.Settings.Default.IsUpgrade==false)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.IsUpgrade = true;
                Properties.Settings.Default.Save();
            }
            if (Properties.Settings.Default.MainWindowSize.Height > 0)
            {
                this.Left = Properties.Settings.Default.MainWindowLocation.X;
                this.Top = Properties.Settings.Default.MainWindowLocation.Y;
                this.Width = Properties.Settings.Default.MainWindowSize.Width;
                this.Height = Properties.Settings.Default.MainWindowSize.Height;
            }

            TopMostItem.IsChecked = Properties.Settings.Default.TopMost;
            this.Topmost = Properties.Settings.Default.TopMost;
            ViewHitRateItem.IsChecked = Properties.Settings.Default.ViewHitRate;
            ViewTargetInfoItem.IsChecked = Properties.Settings.Default.ViewTargetInfo;
            ViewFocusTargetInfoItem.IsChecked = Properties.Settings.Default.ViewFocusTargetInfo;
            targetInfoWindow.Left = Properties.Settings.Default.TargetInfoLocation.X;
            targetInfoWindow.Top = Properties.Settings.Default.TargetInfoLocation.Y;
            targetInfoWindow.Width = Properties.Settings.Default.TargetInfoWindowWidth;
            focusTargetInfoWindow.Left = Properties.Settings.Default.ForcusTargetInfoLocation.X;
            focusTargetInfoWindow.Top = Properties.Settings.Default.ForcusTargetInfoLocation.Y;
            focusTargetInfoWindow.Width = Properties.Settings.Default.FocusTargetInfoWindowWidth;

            #endregion

        }
        void SetStatus(string text)
        {
            StatusTextBlock.Text = text;
        }

        void UpdateTime(DateTime time)
        {
            TimeSpan span = time - startDateTime;
            TimespanTextBlock.Text = String.Format("{0:hh\\:mm\\:ss}", span);
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (restart)
            {
                Start();
                restart = false;
            }
            else
            {
                SetStatus("停止中");
            }
        }


        List<EntitiesSnap> EntitySnapList;
        string[] PetNames = new string[] { "カーバンクル・エメラルド", "カーバンクル・トパーズ", "タイタン・エギ", "ガルーダ・エギ", "イフリート・エギ" };


        private void Start()
        {

            timer.Stop();
            if (!InitializeRep())
            {//初期化失敗
                SetStatus("停止中");
                restart = false;
                return;
            }
            SetStatus("記録中");
            startDateTime = DateTime.Now;
            UpdateMainWindow(startDateTime);
            timer.Start();
            backgroundWorker.RunWorkerAsync();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                chatlog.GetChatLogLines();
                while (!backgroundWorker.CancellationPending && !cancel)
                {
                    DateTime now = DateTime.Now;
                    EntitySnapList.Insert(0, new EntitiesSnap(now, GetEntities()));
                    UpdateRep();
                    for (int i = EntitySnapList.Count - 1; i > 0; i--)
                    {
                        if (EntitySnapList[i].timestamp.AddSeconds(3) < now)
                        {
                            EntitySnapList.RemoveAt(i);
                        }
                    }
                    while (now.AddMilliseconds(300) > DateTime.Now)
                    {
                        System.Threading.Thread.Sleep(1);
                    }
                }
            }
            catch(Exception _e)
            {
                System.Diagnostics.Debug.WriteLine("backgroundWorker_DoWork " + _e.Message);
                System.Diagnostics.Debug.WriteLine(_e.StackTrace);
            }
        }

        /// <summary>
        /// REPの初期化
        /// </summary>
        private bool InitializeRep()
        {
            try
            {
                if (ChoisedProcessID > 0)
                {
                    lib = new FFXIVLIB(ChoisedProcessID);
                }
                else
                {
                    Process[] ffxivprocs = Process.GetProcessesByName("ffxiv");
                    if (ffxivprocs.Length == 1)
                    {
                        lib = new FFXIVLIB();
                    }
                    else if (ffxivprocs.Length > 1)
                    {

                        ChoiceProcForm choicefrm = new ChoiceProcForm();
                        if (choicefrm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            lib = new FFXIVLIB(choicefrm.SelectedFFXIVProcessID);
                            ChoisedProcessID = choicefrm.SelectedFFXIVProcessID;
                        }
                        else
                        {
                            this.Close();
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                chatlog = lib.GetChatlog();
                RepPersonList.Clear();
                Entities = GetEntities();
                EntitySnapList = new List<EntitiesSnap>();
                EntitySnapList.Add(new EntitiesSnap(DateTime.Now, Entities));
                chatlogparser = new ChatlogParser();

                if (Entities.Length == 0)
                    return false;

                //自分
                Entity myself = Entities[0];
                MyRepPerson = new RepPerson(myself.Name, PersonType.MySelf, myself.Job);
                RepPersonList.Add(MyRepPerson);
                chatlogparser.AddDDPerson(MyRepPerson.ddperson);

                //PTメンバー
                int cnt = lib.GetPartyMemberCount();
                for (int i = 0; i < cnt; i++)
                {
                    PartyMember pm = lib.GetPartyMemberInfo(i);
                    if (pm != null)
                    {
                        if (pm.Name == MyRepPerson.name) continue;
                        //if (RepPersonList.Count(obj => obj.name == pm.Name) > 0) continue;
                        RepPerson repperson = new RepPerson(pm.Name, PersonType.PTMember, pm.Job);
                        RepPersonList.Add(repperson);
                        chatlogparser.AddDDPerson(repperson.ddperson);
                    }
                }

                //ペット
                foreach (string petname in PetNames)
                {
                    RepPerson repperson = new RepPerson(petname, PersonType.Pet, JOB.SPN);
                    RepPersonList.Add(repperson);
                    chatlogparser.AddDDPerson(repperson.ddperson);
                }
                DateTime time = DateTime.Now;


                System.Diagnostics.Debug.WriteLine("Initialize Time = {0}", (DateTime.Now - time).TotalMilliseconds);

                return true;
            }
            catch(Exception _e)
            {
                ChoisedProcessID = 0;
                MessageBox.Show("Chocorepの起動に失敗しました。"+ _e.Message);
                return false;
            }
        }

        /// <summary>
        /// REP更新
        /// </summary>
        private void UpdateRep()
        {
            DateTime time = DateTime.Now;
            //ログは遅れるので
            Entities = GetEntities();
            foreach (RepPerson person in RepPersonList)
            {
                person.UpdateBuff(time, Entities);
            }

            if (chatlog.IsNewLine())
            {
                foreach (Chatlog.Entry ent in chatlog.GetChatLogLines())
                {
                    object res = null;
                    FFXIVLog log = FFXIVLog.ParseSingleLog(ent.Raw);
                    //フィルタ
                    if (log.LogTypeHexString == "003C") continue;//エラーログ

                    //解析用
                    PreciRepDataSet.ParsingReportRow row = preciRepDataSet1.ParsingReport.NewParsingReportRow();
                    row.ID = preciRepDataSet1.ParsingReport.Count;
                    preciRepDataSet1.ParsingReport.AddParsingReportRow(row);
                    row.DateTime = time;
                    row.LogHex = log.LogTypeHexString;
                    row.LogBody = log.LogBodyReplaceTabCode;
                    if (log.LogType >= 0x08 && log.LogType <= 0x0b)
                    {
                        row.LogType = "myself";
                    }
                    else if (log.LogType >= 0x10 && log.LogType <= 0x13)
                    {
                        row.LogType = "ptmember";
                    }
                    else if (log.LogType >= 0x40 && log.LogType <= 0x4C)
                    {
                        row.LogType = "pet";
                    }
                    else
                    {
                        row.LogType = "others";
                    }

                    try
                    {
                        chatlogparser.Parse(time, log,EntitySnapList.ToArray(), out res);
                        if (res != null)
                        {
                            Parsing(res, row);
                        }
                    }
                    catch (Exception _e)
                    {
                        System.Diagnostics.Debug.WriteLine("UpdateRep() "+ _e.Message);
                        System.Diagnostics.Debug.WriteLine(_e.StackTrace);
                    }
                }
            }

        }

        private void Parsing(object res, PreciRepDataSet.ParsingReportRow row)
        {
            Entity src = null, dest = null;
            #region SrcName, RepType
            if (res is ActionDone)
            {
                var result = (ActionDone)res;
                row.ActionName = result.actionname;
                row.SrcName = result.Src.Name;
                row.RepType = "Action Done";
                src = result.Src;
            }
            else if (res is AutoAttackDD)
            {
                var result = (AutoAttackDD)res;
                row.SrcName = result.Src.Name;
                row.RepType = "AA HIT";
                row.Num = result.damage;
                src = result.Src;
                dest = result.Dest;
            }
            else if (res is AutoAttackMiss)
            {
                var result = (AutoAttackMiss)res;
                row.SrcName = result.Src.Name;
                row.RepType = "AA MISS";
                src = result.Src;
                dest = result.Dest;
            }
            else if (res is ActionDD)
            {
                var result = (ActionDD)res;
                row.ActionName = result.actionName;
                row.SrcName = result.Src.Name;
                row.DestName = result.Dest.Name;
                row.RepType = "Action HIT";
                row.Num = result.damage;
                src = result.Src;
                dest = result.Dest;
            }
            else if (res is ActionMiss)
            {
                var result = (ActionMiss)res;
                row.SrcName = result.Src.Name;
                row.ActionName = result.ActionName;
                row.DestName = result.Dest.Name;
                row.RepType = "Action MISS";
                src = result.Src;
                dest = result.Dest;
            }
            else if (res is AddDamage)
            {
                var result = (AddDamage)res;
                row.ActionName = result.Buffname;
                row.SrcName = result.Src.Name;
                row.RepType = "Add Damage";
                src = result.Src;
                row.Num = result.damage;
                dest = result.Dest;
            }
            #endregion

            #region src dest buff
            if (src != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (BUFF b in src.Buffs.Where(obj => obj.BuffID > 0))
                {
                    string buffname = ResourceParser.GetBuffName(b.BuffID);
                    if (String.IsNullOrEmpty(buffname))
                        buffname = b.BuffID.ToString();

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

        private Entity[] GetEntities()
        {
            List<Entity> entlist = new List<Entity>();
            for (int i = 0; i < 100; i++)
            {
                Entity ent = lib.GetEntityInfo(i);
                if (ent == null) continue;
                entlist.Add(ent);
            }
            return entlist.ToArray();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {

            timer.Stop();
            preciRepDataSet1.Clear();
            if (backgroundWorker.IsBusy)
            {
                SetStatus("再スタートします");
                restart = true;
                backgroundWorker.CancelAsync();
            }
            else
            {
                Start();
            }
        }

        /// <summary>
        /// メインウィンドウを再描画
        /// </summary>
        /// <param name="time"></param>
        void UpdateMainWindow(DateTime time)
        {
            try
            {
                UpdateTime(time);

                int rowid = 0;

                //ターゲット情報
                Entity target = lib.GetCurrentTarget();
                Entity focust = lib.GetFocusTarget();
                if (target != null)
                {
                    float hpp = 100 * (float)target.CurrentHP / (float)target.MaxHP;

                    targetInfoWindow.TargetName = target.Name;
                    targetInfoWindow.TargetHPP = String.Format("{0:0.#}%", hpp);
                    targetInfoWindow.TargetHP = String.Format("{0}/{1}", target.CurrentHP, target.MaxHP);

                }
                else
                {
                    targetInfoWindow.TargetName ="";
                    targetInfoWindow.TargetHPP = "";
                    targetInfoWindow.TargetHP = "";
                }
                if (focust != null)
                {
                    float hpp = 100 * (float)focust.CurrentHP / (float)focust.MaxHP;
                    focusTargetInfoWindow.TargetName = focust.Name;
                    focusTargetInfoWindow.TargetHPP = String.Format("{0:0.#}%", hpp);
                    focusTargetInfoWindow.TargetHP = String.Format("{0}/{1}", focust.CurrentHP, focust.MaxHP);
                }
                else
                {
                    focusTargetInfoWindow.TargetName = "";
                    focusTargetInfoWindow.TargetHPP = "";
                    focusTargetInfoWindow.TargetHP = "";
                }

                //TOPMOST処理
                if (targetInfoWindow.IsVisible)
                {
                    targetInfoWindow.Topmost = true;
                }
                if (focusTargetInfoWindow.IsVisible)
                {
                    focusTargetInfoWindow.Topmost = true;
                }

                //データ更新
                for (int i = 0; i < RepPersonList.Count; i++)
                {
                    RepPerson person = RepPersonList[i];

                    PreciRepDataSet.RepEssentialRow row = preciRepDataSet1.RepEssential.FindByPlayerName(person.name);
                    if (row == null)
                    {
                        if (PetNames.Count(obj => obj == person.name) > 0)
                        {//ペットの場合
                            if (person.GetTotalDmg(time) == 0) continue;
                        }

                        row = preciRepDataSet1.RepEssential.NewRepEssentialRow();
                        row.PlayerName = person.name;
                        preciRepDataSet1.RepEssential.AddRepEssentialRow(row);
                    }

                    //RepEssential データ更新
                    row.Job = ((JOB)System.Enum.Parse(typeof(JOB), person.ddperson.Job.ToString())).ToString();
                    row.AADamage = person.GetAADamage();
                    row.DDamage = person.GetActionDamage();
                    row.DotDamage = person.GetDoTDamage(time);
                    row.AddDamage = person.GetAddDamage();
                    row.LimitBreak = person.GetLimitBreakDamage();
                    row.TotalDamage = row.AADamage + row.DDamage + row.DotDamage;
                    row.DDCount = person.GetActionDDCount();
                    row.AACount = person.GetAACount();
                    row.DoTCount = person.GetDoTCount();
                    row.HitCount = person.GetHitCount();
                    row.CritCount = person.GetCritCount();
                    row.MissCount = person.GetMissCount();
                    row.HitRate = 100 * person.GetHitRate();
                    row.CritRate = 100 * person.GetCritRate();
                    row.DamageBase = person.CalcDamageBase();
                    row.DPS = person.GetDPS(time);

                    TextBlock nametb = (TextBlock)NameStackPanel.Children[rowid];
                    TextBlock jobtb = (TextBlock)JobStatckPanel.Children[rowid];
                    TextBlock dmgtb = (TextBlock)DmgStackPanel.Children[rowid];
                    rowid++;

                    nametb.Text = row.PlayerName;
                    jobtb.Text = row.Job;
                    if (ViewHitRateItem.IsChecked)
                    {
                        if (row.HitRate == 100)
                        {
                            dmgtb.Text = String.Format("{0} 100.0%", row.TotalDamage.ToString(), row.HitRate);
                        }
                        else
                        {
                            dmgtb.Text = String.Format("{0} {1:00.00}%", row.TotalDamage.ToString(), row.HitRate);
                        }
                    }
                    else
                    {
                        dmgtb.Text = String.Format("{0}", row.TotalDamage);
                    }
                }
                TrimTextBlock(NameStackPanel, preciRepDataSet1.RepEssential.Count);
                TrimTextBlock(JobStatckPanel, preciRepDataSet1.RepEssential.Count);
                TrimTextBlock(DmgStackPanel, preciRepDataSet1.RepEssential.Count);

                FitColWidth(ViewTargetInfoItem.IsChecked);
            }
            catch (Exception _e)
            {
                System.Diagnostics.Debug.WriteLine("in UpdateMainWindow " + _e.Message);
                System.Diagnostics.Debug.WriteLine(_e.StackTrace);
            }
        }

        void FitColWidth(bool IsTargetView)
        {
            double colwidth = MeasureGridSize(DmgStackPanel);
            dmgCol.Width = new GridLength(colwidth, GridUnitType.Pixel);
        }

        /// <summary>
        /// 表示更新用
        /// </summary>
        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            DateTime time = DateTime.Now;
            UpdateMainWindow(time);
            this.Topmost = TopMostItem.IsChecked;
            timer.Start();
        }

        private double MeasureGridSize(StackPanel panel)
        {
            double dmgcolwidth = dmgCol.MinWidth;
            foreach (TextBlock dmgtb in panel.Children)
            {
                if (dmgcolwidth < dmgtb.DesiredSize.Width + dmgtb.Margin.Right + dmgtb.Margin.Left)
                {
                    dmgcolwidth = dmgtb.DesiredSize.Width + dmgtb.Margin.Right + dmgtb.Margin.Left;
                }
            }
            return dmgcolwidth;
        }

        private void TrimTextBlock(StackPanel panel, int count)
        {
            for (int i = count; i < panel.Children.Count; i++)
            {
                TextBlock orgtextblock = (TextBlock)panel.Children[i];
                orgtextblock.Text = "";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                VersionInfoTable remotev = VersionManager.ReadRemoteVersion();
                string[] versionupfiles = VersionManager.VersionCheck(remotev);
                if (versionupfiles.Length > 0)
                {//バージョンアップあり
                    VersionManager.VersionUp(versionupfiles);
                    VersionManager.OverwriteLocalVersionFile(remotev);
                    MessageBox.Show(this, "アプリケーションを更新しました。再起動します", "再起動の確認", MessageBoxButton.OK, MessageBoxImage.Information);
                    VersionManager.Restart();
                    this.Close();

                }
            }
            catch (Exception _e)
            {
                MessageBox.Show("バージョンアップに失敗しました。"+_e.Message);
            }
            ResetButton_Click(this, null);
        }

        private void TopMostItem_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void TopMostItem_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }

        private void windowbackground_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            this.Left += e.HorizontalChange;
            this.Top += e.VerticalChange;
        }

        private void topthumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            this.Left += e.HorizontalChange;
            this.Top += e.VerticalChange;
        }



        DispatcherTimer t = new DispatcherTimer();
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            t.Tick -= new EventHandler(t_Tick);
            t.Interval = new TimeSpan(0, 0, 0, 1, 0);
            t.Tick += new EventHandler(t_Tick);
            t.Start();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            t.Stop();
            ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;

        }

        void t_Tick(object sender, EventArgs e)
        {
            ResizeMode = System.Windows.ResizeMode.NoResize;
            //dataGrid1.SelectedIndex = -1;
        }

        /// <summary>
        /// 解析データを見る
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenDetailDataForm_Click(object sender, RoutedEventArgs e)
        {
            ParsingReportForm frm = new ParsingReportForm();
            frm.SetDataSet(preciRepDataSet1);
            frm.SetPersons(RepPersonList.ToArray());
            frm.Show();
        }

        /// <summary>
        /// 閉じる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
            }

            Properties.Settings.Default.MainWindowLocation = new System.Drawing.Point((int)Left, (int)Top);
            Properties.Settings.Default.MainWindowSize = new System.Drawing.Size((int)Width, (int)Height);
            Properties.Settings.Default.ViewHitRate = ViewHitRateItem.IsChecked;
            Properties.Settings.Default.TopMost = Topmost;
            Properties.Settings.Default.ViewTargetInfo=ViewTargetInfoItem.IsChecked;
            Properties.Settings.Default.ViewFocusTargetInfo = ViewFocusTargetInfoItem.IsChecked;
            Properties.Settings.Default.TargetInfoLocation = new System.Drawing.Point((int)targetInfoWindow.Left, (int)targetInfoWindow.Top);
            Properties.Settings.Default.TargetInfoWindowWidth = (int)targetInfoWindow.Width;
            Properties.Settings.Default.ForcusTargetInfoLocation = new System.Drawing.Point((int)focusTargetInfoWindow.Left, (int)focusTargetInfoWindow.Top);
            Properties.Settings.Default.FocusTargetInfoWindowWidth = (int)focusTargetInfoWindow.Width;

            Properties.Settings.Default.Save();
            //ターゲットウィンドウを閉じる
            targetInfoWindow.Close();
            focusTargetInfoWindow.Close();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FitColWidth(ViewTargetInfoItem.IsChecked);
            System.Diagnostics.Debug.WriteLine("Width:{0} Height{1}", Width, Height);
        }

        /// <summary>
        /// 命中率表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewHitRate_Checked(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 命中率非表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewHitRate_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void Thumb_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        #region ターゲットウィンドウ
        /// <summary>
        /// ターゲット情報表示　ON
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewTargetInfoItem_Checked(object sender, RoutedEventArgs e)
        {
            targetInfoWindow.Show();
        }

        /// <summary>
        /// ターゲット情報表示　OFF
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewTargetInfoItem_Unchecked(object sender, RoutedEventArgs e)
        {
            targetInfoWindow.Hide();
        }

        private void ViewFocusTargetInfoItem_Checked(object sender, RoutedEventArgs e)
        {
            focusTargetInfoWindow.Show();
        }

        private void ViewFocusTargetInfoItem_Unchecked(object sender, RoutedEventArgs e)
        {
            focusTargetInfoWindow.Hide();
        }
        #endregion
    }
}
