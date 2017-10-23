using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace RemoteRunner.Services.Runner
{
    public class Runner
    {
        public string Run(string message)
        {
            dynamic stuff = JObject.Parse(message);
            JArray a = JArray.Parse(stuff.@params.ToString());
            object[] @params = a.Children().Children().Cast<object>().ToArray();
            MethodInfo theMethod = GetType().GetMethod(stuff.command.ToString());
            return (string)theMethod.Invoke(this, new object[] { @params });
        }

        #region Disk
        public string ViewFile(IReadOnlyList<object> @params)
        {
            try
            {
                return File.ReadAllText(@params[0].ToString());
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string EditFile(IReadOnlyList<object> @params)
        {
            try
            {
                File.WriteAllText(@params[0].ToString(), @params[1].ToString());
                return bool.TrueString;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string FreeSpace(IReadOnlyList<object> @params)
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
        private enum RecycleFlags : int
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000001,
            SHERB_NOSOUND = 0x00000004
        }

        [DllImport("Shell32.dll")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        public string ClearCache(IReadOnlyList<object> @params)
        {
            ClearFolder(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache)));
            return Convert.ToBoolean(SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHERB_NOPROGRESSUI | RecycleFlags.SHERB_NOSOUND | RecycleFlags.SHERB_NOCONFIRMATION)).ToString();

        }

        private static void ClearFolder(DirectoryInfo diPath)
        {
            foreach (FileInfo fiCurrFile in diPath.GetFiles())
            {
                fiCurrFile.Delete();
            }
            foreach (DirectoryInfo diSubFolder in diPath.GetDirectories())
            {
                ClearFolder(diSubFolder);
            }
        }

        public string FormatDrive(IReadOnlyList<object> @params)
        {
            string drive = @params[0] + ":";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "format.com",
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.SystemDirectory,
                    Arguments = "/FS:" + @params[2] +
                                " /Y" +
                                " /V:" + @params[1] +
                                (Convert.ToBoolean(@params[3].ToString()) ? " /Q" : "") +
                                ((@params[2].ToString() == "NTFS" && Convert.ToBoolean(@params[4].ToString())) ? " /C" : "") +
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

        public string FindDuplicates(IReadOnlyList<object> @params)
        {
            FindInDir(new DirectoryInfo(@params[0].ToString()), @params[1].ToString(), true);
            return duplicateFilesList.Aggregate("", (current, s) => current + (s + Environment.NewLine));
        }

        private readonly List<string> duplicateFilesList = new List<string>();
        private void FindInDir(DirectoryInfo dir, string pattern, bool recursive)
        {
            foreach (FileInfo file in dir.GetFiles(pattern))
            {
                duplicateFilesList.Add(file.FullName);
            }
            if (!recursive) return;
            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                FindInDir(subdir, pattern, true);
            }
        }
        #endregion

        #region Device

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }

            public override string ToString()
            {
                return X + ":" + Y;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        public string GetCursorPosition(IReadOnlyList<object> @params)
        {
            GetCursorPos(out POINT lpPoint);
            return lpPoint.ToString();
        }

        public string SetCursorPosition(IReadOnlyList<object> @params)
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            foreach (JObject VARIABLE in @params)
            {
                dict.Add(VARIABLE.Properties().First().Name, VARIABLE.Properties().First().Value);
            }
            SetCursorPos(Convert.ToInt32(@params[0]), Convert.ToInt32(@params[1]));
            GetCursorPos(out POINT lpPoint);
            return lpPoint.ToString();
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        public string LeftClick(IReadOnlyList<object> @params)
        {
            SetCursorPosition(@params);
            mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            return bool.TrueString;
        }

        public string LeftDoubleClick(IReadOnlyList<object> @params)
        {
            SetCursorPosition(@params);
            mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            return bool.TrueString;
        }

        public string RightClick(IReadOnlyList<object> @params)
        {
            SetCursorPosition(@params);
            mouse_event((int)(MouseEventFlags.RIGHTDOWN), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.RIGHTUP), 0, 0, 0, 0);
            return bool.TrueString;
        }

        #endregion

        #region Console

        public string Console(IReadOnlyList<object> @params)
        {
            // создаем процесс cmd.exe с параметрами "ipconfig /all"
            var psiOpt =
                new ProcessStartInfo(@"cmd.exe", "/C " + @params[0].ToString())
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            // скрываем окно запущенного процесса
            // запускаем процесс
            Process procCommand = Process.Start(psiOpt);
            // получаем ответ запущенного процесса
            StreamReader srIncoming = procCommand.StandardOutput;
            // выводим результат
            string result = (srIncoming.ReadToEnd());
            // закрываем процесс
            procCommand.WaitForExit();

            return result;
        }
        #endregion

        #region Process

        public string ProcessList(IReadOnlyList<object> @params)
        {
            return Process.GetProcesses().Aggregate("", (current, winProc) => current + ((winProc.Id + ": " + winProc.ProcessName) + Environment.NewLine));
        }

        public string ProcessKill(IReadOnlyList<object> @params)
        {
            try
            {
                Process.GetProcessById(Convert.ToInt32(@params[0])).Kill();
                return bool.TrueString;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string ProcessStart(IReadOnlyList<object> @params)
        {
            Process process = Process.Start(@params[0].ToString(), @params[1].ToString());
            return process.Id.ToString();
        }
        #endregion

        #region Browser
        public string OpenLink(IReadOnlyList<object> @params)
        {
            return Console(new[] { "start " + @params[0] });
        }
        #endregion

        #region System
        public string ShutDown(IReadOnlyList<object> @params)
        {
            return Console(new[] { "shutdown /s /f /t 0" });
        }

        public string Hibernate(IReadOnlyList<object> @params)
        {
            return Console(new[] { "shutdown /h /f /t 0" });
        }

        public string LogOff(IReadOnlyList<object> @params)
        {
            return Console(new[] { "shutdown /l /f /t 0" });
        }

        public string Restart(IReadOnlyList<object> @params)
        {
            return Console(new[] { "shutdown /r /f /t 0" });
        }

        #endregion
    }
}
