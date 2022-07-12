using System;
using System.Collections.Generic;
using System.Web;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;

namespace aspnetforum.Utils
{
	public static class Message
	{
		public static int AddMessage(DbConnection cn, int topicId, string msg, bool visible, string posterIpAddress, bool suppressNotification)
		{
			if (Formatting.ContainsBadWords(msg)) return 0;

			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			cn.ExecuteNonQuery("INSERT INTO ForumMessages (TopicID, UserID, Body, CreationDate, Visible, IPAddress) VALUES (?, ?, ?, ?, ?, ?)",
				topicId,
				User.CurrentUserID,
				msg,
				Utils.Various.GetCurrTime(),
				visible,
				posterIpAddress);

			//get the last message's ID
			var res = cn.ExecuteScalar("SELECT MAX(MessageID) FROM ForumMessages WHERE TopicID=" + topicId + " AND UserID=" + User.CurrentUserID);
			int messageId = (res == null || res == DBNull.Value) ? 0 : Convert.ToInt32(res);

			//incrementing repliescount (well... actually - re-calculating it)
			res = cn.ExecuteScalar("SELECT Count(MessageID) FROM ForumMessages WHERE TopicID=" + topicId);
			cn.ExecuteNonQuery("UPDATE ForumTopics SET RepliesCount=" + res.ToString() + " WHERE TopicID=" + topicId);

			//incrementing PostsCount in Users table
			//only for registered users (if guestmode is on)
			if (User.CurrentUserID != 0)
			{
				cn.ExecuteNonQuery("UPDATE ForumUsers SET PostsCount=PostsCount+1 WHERE UserID=" + User.CurrentUserID);
			}

			//updating LastMessageID in Topics table
			if (visible)
			{
				cn.ExecuteNonQuery("UPDATE ForumTopics SET LastMessageID=" + messageId + " WHERE TopicID=" + topicId);
			}

			if (!openConn) cn.Close();

			if(!suppressNotification)
				SendNotifications.SendNewMsgNotificationEmails(topicId, messageId, !visible, false);

			//clearing cache
			Forum.ClearFrontPageCacheForGuests();

			//clear cache
			int forumId = Topic.GetForumIdForTopic(topicId, cn);
			Forum.ClearTopicsCache(forumId);

			Achievements.AddSuccess(AchievementType.WelcomeAboard, User.CurrentUserID);
			Achievements.TestAchievements(User.CurrentUserID, AchievementType.Lonely, AchievementType.SomethingToSay, AchievementType.FreeTime);

			return messageId;
		}

		public static void UpdateMessageText(DbConnection cn, int messageId, string msg, bool visible)
		{
			if (Formatting.ContainsBadWords(msg)) return;

			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			// Record last editor and date at the end of the message.
			msg += "\r\n[i]edited by " + HttpContext.Current.Session["aspnetforumUserName"] +
					" on " + Utils.Various.GetCurrTime().ToShortDateString() + "[/i]";

			cn.ExecuteNonQuery("UPDATE ForumMessages SET Body=?, Visible=? WHERE MessageID=" + messageId, msg, visible);
			
			if (!openConn) cn.Close();
		}

		/// <summary>
		/// deletes a personal message
		/// </summary>
		public static void DeletePersonalMessage(int deleterUserId, int deletedMessageID, DbConnection cn)
		{
			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			var msg = cn.ExecuteData(@"SELECT FromUserID, ToUserID, HiddenBySender, HiddenByRecipient FROM ForumPersonalMessages WHERE MessageID=?", deletedMessageID)[0];
			bool isDeleterSender = (deleterUserId == Convert.ToInt32(msg["FromUserID"]));
			bool isDeleterRecipient = (deleterUserId == Convert.ToInt32(msg["ToUserID"]));
			bool hiddenBySender = (bool)msg["HiddenBySender"]; //already deleted by sender?
			bool hiddenByRecipient = (bool)msg["HiddenByRecipient"]; //already deleted by sender?

			hiddenBySender = hiddenBySender || isDeleterSender;
			hiddenByRecipient = hiddenByRecipient || isDeleterRecipient;

			if (hiddenByRecipient && hiddenBySender) //both people chose to hide the msg. delete it
			{
				Utils.Attachments.DeleteMessageAttachments(deletedMessageID, true, cn);
				cn.ExecuteNonQuery("DELETE FROM ForumPersonalMessages WHERE MessageID=?", deletedMessageID);
			}
			else
				cn.ExecuteNonQuery("UPDATE ForumPersonalMessages SET HiddenBySender=?, HiddenByRecipient=? WHERE MessageID=?", hiddenBySender, hiddenByRecipient, deletedMessageID);

			if (!openConn) cn.Close();
		}

		/// <summary>
		/// deletes message, returns TRUE if it was the last message in the topic
		/// </summary>
		/// <param name="msgid"></param>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public static bool DeleteMessage(int msgid, DbConnection cn)
		{
			bool retVal;

			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			int topicId = 0, userId = 0;

			Attachments.DeleteMessageAttachments(msgid, false, cn);

			//decreasing user posts count (-1)
			DbDataReader dr = cn.ExecuteReader("SELECT UserID, TopicID FROM ForumMessages WHERE MessageID=" + msgid);
			if (dr.Read())
			{
				userId = Convert.ToInt32(dr["UserID"]);
				topicId = Convert.ToInt32(dr["TopicID"]);
				dr.Close();
				cn.ExecuteNonQuery("UPDATE ForumUsers SET PostsCount = PostsCount-1 WHERE UserID=" + userId);
			}

			//deleting message
			cn.ExecuteNonQuery("DELETE FROM ForumMessages WHERE MessageID=" + msgid);

			//updating LastMessageID in Topics table
			var res = cn.ExecuteScalar("SELECT MAX(MessageID) FROM ForumMessages WHERE Visible=? AND TopicID=" + topicId, true);
			if (res != null && !(res is DBNull))
			{
				int maxmsg = Convert.ToInt32(res);
				cn.ExecuteNonQuery("UPDATE ForumTopics SET RepliesCount=RepliesCount-1, LastMessageID=" + maxmsg + " WHERE TopicID=" + topicId);
				retVal = false;
				
				//clear cache
				int forumId = Topic.GetForumIdForTopic(topicId, cn);
				Forum.ClearTopicsCache(forumId);
			}
			else //it was the last message - delete the topic
			{
				Topic.DeleteTopic(topicId, cn);
				retVal = true;
			}

			if (!openConn) cn.Close();

			//clearing cache
			Forum.ClearFrontPageCacheForGuests();
			
			return retVal;
		}

		/// <summary>
		/// get message's date and username by messageID
		/// (this is used to show "last message" column in the topics list, in the forums list etc.)
		/// </summary>
		public static string GetMsgInfoByID(object messageId, DbCommand cmd)
		{
			return GetMsgInfoByID(messageId, null, null, cmd);
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
		public static string GetMsgInfoByID(object messageId, object topicSubject, int? repliesCount, DbCommand cmd)
		{
			if (messageId.ToString().Trim() == string.Empty || messageId.ToString() == "0") return string.Empty;

			string retval = "";

			//this method is called from grids, so
			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cmd.Connection.State == ConnectionState.Open);
			if (!openConn) cmd.Connection.Open();

			int topicId = 0, userId = 0;
			string userName = "", firstName = "", lastName = "", body = ""; DateTime creationDate = DateTime.Now;

			cmd.CommandText = @"SELECT ForumMessages.CreationDate, ForumUsers.UserName, ForumMessages.UserID, ForumMessages.TopicID, ForumUsers.FirstName, ForumUsers.LastName, ForumMessages.Body
				FROM ForumMessages
				LEFT JOIN ForumUsers ON ForumUsers.UserID = ForumMessages.UserID
				WHERE ForumMessages.MessageID=" + messageId;
			DbDataReader dr = cmd.ExecuteReader();
			if (dr.Read())
			{
				topicId = Convert.ToInt32(dr["TopicID"]);
				userId = dr["UserID"] != DBNull.Value ? Convert.ToInt32(dr["UserID"]) : 0;
				userName = dr["UserName"].ToString();
				firstName = dr["FirstName"].ToString();
				lastName = dr["LastName"].ToString();
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

			retval = FormatMessageInfo(Convert.ToInt32(messageId), topicId, userId, userName, firstName, lastName, topicSubject, creationDate, repliesCount, body);

			if (!openConn) cmd.Connection.Close();

			return retval.ToString();
		}

		private static int? RecalculateLastMessageID(int topicId)
		{
			int? maxMsgId;
			using (var cn = Utils.DB.CreateOpenConnection())
			{
				maxMsgId = cn.ExecuteScalar("SELECT MAX(MessageID) FROM ForumMessages WHERE TopicID=?", topicId) as int?;
				if (maxMsgId.HasValue)
					cn.ExecuteNonQuery("UPDATE ForumTopics SET LastMessageID=? WHERE TopicID=?", maxMsgId.Value, topicId);
			}
			return maxMsgId;
		}

		//(this is used to show "last message" column in the topics list, in the forums list etc.)
		public static string FormatMessageInfo(object messageId, object topicId, object userId, object userName, object firstName, object lastName, object topicSubject, object creationDate, int? msgCount)
		{
			return FormatMessageInfo(messageId, topicId, userId, userName, firstName, lastName, topicSubject, creationDate, msgCount, topicSubject);
		}
		public static string FormatMessageInfo(object messageId, object topicId, object userId, object userName, object firstName, object lastName, object topicSubject, object creationDate, int? msgCount, object msgBody)
		{
			if (msgBody == null || msgBody is DBNull) //msgbody is null - probably cause messageId is wrong, probably cause LastMsgID is wrong
			{
				int? maxmsgid = RecalculateLastMessageID(Convert.ToInt32(topicId));
				if(maxmsgid.HasValue)
					return GetMsgInfoByID(maxmsgid.Value, Utils.DB.CreateCommand());
			}

			StringBuilder retval = new StringBuilder();

			string uname;
			if (userId != null && !(userId is DBNull) && Convert.ToInt32(userId) != 0) uname = User.GetUserDisplayName(userName, firstName, lastName);
			else uname = Resources.various.Guest;

			string messageUrl = Various.GetTopicURL(topicId, topicSubject);
			string queryString = (messageUrl.IndexOf("?") > -1) ? "&amp;" : "?";

			//claculate link to last page
			if (!msgCount.HasValue) msgCount = 0;
			int pageCount = (int)Math.Ceiling(Convert.ToDouble(msgCount) / Settings.PageSize);
			if (pageCount > 1)
				messageUrl += queryString + "page=" + (pageCount-1);

			messageUrl += "#post" + messageId;

			retval.Append("<div class='gray2'>");
			//retval.Append(creationDate.ToString("ddd, d MMM, yy, "));
			if (creationDate != null && !(creationDate is DBNull))
			{
				DateTime dtCreationDate = Convert.ToDateTime(creationDate);
				retval.Append(ForumPage.ToAgoString(dtCreationDate));
			}
			retval.Append("<br>");

			retval.Append(Resources.various.From);
			retval.Append(" <a href=\"");
			retval.Append(messageUrl);
			retval.Append("\" title=\"");
			retval.Append(Formatting.StripBBCode(msgBody.ToString().Replace("\"", "")));
			retval.Append("\">");
			retval.Append(uname);
			retval.Append("</a>");

			retval.Append("</div>");

			return retval.ToString();
		}

		public static void ReportToModerator(int msgid, int userId, string report, DbConnection cn)
		{
			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			object res = cn.ExecuteScalar("SELECT UserID FROM ForumComplaints WHERE MessageID=" + msgid + " and UserID=" + userId);
			if (res == null)
			{
				cn.ExecuteNonQuery("INSERT INTO ForumComplaints (UserID, MessageID, ComplainText) VALUES (?, ?, ?)", userId, msgid, report);
			}

			if (!openConn) cn.Close();
		}

		//updating LastMessageID in Topics table
		public static void RefreshLastMessageIdForTopic(int topicId, DbConnection cn)
		{
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			//updating LastMessageID in Topics table
			var res = cn.ExecuteScalar("SELECT MAX(MessageID) FROM ForumMessages WHERE Visible=? AND TopicID=" + topicId, true);
			int maxmsg = (res == null || res == DBNull.Value) ? 0 : Convert.ToInt32(res);
			cn.ExecuteNonQuery("UPDATE ForumTopics SET LastMessageID=" + maxmsg + " WHERE TopicID=" + topicId);

			if (!openConn) cn.Close();
		}

		public static void ApproveMessage(int approvedMessageID, DbConnection cn)
		{
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();
			
			cn.ExecuteNonQuery("UPDATE ForumMessages SET Visible=? WHERE MessageID=?", true, approvedMessageID);

			int topicId;
			var res = cn.ExecuteScalar("SELECT TopicID FROM ForumMessages WHERE MessageID=?", approvedMessageID);
			topicId = Convert.ToInt32(res);

			//approving the topic as well (just in case)
			cn.ExecuteNonQuery("UPDATE ForumTopics SET Visible=? WHERE TopicID=?", true, topicId);

			//updating LastMessageID in Topics table
			RefreshLastMessageIdForTopic(topicId, cn);

			//clear cache
			int forumId = Topic.GetForumIdForTopic(topicId, cn);
			Forum.ClearTopicsCache(forumId);
			ModeratorStats.ResetUnapprovedCountCache();

			if (!openConn) cn.Close();

			//send notification to subscribed users
			SendNotifications.SendNewMsgNotificationEmails(topicId, approvedMessageID, false, true);
		}

		public static void AcceptMessage(int acceptedMessageId, DbConnection cn)
		{
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			var res = cn.ExecuteScalar("SELECT TopicID FROM ForumMessages WHERE MessageID=?", acceptedMessageId);
			int topicId = Convert.ToInt32(res);

			//clearing all other msgs
			cn.ExecuteNonQuery("UPDATE ForumMessages SET AcceptedAnswer=? WHERE TopicID=?", false, topicId);

			cn.ExecuteNonQuery("UPDATE ForumMessages SET AcceptedAnswer=? WHERE MessageID=?", true, acceptedMessageId);

			if (!openConn) cn.Close();
		}

		public static int GetOnModerationThreadsCount(int userId)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				object count;
				if (User.IsAdministrator(userId))
				{
					count = cn.ExecuteScalar(@"SELECT count(ForumMessages.MessageID)
					FROM (ForumMessages LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID)
					INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID
					WHERE ForumMessages.Visible=?", false);
				}
				else
				{
					count = cn.ExecuteScalar(@"SELECT count(ForumMessages.MessageID)
					FROM (ForumMessages LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID)
					INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID
					WHERE ForumMessages.Visible=?
					AND ForumTopics.ForumID IN (SELECT DISTINCT ForumID FROM ForumModerators WHERE UserID=" + userId + @")", false);
				}

				return Convert.ToInt32(count);
			}
		}

		/// <summary>
		/// rates a message up or down and returns the message's overall rating
		/// </summary>
		public static int? RateMessage(int messageId, int userId, int score)
		{
			if (userId == 0 || messageId == 0) return null;
			if (score != -1 && score != 1) return null;


			int? msgRating = null;
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				object res = cn.ExecuteScalar("SELECT VoterUserID FROM ForumMessageRating WHERE VoterUserID=? AND MessageID=?", userId, messageId);
				bool alreadyVoted = (res != null);

				res = cn.ExecuteScalar("SELECT UserID FROM ForumMessages WHERE MessageID=?", messageId);
				bool ownMessage = (Convert.ToInt32(res) == userId);

				if (ownMessage)
					Achievements.AddSuccess(AchievementType.Selfish, userId);

				if (!alreadyVoted && !ownMessage) //not already voted and not voting for own post
				{
					//save rating
					cn.ExecuteNonQuery("INSERT INTO ForumMessageRating(MessageID, VoterUserID, Score) VALUES (?, ?, ?)", messageId, userId, score);

					//getting message rating
					msgRating = Convert.ToInt32(cn.ExecuteScalar("SELECT SUM(Score) FROM ForumMessageRating WHERE MessageID=?", messageId));

					//caching msg rating in messages tbl
					cn.ExecuteNonQuery("UPDATE ForumMessages SET Rating=" + msgRating + " WHERE MessageID=?", messageId);

					//caching user rating in user tbl
					int authorId = Convert.ToInt32(cn.ExecuteScalar("SELECT UserID FROM ForumMessages WHERE MessageID=?", messageId));

					int userRating = Convert.ToInt32(cn.ExecuteScalar(@"SELECT SUM(Rating) FROM ForumMessages WHERE UserID=?", authorId));

					cn.ExecuteNonQuery("UPDATE ForumUsers SET ReputationCache = " + userRating + " WHERE UserID=" + authorId);
				}

				return msgRating;
			}
		}

		public static void DeletePersonalConversationWithUser(int deleterUserId, int userId, DbConnection cn)
		{
			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			var msgs = cn.ExecuteData(@"SELECT MessageID FROM ForumPersonalMessages WHERE (FromUserID=? AND ToUserID=?) OR (FromUserID=? AND ToUserID=?)", deleterUserId, userId, userId, deleterUserId);
			foreach (var m in msgs)
			{
				DeletePersonalMessage(deleterUserId, Convert.ToInt32(m["MessageID"]), cn);
			}

			if (!openConn) cn.Close();
		}
	}
}
