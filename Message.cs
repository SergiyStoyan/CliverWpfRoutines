/********************************************************************************************
        Author: Sergey Stoyan
        sergey.stoyan@gmail.com
        sergey.stoyan@hotmail.com
        stoyan@cliversoft.com
        http://www.cliversoft.com
********************************************************************************************/

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;

namespace Cliver.Wpf
{
    /// <summary>
    /// Show MessageWindow with predefined features
    /// </summary>
    public static class Message
    {
        //public static ResourceDictionary ResourceDictionary = null;//set style
        //public static string ResourceDictionaryPath
        //{
        //    get
        //    {
        //        return resourceDictionaryPath;
        //    }
        //    set
        //    {
        //        resourceDictionaryPath = value;
        //        if (resourceDictionaryPath != null)
        //        {
        //            ResourceDictionary = new ResourceDictionary();
        //            Uri u = new System.Uri(resourceDictionaryPath, System.UriKind.Absolute);
        //            System.Windows.Application.LoadComponent(ResourceDictionary, u);
        //        }
        //        else
        //            ResourceDictionary = null;
        //    }
        //}
        //static string resourceDictionaryPath = null;

        /// <summary>
        /// The message window position on the screen
        /// </summary>
        public static WindowStartupLocation? Location = null;

        /// <summary>
        /// Whether the message box must be displayed in the Windows taskbar.
        /// </summary>
        public static bool ShowInTaskbar = true;

        /// <summary>
        /// Whether the message box must be displayed topmost.
        /// </summary>
        public static bool TopMost = false;

        /// <summary>
        /// Owner that is used by default
        /// </summary>
        public static Window Owner = null;

        /// <summary>
        /// Autosize buttons by text
        /// </summary>
        public static bool ButtonAutosize = false;

        /// <summary>
        /// Display only one message box for all same messages throuwn. When the first one is being diplayed, the rest are ignored.
        /// </summary>
        public static bool NoDuplicate = true;

        public static string ProductName = /*Application.Current != null ? ProductName :*/ ProgramRoutines.GetAppName();

        public static bool ShowDetailsOnException =
#if DEBUG
            true
#else
            false
#endif
            ;

        public static void Inform(string message, Window owner = null)
        {
            ShowDialog(ProductName, getIcon(Icons.Information), message, new string[1] { "OK" }, 0, owner);
        }

        public static void Exclaim(string message, Window owner = null)
        {
            ShowDialog(ProductName, getIcon(Icons.Exclamation), message, new string[1] { "OK" }, 0, owner);
        }

        public static void Exclaim(Exception e, Window owner = null)
        {
            if (!ShowDetailsOnException)
                ShowDialog(ProductName, getIcon(Icons.Exclamation), e.Message, new string[1] { "OK" }, 0, owner);
            else
                ShowDialog(ProductName, getIcon(Icons.Exclamation), GetExceptionDetails(e), new string[1] { "OK" }, 0, owner);
        }

        public static void Warning(string message, Window owner = null)
        {
            ShowDialog(ProductName, getIcon(Icons.Warning), message, new string[1] { "OK" }, 0, owner);
        }

        public static void Warning(Exception e, Window owner = null)
        {
            if (!ShowDetailsOnException)
                ShowDialog(ProductName, getIcon(Icons.Warning), e.Message, new string[1] { "OK" }, 0, owner);
            else
                ShowDialog(ProductName, getIcon(Icons.Warning), GetExceptionDetails(e), new string[1] { "OK" }, 0, owner);
        }

        public static void Error(Exception e, Window owner = null)
        {
            if (!ShowDetailsOnException)
                Error(e.Message, owner);
            else
                ShowDialog(ProductName, getIcon(Icons.Error), GetExceptionDetails(e), new string[1] { "OK" }, 0, owner);
        }

        public static string GetExceptionDetails(Exception e)
        {
            List<string> ms = new List<string>();
            bool stack_trace_added = false;
            for (; e != null; e = e.InnerException)
            {
                string s = e.Message;
                if (!stack_trace_added && e.StackTrace != null)
                {
                    stack_trace_added = true;
                    s += "\r\n" + e.StackTrace;
                }
                ms.Add(s);
            }
            return string.Join("\r\n=>\r\n", ms);
        }

        public static void Error2(Exception e, Window owner = null)
        {
            Error(e.Message, owner);
        }

        public static void Error2(string subtitle, Exception e, Window owner = null)
        {
            Error(subtitle + "\r\n\r\n" + e.Message, owner);
        }

        public static void Error(string message, Window owner = null)
        {
            ShowDialog(ProductName, getIcon( Icons.Error), message, new string[1] { "OK" }, 0, owner);
        }

        public static bool YesNo(string question, Window owner = null, Icons icon = Icons.Question)
        {
            return ShowDialog(ProductName, getIcon(icon), question, new string[2] { "Yes", "No" }, 0, owner) == 0;
        }

        public static int? ShowDialog(string title, Icon icon, string message, string[] buttons, int default_button, Window owner, bool? button_autosize = null, bool? no_duplicate = null, bool? topmost = null, WindowStartupLocation? location = null)
        {
            owner = owner ?? Owner;
            if (owner != null && !owner.IsVisible)
                owner = null;
            if (owner != null)
            {
                int? result = null;
                owner.Dispatcher.Invoke(() =>
                {
                    result = show_dialog(title, icon, message, buttons, default_button, owner, button_autosize, no_duplicate, topmost);
                });
                return result;
            }
            if (System.Threading.Thread.CurrentThread.GetApartmentState() != System.Threading.ApartmentState.STA)
            {
                int? result = null;
                System.Threading.Thread thread = new System.Threading.Thread(() =>
                    {
                        result = show_dialog(title, icon, message, buttons, default_button, owner, button_autosize, no_duplicate, topmost);
                    });
                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.Start();
                thread.Join();
                return result;
            }
            return show_dialog(title, icon, message, buttons, default_button, owner, button_autosize, no_duplicate, topmost, location);
        }

        static int? show_dialog(string title, Icon icon, string message, string[] buttons, int default_button, Window owner, bool? button_autosize = null, bool? no_duplicate = null, bool? topmost = null, WindowStartupLocation? location = null)
        {
            string caller = null;
            if (no_duplicate ?? NoDuplicate)
            {
                StackTrace st = new StackTrace(true);
                StackFrame sf = null;
                for (int i = 1; i < st.FrameCount; i++)
                {
                    sf = st.GetFrame(i);
                    string file_name = sf.GetFileName();
                    if (file_name == null || !Regex.IsMatch(file_name, @"\\Message\.cs$"))
                        break;
                }
                caller = sf.GetMethod().Name + "," + sf.GetNativeOffset().ToString();
                string m = null;
                lock (callers2message)
                {
                    if (callers2message.TryGetValue(caller, out m) && m == message)
                        return -1;
                    callers2message[caller] = message;
                }
            }

            MessageWindow mf = new MessageWindow(title, icon, message, buttons, default_button, owner/*, button_autosize ?? ButtonAutosize*/);

            mf.ShowInTaskbar = ShowInTaskbar;
            mf.Topmost = topmost ?? TopMost;
            //mf.TopLevel = top_most ?? TopLevel;           
            mf.WindowStartupLocation = location ?? (Location ?? (owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen));

            int? result = mf.ShowDialog();

            if (no_duplicate ?? NoDuplicate)
                lock (callers2message)
                {
                    callers2message.Remove(caller);
                }

            return result;
        }
        static Dictionary<string, string> callers2message = new Dictionary<string, string>();
        public enum Icons
        {
            Information,
            Warning,
            Error,
            Question,
            Exclamation,
        }
        //static Icon get_icon(Icons icon)
        //{
        //    switch (icon)
        //    {
        //        case Icons.Information:
        //            return SystemIcons.Information;
        //        case Icons.Warning:
        //            return SystemIcons.Warning;
        //        case Icons.Error:
        //            return SystemIcons.Error;
        //        case Icons.Question:
        //            return SystemIcons.Question;
        //        case Icons.Exclamation:
        //            return SystemIcons.Exclamation;
        //        default: throw new Exception("No option: " + icon);
        //    }
        //}
        static Icon getIcon(Icons icon)
        {
            WinApi.Shell32.SHSTOCKICONID iId;
            switch (icon)
            {
                case Icons.Information:
                    iId = WinApi.Shell32.SHSTOCKICONID.SIID_INFO;
                    break;
                case Icons.Warning:
                    iId = WinApi.Shell32.SHSTOCKICONID.SIID_WARNING;
                    break;
                case Icons.Error:
                    iId = WinApi.Shell32.SHSTOCKICONID.SIID_ERROR;
                    break;
                case Icons.Question:
                    iId = WinApi.Shell32.SHSTOCKICONID.SIID_HELP;
                    break;
                case Icons.Exclamation:
                    iId = WinApi.Shell32.SHSTOCKICONID.SIID_WARNING;
                    break;
                default: throw new Exception("No option: " + icon);
            }
            var sii = new WinApi.Shell32.SHSTOCKICONINFO();
            sii.cbSize = (UInt32)System.Runtime.InteropServices.Marshal.SizeOf(typeof(WinApi.Shell32.SHSTOCKICONINFO));
            WinApi.Shell32.SHGetStockIconInfo(iId, WinApi.Shell32.SHGSI.SHGSI_ICON, ref sii);
            return Icon.FromHandle(sii.hIcon);
        }
    }
}