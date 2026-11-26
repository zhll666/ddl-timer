using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ChronoRuler
{
    internal static class Store
    {
        private static string _customPath;

        private static readonly string[] DateFormats =
        {
            "yyyy-MM-dd HH:mm", "yyyy-M-d H:mm",
            "yyyy-MM-dd HH:mm:ss", "yyyy-M-d H:mm:ss",
            "yyyy/MM/dd HH:mm", "yyyy/M/d H:mm",
            "yyyy/MM/dd HH:mm:ss", "yyyy/M/d H:mm:ss",
            "yyyy年M月d日 HH:mm", "yyyy年M月d日 H:mm"
        };

        private static readonly string[] StampFormats =
        {
            "yyyy-MM-dd HH:mm:ss", "yyyy-M-d H:mm:ss",
            "yyyy/MM/dd HH:mm:ss", "yyyy/M/d H:mm:ss"
        };

        public static string DataPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_customPath)) return _customPath;
                string env = Environment.GetEnvironmentVariable("DDLTIMER_DATA");
                if (!string.IsNullOrEmpty(env)) return env;
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.txt");
            }
        }

        private static string AppDataFilePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DDLTimer");
                return Path.Combine(dir, "data.txt");
            }
        }

        private static bool IsFixedPath
        {
            get
            {
                return !string.IsNullOrEmpty(_customPath) ||
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DDLTIMER_DATA"));
            }
        }

        public static void UsePath(string path)
        {
            _customPath = path;
        }

        public static AppData Load()
        {
            string path = DataPath;
            bool allowLegacy = !IsFixedPath;
            bool migrated = false;
            AppData data = null;

            if (File.Exists(path))
            {
                try
                {
                    data = ReadText(path);
                }
                catch
                {
                    BackupCorrupt(path);
                    data = null;
                }
            }

            // Older builds kept data in %APPDATA%. Move it beside the exe when possible.
            if (data == null && allowLegacy)
            {
                string appPath = AppDataFilePath;
                if (File.Exists(appPath))
                {
                    try
                    {
                        data = ReadText(appPath);
                        migrated = data != null;
                    }
                    catch
                    {
                        BackupCorrupt(appPath);
                        data = null;
                    }
                }
            }

            if (data == null && allowLegacy)
            {
                string legacy = FindLegacyJson();
                if (legacy != null)
                {
                    try
                    {
                        data = ReadJson(legacy);
                        migrated = data != null;
                    }
                    catch
                    {
                        data = null;
                    }
                }
            }

            if (data == null) data = new AppData();
            Normalize(data);
            if (migrated) Save(data);
            return data;
        }

        private static AppData ReadText(string path)
        {
            AppData data = new AppData();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            Project current = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    if (current != null) data.Projects.Add(current);
                    current = new Project();
                    current.Id = Guid.NewGuid().ToString("N");
                    int ordinal;
                    if (int.TryParse(line.Substring(1, line.Length - 2).Trim(), out ordinal))
                        current.Ordinal = ordinal;
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string value = line.Substring(eq + 1).Trim();

                if (current == null)
                {
                    if (key == "pxhour")
                    {
                        double px;
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out px))
                            data.PxHour = px;
                    }
                    else if (key == "zoom")
                    {
                        int zoom;
                        if (int.TryParse(value, out zoom)) data.Zoom = zoom;
                    }
                    continue;
                }

                if (key == "name")
                {
                    current.Name = value;
                }
                else if (key == "end" || key == "endat")
                {
                    DateTime t;
                    if (DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out t))
                        current.EndAt = Ui.ToMs(t);
                }
                else if (key == "color")
                {
                    current.Color = ParseColor(value);
                }
                else if (key == "created")
                {
                    DateTime t;
                    if (DateTime.TryParseExact(value, StampFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out t))
                        current.Created = Ui.ToMs(t);
                }
            }

            if (current != null) data.Projects.Add(current);
            return data;
        }

        private static int ParseColor(string value)
        {
            string s = (value ?? "").Trim();
            if (s.StartsWith("#")) s = s.Substring(1);
            if (s.Length == 8) s = s.Substring(2);
            if (s.Length != 6) return 0;
            int r, g, b;
            if (!int.TryParse(s.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) ||
                !int.TryParse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) ||
                !int.TryParse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
                return 0;
            return Color.FromArgb(255, r, g, b).ToArgb();
        }

        private static void Normalize(AppData data)
        {
            if (data.Projects == null) data.Projects = new List<Project>();
            long now = Ui.NowMs();

            List<Project> list = new List<Project>();
            for (int i = 0; i < data.Projects.Count; i++)
            {
                Project p = data.Projects[i];
                if (p == null) continue;
                if (p.Ordinal <= 0) p.Ordinal = i + 1;
                list.Add(p);
            }

            list.Sort(delegate(Project a, Project b)
            {
                // Remaining time order is deadline order: every project is
                // measured against the same current time.
                int c = a.EndAt.CompareTo(b.EndAt);
                if (c != 0) return c;
                c = a.Ordinal.CompareTo(b.Ordinal);
                if (c != 0) return c;
                return a.Created.CompareTo(b.Created);
            });

            HashSet<string> ids = new HashSet<string>();
            List<Project> clean = new List<Project>();
            for (int i = 0; i < list.Count; i++)
            {
                Project p = list[i];
                if (string.IsNullOrEmpty(p.Id) || !ids.Add(p.Id))
                {
                    p.Id = Guid.NewGuid().ToString("N");
                    ids.Add(p.Id);
                }
                p.Ordinal = i + 1;

                if (string.IsNullOrEmpty(p.Name)) p.Name = "项目 " + (i + 1);
                p.Name = p.Name.Replace("\r", " ").Replace("\n", " ").Trim();
                if (p.Name.Length == 0) p.Name = "项目 " + (i + 1);
                if (p.Name.Length > 24) p.Name = p.Name.Substring(0, 24);

                if (p.Created <= 0) p.Created = now;
                if (p.EndAt <= 0) p.EndAt = now + 3600000;

                Color c = Color.FromArgb(p.Color);
                if (c.A == 0) c = Pal.ProjectColor(i);
                p.Color = Color.FromArgb(255, c.R, c.G, c.B).ToArgb();
                clean.Add(p);
            }
            data.Projects = clean;

            if (data.Zoom < 0 || data.Zoom > 2) data.Zoom = 1;
            if (data.PxHour <= 0)
            {
                data.PxHour = data.Zoom == 0 ? 24 : (data.Zoom == 1 ? 120 : 400);
            }
            if (data.PxHour < 1) data.PxHour = 1;
            if (data.PxHour > 2000) data.PxHour = 2000;
        }

        public static bool Save(AppData data)
        {
            Normalize(data);
            if (TrySaveTo(DataPath, data)) return true;
            if (!IsFixedPath && TrySaveTo(AppDataFilePath, data)) return true;
            return false;
        }

        private static bool TrySaveTo(string path, AppData data)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                StringBuilder sb = new StringBuilder();
                sb.Append("# ddl计时器 · 本地记忆文件\r\n");
                sb.Append("# 结束时间 / 颜色 / 项目序数 / 项目名称；下次启动自动读取。\r\n");
                sb.Append("# 可直接编辑这个 txt，保存后重新打开软件生效。\r\n");
                sb.Append("version=1\r\n");
                sb.Append("pxHour=").Append(data.PxHour.ToString("0.###", CultureInfo.InvariantCulture)).Append("\r\n");
                sb.Append("zoom=").Append(data.Zoom).Append("\r\n");
                sb.Append("count=").Append(data.Projects.Count).Append("\r\n\r\n");

                for (int i = 0; i < data.Projects.Count; i++)
                {
                    Project p = data.Projects[i];
                    DateTime end = DateTimeOffset.FromUnixTimeMilliseconds(p.EndAt).ToLocalTime().DateTime;
                    DateTime created = DateTimeOffset.FromUnixTimeMilliseconds(p.Created).ToLocalTime().DateTime;

                    sb.Append('[').Append(p.Ordinal).Append("]\r\n");
                    sb.Append("name=").Append(CleanName(p.Name)).Append("\r\n");
                    sb.Append("end=").Append(end.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).Append("\r\n");
                    sb.Append("color=#").Append(p.Color.ToString("X8").Substring(2)).Append("\r\n");
                    sb.Append("created=").Append(created.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append("\r\n\r\n");
                }

                string tmp = path + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(true));
                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(tmp, path, path + ".bak", true);
                    }
                    catch
                    {
                        File.Copy(tmp, path, true);
                        File.Delete(tmp);
                    }
                }
                else
                {
                    File.Move(tmp, path);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string CleanName(string name)
        {
            string s = (name ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            if (s.Length == 0) s = "未命名";
            if (s.Length > 24) s = s.Substring(0, 24);
            return s;
        }

        private static void BackupCorrupt(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Copy(path, path + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss"), true);
            }
            catch
            {
            }
        }

        private static string FindLegacyJson()
        {
            string app = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string[] candidates =
            {
                Path.Combine(app, "DDLTimer", "data.json"),
                Path.Combine(app, "ChronoRuler", "data.json")
            };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i])) return candidates[i];
            }
            return null;
        }

        private static AppData ReadJson(string path)
        {
            byte[] raw = File.ReadAllBytes(path);
            int offset = 0;
            if (raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF)
                offset = 3;
            using (MemoryStream ms = new MemoryStream(raw, offset, raw.Length - offset))
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AppData));
                return ser.ReadObject(ms) as AppData;
            }
        }
    }
}
