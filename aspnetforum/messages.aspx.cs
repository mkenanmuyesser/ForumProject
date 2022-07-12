using System;
using System.Collections;
using System.Collections.Generic;
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
using System.Configuration;
using System.Text;
using System.IO;
using System.Web.Caching;
using aspnetforum.Utils;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for messages.
	/// </summary>
	public partial class messages : ForumPage
	{
		public int _topicID;
		protected int _forumID;
		protected bool _isModerator = false;
		protected string pagerString = "";
		bool _sortDesc;
		bool _bMailNotificationsEnabled;
		bool _allowGuestPosts;
		bool _bTopicClosed;
		bool _bMembersOnly = false;
		int _pollID = 0;
		int _maxvotecount = 0;
		int _topicAuthorId;
		string _forumDescription;
		bool _restrictTopicCreation;
		string _forumTitle = "";
		bool _premoderated;
		public string _topicSubject;

		private void GetGeneralApplicationSettings()
		{
			_bMailNotificationsEnabled = Utils.Settings.MailNotificationsEnabled;
			try
			{
				_allowGuestPosts = Utils.Settings.AllowGuestPosts;
				_sortDesc = Utils.Settings.MsgSortDescending;
			}
			catch
			{
				_allowGuestPosts = false;
				_sortDesc = false;
			}
		}

		private bool ExtractTopicID()
		{
			int.TryParse(Request.QueryString["TopicID"], out _topicID);
			if (_topicID == 0)
			{
				Response.Write("topic not found");
				Response.TrySkipIisCustomErrors = true;
				Response.StatusCode = 404;
				Response.End();
				return false;
			}
			return true;
		}

		public static void IncreaseViewsCounter(DbConnection cn, int topicId)
		{
			//ms access workaroud for upgrading customers - Access inserts NULL value to existing rows even if default value specified
			cn.ExecuteNonQuery("UPDATE ForumTopics SET ViewsCount=0 WHERE ViewsCount IS NULL AND TopicID=" + topicId);
			
			//now increase counter
			cn.ExecuteNonQuery("UPDATE ForumTopics SET ViewsCount=ViewsCount+1 WHERE TopicID=" + topicId);
		}

		private void BindForumsDropDown()
		{
			DbDataReader dr = Cn.ExecuteReader("SELECT ForumID, Title FROM Forums");
			DataTable dt = new DataTable();
			dt.Load(dr);
			dr.Close();
			ddlForumsTop.DataSource = dt;
			ddlForumsTop.DataBind();
		}
	
		protected void Page_Load(object sender, System.EventArgs e)
		{
			if (!ExtractTopicID()) return;

			GetGeneralApplicationSettings();

			//if its RSS and RSS exists in cache - show it right away (to prevent connecting to the database)
			if (Request.QueryString["rss"] == "1" && Cache["MessagesRSS" + _topicID] != null)
			{
				string rss = Cache["MessagesRSS" + _topicID] as string;
				SendOutRssAndQuit(rss);
				return;
			}

			Cn.Open();

			if (!GetGeneralTopicInfo())
			{
				Cn.Close(); Response.Write("topic not found"); Response.TrySkipIisCustomErrors = true; Response.StatusCode = 404; Response.End(); return; //topic not found
			}

			bool denyAnonymousUser = (_bMembersOnly && CurrentUserID == 0);
			//if the forum is members only - STOP for anonymous guests or STOP for non-group members
			if (denyAnonymousUser || !Utils.Forum.CheckForumReadPermissions(_forumID, CurrentUserID))
			{
				Cn.Close();
				if (Request.QueryString["rss"] != "1") //show error messages - but only if its not RSS
				{
					divError.Visible = tblError.Visible = true;
					if (denyAnonymousUser)
						divError.InnerHtml = "The forum <b>\"" + _forumTitle + "\"</b> is for authenticated users only. Please login or register.";
					else
						divError.InnerHtml = "Access denied. No permission.";
				}
				else
				{
					//save empty string to the rss-cache so the cached rss-string won't compromise us
					Cache.Add("TopicsRSS" + _forumID, "", null, DateTime.Now.AddMinutes(30), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
					Response.End();
				}
				return;
			}

			//is it rss request?
			if (Request.QueryString["rss"] == "1")
			{
				string rss = GetRssXML();
				Cn.Close();
				SendOutRssAndQuit(rss);
				return;
			}

			//lblCurTopic.Text = lblCurTopicBottom.Text = _topicSubject;
			Title = _topicSubject + " - " + _forumTitle + (Request.QueryString["page"] != null ? " - Page " + Request.QueryString["page"] : "");
			MetaDescription = Title;
			MetaKeywords = Title;
			divDescription.InnerHtml = _forumDescription;
			divDescription.Visible = !string.IsNullOrEmpty(_forumDescription);

			lblCurForum.Text = Utils.Forum.GetForumBreadCrumbs(_forumID, Cn);

			//hide "add post" link if user is guest or topic readonly
			spanAddPost.Visible = divQuickReply.Visible =
				(CurrentUserID != 0 || _allowGuestPosts)
				&& !_bTopicClosed
				&& Utils.Forum.CheckForumPostPermissions(_forumID, CurrentUserID);

			//if moderator - fill the ddlForums dropdownlist (for moving topic to another forum)
			spanMoveTop.Visible = spanMergeTop.Visible = _isModerator;
			if (_isModerator && !IsPostBack)
			{
				BindForumsDropDown();
			}

			lblClosedTop.Visible = _bTopicClosed;

			//subscribe/unsubscribe visibility
			SubscribeButtonVisibility();

			if (Request.UserAgent != null && !Regex.IsMatch(Request.UserAgent, @"bot|crawler|baiduspider|yahoo! slurp|mediapartners-google", RegexOptions.IgnoreCase)) //if not a bot
				IncreaseViewsCounter(Cn, _topicID);

			//is it a poll?
			ShowPollIfAny();

			//bind repeater
			int currentPage, lastMessageId;
			BindMessagesRepeater(_topicID, Cn, rptMessagesList, _isModerator, _sortDesc, Request, PageSize, out pagerString, out currentPage, out lastMessageId);

			Cn.Close();

			//mark this topic as "read"
			UnreadTracker.TrackTopicReading(_topicID, lastMessageId);

			//now set canonical link (for SEO)
			string canonicalUrl = Utils.Various.GetTopicURL(_topicID, _topicSubject);
			string queryStrDelim = (canonicalUrl.IndexOf("?") > -1) ? "&" : "?"; //is it "?" or "&"? i.e. are we using SEO urls or not?
			if (currentPage != 0)
			{
				if (Request.QueryString["page"] != null || Request.QueryString["lastpage"] != null)
				{
					canonicalUrl += queryStrDelim + "page=" + currentPage;
				}
			}
			lnkCanonical.Attributes["href"] = canonicalUrl;

			//show "rated only" link
			if (rptMessagesList.Items.Count > 1 && Request.QueryString["ratedOnly"] != "1") //dont show link for empty topics - good for SEO, prevents dup-pages issue
			{
				string topicLnk = Request.RawUrl;
				lnkRatedOnly.HRef = topicLnk.IndexOf("?") > -1 ? topicLnk + "&ratedOnly=1" : topicLnk + "?ratedOnly=1";
			}
			else
				lnkRatedOnly.Visible = false;

			rssLink.HRef = "messages.aspx?TopicID=" + _topicID + "&rss=1";
			rssDiscoverLink.Attributes["href"] = rssLink.HRef;
		}

		private void ShowPollIfAny()
		{
			object res = Cn.ExecuteScalar("SELECT PollID FROM ForumPolls WHERE TopicID=" + _topicID);
			if (res == null) //it is NOT a poll
				return;

			_pollID = Convert.ToInt32(res);
			divPoll.Visible = true;
			bool bShowResults;
			DbDataReader dr;

			//get poll name
			dr = Cn.ExecuteReader("SELECT * FROM ForumPolls WHERE PollID=" + _pollID);
			dr.Read();
			lblPollName.Text = dr["Question"].ToString();
			dr.Close();

			if (CurrentUserID != 0) //check if current user already voted
			{
				res = Cn.ExecuteScalar("SELECT UserID FROM ForumPollAnswers WHERE OptionID IN (SELECT OptionID FROM ForumPollOptions WHERE PollID=" + _pollID + ") AND UserID=" + CurrentUserID);
				bShowResults = (res != null); //user has voted
			}
			else
				bShowResults = true;

			rblOptions.Visible = !bShowResults;
			rptVoteResults.Visible = bShowResults;
			btnVote.Visible = !bShowResults;

			if (bShowResults) //showing poll results
			{
				DataTable dt = new DataTable();
				dr = Cn.ExecuteReader(
					@"SELECT COUNT(ForumPollAnswers.UserID) as VoteCount, ForumPollOptions.OptionID, ForumPollOptions.OptionText FROM ForumPollAnswers
					RIGHT OUTER JOIN ForumPollOptions ON ForumPollOptions.OptionID = ForumPollAnswers.OptionID
					WHERE ForumPollOptions.PollID=" + _pollID + @"
					GROUP BY ForumPollOptions.OptionID, ForumPollOptions.OptionText");
				dt.Load(dr);
				dr.Close();

				//now let's fin max vote count\
				_maxvotecount=0;
				foreach (DataRow row in dt.Rows)
				{
					if (_maxvotecount <= Convert.ToInt32(row["VoteCount"]))
						_maxvotecount =  Convert.ToInt32(row["VoteCount"]);
				}

				rptVoteResults.DataSource = dt;
				rptVoteResults.DataBind();
			}
			else if (!IsNonLoginPostBack) //bind poll voting controls
			{
				dr = Cn.ExecuteReader("SELECT OptionID, OptionText FROM ForumPollOptions WHERE PollID=" + _pollID);
				rblOptions.DataSource = dr;
				rblOptions.DataBind();
				dr.Close();
			}
		}

		protected void rptMessagesList_ItemDataBound(object sender, RepeaterItemEventArgs e)
		{
			if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
			{
				DataRowView record = (DataRowView)e.Item.DataItem;

				//allow quoting
				if (_allowGuestPosts || CurrentUserID != 0)
				{
					HtmlAnchor lnkQuote = (HtmlAnchor)e.Item.FindControl("lnkQuote");
					lnkQuote.Visible = divQuickReply.Visible; //hide "reply with quote"
					lnkQuote.HRef = "addpost.aspx?TopicID=" + record["TopicID"] + "&Quote=" + record["MessageID"];
				}

				//allow complaining for non-guests
				e.Item.FindControl("btnComplain").Visible = (CurrentUserID != 0);

				// Moderators and message owners can delete and edit messages.
				if (CurrentUserID != 0
					&& (_isModerator || (!Utils.Settings.DisableEditing && Convert.ToInt32(record["UserID"]) == CurrentUserID))
					)
				{
					//show "delete" button for moderators
					e.Item.FindControl("btnModeratorDelete").Visible = true;

					// Show "edit" button for moderators.
					HtmlAnchor lnkEdit = (HtmlAnchor)e.Item.FindControl("lnkEdit");
					lnkEdit.Visible = true;
					lnkEdit.HRef = "addpost.aspx?TopicID=" + record["TopicID"] + "&Edit=" + record["MessageID"];
				}
				else
				{
					// Need to explicitly turn off this special case control.
					e.Item.FindControl("lnkEdit").Visible = false;
				}

				//topic-starter can "accept" messages as answers
				e.Item.FindControl("btnAcceptAnswer").Visible = (_topicAuthorId == CurrentUserID && !Convert.ToBoolean(record["AcceptedAnswer"]));

				//enable rating for non-guest users
				e.Item.FindControl("spanRate").Visible = (Utils.Settings.EnableRating && CurrentUserID != 0);

				if (_isModerator)
				{
					if (!Convert.ToBoolean(record["Visible"]))
					{
						e.Item.FindControl("btnModeratorApprove").Visible = true;
					}
				}

				//show attachments
				Repeater nestedRepeater = e.Item.FindControl("rptFiles") as Repeater;
				DataView filesView = record.CreateChildView("MessagesFiles");
				bool filesExist = filesView.Count > 0;
				if (filesExist)
				{
					nestedRepeater.DataSource = filesView;
					nestedRepeater.DataBind();
				}
				nestedRepeater.Visible = filesExist;

				//showing body
				Literal ltrBody = e.Item.FindControl("ltrBody") as Literal;
				string body = record["Body"].ToString();
				if (filesExist)
					body = Utils.Formatting.FormatInlineAttachmetns(body, filesView);
				ltrBody.Text = Utils.Formatting.FormatMessageHTML(body);
			}
		}

		private void SubscribeButtonVisibility()
		{
			if (!_bMailNotificationsEnabled || CurrentUserID == 0)
			{
				btnSubscribeTop.Visible = spanSubcriptionTop.Visible = false;
				btnUnsubscribeTop.Visible = false;
				return;
			}
			spanSubcriptionTop.Visible = true;
			object res = Cn.ExecuteScalar("SELECT TopicID FROM ForumSubscriptions WHERE UserID=" + CurrentUserID + " AND TopicID=" + _topicID);
			btnSubscribeTop.Visible = (res == null);
			btnUnsubscribeTop.Visible = (res != null);
		}

		/// <summary>
		/// the method is static cause its called from iphone version of the page also
		/// </summary>
		public static void BindMessagesRepeater(int topicId, DbConnection cn, Repeater rptMessagesList, bool isModerator, bool sortDesc, HttpRequest request, int pageSize, out string pagerString)
		{
			int tmp1, tmp2; BindMessagesRepeater(topicId, cn, rptMessagesList, isModerator, sortDesc, request, pageSize, out pagerString, out tmp1, out tmp2); //overload
		}
		public static void BindMessagesRepeater(int topicId, DbConnection cn, Repeater rptMessagesList, bool isModerator, bool sortDesc, HttpRequest request, int pageSize, out string pagerString, out int currentPageNumber, out int lastMessageId)
		{
			bool ratedMessagesOnly = (request.QueryString["ratedOnly"] == "1");
			int firstMessageId = 0;
			if (ratedMessagesOnly)
				firstMessageId = Utils.Topic.GetFirstMessageIdInTopic(topicId, cn);

			List<object> parameters = new List<object>();

			DataSet ds = new DataSet();
			ds.Tables.Add("Messages");
			ds.Tables.Add("UploadedFiles");

			string sql = @" SELECT ForumMessages.TopicID, ForumMessages.MessageID, ForumUsers.UserName, ForumUsers.AvatarFileName,
					ForumUsers.Signature, ForumMessages.CreationDate, ForumMessages.Body, ForumMessages.Visible, ForumMessages.UserID,
					ForumUsers.PostsCount, ForumMessages.Rating, ForumMessages.IPAddress, ForumUsers.FirstName, ForumUsers.LastName,
					ForumMessages.Rating as SortRating, ForumUsers.UseGravatar, ForumUsers.Email, ForumMessages.AcceptedAnswer
				FROM ForumMessages
				LEFT JOIN ForumUsers ON ForumMessages.UserID = ForumUsers.UserID
				WHERE ForumMessages.TopicID=? ";
			parameters.Add(topicId);

			//get messages
			if(!isModerator) //only "visible" msgs for non-moderators
			{
				sql += @" AND ForumMessages.Visible=? ";
				parameters.Add(true);
			}

			//showing only rated messages AND the first message in the topic, sorted by rating desc ("union" used to display the 1st message 1st despite of te rating)
			if (ratedMessagesOnly)
			{
				sql = string.Format(@"SELECT ForumMessages.TopicID, ForumMessages.MessageID, ForumUsers.UserName, ForumUsers.AvatarFileName,
						ForumUsers.Signature, ForumMessages.CreationDate, ForumMessages.Body, ForumMessages.Visible, ForumMessages.UserID,
						ForumUsers.PostsCount, ForumMessages.Rating, ForumMessages.IPAddress, ForumUsers.FirstName, ForumUsers.LastName,
						100000 as SortRating, ForumUsers.UseGravatar, ForumUsers.Email, ForumMessages.AcceptedAnswer
					FROM ForumMessages
					LEFT JOIN ForumUsers ON ForumMessages.UserID = ForumUsers.UserID WHERE ForumMessages.MessageID={0} ", firstMessageId)
					+ " UNION ALL "
					+ sql
					+ string.Format(" AND (ForumMessages.Rating>0 AND ForumMessages.MessageID<>{0}) ORDER BY SortRating DESC", firstMessageId);
			}
			else
			{
				sql += " ORDER BY ForumMessages.MessageID";
				if (sortDesc) sql += " DESC";
			}

			DbDataReader dr = cn.ExecuteReader(sql, parameters.ToArray());
			ds.Tables[0].Load(dr);
			dr.Close();

			//lets find out the last message id in this topic
			lastMessageId = 0;
			if (ds.Tables[0].Rows.Count > 0)
				lastMessageId = Convert.ToInt32(ds.Tables[0].Rows[ds.Tables[0].Rows.Count - 1]["MessageID"]); //get the last row

			//now get file uploaded in this topic
			dr = cn.ExecuteReader("SELECT FileID, FileName, MessageID, UserID FROM ForumUploadedFiles WHERE MessageID IN (SELECT MessageID FROM ForumTopics WHERE TopicID=" + topicId + ")");
			ds.Tables[1].Load(dr);
			dr.Close();

			ds.Relations.Add(new DataRelation("MessagesFiles", ds.Tables[0].Columns["MessageID"], ds.Tables[1].Columns["MessageID"], false));

			PagedDataSource pagedSrc = new PagedDataSource();
			pagedSrc.DataSource = ds.Tables[0].DefaultView;
			pagedSrc.AllowPaging = true;
			pagedSrc.PageSize = ratedMessagesOnly ? 100000 : pageSize; //if we're showing rated msgs only - no paging then
			currentPageNumber = 0;
			if (request.QueryString["page"] != null)
				int.TryParse(request.QueryString["page"], out currentPageNumber);
			else if (request.QueryString["lastpage"] != null)
				currentPageNumber = pagedSrc.PageCount - 1;
			pagedSrc.CurrentPageIndex = currentPageNumber;

			//prepare a string for the "pager" at the bottom
			pagerString = "";
			if (pagedSrc.PageCount > 1)
			{
				string url = HttpContext.Current.Request.RawUrl.ToLower(); //get current URL
				url = Regex.Replace(url, @"[\?\&]page=\d+", ""); //remove paging from current URL
				url = Regex.Replace(url, @"[\?\&]lastpage=1", "", RegexOptions.IgnoreCase); //remove "lastpage" from current URL
				pagerString = Utils.Various.GetPaginationString(currentPageNumber, pagedSrc.PageCount, url);
			}

			rptMessagesList.DataSource = pagedSrc;
			rptMessagesList.DataBind();
		}

		private bool GetGeneralTopicInfo()
		{
			if (!Utils.Topic.GetBasicTopicInfo(_topicID, Cn, out _forumID, out _topicAuthorId, out _topicSubject, out _bTopicClosed))
				return false; //not found in db

			if(!Utils.Forum.GetBasicForumInfo(_forumID, Cn, out _forumTitle, out _forumDescription, out _restrictTopicCreation, out _premoderated, out _bMembersOnly))
				return false; // not found in db

			_isModerator = IsModerator(_forumID);

			return true;
		}

		protected void rptMessagesList_ItemCommand(object source, RepeaterCommandEventArgs e)
		{
			//delete message
			if (e.CommandName == "delete")
			{
				int deletedMessageId = int.Parse(e.CommandArgument.ToString());
				Cn.Open();
				bool topicDeleted = Utils.Message.DeleteMessage(deletedMessageId, Cn);
				if (topicDeleted) //no messages left in the topic
				{
					Cn.Close();
					Response.Redirect(Utils.Various.GetForumURL(_forumID, _forumTitle));
					return;
				}
				BindMessagesRepeater(_topicID, Cn, rptMessagesList, _isModerator, _sortDesc, Request, PageSize, out pagerString);
				Cn.Close();
			}
			//delete message
			if (e.CommandName == "complain")
			{
				int reportMessageId = int.Parse(e.CommandArgument.ToString());
				Cn.Open();
				Utils.Message.ReportToModerator(reportMessageId, CurrentUserID, "", Cn);
				BindMessagesRepeater(_topicID, Cn, rptMessagesList, _isModerator, _sortDesc, Request, PageSize, out pagerString);
				Cn.Close();
				ClientScript.RegisterStartupScript(GetType(), "reported", "<script type='text/javascript'>alert('reported');</script>");
			}
			//approve message (for premoderated forum)
			if( e.CommandName=="approve")
			{
				int approvedMessageId = int.Parse(e.CommandArgument.ToString());

				Cn.Open();
				Utils.Message.ApproveMessage(approvedMessageId, Cn);
				BindMessagesRepeater(_topicID, Cn, rptMessagesList, _isModerator, _sortDesc, Request, PageSize, out pagerString);
				Cn.Close();
			}

			//"accept" message (by topic-starter)
			if (e.CommandName == "acceptAnswer")
			{
				int acceptedMessageId = int.Parse(e.CommandArgument.ToString());

				Cn.Open();
				Utils.Message.AcceptMessage(acceptedMessageId, Cn);
				BindMessagesRepeater(_topicID, Cn, rptMessagesList, _isModerator, _sortDesc, Request, PageSize, out pagerString);
				Cn.Close();
			}
		}

		private string GetRssXML()
		{
			//Cn.Open(); the connection should be already open!!!!!!

			StringBuilder retval = new StringBuilder();

			retval.Append("<?xml version=\"1.0\"?>\r\n");
			retval.Append("<rss version=\"2.0\">\r\n");
			retval.Append("<channel>\r\n");
			retval.Append("<title>" + Utils.Settings.ForumTitle.Replace("&", "&amp;") + " - " + _forumTitle.Replace("&", "&amp;") + " - " + _topicSubject.Replace("&", "&amp;") + " - Messages</title>\r\n");
			retval.Append("<link>" + Utils.Various.ForumURL + Utils.Various.GetTopicURL(_topicID, _topicSubject) + "</link>\r\n");
			retval.Append("<description>" + Utils.Settings.ForumTitle.Replace("&", "&amp;") + " - " + _forumTitle.Replace("&", "&amp;") + " - " + _topicSubject.Replace("&", "&amp;") + " - Messages</description>\r\n");
			retval.Append("<language>en-us</language>\r\n");
			retval.Append("<docs>http://blogs.law.harvard.edu/tech/rss</docs>\r\n");
			retval.Append("<generator>Jitbit AspNetForum</generator>\r\n");
			
			DbDataReader dr = Cn.ExecuteReader(
				@"SELECT ForumMessages.TopicID, ForumMessages.MessageID, ForumUsers.UserName, ForumUsers.FirstName, ForumUsers.LastName, ForumMessages.CreationDate,
					ForumMessages.Body, ForumMessages.Visible, ForumMessages.UserID, ForumUsers.PostsCount
				FROM ForumMessages
				LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID
				WHERE ForumMessages.Visible=? AND ForumMessages.TopicID=" + _topicID + " ORDER BY ForumMessages.CreationDate DESC", true);
			if (dr.HasRows)
			{
				bool firstRecord = true;
				while (dr.Read())
				{
					if (firstRecord) //first record
					{
						retval.Append(string.Format("<pubDate>{0}</pubDate>\r\n", ((DateTime)dr["CreationDate"]).ToString("r")));
						retval.Append(string.Format("<lastBuildDate>{0}</lastBuildDate>\r\n", ((DateTime)dr["CreationDate"]).ToString("r")));
						firstRecord = false;
					}

					//items
					retval.Append("<item>\r\n");
					retval.Append(string.Format("<link>{0}</link>\r\n", Utils.Various.ForumURL + Utils.Various.GetTopicURL(_topicID, _topicSubject)));
					retval.Append("<title>Message from " + Utils.User.GetUserDisplayName(dr["UserName"], dr["FirstName"], dr["LastName"]).Replace("&", "&amp;") + "</title>\r\n");
					retval.Append(string.Format("<description><![CDATA[{0}]]></description>\r\n", Utils.Formatting.FormatMessageHTML(dr["Body"].ToString())));
					retval.Append(string.Format("<pubDate>{0}</pubDate>\r\n", ((DateTime)dr["CreationDate"]).ToString("r")));
					retval.Append("</item>\r\n");
				}
			}
			dr.Close();

			retval.Append("</channel>\r\n");
			retval.Append("</rss>\r\n");

			//cache the rss content
			Cache.Add("MessagesRSS" + _topicID, retval.ToString(), null, DateTime.Now.AddHours(1), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);

			return retval.ToString();
		}

		protected void btnSubscribe_Click(object sender, System.EventArgs e)
		{
			if (CurrentUserID == 0) return; //just in case
			Cn.Open();
			Utils.SendNotifications.UpdateTopicNotificationSettings(CurrentUserID, _topicID, true, Cn);
			SubscribeButtonVisibility();
			Cn.Close();
		}

		protected void btnUnsubscribe_Click(object sender, System.EventArgs e)
		{
			if (CurrentUserID == 0) return; //just in case
			Cn.Open();
			Utils.SendNotifications.UpdateTopicNotificationSettings(CurrentUserID, _topicID, false, Cn);
			SubscribeButtonVisibility();
			Cn.Close();
		}

		protected void btnMoveTop_Click(object sender, System.EventArgs e)
		{
			MoveTopic(ddlForumsTop);
		}


		protected void btnMergeTop_Click(object sender, System.EventArgs e)
		{
			MergeTopics(tbTopMergeThreadId);
		}

		private void MergeTopics(TextBox tbMergeTopicId)
		{
			int mergeTopicId = 0;
			if (int.TryParse(tbMergeTopicId.Text, out mergeTopicId))
			{
				if (mergeTopicId == _topicID) return;

				string topicSubj;
				Utils.Topic.MergeTopics(mergeTopicId, _topicID, out topicSubj);
				Response.Redirect(Utils.Various.GetTopicURL(mergeTopicId, topicSubj));
			}
		}

		private void MoveTopic(DropDownList forumDropDown)
		{
			if (forumDropDown.SelectedValue == "") return;
			int forumId = int.Parse(forumDropDown.SelectedValue);

			Utils.Topic.MoveTopic(_topicID, forumId);

			Cn.Open();
			GetGeneralTopicInfo();
			Cn.Close();
		}

		//gets thewidth of the voting bar
		public int GetVotingBarWidth(object votecount)
		{
			if (_maxvotecount != 0)
				return 200 * Convert.ToInt32(votecount) / _maxvotecount;
			else
				return 0;
		}

		protected void btnVote_Click(object sender, EventArgs e)
		{
			if (rblOptions.SelectedValue == "") return;
			Cn.Open();
			Cn.ExecuteNonQuery("INSERT INTO ForumPollAnswers (UserID, OptionID) VALUES (?, ?)", CurrentUserID, rblOptions.SelectedValue);
			ShowPollIfAny();
			Cn.Close();
		}

		protected void btnQuickReply_Click(object sender, EventArgs e)
		{
			string msg = tbQuickReply.Text.Trim();
			if (msg == "") return;
			msg = msg.Replace("<", "&lt;").Replace(">", "&gt;");

			Cn.Open();
			int messageId = Utils.Message.AddMessage(Cn, _topicID, msg, !_premoderated || _isModerator, Utils.Various.GetUserIpAddress(Request), false);

			if (_premoderated && !_isModerator)
			{
				Cn.Close();
				Response.Redirect("premoderatedmessage.aspx");
			}
			else
			{
				//count messages to compute the number of pages
				//(needed to get the user redirected to the last page)
				string url = Utils.Topic.GetNewlyPostedMessageUrl(_topicID, messageId, Cn, PageSize);
				Cn.Close();
				Response.Redirect(url);
			}
		}

		protected static string GetPostClassName(object isAccepted)
		{
			if (isAccepted != null && !(isAccepted is DBNull) && Convert.ToBoolean(isAccepted))
				return "acceptedAnswer";
			return "";
		}

		protected static string RenderMsgRating(object messageId, object rating)
		{
			string sign = "";
			StringBuilder retval = new StringBuilder();
			retval.Append("<span id='spanRating");
			retval.Append(messageId);
			retval.Append("' title='This message has been rated by users' ");

			if (rating != null && !(rating is DBNull))
			{
				int iRating = Convert.ToInt32(rating);

				if (iRating != 0)
				{
					string color = (iRating < 0) ? "red" : "green";
					retval.Append(" style='color:"); retval.Append(color); retval.Append("'");
				}

				if (iRating > 0)
					sign = "+";

				//return string.Format("<span style='color:{0}'>{1}</span>", color, iRating);
			}
			retval.Append(">");
			retval.Append(sign);
			retval.Append(rating);
			retval.Append("</span>");
			
			return retval.ToString();
		}
	}
}