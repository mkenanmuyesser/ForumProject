using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;

namespace Jitbit.Utils
{
	public static class StringUtils
	{
		private static Regex regexURL = new Regex(@"(\[url=([^\]]+)\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regexURL2 = new Regex(@"(\[url\]([^\[]+)\[/url\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regexImg = new Regex(@"\[img width=(\d+) height=(\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regexCOLOR = new Regex(@"(\[color=([^\]]+)\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regexBgColor = new Regex(@"(\[bgcolor=([^\]]+)\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regexSIZE = new Regex(@"(\[size=([^\]]+)\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regexFONT = new Regex(@"(\[font=([^\]]+)\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regexHttp = new Regex(@"(\s|^)(https*://([^\s]+))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex undoItalicInsidePre = new Regex(@"<pre>(.*<i>.*)</pre>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regexQUOTE = new Regex(@"(\[quote=?([^\]]*)\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regexYoutube = new Regex(@"\[youtube](.*?)\[/youtube]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static Regex regexAnchor = new Regex("<a\\s[^<>]*href=[\"']?([^<>]*?)[\"']?(\\s[^<>]*)?>([^<>]*)<\\/a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regexImgHtml = new Regex("<img\\s[^<>]*src=[\"']?([^<>]*?)[\"']?(\\s[^<>]*)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// returns left N chars from a string, or the whole string if N > length
		/// </summary>
		public static string Left(this string input, int length)
		{
			return input.Length > length ? input.Substring(0, length) : input;
		}

		public static bool IsNumeric(this string input)
		{
			double aaa;
			return double.TryParse(input, out aaa);
		}

		public static string FormatForURL(this string input)
		{
			string result = input.Trim().ToLower();
			result = Regex.Replace(result, @"[^a-z\s]", ""); //kill all except spaces and latin chars
			result = Regex.Replace(result, @"\s", "-");
			return HttpUtility.UrlEncode(result);
		}

		public static string ToTimeString(this TimeSpan input)
		{
			return ((int)input.TotalHours).ToString("00") + input.ToString(@"\:mm\:ss");
		}

		/// <summary>
		/// checks if IP address matches a pattern "192.168.*.*"
		/// </summary>
		/// <param name="ip">ip-address</param>
		/// <param name="pattern">pattern in a form of "*.*.*.*"</param>
		/// <returns></returns>
		public static bool IpAddressMatchesPattern(string ip, string pattern)
		{
			if (ip == pattern) return true;
			string regexPattern = pattern.Replace("*", "\\d+").Replace(".", "\\.");
			return Regex.IsMatch(ip, regexPattern);
		}

		public static string GetGravatarUrl(string email, int width = 32)
		{
			email = email ?? "";
			string emailHash = CryptoUtils.MD5Hash(email.ToLower().Trim()).ToLower();
			//= Request.Url.GetLeftPart(UriPartial.Authority) + VirtualPathUtility.ToAbsolute(relativePath)
			return "https://secure.gravatar.com/avatar/" + emailHash + "?s=" + width + "&d=mm";
		}

		public static string GetDateDayName(object date, DateTime currDate, string today, string yesterday, string tomorrow, bool shortDate = false)
		{
			if (date == null || date is DBNull)
				return "";

			DateTime dtDate = ((DateTime)date).Date;

			if (dtDate == currDate)
				return today;
			else if (dtDate == currDate.Date.AddDays(-1))
				return yesterday;
			else if (dtDate == currDate.Date.AddDays(1))
				return tomorrow;
			else
				if (shortDate)
					return dtDate.ToShortDateString();
				else
					return dtDate.ToLongDateString();
		}

		public static string ToAgoString(this DateTime date, string secondsAgo = "seconds ago", string minutesAgo = "min ago", string hoursAgo = "hours ago", string daysAgo = "days ago", string defaultFormatString = "d", DateTime? currentTime = null)
		{
			if(currentTime==null) currentTime = DateTime.Now;
			var ts = new TimeSpan(currentTime.Value.Ticks - date.Ticks);
			double delta = Math.Abs(ts.TotalSeconds);

			if (delta < 60)
			{
				return ts.Seconds + " " + secondsAgo;
			}
			if (delta < 2700) // 45 * 60
			{
				return ts.Minutes + " " + minutesAgo;
			}
			if (delta < 86400) // 24 * 60 * 60
			{
				return (ts.Hours == 0 ? 1 : ts.Hours) + " " + hoursAgo;
			}
			if (delta < 2592000) // 30 * 24 * 60 * 60
			{
				return ts.Days + " " + daysAgo;
			}

			return date.ToString(defaultFormatString);
		}

		private static Regex _rxSpan = new Regex(@"<span[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex _rxTable = new Regex(@"<(/?table|/?tr|/?td)[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex _rxDiv1 = new Regex(@"</div>\s*</div>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex _rxDiv2 = new Regex(@"<div[^>]*>\s*<div", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex _rxDiv3 = new Regex(@"</div>\s*<div", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex _rxDiv4 = new Regex(@"\r\n<div>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex _rxP1 = new Regex(@"</p>\s*</*p>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex _rxP2 = new Regex(@"<p[^>]*>\s*<p", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex _rxP3 = new Regex(@"</p>\s*<p", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public static string HTML2BBCode(string input)
		{
			string msg = input.Trim();
			if (msg.Length == 0) return string.Empty;

			msg = EscapeBbCode(msg);

			//let's remove the linebreaks, cause they mean nothing in HTML,
			//but they will waste space after converting to plaintext
			msg = msg.Replace("\r", "").Replace("\n", " ");

			msg = msg.ReplaceEx("<b>", "[b]").ReplaceEx("</b>", "[/b]");
			msg = msg.ReplaceEx("<strong>", "[b]").ReplaceEx("</strong>", "[/b]");
			msg = msg.ReplaceEx("<i>", "[i]").ReplaceEx("</i>", "[/i]");
			msg = msg.ReplaceEx("<em>", "[i]").ReplaceEx("</em>", "[/i]");
			msg = msg.ReplaceEx("<u>", "[u]").ReplaceEx("</u>", "[/u]");
			msg = msg.ReplaceEx("<br>", "\r\n").ReplaceEx("<br/>", "\r\n").ReplaceEx("<br />", "\r\n");
			msg = msg.ReplaceEx("<hr>", "[hr]").ReplaceEx("<hr/>", "[hr]");
			msg = msg.ReplaceEx("<ul>", "[ul]").ReplaceEx("</ul>", "[/ul]");
			msg = msg.ReplaceEx("<ol>", "[ol]").ReplaceEx("</ol>", "[/ol]");
			msg = msg.ReplaceEx("<li>", "[li]").ReplaceEx("</li>", "[/li]");

			msg = msg.Replace("&quot;", "\"");

			//removing spans, cause sometimes they ruin the double-enter fixer below...
			msg = _rxSpan.Replace(msg, "");
			msg = msg.ReplaceEx(@"</span>", "");

			//to prevent double-breaks
			for (int i = 0; i < 3; i++) //nested divs - replace with one div, run 3 times just in case
			{
				msg = _rxDiv1.Replace(msg, @"</div>");
				msg = _rxDiv2.Replace(msg, @"<div");
			}
			//to prevent double-breaks
			msg = _rxDiv3.Replace(msg, @"<div");
			msg = _rxDiv4.Replace(msg, "\r\n");
			msg = _rxP1.Replace(msg, @"</p>");
			msg = _rxP2.Replace(msg, @"<p");
			msg = _rxP3.Replace(msg, @"<p");

			//finally - replace with line-breaks
			msg = msg.ReplaceEx("</p>", "\r\n").ReplaceEx("<p", "\r\n<p");
			msg = msg.ReplaceEx("</div>", "\r\n").ReplaceEx("<div", "\r\n<div");

			//[URL]
			msg = regexAnchor.Replace(msg, "[url=$1]$3[/url]");

			//[IMG]
			msg = regexImgHtml.Replace(msg, "[img]$1[/img]");

			//[table]
			msg = _rxTable.Replace(msg, x => "[" + x.Groups[1].Value.ToLower() + "]");

			return StripHTML(msg).Replace("&lt;", "<").Replace("&gt;", ">").Trim();
		}

		public static string BBCode2HTML(string input, string quoteWrote = "", bool nofollowLinks = true)
		{
			if (String.IsNullOrEmpty(input)) return string.Empty;

			string sb = input;

			//remove html tags
			sb = sb.Replace("<", "&lt;");
			sb = sb.Replace(">", "&gt;");

			sb = sb.Replace("[b]", "<b>").Replace("[/b]", "</b>");
			sb = sb.Replace("[i]", "<i>").Replace("[/i]", "</i>");
			sb = sb.Replace("[u]", "<u>").Replace("[/u]", "</u>");
			sb = sb.ReplaceEx("[table]", "<table>").ReplaceEx("[/table]", "</table>");
			sb = sb.ReplaceEx("[tr]", "<tr>").ReplaceEx("[/tr]", "</tr>");
			sb = sb.ReplaceEx("[td]", "<td>").ReplaceEx("[/td]", "</td>");
			sb = sb.Replace("[code]", "<code>").Replace("[/code]", "</code>");
			sb = sb.Replace("[img]", "<img src=\"").Replace("[/img]", "\" border=\"0\">");
			sb = sb.Replace("[ul]", "<ul>").Replace("[/ul]", "</ul>");
			sb = sb.Replace("[list]", "<ul>").Replace("[/list]", "</ul>");
			sb = sb.Replace("[ol]", "<ol>").Replace("[/ol]", "</ol>");
			sb = sb.Replace("[li]", "<li>").Replace("[/li]", "</li>");
			sb = sb.Replace("[hr]", "<hr/>");

			//now, if there's "[i]" inside "[code]" this is hardly italic text,
			//it must be some souce-code pasted, like "printf(array[i]);" so lets undo it
			for (Match m = undoItalicInsidePre.Match(sb); m.Success; m = m.NextMatch())
				sb = sb.Replace(m.Groups[0].ToString(), m.Groups[1].ToString().Replace("<i>", "[i]"));     

			//[URL]
			var noFollowAttr = nofollowLinks ? "rel=\"nofollow\"" : "";
			sb = regexURL.Replace(sb, "<a href=\"$2\" target=\"_blank\" " + noFollowAttr + ">");
			sb = regexURL2.Replace(sb, "<a href=\"$2\" target=\"_blank\" " + noFollowAttr + ">$2</a>");

			sb = sb.Replace("[/url]", "</a>");
			sb = sb.Replace("[/URL]", "</a>");

			//[IMG WIDTH HEIGHT]
			sb = regexImg.Replace(sb, "<img width=$1 height=$2 src=\"");

			//[COLOR]
			sb = regexCOLOR.Replace(sb, "<span style=\"color:$2\">");
			sb = sb.Replace("[/color]", "</span>");
			
			//[BGCOLOR]
			sb = regexBgColor.Replace(sb, "<span style=\"background-color:$2\">");
			sb = sb.Replace("[/bgcolor]", "</span>");

			//[SIZE]
			sb = regexSIZE.Replace(sb, "<span style=\"font-size:$2pt\">");
			sb = sb.Replace("[/size]", "</span>");

			//[FONT]
			sb = regexFONT.Replace(sb, "<span style=\"font-family:$2\">");
			sb = sb.Replace("[/font]", "</span>");

			//[YOUTUBE]
			sb = regexYoutube.Replace(sb, "<p style='text-align:center'><iframe width='420' height='315' src='//www.youtube.com/embed/$1' frameborder='0' allowfullscreen></iframe></p>");

			//[QUOTE] (format - "[quote=username]blahblah[\quote]")
			for (Match m = regexQUOTE.Match(sb); m.Success; m = m.NextMatch())
			{
				string strReplace;
				if (m.Groups[2].Length > 0) //if we're quoting SOMEONE  - "[quote=john]"
					strReplace = "<b>" + m.Groups[2] + "</b> " + quoteWrote + ":<br/><blockquote>";
				else //or just quoing - "[quote]"
					strReplace = "<blockquote>";
				sb = sb.Replace(m.Groups[0].ToString(), strReplace);
			}
			sb = sb.Replace("[/quote]", "</blockquote>");

			//http
			sb = ReplaceHttpWithLink(sb);

			//<br>
			sb = sb.Replace("\r", "");
			//we need to manually strip newlines between some tags, otherwise they will screw the markup. I know it's ugly, seems to be no other way
			sb = sb.Replace("<table>\n<tr>", "<table><tr>");
			sb = sb.Replace("</td>\n<td>", "</td><td>");
			sb = sb.Replace("<tr>\n<td>", "<tr><td>");
			sb = sb.Replace("</td>\n</tr>", "</td></tr>");
			sb = sb.Replace("</tr>\n<tr>", "</tr><tr>");
			sb = sb.Replace("</tr>\n</table>", "</tr></table>");

			sb = sb.Replace("\n", "<br/>");

			//bbcode-escaped tags
			sb = sb.Replace("\\[", "[").Replace("\\]", "]");

			return sb;
		}

		public static string EscapeBbCode(string input)
		{
			return input.Replace("[", "\\[").Replace("]", "\\]");
		}

		public static string ReplaceHttpWithLink(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return input;
			string sb = regexHttp.Replace(input, "$1<a href=\"$2\" target=\"_blank\" rel=\"nofollow\">$2</a>");
			return sb;
		}

		public static string SmartTrim(this string text, int length)
		{
			if (text == null)
			{
				throw new ArgumentNullException("text");
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException();
			}
			if (text.Length <= length)
			{
				return text;
			}
			int lastSpaceBeforeMax = text.LastIndexOf(' ', length);
			if (lastSpaceBeforeMax == -1)
			{
				// Perhaps define a strategy here? Could return empty string,
				// or the original
				//throw new ArgumentException("Unable to trim word");
				return text.Substring(0, length);
			}
			return text.Substring(0, lastSpaceBeforeMax);
		}

		/// <summary>
		/// removes everything outside the <body> tag, <script> tags and all betwwen 'em
		/// removes all html tags at all
		/// </summary>
		/// <param name="html"></param>
		/// <returns></returns>
		public static string StripHTML(string html)
		{
			string output = html;

			output = ReplaceEx(output, "<br>", "\r\n");
			output = ReplaceEx(output, "<br/>", "\r\n");
			output = ReplaceEx(output, "<br />", "\r\n");
			output = ReplaceEx(output, "</p>", "\r\n");
			output = ReplaceEx(output, "</div>", "\r\n");

			/*output = output.Replace("<br>", Environment.NewLine);
			output = output.Replace("<br/>", Environment.NewLine);
			output = output.Replace("<br />", Environment.NewLine);*/

			//remove the opening <body> tag
			if (output.ToLower().Contains("<body"))
			{
				output = output.Substring(output.ToLower().IndexOf("<body") + 5);
				output = output.Substring(output.IndexOf(">") + 1);
			}

			//remove the closing </body> tag
			if (output.ToLower().Contains("</body>"))
			{
				output = output.Substring(0, output.ToLower().IndexOf("</body>"));
			}

			//remove <head> tags and everything between
			output = Regex.Replace(output, @"<head.*?>.*?<\/head>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

			//remove <style> tags and everything between
			output = Regex.Replace(output, @"<style.*?>.*?<\/style>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

			//remove <script> tags and everything between
			output = Regex.Replace(output, @"<script.*?>.*?<\/script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

			//strip all html tags
			output = Regex.Replace(output, @"<.*?>", string.Empty, RegexOptions.Singleline);

			//strip &nbsp;
			output = output.Replace("&nbsp;", " ");

			return output;
		}

		private static Regex _rgxStripBbCodeImg = new Regex(@"\[img[^\]]*\][^\]]*($|\[/img\])", RegexOptions.Compiled);
		private static Regex _rgxStripBbCode = new Regex(@"\[(.|\n)*?\]", RegexOptions.Compiled);
		public static string StripBBCode(string input)
		{
			//strip [img] tag
			//this regex matches [img] tag - either till the closing [/img] is found, or it is the end of the string
			//because the [img] tag can have "data:image/base64" shit that is missing closing tag (text too long)
			string output = _rgxStripBbCodeImg.Replace(input, string.Empty);

			//strip all BBCode tags
			return _rgxStripBbCode.Replace(output, string.Empty);
		}

		private static Regex _rgxCrLfs = new Regex(@"(\r?\n){3,}", RegexOptions.Compiled);
		public static string RemoveRepeatingCRLFs(string input)
		{
			return _rgxCrLfs.Replace(input, "\n\n");
		}

		//case-insensitive string-replace
		private static string ReplaceEx(this string original, string pattern, string replacement)
		{
			int count = 0, position0 = 0, position1 = 0;
			string upperString = original.ToUpper();
			string upperPattern = pattern.ToUpper();

			int inc = (original.Length / pattern.Length) *
					  (replacement.Length - pattern.Length);
			char[] chars = new char[original.Length + Math.Max(0, inc)];
			while ((position1 = upperString.IndexOf(upperPattern, position0)) != -1)
			{
				for (int i = position0; i < position1; ++i)
					chars[count++] = original[i];
				for (int i = 0; i < replacement.Length; ++i)
					chars[count++] = replacement[i];
				position0 = position1 + pattern.Length;
			}
			if (position0 == 0) return original;
			for (int i = position0; i < original.Length; ++i)
				chars[count++] = original[i];
			return new string(chars, 0, count);
		}

		//checks if an input string has "</body>", "</div>" or "</span>" in it
		public static bool HasCommonHtmlTags(string input)
		{
			string lowInput = input.ToLower();
			return (lowInput.IndexOf("</body>") > -1
				|| lowInput.IndexOf("</div>") > -1
				|| lowInput.IndexOf("</span>") > -1
				|| lowInput.IndexOf("</p>") > -1
				|| lowInput.IndexOf("</script>") > -1);
		}

		/// <summary>
		/// if the address does not start with "http://" this method adds "http://" prefix
		/// </summary>
		public static string NormalizeHttpAddress(string address)
		{
			if (address.Trim().Length > 0
				&& !address.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
				return "http://" + address;
			else
				return address;
		}

		/// <summary>
		/// Text similarity algorythm. 
		/// </summary>
		/// <param name="s"></param>
		/// <param name="t"></param>
		/// <returns>Similarity index. Less is better.</returns>
		public static float Similarity(string s, string t)
		{
			s = s.ToLower();
			t = t.ToLower();
			int n = s.Length; 
			int m = t.Length; 
			int[,] d = new int[n + 1, m + 1]; 
			int cost; // cost

			if (n == 0) return m;
			if (m == 0) return n;

			for (int i = 0; i <= n; d[i, 0] = i++) ;
			for (int j = 0; j <= m; d[0, j] = j++) ;

			for (int i = 1; i <= n; i++)
			{
				for (int j = 1; j <= m; j++)
				{
					cost = (t.Substring(j - 1, 1) == s.Substring(i - 1, 1) ? 0 : 1);
					d[i, j] = System.Math.Min(System.Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
				}
			}

			return (float)d[n, m]/t.Length;
		}

		public static void GetFirstLastNameFromFullName(string fullname, out string firstname, out string lastname)
		{
			firstname = lastname = "";

			if (!string.IsNullOrWhiteSpace(fullname))
			{
				fullname = fullname.Trim();
				if (fullname.Contains(",")) //it's probably in this form "Smith, John"
				{
					int pos = fullname.IndexOf(",");
					firstname = fullname.Substring(pos + 1).Trim();
					lastname = fullname.Substring(0, pos).Trim();
				}
				else if (fullname.Contains(" "))
				{
					int pos = fullname.IndexOf(" "); //it's in the normal form "John Smith"
					if (pos > 0)
					{
						firstname = fullname.Substring(0, pos).Trim();
						lastname = fullname.Substring(pos + 1).Trim();
					}
				}
				else
					firstname = fullname;
			}
		}
		public static int CountOccurences(string needle, string haystack)
		{
			return (haystack.Length - haystack.Replace(needle, "").Length) / needle.Length;
		}
	}
}
