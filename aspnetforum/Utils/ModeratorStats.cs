using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jitbit.Utils;

namespace aspnetforum.Utils
{
	public static class ModeratorStats
	{
		public static int GetComplaintsCount()
		{
			int count = HttpContext.Current.Session.GetWithTimeout("complaints") as int? ?? -1;

			if (count == -1)
			{
				string sql;
				if (User.IsAdministrator(User.CurrentUserID))
				{
					sql = @"SELECT count(MessageID)	FROM ForumComplaints";
				}
				else
				{
					sql = @"SELECT COUNT(ForumMessages.MessageID)
					FROM (ForumMessages
					INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID)
					INNER JOIN ForumComplaints ON ForumMessages.MessageID=ForumComplaints.MessageID
					WHERE ForumTopics.ForumID IN (SELECT DISTINCT ForumID FROM ForumModerators WHERE UserID=" + User.CurrentUserID + @")";
				}

				using (var cn = DB.CreateOpenConnection())
				{
					count = Convert.ToInt32(cn.ExecuteScalar(sql));
				}
			}

			HttpContext.Current.Session.AddWithTimeout("complaints", count, TimeSpan.FromMinutes(10));

			return count;
		}

		public static int GetUnapprovedMsgsCount()
		{
			int count = HttpContext.Current.Session.GetWithTimeout("unapprovedposts") as int? ?? -1;

			if (count == -1)
			{
				string sql;
				if (User.IsAdministrator(User.CurrentUserID))
				{
					sql = @"SELECT COUNT(ForumMessages.MessageID) FROM ForumMessages WHERE ForumMessages.Visible=?";
				}
				else
				{
					sql = @"SELECT COUNT(ForumMessages.MessageID)
					FROM (ForumMessages
					LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID)
					INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID
					WHERE ForumMessages.Visible=?
					AND ForumTopics.ForumID IN (SELECT DISTINCT ForumID FROM ForumModerators WHERE UserID=" + User.CurrentUserID + @")";
				}

				using (var cn = DB.CreateOpenConnection())
				{
					count = Convert.ToInt32(cn.ExecuteScalar(sql, false));
				}
			}

			HttpContext.Current.Session.AddWithTimeout("unapprovedposts", count, TimeSpan.FromMinutes(10));

			return count;
		}

		public static void ResetUnapprovedCountCache()
		{
			HttpContext.Current.Session.Remove("unapprovedposts");
		}

		public static void ResetComplaintsCountCache()
		{
			HttpContext.Current.Session.Remove("complaints");
		}
	}
}