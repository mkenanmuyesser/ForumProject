using System;
using System.Collections;
using System.Configuration;
using System.Data.Common;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using Jitbit.Utils;

namespace aspnetforum.Utils
{
	public class SendNotifications
	{
		public static void SendEmailToUserGroup(int groupId, string subject, string body, bool sendPm = false)
		{
			List<string> mailRecipients = new List<string>();
			List<int> pmRecipients = new List<int>();

			using (var cn = DB.CreateOpenConnection())
			{
				//addin subscribers to the recipients list
				var usersInGroup = User.GetUserIdsInGroup(groupId);

				DbDataReader dr = cn.ExecuteReader(
					@"SELECT UserID, Email
					FROM ForumUsers
					WHERE UserID IN (" + usersInGroup.Select(x => x.ToString()).Aggregate((x, y) => x + "," + y) + ") AND Disabled=0");

				while (dr.Read())
				{
					if (sendPm)
					{
						pmRecipients.Add(Convert.ToInt32(dr["UserID"]));
					}
					else
					{
						if (AsyncSendMail.MailAddressTryParse(dr["Email"].ToString()))
							mailRecipients.Add(dr["Email"].ToString());
					}
				}
				dr.Close();
			}

			if (sendPm)
			{
				foreach (var userId in pmRecipients)
					User.SendPM(userId, subject + "\n\n" + body);
			}
			else
			{
				AsyncSendMail mailer = new AsyncSendMail(
					Settings.MailServer,
					!string.IsNullOrEmpty(Settings.MailServerLogin),
					Settings.MailServerLogin,
					Settings.MailServerPassword,
					Settings.MailServerPort,
					Settings.MailUseSSL,
					mailRecipients.ToArray(),
					Settings.MailFromAddress,
					null,
					subject,
					body,
					null,
					false);
				mailer.SendSynchronously();
			}
		}

		public static void UpdateTopicNotificationSettings(int userId, int topicId, bool notify, DbConnection cn)
		{
			if (!Settings.MailNotificationsEnabled) return;

			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			cn.ExecuteNonQuery("DELETE FROM ForumSubscriptions WHERE UserID=" + userId + " AND TopicID=" + topicId);
			if (notify)
			{
				cn.ExecuteNonQuery("INSERT INTO ForumSubscriptions (UserID, TopicID) VALUES (" + userId + ", " + topicId + ")");
			}

			if (!openConn) cn.Close();
		}

		//the method sends notifications of new forum MESSAGES
		public static void SendNewMsgNotificationEmails(int topicID, int messageID, bool sendToModeratorsOnly, bool sendToAllButModerators)
		{
			if (!Settings.MailNotificationsEnabled) return;

			int forumID;
			List<string> recipients = new List<string>();
			string subj, msgBody, msgAuthor, forumName;

			using (DbConnection cn = DB.CreateOpenConnection())
			{
				//getting topic info
				DbDataReader dr = cn.ExecuteReader("SELECT ForumID, Subject FROM ForumTopics WHERE TopicID=" + topicID);
				dr.Read();
				forumID = Convert.ToInt32(dr["ForumID"]);
				subj = dr["Subject"].ToString();
				dr.Close();

				//getting the message text and author
				dr = cn.ExecuteReader(@"SELECT ForumMessages.UserID, ForumMessages.Body, ForumUsers.UserName, ForumUsers.FirstName, ForumUsers.LastName
					FROM ForumMessages
					LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID
					WHERE MessageID=" + messageID);
				dr.Read();
				msgBody = dr["Body"].ToString();
				int userID = Convert.ToInt32(dr["UserID"]);
				msgAuthor = Utils.User.GetUserDisplayName(dr["UserName"], dr["FirstName"], dr["LastName"]);
				dr.Close();

				//addin TOPIC subscribers to the recipients list
				if (!sendToModeratorsOnly)
				{
					dr = cn.ExecuteReader(@"SELECT ForumUsers.Email FROM ForumSubscriptions
						INNER JOIN ForumUsers ON ForumSubscriptions.UserID = ForumUsers.UserID
						WHERE ForumUsers.Disabled=0 AND ForumUsers.UserID<>" + userID + " AND ForumSubscriptions.TopicID=" + topicID);
					while (dr.Read())
					{
						string email = dr["Email"].ToString();
						if (!recipients.Contains(email)) recipients.Add(email);
					}
					dr.Close();


					//adding FORUM subscribers to the recipients list
					dr = cn.ExecuteReader(@"SELECT ForumUsers.Email
					FROM ForumNewForumMsgSubscriptions
					INNER JOIN ForumUsers ON ForumNewForumMsgSubscriptions.UserID = ForumUsers.UserID
					WHERE ForumUsers.Disabled=0 AND ForumUsers.UserID<>" + userID + " AND ForumNewForumMsgSubscriptions.ForumID=" + forumID);
					while (dr.Read())
					{
						string email = dr["Email"].ToString();
						if (!recipients.Contains(email)) recipients.Add(email);
					}
					dr.Close();
				}

				//addin moderators to the recipients list
				if (Settings.NotifyModeratorOfNewMessages && !sendToAllButModerators)
				{
					dr = cn.ExecuteReader("SELECT ForumUsers.Email FROM ForumModerators INNER JOIN ForumUsers ON ForumModerators.UserID = ForumUsers.UserID WHERE ForumUsers.UserID<>" + userID + " AND ForumModerators.ForumID=" + forumID);
					while (dr.Read())
					{
						string email = dr["Email"].ToString();
						if (!recipients.Contains(email)) recipients.Add(email);
					}
					dr.Close();
				}

				forumName = cn.ExecuteScalar("SELECT Title FROM Forums WHERE ForumID=" + forumID).ToString();
			}

			string topicurl = Various.ForumURL + Various.GetTopicURL(topicID, subj);

			//adding "?lastpage=1" to the url
			if (topicurl.IndexOf("?") > -1) topicurl += "&lastpage=1";
			else topicurl += "?lastpage=1";

			//adding link to the actual post
			topicurl += "#post" + messageID.ToString();

			string htmlBody = Resources.various.ThreadUpdatedEmailBody +
				"<BR><P>\"" + forumName +
				"\" - \"" + subj +
				"\" - From: \"" + msgAuthor +
				"\"<BR><P>" + Formatting.FormatMessageHTML(msgBody) +
				"<BR><P><A HREF=\"" + topicurl + "\">" + topicurl + "</A>";
			string subject = Settings.ForumTitle + " - " + Resources.various.ThreadUpdatedEmailSubject;

			SendEmail(recipients.ToArray(), subject, htmlBody, isHtml: true);
		}

		//the method sends notifications of new forum TOPICS
		public static void SendNewTopicNotificationEmails(int topicID, string msgBody, bool sendToModeratorsOnly, bool sendToAllButModerators)
		{
			if (!Settings.MailNotificationsEnabled) return;

			if (sendToAllButModerators && sendToModeratorsOnly) return; //nonsense

			DbCommand cmd = DB.CreateCommand();
			cmd.Connection.Open();

			//get topic subj and author
			cmd.CommandText = @"SELECT ForumTopics.UserID, ForumTopics.Subject, ForumTopics.ForumID, ForumUsers.UserName, ForumUsers.FirstName, ForumUsers.LastName
				FROM ForumTopics
				LEFT JOIN ForumUsers ON ForumTopics.UserID=ForumUsers.UserID
				WHERE TopicID=" + topicID;
			DbDataReader dr = cmd.ExecuteReader();
			dr.Read();
			string subject = dr["Subject"].ToString();
			int userID = Convert.ToInt32(dr["UserID"]);
			int forumID = Convert.ToInt32(dr["ForumID"]);
			string msgAuthor = Utils.User.GetUserDisplayName(dr["UserName"], dr["FirstName"], dr["LastName"]);
			dr.Close();

			List<string> recipients = new List<string>(); //notification recipients

			//addin subscribers to the recipients list
			if (!sendToModeratorsOnly)
			{
				cmd.CommandText = @"SELECT ForumUsers.Email FROM ForumNewTopicSubscriptions
				INNER JOIN ForumUsers ON ForumNewTopicSubscriptions.UserID = ForumUsers.UserID
				WHERE ForumUsers.Disabled=0 AND ForumUsers.UserID<>" + userID + " AND ForumNewTopicSubscriptions.ForumID=" + forumID;
				dr = cmd.ExecuteReader();
				while (dr.Read())
				{
					string email = dr["Email"].ToString();
					if (!recipients.Contains(email)) recipients.Add(email);
				}
				dr.Close();
			}

			//addin moderators to the recipients list
			if (Settings.NotifyModeratorOfNewMessages && !sendToAllButModerators)
			{
				cmd.CommandText = "SELECT ForumUsers.Email FROM ForumModerators INNER JOIN ForumUsers ON ForumModerators.UserID = ForumUsers.UserID WHERE ForumUsers.UserID<>" + userID + " AND ForumModerators.ForumID=" + forumID;
				dr = cmd.ExecuteReader();
				while (dr.Read())
				{
					string email = dr["Email"].ToString();
					if (!recipients.Contains(email)) recipients.Add(email);
				}
				dr.Close();
			}

			cmd.CommandText = "SELECT Title FROM Forums WHERE ForumID=" + forumID;
			string forumName = cmd.ExecuteScalar().ToString();

			cmd.Connection.Close();

			string topicurl = Various.ForumURL + Various.GetTopicURL(topicID, forumName);
			//adding "?lastpage=1" to the url
			if (topicurl.IndexOf("?") > -1) topicurl += "&lastpage=1";
			else topicurl += "?lastpage=1";
			
			//Create an HTML version of the messageBody as well.
			string htmlBody = Resources.various.NewThreadEmailBody +
				"<BR><P>\"" + forumName +
				"\" - \"" + subject +
				"\" - From: \"" + msgAuthor +
				"\"<BR><P>" + Formatting.FormatMessageHTML(msgBody) +
				"<BR><P><A HREF=\"" + topicurl + "\">" + topicurl + "</A>";

			string mailsubj = Settings.ForumTitle + " - " + Resources.various.NewThreadEmailSubject;

			SendEmail(recipients.ToArray(), mailsubj, htmlBody, isHtml: true);
		}

		//the method sends notifications of new personal messages
		public static void SendPersonalNotificationEmails(int toUserID, string url, string msgBody)
		{
			string htmlBody = Resources.various.NewPersonalEmailBody + ": <A HREF=\"" + url + "\">" + url + "</A>" +"<BR><P><P>" + Formatting.FormatMessageHTML(msgBody); 
			string subject = Settings.ForumTitle + " - " + Resources.various.NewPersonalSubject;

			DbCommand cmd = DB.CreateCommand();
			cmd.Connection.Open();
			cmd.CommandText = "SELECT ForumUsers.Email FROM ForumUsers WHERE UserID=" + toUserID;
			DbDataReader dr = cmd.ExecuteReader();
			if(dr.Read())
			{
				string[] recipients = new string[1];
				recipients[0] = dr["Email"].ToString();

				SendEmail(recipients, subject, htmlBody, isHtml: true);
			}
			dr.Close();
			cmd.Connection.Close();
		}

		public static void SendEmail(string[] recipients, string subject, string body, bool async = true, bool isHtml = false)
		{
			if (recipients == null || recipients.Length == 0) return;

			AsyncSendMail mailer = new AsyncSendMail(
				Settings.MailServer,
				!string.IsNullOrEmpty(Settings.MailServerLogin),
				Settings.MailServerLogin,
				Settings.MailServerPassword,
				Settings.MailServerPort,
				Settings.MailUseSSL,
				recipients,
				Settings.MailFromAddress,
				null,
				subject,
				body,
				null,
				isHtml);
			if (async && !Settings.EmailDebug)
				mailer.SendAsynchronously();
			else
				mailer.SendSynchronously();
		}

		//the method sends a "welcome" email after the user hss been activated by admin
		public static void SendWelcomeEmail(string to, string url)
		{
			string body = Resources.various.WelcomeEmailBody + "\r\n\r\n" + url;
			string subject = Settings.ForumTitle + " - " + Resources.various.WelcomeEmailSubject;

			string[] recipients = new string[1];
			recipients[0] = to;

			SendEmail(recipients, subject, body);
		}

		//the method sends notifications of new user registrations to administrators
		public static void SendNewUserRegAdminNotification(string url)
		{
			string body = "New user has registered at the forum.\r\n\r\nLink to the user's profile: " + url;
			string subject = Settings.ForumTitle + " - new user registration";

			DbCommand cmd = DB.CreateCommand();
			cmd.Connection.Open();
			cmd.CommandText = "SELECT ForumUsers.Email FROM ForumUsers INNER JOIN ForumAdministrators ON ForumUsers.UserID=ForumAdministrators.UserID";

			DbDataReader dr = cmd.ExecuteReader();
			List<string> recipients = new List<string>();
			while (dr.Read())
			{
				recipients.Add(dr["Email"].ToString());
			}

			if (recipients.Count > 0)
			{
				SendEmail(recipients.ToArray(), subject, body);
			}
			dr.Close();
			cmd.Connection.Close();
		}
	}
}
