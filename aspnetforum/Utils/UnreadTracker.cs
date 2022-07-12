using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using Jitbit.Utils;

namespace aspnetforum.Utils
{
	//http://stackoverflow.com/a/20594210/56621
	public static class UnreadTracker
	{
		//translate a dict into a string like "23,2;234,342;83264,23;"
		private static string SerializeToString(this Dictionary<int, int> dictionary)
		{
			var retval = new StringBuilder();
			foreach (var k in dictionary.Keys)
			{
				retval.Append(k); retval.Append(','); retval.Append(dictionary[k]); retval.Append(';');
			}
			return retval.ToString();
		}

		//translate a string like "23,2;234,342;83264,23;" into a dict
		private static Dictionary<int, int> DeSerializeFromString(string input)
		{
			var dict = new Dictionary<int, int>();
			if (input != null)
			{
				var pairs = input.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var p in pairs)
				{
					var keyValue = p.Split(new[] { ',' });
					if (keyValue.Length < 2) continue;
					try
					{
						dict.Add(int.Parse(keyValue[0]), int.Parse(keyValue[1]));
					}
					catch
					{
						continue; //int.parse problem or key exists. Anyays - it's invalid format, just move on.
					}
				}
			}
			return dict;
		}

		private static Dictionary<int, int> GetTrackingDictionary()
		{
			//cache in session to prevent parsing a cookie each time
			var dict = HttpContext.Current.Session["UnreadTracker"] as Dictionary<int, int>;
			if (dict != null) return dict;

			var cookie = HttpContext.Current.Request.Cookies["readtopics"];
			if (cookie != null)
			{
				dict = DeSerializeFromString(cookie.Value);
				HttpContext.Current.Session["UnreadTracker"] = dict;
				return dict;
			}

			return new Dictionary<int, int>();
		}

		private static void SaveTrackingDictionaryInCookiesAndSession(Dictionary<int, int> dict)
		{
			//cache in session to prevent parsing a cookie each time
			HttpContext.Current.Session["UnreadTracker"] = dict;
			var cookie = new HttpCookie("readtopics", dict.SerializeToString()) { Expires = DateTime.Now.AddDays(5) };
			HttpContext.Current.Response.Cookies.Add(cookie);
		}

		public static void TrackTopicReading(int topicId, int lastReadMessageId)
		{
			//todo
			var dict = GetTrackingDictionary();
			
			if(!dict.ContainsKey(topicId))
				dict.Add(topicId, lastReadMessageId);
			else
				dict[topicId] = lastReadMessageId;

			if (dict.Count > 30) //do not track more that 30 topics to prevent cookie overload
			{
				//find min msg-id in the dictionary and remove it, it's the most outdated topic
				var minMsgId = dict.Values.Min();
				dict.Remove(dict.Where(x => x.Value == minMsgId).First().Key);
			}
			SaveTrackingDictionaryInCookiesAndSession(dict);
		}

		private static int GetUpdatedThreadCount()
		{
			int count = HttpContext.Current.Session.GetWithTimeout("ForumUpdatedThreadsCount") as int? ?? -1;

			if (count == -1)
			{
				count = Utils.UnreadTracker.GetUpdatedThreads().Rows.Count;
				HttpContext.Current.Session.AddWithTimeout("ForumUpdatedThreadsCount", count, TimeSpan.FromMinutes(5));
			}
			return count;
		}

		public static DataTable GetUpdatedThreads()
		{
			string strSQLAllowedForums = Utils.Forum.GetReadableForumsForUserString(Utils.User.CurrentUserID);

			string strTopicConditon = "";
			var dict = GetTrackingDictionary();
			if (dict.Count > 0)
			{
				strTopicConditon += " AND (";
				foreach (var pair in dict)
				{
					strTopicConditon += "(ForumTopics.TopicID=" + pair.Key + " AND ForumTopics.LastMessageID>" + pair.Value + ") OR ";
				}
				strTopicConditon += "(ForumTopics.TopicID NOT IN (" + dict.Keys.Select(i => i.ToString()).Aggregate((i, j) => i + "," + j) + ") AND ForumMessages.CreationDate>?)  )";
			}

			using (var cn = DB.CreateOpenConnection())
			{
				var dr = cn.ExecuteReader(
					@"SELECT TOP 30 ForumTopics.TopicID, ForumTopics.Subject, ForumTopics.LastMessageID, ForumTopics.RepliesCount
					FROM ForumTopics
					INNER JOIN ForumMessages ON ForumTopics.LastMessageID=ForumMessages.MessageID
					WHERE ForumTopics.Visible=?
					AND ForumTopics.ForumID IN (" + strSQLAllowedForums + @")
					AND ForumMessages.UserID<>?
					" + strTopicConditon + @"
					ORDER BY ForumTopics.LastMessageID DESC", true, User.CurrentUserID, User.GetCurrentUserPreviousLoginDate());

				DataTable dt = new DataTable();
				dt.Load(dr);
				dr.Close();

				//reset the cache
				HttpContext.Current.Session["ForumUpdatedThreadsCount"] = dt.Rows.Count;

				return dt;
			}
		}

		//topic is "UNREAD" if it's found in the disctionary AND has new messages, OR it's not found AND it's newer than "previous login date"
		public static bool IsTopicUnread(int topicId, int lastTopicMessageId, DateTime? lastMessageDate)
		{
			var dict = GetTrackingDictionary();
			if (dict.ContainsKey(topicId))
				return dict[topicId] < lastTopicMessageId;
			else
				return lastMessageDate.HasValue && lastMessageDate.Value > User.GetCurrentUserPreviousLoginDate();
		}
	}
}