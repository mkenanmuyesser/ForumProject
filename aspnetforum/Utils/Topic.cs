using System;
using System.Collections.Generic;
using System.Web;
using System.Data.Common;
using System.Data;
using System.IO;
using System.Text;
using Jitbit.Utils;

namespace aspnetforum.Utils
{
	public static class Topic
	{
		public static string GetNewlyPostedMessageUrl(int topicId, int messageId, DbConnection cn, int pageSize)
		{
			int numMessages = Convert.ToInt32(cn.ExecuteScalar("SELECT COUNT(MessageID) FROM ForumMessages WHERE Visible=? AND TopicID=" + topicId, true));
			int numPages = (numMessages - 1) / pageSize;

			string url = (numPages > 0)
							 ? "messages.aspx?TopicID=" + topicId + "&Page=" + numPages
							 : "messages.aspx?TopicID=" + topicId;
			url += "&MessageID=" + messageId;
			return url;
		}

		public static int GetFirstMessageIdInTopic(int topicId, DbConnection cn)
		{
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			int retval = 0;
			try { retval = Convert.ToInt32(cn.ExecuteScalar("SELECT MIN(MessageID) FROM ForumMessages WHERE TopicID=?", topicId)); }
			catch { }

			if (!openConn) cn.Close();

			return retval;
		}

		public static void MoveTopic(int topicId, int toForumId)
		{
			int oldForumId;
			using (var cn = DB.CreateOpenConnection())
			{
				oldForumId = Convert.ToInt32(cn.ExecuteScalar("SELECT ForumID FROM ForumTopics WHERE TopicID=?", topicId));
				cn.ExecuteNonQuery("UPDATE ForumTopics SET ForumID=? WHERE TopicID=?", toForumId, topicId);
			}

			//rebuild the cache
			Forum.ClearTopicsCache(oldForumId);
			Forum.ClearTopicsCache(toForumId);
			Forum.ClearFrontPageCacheForGuests();
		}

		public static void MergeTopics(int topicIdToMergeInto, int topicIdBeginMerged, out string newTopicSubject)
		{
			if (topicIdToMergeInto == topicIdBeginMerged)
			{
				newTopicSubject = "";
				return;
			}

			using (var cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("UPDATE ForumMessages SET TopicID=? WHERE TopicID=?", topicIdToMergeInto, topicIdBeginMerged);

				cn.ExecuteNonQuery("UPDATE ForumSubscriptions SET TopicID=? WHERE TopicID=?", topicIdToMergeInto, topicIdBeginMerged);

				cn.ExecuteNonQuery("UPDATE ForumSubscriptions SET TopicID=? WHERE TopicID=?", topicIdToMergeInto, topicIdBeginMerged);

				cn.ExecuteNonQuery("UPDATE ForumPolls SET TopicID=? WHERE TopicID=?", topicIdToMergeInto, topicIdBeginMerged);

				cn.ExecuteNonQuery("DELETE FROM ForumTopics WHERE TopicID=?", topicIdBeginMerged);

				//calculating repliescount
				object res = cn.ExecuteScalar("SELECT Count(MessageID) FROM ForumMessages WHERE TopicID=?", topicIdToMergeInto);
				cn.ExecuteNonQuery("UPDATE ForumTopics SET RepliesCount=? WHERE TopicID=?", res, topicIdToMergeInto);

				//getting forumid (to clear cahe) and subj (to return an use URL building later)
				DbDataReader dr = cn.ExecuteReader("SELECT ForumID, Subject FROM ForumTopics WHERE TopicID=?", topicIdToMergeInto);
				dr.Read();
				newTopicSubject = dr["Subject"].ToString();
				int forumId = Convert.ToInt32(dr["ForumID"]);
				dr.Close();
				cn.Close();

				//rebuild the cache
				Forum.ClearTopicsCache(forumId);
				Forum.ClearFrontPageCacheForGuests();
			}
		}

		public static int GetForumIdForTopic(int topicId, DbConnection cn)
		{
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			int forumId = Convert.ToInt32(cn.ExecuteScalar("SELECT ForumID FROM ForumTopics WHERE TopicID=?", topicId));

			if (!openConn) cn.Close();

			return forumId;
		}

		public static void ApproveTopic(int approvedTopicID, DbConnection cn)
		{
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			cn.ExecuteNonQuery("UPDATE ForumTopics SET Visible=? WHERE TopicID=?", true, approvedTopicID);

			cn.ExecuteNonQuery("UPDATE ForumMessages SET Visible=? WHERE TopicID=?", true, approvedTopicID);
			
			string firstMsgInTopic = cn.ExecuteScalar("SELECT TOP 1 Body FROM ForumMessages WHERE TopicID=" + approvedTopicID + " ORDER BY MessageID") as string;
			if (firstMsgInTopic == null) firstMsgInTopic = "";

			Message.RefreshLastMessageIdForTopic(approvedTopicID, cn);

			//clear cache
			int forumId = GetForumIdForTopic(approvedTopicID, cn);
			Forum.ClearTopicsCache(forumId);

			if (!openConn) cn.Close();

			SendNotifications.SendNewTopicNotificationEmails(approvedTopicID, firstMsgInTopic, false, true);
		}

		/// <summary>
		/// get message's date and username by messageID
		/// (this is used to show "last message" column in the topics list, in the forums list etc.)
		/// </summary>
		public static string GetTopicInfoBMessageyID(object messageId, DbCommand cmd)
		{
			return GetTopicInfoBMessageyID(messageId, null, null, cmd);
		}
		/// <summary>
		/// OVERLOAD: get message's date and username by messageID
		/// (this is used to show "last message" column in the topics list, in the forums list etc.)
		/// thi method has SUBJECT and REPLIESCOUNT input parameters that saves DB query and saves performance
		/// </summary>
		/// <param name="messageId"></param>
		/// <param name="topicSubject">pass this param to prevent quering the db and save performance</param>
		/// <param name="repliesCount">pass this param to prevent quering the db and save performance</param>
		/// <param name="cmd"></param>
		public static string GetTopicInfoBMessageyID(object messageId, object topicSubject, int? repliesCount, DbCommand cmd)
		{
			if (messageId.ToString().Trim() == string.Empty || messageId.ToString() == "0") return string.Empty;

			string retval = "";

			//this method is called from grids, so
			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cmd.Connection.State == ConnectionState.Open);
			if (!openConn) cmd.Connection.Open();

			int topicId = 0, userId = 0;
			string body = ""; DateTime creationDate = DateTime.Now;

			cmd.CommandText = @"SELECT ForumMessages.CreationDate, ForumMessages.UserID, ForumMessages.TopicID, ForumMessages.Body
				FROM ForumMessages
				WHERE ForumMessages.MessageID=" + messageId;
			DbDataReader dr = cmd.ExecuteReader();
			if (dr.Read())
			{
				topicId = Convert.ToInt32(dr["TopicID"]);
				userId = dr["UserID"] != DBNull.Value ? Convert.ToInt32(dr["UserID"]) : 0;
				creationDate = Convert.ToDateTime(dr["CreationDate"]);
				body = dr["Body"].ToString();
			}
			dr.Close();

			if (topicSubject == null)
			{
				cmd.CommandText = "SELECT Subject, RepliesCount FROM ForumTopics WHERE TopicID=" + topicId;
				dr = cmd.ExecuteReader();
				if (dr.Read())
				{
					topicSubject = dr["Subject"].ToString();
					repliesCount = dr["RepliesCount"] as int?;
				}
				else
				{
					topicSubject = "";
				}
				dr.Close();
			}

			retval = FormatTopicInfo(Convert.ToInt32(messageId), topicId, userId, topicSubject, creationDate, repliesCount, body);

			if (!openConn) cmd.Connection.Close();

			return retval.ToString();
		}

		public static string FormatTopicInfo(object messageId, object topicId, object userId, object topicSubject, object creationDate, int? msgCount, object msgBody)
		{
			StringBuilder retval = new StringBuilder();

			string messageUrl = Various.GetTopicURL(topicId, topicSubject);
			string queryString = (messageUrl.IndexOf("?") > -1) ? "&amp;" : "?";

			//claculate link to last page
			if (!msgCount.HasValue) msgCount = 0;
			int pageCount = (int)Math.Ceiling(Convert.ToDouble(msgCount) / Settings.PageSize);
			if (pageCount > 1)
				messageUrl += queryString + "page=" + (pageCount - 1);

			messageUrl += "#post" + messageId;

			retval.Append("<a href=\"");
			retval.Append(messageUrl);
			retval.Append("\" title=\"");
			retval.Append(Formatting.StripBBCode(msgBody.ToString().Replace("\"", "")));
			retval.Append("\">");

			retval.Append(topicSubject.ToString().Left(20));

			retval.Append("...</a>");

			//retval.Append(creationDate.ToString("ddd, d MMM, yy, "));
			if (creationDate != null && !(creationDate is DBNull))
			{
				DateTime dtCreationDate = Convert.ToDateTime(creationDate);
				retval.Append("<div>" + ForumPage.ToAgoString(dtCreationDate) + "</div>");
			}

			return retval.ToString();
		}

		public static DataTable GetTopicsInAForum(DbConnection cn, int forumID, bool isModerator, bool forumIsPremoderated)
		{
			if ((!forumIsPremoderated || !isModerator) //look into the cache only if it's not a premoderated forum, of current user is NOT moderator
			    && HttpRuntime.Cache["Topics" + forumID] != null)
				return HttpRuntime.Cache["Topics" + forumID] as DataTable;

			string sqlText = "";
			List<object> sqlParams = new List<object>();

			//see the "LEFT JOIN" by LastMessageID
			//its in case the forum is premoderated, in this case LastMessageID is 0 for the first msg in the topic
			if (!isModerator) //only "visible" topics for non-moderators
			{
				sqlText = @"SELECT ForumMessages.CreationDate, ForumTopics.TopicID, ForumTopics.Subject, ForumTopics.Visible, ForumTopics.LastMessageID, ForumTopics.RepliesCount AS Messages, ForumTopics.IsSticky, ForumTopics.IsClosed, ForumTopics.ViewsCount, ForumUsers.UserName, ForumUsers.UserID, ForumUsers.FirstName, ForumUsers.LastName,
					ForumUsers_LastMsg.UserID as LastUserID, ForumUsers_LastMsg.UserName as LastUserName, ForumUsers_LastMsg.FirstName as LastFirstName, ForumUsers_LastMsg.LastName as LastLastName, ForumMessages.Body, ForumMessages.CreationDate
					FROM ((ForumTopics
					LEFT JOIN ForumMessages ON ForumMessages.MessageID = ForumTopics.LastMessageID)
					LEFT JOIN ForumUsers ON ForumTopics.UserID = ForumUsers.UserID)
					LEFT JOIN ForumUsers ForumUsers_LastMsg ON ForumUsers_LastMsg.UserID=ForumMessages.UserID
					WHERE ForumTopics.ForumID=" + forumID + @" AND ForumTopics.Visible=?
					ORDER BY ForumTopics.IsSticky DESC, ForumTopics.LastMessageID DESC";
				sqlParams.Add(true);
			}
			else //moderators: show also "invisible" topics (invisible = need to be approved first)
			{
				sqlText = @"SELECT ForumMessages.CreationDate, ForumTopics.TopicID, ForumTopics.Subject, ForumTopics.Visible, ForumTopics.LastMessageID, ForumTopics.RepliesCount AS Messages, ForumTopics.IsSticky, ForumTopics.IsClosed, ForumTopics.ViewsCount, ForumUsers.UserName, ForumUsers.UserID, ForumUsers.FirstName, ForumUsers.LastName,
					ForumUsers_LastMsg.UserID as LastUserID, ForumUsers_LastMsg.UserName as LastUserName, ForumUsers_LastMsg.FirstName as LastFirstName, ForumUsers_LastMsg.LastName as LastLastName, ForumMessages.Body, ForumMessages.CreationDate
					FROM ((ForumTopics
					LEFT JOIN ForumMessages ON ForumMessages.MessageID = ForumTopics.LastMessageID)
					LEFT JOIN ForumUsers ON ForumTopics.UserID = ForumUsers.UserID)
					LEFT JOIN ForumUsers ForumUsers_LastMsg ON ForumUsers_LastMsg.UserID=ForumMessages.UserID
					WHERE ForumTopics.ForumID=" + forumID + @"
					ORDER BY ForumTopics.IsSticky DESC, ForumTopics.LastMessageID DESC";
			}

			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();
			DataTable dt = new DataTable();
			DbDataReader dr = cn.ExecuteReader(sqlText, sqlParams.ToArray());
			dt.Load(dr);
			dr.Close();
			if (!openConn) cn.Close();

			//save it to cache (the cahce is reset when new message added to topic, or new topic etc)
			//save ONLY if it's not a premoderated forum, of current user is NOT moderator
			if (!forumIsPremoderated || !isModerator)
			{
				HttpRuntime.Cache.Add("Topics" + forumID, dt, null, DateTime.Now.AddMinutes(15),
				                      System.Web.Caching.Cache.NoSlidingExpiration,
				                      System.Web.Caching.CacheItemPriority.Normal,
				                      null);
			}

			return dt;
		}

		public static void StickTopic(int topicId, bool isSticky, DbConnection cn)
		{
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			cn.ExecuteNonQuery("UPDATE ForumTopics SET IsSticky=? WHERE TopicID=?", isSticky ? 1 : 0, topicId);

			//clear cache
			int forumId = GetForumIdForTopic(topicId, cn);
			Forum.ClearTopicsCache(forumId);

			if (!openConn) cn.Close();
		}

		public static void CloseTopic(int topicId, bool close, DbConnection cn)
		{
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			cn.ExecuteNonQuery("UPDATE ForumTopics SET IsClosed=? WHERE TopicID=?", close, topicId);

			//clear cache
			int forumId = GetForumIdForTopic(topicId, cn);
			Forum.ClearTopicsCache(forumId);

			if (!openConn) cn.Close();
		}

		public static void DeleteTopic(int topicId, DbConnection cn)
		{
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			int forumId = GetForumIdForTopic(topicId, cn);

			Attachments.DeleteTopicAttachments(topicId, cn);

			cn.ExecuteNonQuery("DELETE FROM ForumPollAnswers WHERE OptionID IN (SELECT OptionID FROM ForumPollOptions WHERE PollID IN (SELECT PollID FROM ForumPolls WHERE TopicID=?))", topicId);
			cn.ExecuteNonQuery("DELETE FROM ForumPollOptions WHERE PollID IN (SELECT PollID FROM ForumPolls WHERE TopicID=?)", topicId);
			cn.ExecuteNonQuery("DELETE FROM ForumPolls WHERE TopicID=?", topicId);
			cn.ExecuteNonQuery("DELETE FROM ForumUploadedFiles WHERE MessageID IN (SELECT MessageID FROM ForumMessages WHERE TopicID=?)", topicId);
			cn.ExecuteNonQuery("DELETE FROM ForumMessages WHERE TopicID=?" ,topicId);
			cn.ExecuteNonQuery("DELETE FROM ForumSubscriptions WHERE TopicID=?",topicId);
			cn.ExecuteNonQuery("DELETE FROM ForumTopics WHERE TopicID=?", topicId);

			if (!openConn) cn.Close();

			//clearing cache
			Forum.ClearFrontPageCacheForGuests();
			Forum.ClearTopicsCache(forumId);
		}

		/// <summary>
		/// creates a new empty topic (with NO MESSAGES)
		/// </summary>
		public static int CreateTopic(DbConnection cn, int forumId, int userId, string subject, string msgBody, bool visible)
		{
			if (Formatting.ContainsBadWords(subject)) return 0;

			int topicId;
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			cn.ExecuteNonQuery("INSERT INTO ForumTopics (ForumID, UserID, Subject, Visible) VALUES (?, ?, ?, ?)", forumId, userId, subject, visible);

			//now get the topicid we just created
			topicId = Convert.ToInt32(cn.ExecuteScalar("SELECT MAX(TopicID) FROM ForumTopics WHERE Subject=?", subject));

			if (!openConn) cn.Close();

			//this "IF" is here to make the code more readable
			//I know its lame, dont bother
			if(visible)
				Utils.SendNotifications.SendNewTopicNotificationEmails(topicId, msgBody, false, false);
			else
				Utils.SendNotifications.SendNewTopicNotificationEmails(topicId, msgBody, true, false);

			//clear cache
			Forum.ClearTopicsCache(forumId);
			Achievements.AddSuccess(AchievementType.TopicStarter, userId);
			return topicId;
		}

		//get topic subj and author etc
		public static bool GetBasicTopicInfo(int topicId, DbConnection cn, out int forumId, out int userId, out string topicSubject, out bool isClosed)
		{
			topicSubject = "";
			userId = 0;
			forumId = 0;
			isClosed = false;

			DbDataReader dr = cn.ExecuteReader("SELECT UserID, Subject, ForumID, IsClosed FROM ForumTopics WHERE TopicID=" + topicId);
			bool retVal;
			if (retVal = dr.Read())
			{
				topicSubject = dr["Subject"].ToString();
				userId = Convert.ToInt32(dr["UserID"]);
				forumId = Convert.ToInt32(dr["ForumID"]);
				isClosed = Convert.ToBoolean(dr["IsClosed"]);
			}
			dr.Close();
			return retVal;
		}

		/// <summary>
		/// creates a poll in a given topic
		/// </summary>
		public static void CreatePoll(DbConnection cn, int topicId, string question, List<string> options)
		{
			if (string.IsNullOrEmpty(question)) return;
			if (options == null || options.Count == 0) return;

			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			//add a poll
			cn.ExecuteNonQuery("INSERT INTO ForumPolls (TopicID, Question) VALUES (?, ?)", topicId, question);

			//now get the ID of the poll we just created
			int pollId = Convert.ToInt32(cn.ExecuteScalar("SELECT MAX(PollID) FROM ForumPolls WHERE TopicID=?", topicId));

			//get options from the Request.Form colecction
			foreach(string option in options)
			{
				//add option
				cn.ExecuteNonQuery("INSERT INTO ForumPollOptions (PollID, OptionText) VALUES (?, ?)", pollId, option);
			}

			if (!openConn) cn.Close();
		}

		public static void ChangeTopicSubject(DbConnection cn, int topicId, string subject)
		{
			if (Formatting.ContainsBadWords(subject)) return;

			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			subject = subject.Replace("<", "&lt;").Replace(">", "&gt;");
			
			cn.ExecuteNonQuery("UPDATE ForumTopics SET Subject = ? WHERE TopicID = ?", subject, topicId);

			//clear cache
			int forumId = GetForumIdForTopic(topicId, cn);
			Forum.ClearTopicsCache(forumId);

			if (!openConn) cn.Close();
		}
	}
}
