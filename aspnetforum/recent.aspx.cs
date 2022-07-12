using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data.Common;
using System.Configuration;
using System.Text;
using System.Web.Caching;
using aspnetforum.Utils;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for recent.
	/// </summary>
	public partial class recent : ForumPage
	{
		protected void Page_Load(object sender, System.EventArgs e)
		{
			bool bRss = (Request.QueryString["rss"] == "1");

			MetaDescription = Settings.ForumTitle + " - most recent forum posts. Sorted top to bottom.";

			if (bRss)
			{
				Response.Clear();
				Response.ContentType = "text/xml";
				Response.Write(GetRssXML());
				Response.End();
				return;
			}

			//sql injection prevention
			if (!String.IsNullOrEmpty(Request.QueryString["rss"])) //it's not empty and != 1
			{
				Response.TrySkipIisCustomErrors = true;
				Response.StatusCode = 400;
				Response.Write("Bad request");
				Response.End();
				return;
			}
		}

		private string GetRssXML()
		{
			if (Cache["RecentRSS"] != null)
			{
				return Cache["RecentRSS"] as string;
			}

			string retval = "";

			retval += "<?xml version=\"1.0\"?>\r\n";
			retval += "<rss version=\"2.0\">\r\n";
			retval += "<channel>\r\n";
			retval += "<title>" + Utils.Settings.ForumTitle.Replace("&", "&amp;") + " - Recent Posts</title>\r\n";
			retval += "<link>" + Utils.Various.ForumURL + "recent.aspx</link>\r\n";
			retval += "<description>" + Utils.Settings.ForumTitle.Replace("&", "&amp;") + " - Recent Posts</description>\r\n";
			retval += "<language>en-us</language>\r\n";
			retval += "<docs>http://blogs.law.harvard.edu/tech/rss</docs>\r\n";
			retval += "<generator>Jitbit AspNetForum</generator>\r\n";

			Cn.Open();
			DbDataReader dr = Cn.ExecuteReader(@"SELECT TOP 30 ForumMessages.Body, ForumMessages.CreationDate, ForumTopics.TopicID, ForumTopics.Subject,
					ForumUsers.UserName, ForumUsers.FirstName, ForumUsers.LastName, ForumMessages.UserID, ForumUsers.PostsCount
				FROM (ForumMessages INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID)
				LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID
				WHERE ForumTopics.ForumID NOT IN (SELECT ForumID FROM ForumGroupPermissions WHERE AllowReading=?)
				AND ForumTopics.ForumID NOT IN (SELECT ForumID FROM Forums WHERE MembersOnly=?)
				ORDER BY ForumMessages.MessageID DESC", true, true);
			
			if(dr.HasRows)
			{
				int i = 0;
				while(dr.Read())
				{
					if(i==0) //first record
					{
						retval += string.Format("<pubDate>{0}</pubDate>\r\n", ((DateTime)dr["CreationDate"]).ToString("r") );
						retval += string.Format("<lastBuildDate>{0}</lastBuildDate>\r\n", ((DateTime)dr["CreationDate"]).ToString("r") );
					}
					i++;

					//items
					retval += "<item>\r\n";
					retval += string.Format("<link>{0}</link>\r\n", Utils.Various.ForumURL + Utils.Various.GetTopicURL(dr["TopicID"], dr["Subject"]));
					retval += "<title>Topic &quot;" + dr["Subject"].ToString().Replace("&", "&amp;") + "&quot; a message from " + Utils.User.GetUserDisplayName(dr["UserName"], dr["FirstName"], dr["LastName"]).Replace("&", "&amp;") + "</title>\r\n";
					retval += string.Format("<description><![CDATA[{0}]]></description>\r\n", Utils.Formatting.FormatMessageHTML(dr["Body"].ToString()));
					retval += string.Format("<pubDate>{0}</pubDate>\r\n", ((DateTime)dr["CreationDate"]).ToString("r") );
					retval += "</item>\r\n";
				}
			}
			dr.Close();
			Cn.Close();

			retval += "</channel>\r\n";
			retval += "</rss>\r\n";

			Cache.Add("RecentRSS", retval.ToString(), null, DateTime.Now.AddMinutes(15), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);

			return retval;
		}
	}
}
