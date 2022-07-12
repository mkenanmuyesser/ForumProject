using System;
using System.Configuration;
using System.Text;
using System.Web;
using System.Resources;
using System.Web.Configuration;
using System.Data.Common;
using System.Data;
using Jitbit.Utils;

namespace aspnetforum.Utils
{
	public struct ForumStats
	{
		public int MemberCount, ThreadCount, PostCount;
	}

	public partial class Various
	{
		//returns current datetime including the "ServerTimeOffset" setting
		public static DateTime GetCurrTime()
		{
			return DateTime.Now.AddHours(Settings.ServerTimeOffset);
		}

		//for backward compatibility only
		[Obsolete("This is obsolete, use User.DisplayUserInfo instead")]
		public static string DisplayUserInfo(object uID, object uname, object postsCount, object avatarFileName, object firstName, object lastName)
		{
			return User.DisplayUserInfo(uID, uname, postsCount, avatarFileName, null, firstName, lastName);
		}

		//for backward compatibility only
		[Obsolete("This is obsolete, use User.CurrentUserID instead")]
		public static int CurrentUserID { get { return User.CurrentUserID; } }

		//returns true, if SEO-friendly URLs are enabled (if module is loaded)
		public static bool SEOUrlsEnabled
		{
			get
			{
				return ForumSEOHttpModule.SEOUrlsEnabled;
			}
		}

		public static string GetSafeFileNameFromQueryStirng(string queryString)
		{
			return queryString.Replace("..", "").Replace(@"\", "").Replace("/", "");
		}

		public static string GetUserIpAddress(HttpRequest request)
		{
			if (request == null) return "";

			// Look for a proxy address first
			string ip = request.Headers["HTTP_X_FORWARDED_FOR"];
			if (string.IsNullOrEmpty(ip))
				ip = request.Headers["X-Forwarded-For"];
			if (string.IsNullOrEmpty(ip))
				ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

			// If there is no proxy, get the standard remote address
			if (string.IsNullOrEmpty(ip) || ip.ToLower() == "unknown")
			{
				ip = request.UserHostAddress;
			}

			return ip;
		}

		//static method returns the topic url
		//if the module IS LOADED, it returns the topic URL like "topic123-title-of-the-topic.aspx"
		//if not loade - return "messages.aspx?TopicID=123"
		public static string GetTopicURL(object topicID, object subject, bool lastPage = false)
		{
			string url;
			if (!SEOUrlsEnabled)
			{
				url = "messages.aspx?TopicID=" + topicID;
				if (lastPage) url += "&lastpage=1";
			}
			else
			{
				url = "topic" + topicID + "-" + subject.ToString().FormatForURL() + ".aspx";
				if (lastPage) url += "?lastpage=1";
			}
			return url;
		}

		//static method returns the forum url
		//if the module IS LOADED, it returns the topic URL like "forum123-title-of-the-forum.aspx"
		//if not loade - return "topics.aspx?ForumID=123"
		public static string GetForumURL(object forumID, object subject)
		{
			if (!SEOUrlsEnabled)
				return "topics.aspx?ForumID=" + forumID;
			else
				return "forum" + forumID + "-" + subject.ToString().FormatForURL() + ".aspx";
		}

		/// <summary>
		/// gets the forum-application URL (like "http://myserver.com/forum"
		/// </summary>
		public static string ForumURL
		{
			get
			{
				if (!string.IsNullOrEmpty(Settings.ForumURL)) return Settings.ForumURL;

				if (HttpContext.Current == null) return null;

				string url = HttpContext.Current.Request.Url.ToString();
				url = url.Substring(0, url.LastIndexOf("/") + 1);
				return url;
			}
		}


		[Obsolete("This is obsolete, use Attachments.GetThumbnail instead")]
		public static string GetThumbnail(string filename, int userID)
		{
			return Attachments.GetThumbnail(filename, userID);
		}

		public static ForumStats GetStats()
		{
			HttpContext context = HttpContext.Current;

			//here we're checking the cache
			if (context.Cache["ForumStats"] == null)
			{
				ForumStats retval;
				DbCommand cmd = Utils.DB.CreateCommand();
				cmd.Connection.Open();
				cmd.CommandText = "SELECT COUNT(*) FROM ForumTopics";
				retval.ThreadCount = Convert.ToInt32(cmd.ExecuteScalar());
				cmd.CommandText = "SELECT COUNT(*) FROM ForumMessages";
				retval.PostCount = Convert.ToInt32(cmd.ExecuteScalar());
				cmd.CommandText = "SELECT COUNT(*) FROM ForumUsers";
				retval.MemberCount = Convert.ToInt32(cmd.ExecuteScalar());
				cmd.Connection.Close();
				context.Cache.Add("ForumStats", retval, null, DateTime.Now.AddMinutes(15), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
				return retval;
			}
			else
			{
				return (ForumStats)context.Cache["ForumStats"];
			}
		}

		public static string GetPaginationString(int pageIndex, int totalPages, string baseUrl)
		{
			StringBuilder pagerString = new StringBuilder();
			//string navigationFormat = "Content.aspx?Page={0}";

			// the maximum number of pages to display in the range of numbers
			int maxPages = 3;

			// set defaults
			int currentPage = pageIndex + 1;
			int startPage = 1;
			int endPage = totalPages;

			// adjust startPage if the currentPage is more than maxPages
			if (currentPage > maxPages)
			{
				startPage = pageIndex;
				// display the 1.. first page link
				pagerString.Append("<a href=\"" + baseUrl + "\">1..</a>");
			}

			string queryDelimeter = (baseUrl.IndexOf("?") > -1) ? "&" : "?";

			// adjust endPage if the page count is more than maxPages 
			if (totalPages > maxPages)
				endPage = startPage + maxPages;

			// display the range of page number links (upto maxPages)
			for (int i = startPage; (i <= endPage && i <= totalPages); i++)
			{
				if ((i - 1) != pageIndex)
				{
					pagerString.Append("<a href=\"");
					if (i != 1)
						pagerString.Append(baseUrl + queryDelimeter + "Page=" + (i - 1));
					else
						pagerString.Append(baseUrl); //for SEO - to prevent links like "Page=0" that point to same page
					pagerString.Append("\">");
				}
				else
					pagerString.Append("<span>");
				
				pagerString.Append(i); //page num
				
				if ((i - 1) != pageIndex)
					pagerString.Append("</a>");
				else
					pagerString.Append("</span>");
			}

			if ((endPage < totalPages) && (endPage != totalPages))
			{
				// display the last page link
				if (endPage < totalPages - 1)
					pagerString.Append(" ..");
				pagerString.Append("<a href=\"");
				pagerString.Append(baseUrl + queryDelimeter + "Page=" + (totalPages - 1));
				pagerString.Append("\">");
				pagerString.Append(totalPages.ToString());
				pagerString.Append("</a>");
			}

			return pagerString.ToString();
		}
	}
}
