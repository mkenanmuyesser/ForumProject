using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.Common;

namespace aspnetforum.Utils
{
	public static class Forum
	{
		//get forum info
		public static bool GetBasicForumInfo(int forumId, DbConnection cn, out string title, out string description, out bool restrictTopicCreation, out bool premoderated, out bool membersOnly)
		{
			title = "";
			description ="";
			restrictTopicCreation = false;
			premoderated = false;
			membersOnly = false;

			DbDataReader dr = cn.ExecuteReader("SELECT Title, Description, MembersOnly, RestrictTopicCreation, Premoderated FROM Forums WHERE ForumID=" + forumId);
			bool retVal;
			if (retVal = dr.Read())
			{
				title = dr["Title"].ToString();
				description = dr["Description"].ToString(); ;
				restrictTopicCreation = Convert.ToBoolean(dr["RestrictTopicCreation"]);
				premoderated = Convert.ToBoolean(dr["Premoderated"]);
				membersOnly = Convert.ToBoolean(dr["MembersOnly"]);
			}
			dr.Close();
			return retVal;
		}

		private static int GetForumTopicCount(int forumID, DbConnection cn)
		{
			return GetForumTopicCountRecursive(forumID, cn);
		}

		//to save close/open/close/open connections a million times
		private static int GetForumTopicCountRecursive(int forumID, DbConnection cn)
		{
			int retval = 0;
			retval += Convert.ToInt32(cn.ExecuteScalar("SELECT COUNT(*) FROM ForumTopics WHERE ForumID=" + forumID));

			DataTable dt = new DataTable();
			dt.Load(cn.ExecuteReader("SELECT SubForumID FROM ForumSubforums WHERE ParentForumID=" + forumID));
			foreach (DataRow row in dt.Rows)
				retval += GetForumTopicCountRecursive(Convert.ToInt32(row[0]), cn);

			return retval;
		}

		public static string GetForumBreadCrumbs(int forumID, DbConnection cn)
		{
			string retval = "";

			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			//get the forum title
			object res = cn.ExecuteScalar("SELECT Title FROM Forums WHERE ForumID=" + forumID);
			if (res == null) return string.Empty;
			string forumTitle = res.ToString();

			retval = "<a href=\"" + Various.GetForumURL(forumID, forumTitle) + "\">" + forumTitle + "</a>";

			//check if it's a subforum
			res = cn.ExecuteScalar("SELECT ParentForumID FROM ForumSubforums WHERE SubForumID=" + forumID);
			if (res != null) retval = GetForumBreadCrumbs(Convert.ToInt32(res), cn) + " &raquo; " + retval;

			if (!openConn) cn.Close();

			return retval;
		}

		public static bool CheckForumReadPermissions(int forumID, int userID)
		{
			return GetReadableForumsForUser(userID).Contains(forumID);
		}

		public static bool CheckForumPostPermissions(int forumID, int userID)
		{
			return GetPostableForumsForUser(userID).Contains(forumID);
		}

		//returns forums available for user
		public static DataSet GetForumsForFrontpage(int userId)
		{
			//chack if it's in cache
			if (HttpRuntime.Cache["FrontPageDataSetForAnonymousUsers"] != null && userId == 0)
				return HttpRuntime.Cache["FrontPageDataSetForAnonymousUsers"] as DataSet;

			DataSet ds = new DataSet();
			ds.Tables.Add("ForumGroups");
			ds.Tables.Add("Forums");

			using (var cn = DB.CreateOpenConnection())
			{
				DbDataReader dr = cn.ExecuteReader("SELECT GroupID, GroupName FROM ForumGroups ORDER BY OrderByNumber");

				ds.Tables[0].Load(dr);
				dr.Close();

				string strSQLAllowedForums = GetReadableForumsForUserString(userId);

				string sql =
					@"SELECT Forums.ForumID, Forums.Title, Forums.Description, Forums.GroupID, MAX(ForumTopics.LastMessageID) as LatestMessageID,
					Forums.IconFile, COUNT(ForumTopics.TopicID) as TopicCount
				FROM (Forums LEFT OUTER JOIN ForumTopics ON ForumTopics.ForumID=Forums.ForumID)
				WHERE Forums.ForumID NOT IN (SELECT SubForumID FROM ForumSubforums) " + //not a subforum
					(userId == 0 ? " AND Forums.MembersOnly=0 " : "") +
					@" AND Forums.ForumID IN (" + strSQLAllowedForums + @")
				GROUP BY Forums.ForumID, Forums.Title, Forums.Description, Forums.GroupID, Forums.OrderByNumber, Forums.IconFile
				ORDER BY Forums.OrderByNumber";
				dr = cn.ExecuteReader(sql, true, userId, true);
				ds.Tables[1].Load(dr);
				dr.Close();
			}

			ds.Relations.Add(new DataRelation("ForumGroupsForums", ds.Tables[0].Columns["GroupID"], ds.Tables[1].Columns["GroupID"]));

			if (userId == 0)
			{
				//save it to cache (the cahce is reset when new message added to ANY forum, or when new forums added)
				HttpRuntime.Cache.Add("FrontPageDataSetForAnonymousUsers", ds, null, DateTime.Now.AddHours(1),
						System.Web.Caching.Cache.NoSlidingExpiration,
						System.Web.Caching.CacheItemPriority.Normal,
						null);
			}

			return ds;
		}

		public static void ClearTopicsCache(int forumId)
		{
			HttpRuntime.Cache.Remove("Topics" + forumId);
		}

		public static void ClearAllCache()
		{
			HttpRuntime.Close();
		}

		public static void ClearFrontPageCacheForGuests()
		{
			HttpRuntime.Cache.Remove("FrontPageDataSetForAnonymousUsers");
		}

		public static void DeleteForum(int forumId)
		{
			using (DbConnection cn = Utils.DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("DELETE FROM ForumGroupPermissions WHERE ForumID=" + forumId);
				cn.ExecuteNonQuery("DELETE FROM ForumUploadedFiles WHERE MessageID IN (SELECT MessageID FROM ForumMessages WHERE TopicID IN (SELECT TopicID FROM ForumTopics WHERE ForumID=" + forumId + "))");
				cn.ExecuteNonQuery("DELETE FROM ForumMessages WHERE TopicID IN (SELECT TopicID FROM ForumTopics WHERE ForumID=" + forumId + ")");
				cn.ExecuteNonQuery("DELETE FROM ForumSubscriptions WHERE TopicID IN (SELECT TopicID FROM ForumTopics WHERE ForumID=" + forumId + ")");
				cn.ExecuteNonQuery("DELETE FROM ForumTopics WHERE ForumID=" + forumId);
				cn.ExecuteNonQuery("DELETE FROM ForumSubforums WHERE ParentForumID=" + forumId + " OR SubForumID=" + forumId);
				cn.ExecuteNonQuery("DELETE FROM ForumModerators WHERE ForumID=" + forumId);
				cn.ExecuteNonQuery("DELETE FROM ForumNewTopicSubscriptions WHERE ForumID=" + forumId);
				cn.ExecuteNonQuery("DELETE FROM Forums WHERE ForumID=" + forumId);
			}

			ClearFrontPageCacheForGuests();
		}

		public static void AddForum(string forumName, string forumDescription, int forumGroupId)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery(@"INSERT INTO Forums (Title, Description, GroupID) VALUES (?, ?, ?)", forumName, forumDescription, forumGroupId);
			}

			ClearFrontPageCacheForGuests();
		}

		public static int? GetForumId(string forumName, int groupId)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				var res = cn.ExecuteScalar(@"SELECT ForumID FROM Forums WHERE Title=? AND GroupID=?", forumName, groupId);
				int? forumId = (res == null) ? null : (int?)Convert.ToInt32(res);
				return forumId;
			}
		}

		public static IEnumerable<int> GetReadableForumsForUser(int userId)
		{
			if (User.IsAdministrator(userId))
			{
				using (DbConnection cn = DB.CreateOpenConnection())
				{
					return cn.ExecuteOrm<int>(@"SELECT ForumID FROM Forums"); //admins should see all forums despite the permissions
				}
			}

			var groupsForUser = User.GetGroupIdsForUser(userId);

			string strGroups = groupsForUser.Any()
				? groupsForUser.Select(x => x.ToString()).Aggregate((x, y) => x + "," + y)
				: "0";

			using (DbConnection cn = DB.CreateOpenConnection())
			{
				if (userId == 0) //return "anonymuous" forums for non-authenticated users
					return cn.ExecuteOrm<int>(@"SELECT ForumID FROM Forums
						WHERE MembersOnly=? AND ForumID NOT IN (SELECT ForumID FROM ForumGroupPermissions WHERE AllowReading=?)", false,
						true);

				//"free for all" forums
				var anonymousForums = cn.ExecuteOrm<int>(
					@"SELECT ForumID FROM Forums WHERE ForumID NOT IN (SELECT ForumID FROM ForumGroupPermissions WHERE AllowReading=?)",
					true);

				//query select allowed forums
				var allowedForums =
					cn.ExecuteOrm<int>(
						@"SELECT ForumID FROM ForumGroupPermissions WHERE GroupID IN (" + strGroups + ") AND AllowReading=?", true);

				cn.Close();

				return anonymousForums.Concat(allowedForums).Distinct();
			}
		}

		public static string GetReadableForumsForUserString(int userId)
		{
			var forums = GetReadableForumsForUser(userId);
			return forums.Any()
			       	? forums.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y)
			       	: "0";
		}

		public static IEnumerable<int> GetPostableForumsForUser(int userId)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				if (User.IsAdministrator(userId))
					return cn.ExecuteOrm<int>(@"SELECT ForumID FROM Forums"); //admins should see all forums despite the permissions
			}

			var groupsForUser = User.GetGroupIdsForUser(userId);

				string strGroups = groupsForUser.Any()
					? groupsForUser.Select(x => x.ToString()).Aggregate((x, y) => x + "," + y)
					: "0";

			using (DbConnection cn = DB.CreateOpenConnection())
			{
				//"free for all" forums
				var anonymousForums = cn.ExecuteOrm<int>(
					@"SELECT ForumID FROM Forums WHERE ForumID NOT IN (SELECT ForumID FROM ForumGroupPermissions WHERE AllowPosting=?)", true);

				//query select allowed forums
				var allowedForums = cn.ExecuteOrm<int>(@"SELECT ForumID	FROM ForumGroupPermissions WHERE GroupID IN (" + strGroups + ") AND AllowPosting=?", true);

				cn.Close();

				return anonymousForums.Concat(allowedForums).Distinct();
			}
		}

		public static int? GetForumGroupIdByName(string groupName)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				var res = cn.ExecuteScalar("SELECT GroupID FROM ForumGroups WHERE GroupName=?", groupName);
				int? forumGroup = (res == null) ? null : (int?) Convert.ToInt32(res);
				return forumGroup;
			}
		}

		public static int AddForumGroup(string groupName)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("INSERT INTO ForumGroups (GroupName) VALUES (?)", groupName);
				return Convert.ToInt32(cn.ExecuteScalar("SELECT GroupID FROM ForumGroups WHERE GroupName=?", groupName));
			}
		}
	}
}
