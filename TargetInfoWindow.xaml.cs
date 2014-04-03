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
using System.Windows.Threading;

namespace Chocorep2
{
    /// <summary>
    /// TargetInfoWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TargetInfoWindow : Window
    {

        public string TargetName
        {
            get
            {
                return TargetNameBlock.Text;
            }
            set
            {
                TargetNameBlock.Text = value;
            }
        }
        public string TargetHPP
        {
            get
            {
                return TargetHPPBlock.Text;
            }
            set
            {
                TargetHPPBlock.Text = value;
            }
        }
        public string TargetHP
        {
            get
            {
                return TargetHPBlock.Text;
            }
            set
            {
                TargetHPBlock.Text = value;
            }
        }

        public TargetInfoWindow()
        {
            InitializeComponent();
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
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
        void t_Tick(object sender, EventArgs e)
        {
            ResizeMode = System.Windows.ResizeMode.NoResize;
            //dataGrid1.SelectedIndex = -1;
        }
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            t.Stop();
            ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;

        }
    }
}
