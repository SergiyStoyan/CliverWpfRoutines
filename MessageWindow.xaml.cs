﻿/********************************************************************************************
        Author: Sergey Stoyan
        sergey.stoyan@gmail.com
        sergey.stoyan@hotmail.com
        stoyan@cliversoft.com
        http://www.cliversoft.com
********************************************************************************************/
using System;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using Cliver.Win;

namespace Cliver.Wpf
{
    public partial class MessageWindow : Window
    {
        /// <summary>
        /// Use styling resources defined in the entry assembly.
        /// </summary>
        public static bool UseApplicationResources = true;

        public MessageWindow(string caption, Icon icon, string message, string[] buttons, int default_button, Window owner)
        {
            InitializeComponent();

            if (UseApplicationResources)
                Resources = Application.Current.Resources;

            Icon = Win.AssemblyRoutines.GetAppIconImageSource();
            Title = caption;
            Owner = owner;

            if (icon == null)
                image.Width = 0;
            else
                image.Source = icon.ToImageSource();

            System.Drawing.Size s = Cliver.Win.SystemInfo.GetCurrentScreenSize(new System.Drawing.Point((int)Left, (int)Top), true);
            MaxWidth = SystemParameters.PrimaryScreenWidth * 3 / 4;
            MaxHeight = SystemParameters.PrimaryScreenHeight * 3 / 4;

            this.message.Text = message;

            //if (!yesNo)
            //{
            //    b1.Content = "OK";
            //    b2.Visibility = Visibility.Collapsed;
            //}

            if (buttons != null)
            {
                for (int i = buttons.Length - 1; i >= 0; i--)
                {
                    Button b = new Button();
                    b.Tag = i;
                    b.Content = buttons[i];
                    //b.AutoSize = true;
                    b.Click += b_Click;
                    buttonPanel.Children.Add(b);
                    if (i == default_button)
                        b.Focus();
                }
            }

            Loaded += delegate
            {
                if (ActualWidth >= MaxWidth)
                    this.Width = MaxWidth;
                if (ActualHeight >= MaxHeight)
                    this.Height = MaxHeight;
            };
        }

        void b_Click(object sender, EventArgs e)
        {
            ClickedButton = (int)((Button)sender).Tag;
            Close();
        }

        public int? ClickedButton { get; private set; } = null;

        new public int? ShowDialog()
        {
            base.ShowDialog();
            return ClickedButton;
        }

        public new void Close()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        base.Close();
                    }
                    catch { }//if closed already
                });
            }
            catch { }//if closed already
        }
    }
}