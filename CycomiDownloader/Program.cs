using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace CycomiDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Enter manga url:");
            string mangaUrl = Console.ReadLine();
            string mangaPageHtml = "";
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                mangaPageHtml = wc.DownloadString(mangaUrl);
            }

            string mangaTitle = new Regex("<h2 class=\"left\">(?<title>.+?)<\\/h2>")
                .Match(mangaPageHtml).Groups["title"].Value;
            string mangaPath = Environment.CurrentDirectory + "\\" + mangaTitle + "\\";
            Directory.CreateDirectory(mangaPath);

            Regex chList = new Regex("(?<url>viewer\\.php\\?chapter_id=\\d+&param=other)\">[\\s\\S]+?chapter-number\">(?<chNum>.+?)<\\/p>[\\s\\S]+?chapter_name\">(?<chName>.+?)<\\/p>");
            foreach (Match chListMc in chList.Matches(mangaPageHtml))
            {
                string chLink = "https://cycomi.com/" + chListMc.Groups["url"].Value;
                string chNum = chNumTranslate(chListMc.Groups["chNum"].Value);
                string chName = chListMc.Groups["chName"].Value;

                int dup = 1;
                while (Directory.Exists(mangaPath + chNum + String.Format("({0})", dup)))
                    dup++;
                if (dup > 1)
                    Directory.CreateDirectory(mangaPath + chNum + String.Format("({0})", dup));
                else
                    Directory.CreateDirectory(mangaPath + chNum);

                string chapterPageHtml = "";
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    chapterPageHtml = wc.DownloadString(chLink);
                }
                Parallel.ForEach(new Regex("\"(?<url>https?:\\/\\/storage\\.cycomi\\.com\\/images\\/jpeg\\/page\\/.+?\\/high\\/(?<fileName>.+?))\"")
                    .Matches(chapterPageHtml).Cast<Match>(), mc =>
                    {
                        string fileName = mangaPath + chNum + "\\" 
                                + String.Format(
                                "{0}.{1}", 
                                mc.Groups["fileName"].Value.Split('.').First().PadLeft(3,'0'),
                                mc.Groups["fileName"].Value.Split('.').Last()
                                );
                        using (WebClient wc = new WebClient())
                            wc.DownloadFile(mc.Groups["url"].Value, fileName);
                        //Console.WriteLine("Downloaded " + fileName);
                    });
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Downloaded " + chNum);
                Console.ForegroundColor = ConsoleColor.White;
            }
            Console.WriteLine("END.");
            Console.ReadKey();
        }

        static string chNumTranslate(string ch)
        {
            ch = ch.Normalize(NormalizationForm.FormKC);
            Match chSymbMch = new Regex(@"第(?<n>\d+)話").Match(ch);
            try
            {
                return ch.Replace(chSymbMch.Value, "ch_" + chSymbMch.Groups["n"].Value.PadLeft(3, '0'))
                    .Replace("【", "").Replace("】", "")
                    .Replace("おまけ", "_omake");
            }
            catch
            {
                return ch;
            }
        }
    }
}
