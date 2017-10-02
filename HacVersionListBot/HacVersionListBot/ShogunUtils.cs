using System;
using Newtonsoft.Json;

namespace HacVersionListBot
{
    class ShogunUtils
    {
        // Shogun is awful. So, so awful.
        private static string url_prefix = "https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1";

        public static string GetNsUid(string title_id, string country, string type)
        {
            var url = $"{url_prefix}/contents/ids?shop_id=4&lang=en&country={country}&type={type}&title_ids={title_id}";
            var id_pair_res = NetworkUtils.TryMakeShogunRequest(url);
            if (id_pair_res == null)
            {
                return null;
            }

            try
            {
                dynamic id_pairs = JsonConvert.DeserializeObject(id_pair_res);
                var pairs = id_pairs["id_pairs"];
                foreach (var pair in pairs)
                {
                    if (pair["title_id"] == title_id.ToUpper())
                    {
                        return pair["id"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log($"Error ({ex.Message}) on attempt to get {url} -- response {id_pair_res}.");
                return null;
            }
            return null;
        }

        public static string GetNameFromNsUid(string ns_uid, string country)
        {
            var url = $"{url_prefix}/titles/{ns_uid}?shop_id=4&lang=en&country={country}";
            var title_res = NetworkUtils.TryMakeShogunRequest(url);
            if (title_res == null)
            {
                return null;
            }

            try
            {
                dynamic title_meta = JsonConvert.DeserializeObject(title_res);
                return title_meta["formal_name"];
            }
            catch (Exception ex)
            {
                Program.Log($"Error ({ex.Message}) on attempt to get {url} -- response {title_res}.");
                return null;
            }
        }

        public static string GetBaseTitleId(string title_id)
        {
            return title_id.Substring(0, 13) + "000";
        }

        public static string GetTitleName(string title_id)
        {
            var base_title_id = GetBaseTitleId(title_id);
            foreach (var country in new[] {"US", "GB", "JP", "AU", "TW", "KR"})
            {
                var ns_uid = GetNsUid(base_title_id, country, "title");
                if (ns_uid == null) continue;
                var is_demo = GetNsUid(base_title_id, country, "demo") != null;

                var title_name = GetNameFromNsUid(ns_uid, country);
                if (title_name == null) continue;
                if (is_demo) title_name += " (Demo)";
                if (title_id != base_title_id) title_name += " (Update)";
                return title_name.Replace("\r", "\\r").Replace("\n", "\\n");
            }
            return null;
        }

    }
}
