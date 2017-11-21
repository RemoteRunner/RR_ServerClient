using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace RemoteRunner.Services.Runner
{
    public class Runner
    {
        public string Run(string message)
        {
            dynamic stuff = JObject.Parse(message);
            JObject a = JObject.Parse(stuff.@params.ToString());
            object[] @params = a.Children().Cast<object>().ToArray();
            IDictionary<string, string> paramsDictionary = new Dictionary<string, string>();
            foreach (JProperty param in @params)
                paramsDictionary.Add(param.Name, param.Value.ToString());
            MethodInfo theMethod = GetType().GetMethod(stuff.command.ToString());
            return (string) theMethod.Invoke(this, new object[] {paramsDictionary});
        }

        #region Console

        public string Console(IDictionary<string, string> @params)
        {
            var psiOpt =
                new ProcessStartInfo(@"cmd.exe", "/C " + @params["cmd"])
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            Process procCommand = Process.Start(psiOpt);
            StreamReader srIncoming = procCommand.StandardOutput;
            string result = srIncoming.ReadToEnd();
            procCommand.WaitForExit();

            return result;
        }

        #endregion

        #region Browser

        public string OpenLink(IDictionary<string, string> @params)
        {
            return Console(new Dictionary<string, string> {{"cmd", "start " + @params["link"]}});
        }

        #endregion

        #region Disk

        public string ViewFile(IDictionary<string, string> @params)
        {
            try
            {
                return File.ReadAllText(@params["filename"]);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string EditFile(IDictionary<string, string> @params)
        {
            try
            {
                File.WriteAllText(@params["filename"], @params["content"]);
                return bool.TrueString;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string FreeSpace(IDictionary<string, string> @params)
        {
            var vol = "";
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo myDriveInfo in allDrives)
            {
                if (!myDriveInfo.IsReady) continue;
                double free = myDriveInfo.AvailableFreeSpace;
                double a = free / 1024 / 1024;
                vol += myDriveInfo.Name + ": " + a.ToString("#.## MB") + Environment.NewLine;
            }

            return vol;
        }

        [Flags]
        private enum RecycleFlags
        {
            SherbNoconfirmation = 0x00000001,
            SherbNoprogressui = 0x00000001,
            SherbNosound = 0x00000004
        }

        [DllImport("Shell32.dll")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        public string ClearCache(IDictionary<string, string> @params)
        {
            ClearFolder(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache)));
            return Convert.ToBoolean(SHEmptyRecycleBin(IntPtr.Zero, null,
                    RecycleFlags.SherbNoprogressui | RecycleFlags.SherbNosound | RecycleFlags.SherbNoconfirmation))
                .ToString();
        }

        private static void ClearFolder(DirectoryInfo diPath)
        {
            foreach (FileInfo fiCurrFile in diPath.GetFiles())
                fiCurrFile.Delete();
            foreach (DirectoryInfo diSubFolder in diPath.GetDirectories())
                ClearFolder(diSubFolder);
        }

        public string FormatDrive(IDictionary<string, string> @params)
        {
            string drive = @params["disk"] + ":";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "format.com",
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.SystemDirectory,
                    Arguments = "/FS:" + @params["filesystem"] +
                                " /Y" +
                                " /V:" + @params["label"] +
                                (Convert.ToBoolean(@params["quickformat"]) ? " /Q" : "") +
                                (@params["filesystem"] == "NTFS" && Convert.ToBoolean(@params["compress"])
                                    ? " /C"
                                    : "") +
                                " " + drive,
                    UseShellExecute = false
                };
                //if you want to hide the window
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardInput = true;
                Process formatProcess = Process.Start(psi);
                StreamWriter swStandardInput = formatProcess.StandardInput;
                swStandardInput.WriteLine();
                formatProcess.WaitForExit();
                return true.ToString();
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string FindDuplicates(IDictionary<string, string> @params)
        {
            FindInDir(new DirectoryInfo(@params["root"]), @params["pattern"], true);
            return duplicateFilesList.Aggregate("", (current, s) => current + s + Environment.NewLine);
        }

        private readonly List<string> duplicateFilesList = new List<string>();

        private void FindInDir(DirectoryInfo dir, string pattern, bool recursive)
        {
            foreach (FileInfo file in dir.GetFiles(pattern))
                duplicateFilesList.Add(file.FullName);
            if (!recursive) return;
            foreach (DirectoryInfo subdir in dir.GetDirectories())
                FindInDir(subdir, pattern, true);
        }

        #endregion

        #region Device

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;

            public static implicit operator System.Drawing.Point(Point point)
            {
                return new System.Drawing.Point(point.X, point.Y);
            }

            public override string ToString()
            {
                return X + ":" + Y;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        public string GetCursorPosition(IDictionary<string, string> @params)
        {            
            GetCursorPos(out Point lpPoint);
            return lpPoint.ToString();
        }

        public string SetCursorPosition(IDictionary<string, string> @params)
        {
            SetCursorPos(Convert.ToInt32(@params["x"]), Convert.ToInt32(@params["y"]));
            GetCursorPos(out Point lpPoint);
            return lpPoint.ToString();
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [Flags]
        public enum MouseEventFlags
        {
            Leftdown = 0x00000002,
            Leftup = 0x00000004,
            Middledown = 0x00000020,
            Middleup = 0x00000040,
            Move = 0x00000001,
            Absolute = 0x00008000,
            Rightdown = 0x00000008,
            Rightup = 0x00000010
        }

        public string LeftClick(IDictionary<string, string> @params)
        {
            SetCursorPosition(@params);
            mouse_event((int) MouseEventFlags.Leftdown, 0, 0, 0, 0);
            mouse_event((int) MouseEventFlags.Leftup, 0, 0, 0, 0);
            return bool.TrueString;
        }

        public string LeftDoubleClick(IDictionary<string, string> @params)
        {
            SetCursorPosition(@params);
            mouse_event((int) MouseEventFlags.Leftdown, 0, 0, 0, 0);
            mouse_event((int) MouseEventFlags.Leftup, 0, 0, 0, 0);
            mouse_event((int) MouseEventFlags.Leftdown, 0, 0, 0, 0);
            mouse_event((int) MouseEventFlags.Leftup, 0, 0, 0, 0);
            return bool.TrueString;
        }

        public string RightClick(IDictionary<string, string> @params)
        {
            SetCursorPosition(@params);
            mouse_event((int) MouseEventFlags.Rightdown, 0, 0, 0, 0);
            mouse_event((int) MouseEventFlags.Rightup, 0, 0, 0, 0);
            return bool.TrueString;
        }

        private const int KEYEVENTF_EXTENDEDKEY = 0x1;
        private const int KEYEVENTF_KEYUP = 0x2;

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte key, byte scan, int flags, int extraInfo);
        private static void KeyDown(Keys key)
        {
            keybd_event(ParseKey(key), 0, 0, 0);
        }

        private static void KeyUp(Keys key)
        {
            keybd_event(ParseKey(key), 0, KEYEVENTF_KEYUP, 0);
        }

        private static byte ParseKey(Keys key)
        {
            // Alt, Shift, and Control need to be changed for API function to work with them
            switch (key)
            {
                case Keys.Alt:
                    return 18;
                case Keys.Control:
                    return 17;
                case Keys.Shift:
                    return 16;
                default:
                    return (byte)key;
            }
        }

        public string Keyboard(IDictionary<string, string> @params)
        {
            Enum.TryParse(@params["key"], out Keys key);
            KeyDown(key);
            KeyUp(key);
            return bool.TrueString;
        }

        #endregion

        #region Process

        public string ProcessList(IDictionary<string, string> @params)
        {
            return Process.GetProcesses().Aggregate("",
                (current, winProc) => current + (winProc.Id + ": ") + winProc.ProcessName + Environment.NewLine);
        }

        public string ProcessKill(IDictionary<string, string> @params)
        {
            try
            {
                Process.GetProcessById(Convert.ToInt32(@params["id"])).Kill();
                return bool.TrueString;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string ProcessStart(IDictionary<string, string> @params)
        {
            Process process = Process.Start(@params["exe"], @params["args"]);
            return process.Id.ToString();
        }

        #endregion

        #region System

        public string ShutDown(IDictionary<string, string> @params)
        {
            return Console(new Dictionary<string, string> {{"cmd", "shutdown /s /f /t 0"}});
        }

        public string Hibernate(IDictionary<string, string> @params)
        {
            return Console(new Dictionary<string, string> {{"cmd", "shutdown /h /f /t 0"}});
        }

        public string LogOff(IDictionary<string, string> @params)
        {
            return Console(new Dictionary<string, string> {{"cmd", "shutdown /l /f /t 0"}});
        }

        public string Restart(IDictionary<string, string> @params)
        {
            return Console(new Dictionary<string, string> {{"cmd", "shutdown /r /f /t 0"}});
        }

        #endregion
    }
}