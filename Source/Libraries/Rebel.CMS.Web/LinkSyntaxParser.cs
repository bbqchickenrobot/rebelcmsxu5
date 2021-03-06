﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Rebel.Framework;

namespace Rebel.Cms.Web
{
    public class LinkSyntaxParser
    {
        public static readonly Regex TagMatch = new Regex(@"(<[^>]*?data-rebel-link=""internal""[^>]*?>)", RegexOptions.Compiled);
        public static readonly Regex HrefMatch = new Regex(@"href=""(?<Href>.*?)""", RegexOptions.Compiled);
        public static readonly Regex SrcMatch = new Regex(@"src=""(?<Src>.*?)""", RegexOptions.Compiled);
        public static readonly Regex RebelSrcMatch = new Regex(@"data-rebel-src=""(?<Src>.*?)""", RegexOptions.Compiled);

        /// <summary>
        /// Finds an internal link amongst the input content (generally html) and renders the correct url into the output.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="linkFoundCallback">The link found callback.</param>
        /// <returns></returns>
        public string Parse(string input,
            Func<HiveId, string> linkFoundCallback) 
        {
            var s = TagMatch.Replace(input, x =>
            {
                var strLink = x.ToString();

                // Links
                if(HrefMatch.IsMatch(strLink))
                {
                    var hiveId = new HiveId(HrefMatch.Match(strLink).Groups["Href"].Value);

                    var url = linkFoundCallback(hiveId);

                    strLink = HrefMatch.Replace(strLink, "href=\"" + url + "\"");
                    strLink = strLink.Replace(@"data-rebel-link=""internal""", "");
                }
                // Media
                else if (SrcMatch.IsMatch(strLink) && RebelSrcMatch.IsMatch(strLink))
                {
                    var hiveId = new HiveId(RebelSrcMatch.Match(strLink).Groups["Src"].Value);

                    var url = linkFoundCallback(hiveId);

                    if(!url.IsNullOrWhiteSpace())
                    {
                        strLink = SrcMatch.Replace(strLink, "src=\"" + url + "\"");
                        strLink = RebelSrcMatch.Replace(strLink, "");
                        strLink = strLink.Replace(@"data-rebel-link=""internal""", "");
                    }
                    else
                    {
                        strLink = "";
                    }
                }

                return strLink;
            });

            return s;
        }
    }
}
