using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace HacVersionListBot
{
    class Program
    {
        private static DateTime now = DateTime.Now;
        private static bool keep_log;
        private static StreamWriter log;

        private const string version_url = "https://tagaya.hac.lp1.eshop.nintendo.net/tagaya/hac_versionlist";

        private const int format_version = 1;

        private static Dictionary<string, string> title_names = new Dictionary<string, string>();

        public static string TryGetTitleName(string title_id)
        {
            if (title_names.ContainsKey(title_id))
                return title_names[title_id];
            var server_name = ShogunUtils.GetTitleName(title_id);
            if (server_name == null) return "?";
            // Cache the new name.
            title_names[title_id] = server_name;
            return server_name;
        }

        public static void LoadTitleNames()
        {
            try
            {
                var cache = File.ReadAllLines("known.txt");
                for (var i = 0; i < cache.Length; i += 2)
                {
                    var title_id = cache[i];
                    var title_name = cache[i + 1];
                    title_names[title_id] = title_name;
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to read title name cache: {ex.Message}");
            }
        }

        public static void SaveTitleNames()
        {
            try
            {
                var cache = new List<string>();
                foreach (var title_id in title_names.Keys)
                {
                    cache.Add(title_id);
                    cache.Add(title_names[title_id]);
                }
                File.WriteAllLines("known.txt", cache);
            }
            catch (Exception ex)
            {
                Log($"Failed to save title name cache: {ex.Message}");
            }
        }

        public static void Log(string msg)
        {
            Console.WriteLine(msg);
            log.WriteLine(msg);
        }

        public static void CreateDirectoryIfNull(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }



        static void Main(string[] args)
        {
            CreateDirectoryIfNull("logs");
            CreateDirectoryIfNull("data");

            var log_file = $"logs/{now.ToString("MMMM dd, yyyy - HH-mm-ss")}.log";
            log = new StreamWriter(log_file, false, Encoding.UTF8);


            Log("HacVersionListBot v1.1 - SciresM");
            Log($"{now.ToString("MMMM dd, yyyy - HH-mm-ss")}");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            Log("Installed certificate bypass.");

            LoadTitleNames();

            try
            {
                UpdateVersionList();
            }
            catch (Exception ex)
            {
                keep_log = true;
                Log($"An exception occurred: {ex.Message}");
            }

            SaveTitleNames();

            log.Close();
            if (!keep_log)
                File.Delete(log_file);

        }

        private static void UpdateVersionList()
        {
            var old_json = File.ReadAllText("hac_versionlist");
            dynamic old_list = JsonConvert.DeserializeObject(old_json);

            var old_format = old_list["format_version"];
            var do_comparison = true;

            if (old_format != format_version)
            {
                Log($"The most recently-saved versionlist was a newer format than expected {old_format} != {format_version}.");
                Log("Will not do comparison.");
                do_comparison = false;
            }

            var new_data = NetworkUtils.TryConsoleDownload(version_url);
            if (new_data == null)
            {
                Log($"Failed to download a new versionlist.");
                return;
            }

            File.WriteAllBytes("tmp", new_data);
            var new_json = File.ReadAllText("tmp");
            File.Delete("tmp");
            dynamic new_list = JsonConvert.DeserializeObject(new_json);


            var new_path = $"data/hac_versionlist - {new_list["last_modified"]}.json";

            File.WriteAllBytes(new_path, new_data);



            if (new_list["format_version"] != format_version)
            {
                Log($"The most recently-saved versionlist was a newer format than expected {old_format} != {format_version}.");
                Log("Will not do comparison.");
                do_comparison = false;
            }

            var Lines = new List<string>();

            if (do_comparison)
            {
                if (new_list["last_modified"] != old_list["last_modified"])
                {
                    var first_line = $"Comparing newly updated list {new_list["last_modified"]} != {old_list["last_modified"]}.";
                    Log(first_line);
                    Lines.Add(first_line);
                    var old_vers = JsonConvert.DeserializeObject<VersionList>(old_json);
                    var new_vers = JsonConvert.DeserializeObject<VersionList>(new_json);
                    var only_old = old_vers.titles.Where(t => new_vers.titles.All(t2 => t2.id != t.id)).ToList();
                    var only_new = new_vers.titles.Where(t => old_vers.titles.All(t2 => t2.id != t.id)).ToList();
                    var shared_old = old_vers.titles.Where(t => new_vers.titles.Any(t2 => t2.id == t.id)).OrderBy(t => t.id).ToList();
                    var shared_new = new_vers.titles.Where(t => old_vers.titles.Any(t2 => t2.id == t.id)).OrderBy(t => t.id).ToList();
                    foreach (var removed_title in only_old)
                    {
                        var msg = ($"Title {removed_title.id} is no longer in the versionlist! ({TryGetTitleName(removed_title.id)})");
                        Lines.Add(msg);
                        Log(msg);
                    }
                    foreach (var added_title in only_new)
                    {
                        var msg = ($"New Title {added_title.id} was added, version 0x{added_title.version:X5}, required version 0x{added_title.required_version:X5} ({TryGetTitleName(added_title.id)})");
                        Lines.Add(msg);
                        Log(msg);
                    }
                    for (var i = 0; i < shared_old.Count; i++)
                    {
                        var old_title = shared_old[i];
                        var new_title = shared_new[i];
                        if (old_title.version != new_title.version &&
                            old_title.required_version != new_title.required_version)
                        {
                            var msg = ($"Title {old_title.id} changed: Version 0x{old_title.version:X5} => 0x{new_title.version:X5}, Required 0x{old_title.required_version:X5} => 0x{new_title.required_version:X5} ({TryGetTitleName(old_title.id)})");
                            Lines.Add(msg);
                            Log(msg);

                        }
                        else if (old_title.version != new_title.version)
                        {
                            var msg = ($"Title {old_title.id} changed: Version 0x{old_title.version:X5} => 0x{new_title.version:X5} ({TryGetTitleName(old_title.id)})");
                            Lines.Add(msg);
                            Log(msg);

                        }
                        else if (old_title.required_version != new_title.required_version)
                        {
                            var msg = ($"Title {old_title.id} changed: Required 0x{old_title.required_version:X5} => 0x{new_title.required_version:X5} ({TryGetTitleName(old_title.id)})");
                            Lines.Add(msg);
                            Log(msg);
                        }
                    }
                }
            }
            else
            {
                if (new_list["last_modified"] != old_list["last_modified"])
                {
                    var msg = $"A new versionlist was uploaded, but it has a new format version ({new_list["format_version"]}) that can't currently be analyzed.";
                    Log(msg);
                    Lines.Add(msg);
                }
            }

            if (new_list["last_modified"] != old_list["last_modified"])
            {
                File.WriteAllBytes("hac_versionlist", new_data);
            }

            if (Lines.Count > 0)
            {
                var max_len = 2000 - 3 - 3 - 1 - 3;
                var msg = string.Join("\n", Lines);
                if (msg.Length >= max_len) msg = msg.Substring(0, max_len) + "\n...";
                DiscordUtils.SendMessage($"```{msg}```", new_data, $"hac_versionlist - {new_list["last_modified"]}.json");
            }
        }

        internal class VersionList
        {
            public List<TitleVersion> titles { get; set; }
            public int format_version { get; set; }
            public ulong last_modified { get; set; }
        }

        internal class TitleVersion
        {
            public string id { get; set; }
            public int version { get; set; }
            public int required_version { get; set; }
        }
    }
}
