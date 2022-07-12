using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data.Common;
using System.Text;
using System.IO;
using System.Configuration;
using System.Web.Caching;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for topics.
	/// </summary>
	public partial class topics : ForumPage
	{
		protected int _forumID;
		protected string pagerString = "";
		bool _mailNotificationsEnabled;
		bool _membersOnly = true;
		bool _premoderated = false;
		bool _restrictTopicCreation;
		protected bool _isModerator = false;
		string _forumName;

		private bool ExtractForumID()
		{
			int.TryParse(Request.QueryString["ForumID"], out _forumID);
			if (_forumID == 0)
			{
				Response.Write("forum not found");
				Response.End();
				return false;
			}
			return true;
		}
	
		protected void Page_Load(object sender, System.EventArgs e)
		{
			if (!ExtractForumID()) return;

			//if its RSS and RSS exists in cache - show it right away (to prevent connecting to the database)
			if (Request.QueryString["rss"] == "1" && Cache["TopicsRSS" + _forumID] != null)
			{
				string rss = Cache["TopicsRSS" + _forumID] as string;
				SendOutRssAndQuit(rss);
				return;
			}

			_mailNotificationsEnabled = Utils.Settings.MailNotificationsEnabled;
			_isModerator = IsModerator(_forumID);
			rssLink.HRef = "topics.aspx?ForumID=" + _forumID + "&amp;rss=1";
			rssDiscoverLink.Attributes["href"] = rssLink.HRef;

			Cn.Open();

			//display the forum title and description, check if anonymous access allowed
			if(!GetGeneralForumInfo())
			{
				Cn.Close(); Response.End(); return; //forum not found
			}

			bool denyAnonymousUser = (_membersOnly && CurrentUserID == 0);
			//if the forum is members only - STOP for anonymous guests or STOP for non-group members
			if (denyAnonymousUser || !Utils.Forum.CheckForumReadPermissions(_forumID, CurrentUserID))
			{
				Cn.Close();
				if (Request.QueryString["rss"] != "1") //show error messages - but only if its not RSS
				{
					divError.Visible = tblError.Visible = lblDenied.Visible = true;
					divMain.Visible = spanAddTopic.Visible = divDescription.Visible = false;
					if (denyAnonymousUser)
						divError.InnerHtml = "The forum <b>\"" + _forumName + "\"</b> is for authenticated users only. Please login or register.";
					else
						divError.InnerHtml = "Access denied. No permission.";
				}
				else
				{
					//save empty string to the rss-cache
					Cache.Add("TopicsRSS" + _forumID, "", null, Cache.NoAbsoluteExpiration, TimeSpan.FromHours(1), CacheItemPriority.Normal, null);
					Response.End();
				}
				return;
			}

			//is it rss request?
			if (Request.QueryString["rss"] == "1")
			{
				string rss = GetRssXML();
				Cn.Close();
				Cache.Add("TopicsRSS" + _forumID, rss, null, DateTime.Now.AddHours(1), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
				SendOutRssAndQuit(rss);
				return;
			}

			SubscribeButtonVisibility();

			//breadcrumbs
			lblCurForum.Text = Utils.Forum.GetForumBreadCrumbs(_forumID, Cn);

			//display subforums of the forum
			BindSubforums(Cmd, rptSubForumsList, _forumID, CurrentUserID);

			//hide "add post" link if user is a guest OR topic creation is disabled
			if (
				(CurrentUserID != 0 || Utils.Settings.AllowGuestThreads)
				&&
				(!_restrictTopicCreation || _isModerator)
			   )
			{
				spanAddTopic.Visible = true;
			}
			else
			{
				spanAddTopic.Visible = false;
			}

			//show "register to post" link to guest users
			spanRegister.Visible = (CurrentUserID == 0 && !Utils.Settings.AllowGuestThreads);
			
			BindTopicsRepeater(Cmd, rptTopicsList, PageSize, Request, out pagerString, _isModerator, _forumID, _premoderated);
			if (rptTopicsList.Items.Count == 0 && rptSubForumsList.Items.Count == 0)
			{
				divError.InnerHtml = "The forum is empty, no topics have been created yet.";
				divError.Visible = tblError.Visible = true;
			}
			Cn.Close();
		}

		private bool GetGeneralForumInfo()
		{
			string descr;
			if (!Utils.Forum.GetBasicForumInfo(_forumID, Cn, out _forumName, out descr, out _restrictTopicCreation, out _premoderated, out _membersOnly))
				return false; //not found in db

			divDescription.InnerHtml = descr;
			divDescription.Visible = !string.IsNullOrEmpty(descr);

			Title = _forumName;
			MetaDescription = Utils.Formatting.StripHTML(descr);
			if(!string.IsNullOrEmpty(Request.QueryString["page"]))
			{
				MetaDescription += " : page " + Request.QueryString["page"];
				Title += " : page " + Request.QueryString["page"];
			}
			MetaKeywords = Title;

			return true;
		}

		public static string DisplayUserName(object username, object userId, object firstName, object lastName)
		{
			string strUsername = username.ToString();
			if (strUsername.Trim() == "") return Resources.various.Guest;

			strUsername = Utils.User.GetUserDisplayName(username, firstName, lastName);
			return "<a href=\"viewprofile.aspx?UserID=" + userId + "\">" + strUsername + "</a>";
		}

		private void SubscribeButtonVisibility()
		{
			if(!_mailNotificationsEnabled || CurrentUserID == 0) //if anonymous or disabled
			{
				btnSubscribe.Visible=false;
				btnUnsubscribe.Visible=false;
				spanSubscribe.Visible = false;
				return;
			}
			spanSubscribe.Visible = true;
			Cmd.CommandText = "SELECT ForumID FROM ForumNewTopicSubscriptions WHERE UserID=" + CurrentUserID + " AND ForumID=" + _forumID;
			object res = Cmd.ExecuteScalar();
			btnSubscribe.Visible = (res == null);
			btnUnsubscribe.Visible = (res != null);
			Cmd.CommandText = "SELECT ForumID FROM ForumNewForumMsgSubscriptions WHERE UserID=" + CurrentUserID + " AND ForumID=" + _forumID;
			res = Cmd.ExecuteScalar();
			btnSubscribeMsgs.Visible = (res == null);
			btnUnsubscribeMsgs.Visible = (res != null);
		}

		/// <summary>
		/// the method is static cause its called from iphone version of the page also
		/// </summary>
		public static void BindSubforums(DbCommand cmd, Repeater rptSubForumsList, int forumID, int viewerUserId)
		{
			cmd.CommandText = "SELECT SubForumID FROM ForumSubforums WHERE ParentForumID=" + forumID;
			object res = cmd.ExecuteScalar();
			if (res == null) return;

			string strSQLAllowedForums = Utils.Forum.GetReadableForumsForUserString(Utils.User.CurrentUserID);

			cmd.CommandText =
				string.Format(@"SELECT Forums.ForumID, Forums.Title, Forums.Description, Count(ForumTopics.TopicID) AS Topics, Forums.GroupID, MAX(ForumTopics.LastMessageID) as LatestMessageID, Forums.OrderByNumber, Forums.IconFile
				FROM (Forums LEFT OUTER JOIN ForumTopics ON Forums.ForumID=ForumTopics.ForumID)
				WHERE Forums.ForumID IN (SELECT SubForumID FROM ForumSubforums WHERE ParentForumID={0})
				AND Forums.ForumID IN (" + strSQLAllowedForums + @")
				GROUP BY Forums.ForumID, Forums.Title, Forums.Description, Forums.GroupID, Forums.OrderByNumber, Forums.IconFile
				ORDER BY Forums.OrderByNumber", forumID);
			Utils.DB.FillCommandParamaters(cmd, true, viewerUserId, true);
			DataTable dt = new DataTable();
			DbDataReader dr = cmd.ExecuteReader();
			dt.Load(dr);
			dr.Close();
			rptSubForumsList.DataSource = dt;
			rptSubForumsList.DataBind();
			rptSubForumsList.Visible = rptSubForumsList.Items.Count > 0;
		}

		/// <summary>
		/// the method is static cause its called from iphone version of the page also
		/// </summary>
		public static void BindTopicsRepeater(DbCommand cmd, Repeater rptTopicsList, int pageSize, HttpRequest request, out string pagerString, bool isModerator, int forumID, bool forumIsPremoderated)
		{
			DataTable dt = Utils.Topic.GetTopicsInAForum(cmd.Connection, forumID, isModerator, forumIsPremoderated);

			PagedDataSource pagedSrc = new PagedDataSource();
			pagedSrc.DataSource = dt.DefaultView;
			pagedSrc.AllowPaging = true;
			pagedSrc.PageSize = pageSize;
			int curPage = 0;
			if (request.QueryString["page"] != null)
				int.TryParse(request.QueryString["page"], out curPage);
			pagedSrc.CurrentPageIndex = curPage;

			//prepare a string for the "pager" at the bottom
			pagerString = "";
			if (pagedSrc.PageCount > 1)
			{
				string url = HttpContext.Current.Request.RawUrl.ToLower(); //get current URL
				url = Regex.Replace(url, @"[\?\&]page=\d+", ""); //remove paging from current URL
				pagerString = Utils.Various.GetPaginationString(curPage, pagedSrc.PageCount, url);
			}

			//clear the list
			rptTopicsList.DataSource = null;
			rptTopicsList.DataBind();

			if (pagedSrc.Count > 0) //bind only if not empty (to prevent header/footer from showing
			{
				rptTopicsList.DataSource = pagedSrc;
				rptTopicsList.DataBind();
			}
		}

		private string GetRssXML()
		{
			//Cn.Open(); //should be opened!

			DataTable dt = Utils.Topic.GetTopicsInAForum(Cn, _forumID, false, _premoderated);

			StringBuilder retval = new StringBuilder();

			retval.Append("<?xml version=\"1.0\"?>\r\n");
			retval.Append("<rss version=\"2.0\">\r\n");
			retval.Append("<channel>\r\n");
			retval.Append("<title>" + Utils.Settings.ForumTitle.Replace("&", "&amp;") + " - " + _forumName + " - Latest Topics</title>\r\n");
			retval.Append("<link>" + Utils.Various.ForumURL + Utils.Various.GetForumURL(_forumID, _forumName) + "</link>\r\n");
			retval.Append("<description>" + Utils.Settings.ForumTitle.Replace("&", "&amp;") + " - " + _forumName + " - Latest Topics</description>\r\n");
			retval.Append("<language>en-us</language>\r\n");
			retval.Append("<docs>http://blogs.law.harvard.edu/tech/rss</docs>\r\n");
			retval.Append("<generator>Jitbit AspNetForum</generator>\r\n");

			bool firstRecord = true;
			foreach (DataRow dr in dt.Rows)
			{
				if (firstRecord && (dr["CreationDate"] as DateTime?) != null) //first record && check cause "last message" can be null
				{
					retval.Append(string.Format("<pubDate>{0}</pubDate>\r\n", ((DateTime) dr["CreationDate"]).ToString("r")));
					retval.Append(string.Format("<lastBuildDate>{0}</lastBuildDate>\r\n", ((DateTime) dr["CreationDate"]).ToString("r")));
					firstRecord = false;
				}

				//items
				retval.Append("<item>\r\n");
				retval.Append(string.Format("<link>{0}</link>\r\n", Utils.Various.ForumURL + Utils.Various.GetTopicURL(dr["TopicID"], dr["Subject"])));
				retval.Append("<title>" + dr["Subject"].ToString().Replace("&", "&amp;") + "</title>\r\n");
				retval.Append(string.Format("<description><![CDATA[{0}]]></description>\r\n", Utils.Formatting.FormatMessageHTML(dr["Subject"].ToString())));
				if ((dr["CreationDate"] as DateTime?) != null)
					retval.Append(string.Format("<pubDate>{0}</pubDate>\r\n", ((DateTime) dr["CreationDate"]).ToString("r")));
				retval.Append("</item>\r\n");
			}
			Cn.Close();

			retval.Append("</channel>\r\n");
			retval.Append("</rss>\r\n");

			return retval.ToString();
		}

		protected void rptTopicsList_ItemDataBound(object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e)
		{
			if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
			{
				DataRowView record = (DataRowView)e.Item.DataItem;
				HtmlImage imgTopic = e.Item.FindControl("imgTopic") as HtmlImage;
				bool isSticky = (Convert.ToInt32(record["IsSticky"]) == 1);
				bool isClosed = Convert.ToBoolean(record["IsClosed"]);

				//special icon for "sticky" topics
				if (isSticky)
				{
					imgTopic.Src = "images/topic_sticky.png";
					imgTopic.Alt = "'Sticky' topic";
					imgTopic.Attributes["title"] = imgTopic.Alt;
				}
				else
				{
					//special icon for closed topics
					if (isClosed)
					{
						imgTopic.Src = "images/topic_locked.png";
						imgTopic.Alt = "The topic is closed";
						imgTopic.Attributes["title"] = imgTopic.Alt;
					}
					else
					{
						//special icon for "unread" topics
						if (CurrentUserID != 0 && !(record["LastMessageID"] is DBNull))
						{
							if (Utils.UnreadTracker.IsTopicUnread(Convert.ToInt32(record["TopicID"]), Convert.ToInt32(record["LastMessageID"]), record["CreationDate"] as DateTime?)
								&& (record["LastUserID"] is DBNull || Convert.ToInt32(record["LastUserID"]) != CurrentUserID)//last msg is not form the current user
								)
							{
								imgTopic.Src = "images/topic_unread.png";
								imgTopic.Alt = "The topic has been updated since your last visit";
								imgTopic.Attributes["title"] = imgTopic.Alt;
							}
						}
					}
				}
				imgTopic.Visible = (imgTopic.Src != "");

				if (_isModerator)
				{
					//show "delete" button and other for moderators
					e.Item.FindControl("btnModeratorDelete").Visible = true;
					
					if (!Convert.ToBoolean(record["Visible"]))
					{
						e.Item.FindControl("btnModeratorApprove").Visible = true;
					}

					//show stick/unstick
					if (isSticky)
					{
						e.Item.FindControl("btnModeratorUnstick").Visible = true;
					}
					else
					{
						e.Item.FindControl("btnModeratorStick").Visible = true;
					}

					//show close
					if (!isClosed)
					{
						e.Item.FindControl("btnModeratorClose").Visible = true;
					}
					else
					{
						e.Item.FindControl("btnModeratorReopen").Visible = true;
					}
				}
			}
		}

		protected void rptTopicsList_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
		{
			//delete topic and all messages
			if (e.CommandName == "delete")
			{
				int deletedTopicID = int.Parse(e.CommandArgument.ToString());

				Cn.Open();
				Utils.Topic.DeleteTopic(deletedTopicID, Cn);
				BindTopicsRepeater(Cmd, rptTopicsList, PageSize, Request, out pagerString, _isModerator, _forumID, _premoderated);
				Cn.Close();
				return;
			}
			//approved topic (when premoderated forum)
			if(e.CommandName=="approve")
			{
				int approvedTopicID = int.Parse(e.CommandArgument.ToString());

				Cn.Open();
				Utils.Topic.ApproveTopic(approvedTopicID, Cn);
				BindTopicsRepeater(Cmd, rptTopicsList, PageSize, Request, out pagerString, _isModerator, _forumID, _premoderated);
				Cn.Close();
				return;
			}
			//make "sticky"
			if (e.CommandName == "stick")
			{
				int stickyTopicID = int.Parse(e.CommandArgument.ToString());

				Cn.Open();
				Utils.Topic.StickTopic(stickyTopicID, true, Cn);
				BindTopicsRepeater(Cmd, rptTopicsList, PageSize, Request, out pagerString, _isModerator, _forumID, _premoderated);
				Cn.Close();
				return;
			}
			//make non-"sticky"
			if (e.CommandName == "unstick")
			{
				int stickyTopicID = int.Parse(e.CommandArgument.ToString());

				Cn.Open();
				Utils.Topic.StickTopic(stickyTopicID, false, Cn);
				BindTopicsRepeater(Cmd, rptTopicsList, PageSize, Request, out pagerString, _isModerator, _forumID, _premoderated);
				Cn.Close();
				return;
			}
			//close topic
			if (e.CommandName == "close")
			{
				int closeTopicID = int.Parse(e.CommandArgument.ToString());

				Cn.Open();
				Utils.Topic.CloseTopic(closeTopicID, true, Cn);
				BindTopicsRepeater(Cmd, rptTopicsList, PageSize, Request, out pagerString, _isModerator, _forumID, _premoderated);
				Cn.Close();
				return;
			}
			//reopen topic
			if (e.CommandName == "reopen")
			{
				int closeTopicID = int.Parse(e.CommandArgument.ToString());

				Cn.Open();
				Utils.Topic.CloseTopic(closeTopicID, false, Cn);
				BindTopicsRepeater(Cmd, rptTopicsList, PageSize, Request, out pagerString, _isModerator, _forumID, _premoderated);
				Cn.Close();
				return;
			}
		}

		//returns a string with page links like "1 2 3" for a topic
		protected string ShowPageLinks(object postCount, object topicId, object subject)
		{
			string retval = "";
			if (!(postCount is DBNull))
			{
				int pageCount = (int) Math.Ceiling(Convert.ToDouble(postCount)/Utils.Settings.PageSize);
				if(pageCount>1)
				{
					string topicUrl = Utils.Various.GetTopicURL(topicId, subject);
					retval = Utils.Various.GetPaginationString(-1, pageCount, topicUrl) + " | ";
				}
			}
			return retval;
		}

		protected void btnSubscribe_Click(object sender, System.EventArgs e)
		{
			if (CurrentUserID == 0) return; //just in case
			Cn.Open();
			//delete - just in case
			Cmd.CommandText = "DELETE FROM ForumNewTopicSubscriptions WHERE UserID=" + CurrentUserID + " AND ForumID=" + _forumID;
			Cmd.ExecuteNonQuery();
			Cmd.CommandText = "INSERT INTO ForumNewTopicSubscriptions (UserID, ForumID) VALUES (" + CurrentUserID + ", " + _forumID + ")";
			Cmd.ExecuteNonQuery();
			SubscribeButtonVisibility();
			Cn.Close();
		}

		protected void btnSubscribeMsgs_Click(object sender, System.EventArgs e)
		{
			if (CurrentUserID == 0) return; //just in case
			Cn.Open();
			//delete - just in case
			Cmd.CommandText = "DELETE FROM ForumNewForumMsgSubscriptions WHERE UserID=" + CurrentUserID + " AND ForumID=" + _forumID;
			Cmd.ExecuteNonQuery();
			Cmd.CommandText = "INSERT INTO ForumNewForumMsgSubscriptions (UserID, ForumID) VALUES (" + CurrentUserID + ", " + _forumID + ")";
			Cmd.ExecuteNonQuery();
			SubscribeButtonVisibility();
			Cn.Close();
		}

		protected void btnUnsubscribe_Click(object sender, System.EventArgs e)
		{
			if (CurrentUserID == 0) return; //just in case
			Cn.Open();
			Cmd.CommandText = "DELETE FROM ForumNewTopicSubscriptions WHERE UserID=" + CurrentUserID + " AND ForumID=" + _forumID;
			Cmd.ExecuteNonQuery();
			SubscribeButtonVisibility();
			Cn.Close();
		}

		protected void btnUnsubscribeMsgs_Click(object sender, System.EventArgs e)
		{
			if (CurrentUserID == 0) return; //just in case
			Cn.Open();
			Cmd.CommandText = "DELETE FROM ForumNewForumMsgSubscriptions WHERE UserID=" + CurrentUserID + " AND ForumID=" + _forumID;
			Cmd.ExecuteNonQuery();
			SubscribeButtonVisibility();
			Cn.Close();
		}
	}
}
