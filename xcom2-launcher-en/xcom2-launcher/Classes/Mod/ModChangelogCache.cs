﻿using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XCOM2Launcher.Mod
{
    public static class ModChangelogCache
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(nameof(ModChangelogCache));
        private static readonly ConcurrentDictionary<long, string> Cache = new ConcurrentDictionary<long, string>();

        private static readonly Regex Regexp = new Regex(@"<div class=""detailBox workshopAnnouncement noFooter"">\s*<div class=""changelog headline"">\s*(.*)\s*</div>\s*<p id=""[0-9]+"">(.*)</p>", RegexOptions.Compiled);

        public static async Task<string> GetChangeLogAsync(long workshopID)
        {
            if (Cache.TryGetValue(workshopID, out var changelog))
            {
                return changelog;
            }

            try
            {
                using (var client = new WebClient())
                {
                    var htmlstrip = new Regex("<.*?>", RegexOptions.Compiled);
                    var changelogRaw = await client.DownloadStringTaskAsync("https://steamcommunity.com/sharedfiles/filedetails/changelog/" + workshopID);

                    var output = new StringBuilder();
                    foreach (Match m in Regexp.Matches(changelogRaw))
                    {
                        var desc = m.Groups[2].Value.Replace("<br>", "\r\n\t");
                        desc = WebUtility.HtmlDecode(desc);
                        desc = htmlstrip.Replace(desc, "").Trim();

                        if (desc.Length == 0)
                            desc = "No description.";

                        output.AppendLine(m.Groups[1].Value.Trim());
                        output.AppendLine("\t" + desc);
                        output.AppendLine();
                    }

                    Cache.TryAdd(workshopID, output.ToString());

                    return output.ToString();
                }
            }
            catch (WebException ex)
            {
                Log.Warn($"Error loading changelog for Workshop Id: {workshopID}", ex);
                return "An error occurred while loading the changelog.";
            }
        }
    }
}