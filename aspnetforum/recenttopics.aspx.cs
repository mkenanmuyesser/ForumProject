using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.Common;
using System.Text;
using System.Web.Caching;
using aspnetforum.Utils;

namespace aspnetforum
{
    public partial class recenttopics : ForumPage
    {
        bool bRss;

        protected void Page_Load(object sender, EventArgs e)
        {
            bRss = (Request.QueryString["rss"] == "1");

			MetaDescription = Settings.ForumTitle + " - most recently updated forum topics. Sorted top to bottom.";

            if (!bRss)
            {
                BindRepeater(rptTopicsList);
            }
            else
            {
                Response.Clear();
                Response.ContentType = "text/xml";
                Response.Write(GetRssXML());
                Response.End();
            }
        }

        private string GetRssXML()
        {
            if (Cache["RecentTopicsRSS"] != null)
            {
                return Cache["RecentTopicsRSS"] as string;
            }

			StringBuilder retval = new StringBuilder();
			retval.Append("<?xml version=\"1.0\"?>\r\n");
			retval.Append("<rss version=\"2.0\">\r\n");
			retval.Append("<channel>\r\n");
			retval.Append("<title>" + Utils.Settings.ForumTitle.Replace("&", "&amp;") + " - Recently updated topics</title>\r\n");
			retval.Append("<link>" + Utils.Various.ForumURL + "recenttopics.aspx</link>\r\n");
			retval.Append("<description>" + Utils.Settings.ForumTitle.Replace("&", "&amp;") + " - Recently updated topics</description>\r\n");
			retval.Append("<language>en-us</language>\r\n");
			retval.Append("<docs>http://blogs.law.harvard.edu/tech/rss</docs>\r\n");
			retval.Append("<generator>Jitbit AspNetForum</generator>\r\n");

            Cn.Open();

			DbDataReader dr = Cn.ExecuteReader(@"SELECT TOP 30 ForumTopics.TopicID, ForumTopics.Subject, ForumTopics.LastMessageID, ForumMessages.CreationDate, ForumTopics.RepliesCount
                FROM ForumTopics
                INNER JOIN ForumMessages ON ForumTopics.LastMessageID=ForumMessages.MessageID
                WHERE ForumTopics.Visible=?
                AND ForumTopics.ForumID NOT IN (SELECT DISTINCT ForumID FROM ForumGroupPermissions WHERE AllowReading=?)
                AND ForumTopics.ForumID NOT IN (SELECT ForumID FROM Forums WHERE MembersOnly=?)
                ORDER BY ForumTopics.LastMessageID DESC", true, true, true);

            if (dr.HasRows)
            {
                int i = 0;
                while (dr.Read())
                {
                    if (i == 0) //first record
                    {
                        retval.Append(string.Format("<pubDate>{0}</pubDate>\r\n", ((DateTime)dr["CreationDate"]).ToString("r")));
                        retval.Append(string.Format("<lastBuildDate>{0}</lastBuildDate>\r\n", ((DateTime)dr["CreationDate"]).ToString("r")));
                    }
                    i++;

                    //items
                    retval.Append("<item>\r\n");
                    retval.Append(string.Format("<link>{0}</link>\r\n", Utils.Various.ForumURL + Utils.Various.GetTopicURL(dr["TopicID"], dr["Subject"])));
                    retval.Append("<title>" + dr["Subject"].ToString().Replace("&", "&amp;") + "</title>\r\n");
                    retval.Append(string.Format("<description><![CDATA[{0}]]></description>\r\n", Utils.Formatting.FormatMessageHTML(dr["Subject"].ToString())));
                    if (dr["CreationDate"] != DBNull.Value)
                        retval.Append(string.Format("<pubDate>{0}</pubDate>\r\n", ((DateTime)dr["CreationDate"]).ToString("r")));
                    retval.Append("</item>\r\n");
                }
            }
            dr.Close();
            Cn.Close();

            retval.Append("</channel>\r\n");
            retval.Append("</rss>\r\n");

			Cache.Add("RecentTopicsRSS", retval.ToString(), null, DateTime.Now.AddMinutes(15), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);

            return retval.ToString();
        }

        public static void BindRepeater(Repeater rptTopicsList)
        {
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				DbDataReader dr;

				string sql = @"SELECT TOP 30 ForumTopics.TopicID, ForumTopics.Subject, ForumTopics.LastMessageID, ForumTopics.RepliesCount,
						ForumMessages.UserID as LastUserID, ForumUsers.UserName as LastUserName, ForumUsers.FirstName as LastFirstName,
						ForumUsers.LastName as LastLastName, ForumMessages.Body, ForumMessages.CreationDate
                    FROM (ForumTopics
					INNER JOIN ForumMessages ON ForumMessages.MessageID = ForumTopics.LastMessageID)
					LEFT JOIN ForumUsers ON ForumUsers.UserID=ForumMessages.UserID
                    WHERE ForumTopics.Visible=?";

				if (Utils.User.CurrentUserID == 0) //if anonymous user - hide "membersonly" forums
				{
					dr = cn.ExecuteReader(sql + @"
						AND ForumTopics.ForumID NOT IN (SELECT DISTINCT ForumID FROM ForumGroupPermissions WHERE AllowReading=?)
						AND ForumTopics.ForumID NOT IN (SELECT ForumID FROM Forums WHERE MembersOnly=?)
						ORDER BY ForumTopics.LastMessageID DESC", true, true, true);
				}
				else
				{
					string strSQLAllowedForums = Utils.Forum.GetReadableForumsForUserString(Utils.User.CurrentUserID);

					dr = cn.ExecuteReader(sql + @"
						AND ForumTopics.ForumID IN (" + strSQLAllowedForums + @")
						ORDER BY ForumTopics.LastMessageID DESC", true);
				}

				DataTable dt = new DataTable();
				dt.Load(dr);
				dr.Close();
				rptTopicsList.DataSource = dt;
				rptTopicsList.DataBind();
				cn.Close();
			}
        }
    }
}
