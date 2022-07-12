using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Common;
using System.Data;

namespace aspnetforum.Utils
{
	public class Achievements
	{
		/// <summary>
		/// Tests all available achievements
		/// </summary>
		/// <param name="UserID"></param>
		public static void TestAllAchievments(int UserID)
		{
			TestCollectionOffAchievements(UserID, GetCollectionOfAllAchievements());
		}

		/// <summary>
		/// Tests passed set of achievements
		/// </summary>
		/// <param name="UserID"></param>
		/// <param name="achievemntsIds">Array of needed achievements IDs</param>
		public static void TestAchievements(int UserID, params AchievementType[] achievemntsIds)
		{
			List<Achievement> achievements = GetCollectionOfAllAchievements();
			achievements = achievements.Where(x => achievemntsIds.Contains(x.Id)).ToList<Achievement>();
			TestCollectionOffAchievements(UserID, achievements);
		}

		private static void TestCollectionOffAchievements(int UserID, List<Achievement> all)
		{
			foreach (Achievement a in all)
			{
				if(!UserAlreadyHasThisAchiviement(UserID, a.Id))
				{
					if (a.Test(UserID))
						AddSuccess(a.Id, a.Name, UserID);
				}
			}
		}

		public static List<Achievement> GetAchievementsForUser(int UserID)
		{
			DataTable dt;
			using (var cn = DB.CreateOpenConnection())
			{
				DbDataReader dr = cn.ExecuteReader("SELECT * FROM ForumAchievements WHERE UserID = ? ", UserID);
				dt = new DataTable();
				dt.Load(dr);
				dr.Close();
			}

			List<Achievement> all = GetCollectionOfAllAchievements();
			foreach(var a in all)
			{
				if(dt.Select("AchievementID = " + ((int)a.Id)).Length > 0)
				{
					a.Achieved = true;
				}
			}

			return all;

		}

		private static bool UserAlreadyHasThisAchiviement(int UserID, AchievementType achievementId)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				using (var dr = cn.ExecuteReader("SELECT * FROM ForumAchievements WHERE AchievementID = ? AND UserID = ?", achievementId, UserID))
				{
					return dr.HasRows;
				}
			}
		}

		public static void AddSuccess(AchievementType AchievementId, int UserID)
		{
			if (!UserAlreadyHasThisAchiviement(UserID, AchievementId))
			{
				List<Achievement> all = GetCollectionOfAllAchievements();
				string achievementName = all.Where<Achievement>(a => a.Id == AchievementId).FirstOrDefault().Name;
				AddSuccess(AchievementId, achievementName, UserID);
			}
		}
		//Records to the DB and adds to Session
		private static void AddSuccess(AchievementType AchievementId, string AchievementName, int UserID)
		{
			List<string> ach = (List<string>)HttpContext.Current.Session["achievements"];
			if (ach == null)
				ach = new List<string>();

			using (var cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery(@"INSERT INTO ForumAchievements (AchievementID, UserID, DateCreated, TimesAchieved) VALUES(?, ?, ?, ?)", AchievementId, UserID, DateTime.Now, 1);
			}

			ach.Add(AchievementName);

			HttpContext.Current.Session["achievements"] = ach;
		}

		/// <summary>
		/// Registers JS popup, called only from Page_Load
		/// </summary>
		/// <param name="page"></param>
		public static void RegisterNewAchievements(System.Web.UI.Page page)
		{
			List<string> ach = (List<string>)HttpContext.Current.Session["achievements"];
			if (ach != null)
			{
				string script = "<script>";
				foreach (string a in ach)
				{
					script += "$.jGrowl('" + a + "', {header: 'Achievement unlocked!', life: 4000});";
				}
				page.ClientScript.RegisterStartupScript(typeof(System.Web.UI.Page), "ach", script + "</script>");
				ach.Clear();
			}
		}

		/// <summary>
		/// Set all your achievements here. 
		/// </summary>
		/// <returns></returns>
		private static List<Achievement> GetCollectionOfAllAchievements()
		{
			//making collection of all achievements
			List<Achievement> all = new List<Achievement>();

			Achievement welcome_abroad = new Achievement();
			welcome_abroad.Id = AchievementType.WelcomeAboard;
			welcome_abroad.Name = "Welcome Aboard";
			welcome_abroad.Description = "Write at least one message";
			welcome_abroad.testMethod = WelcomeAbroad;
			all.Add(welcome_abroad);

			Achievement topic_starter = new Achievement();
			topic_starter.Id = AchievementType.TopicStarter;
			topic_starter.Name = "Topic Starter";
			topic_starter.Description = "Start at least one topic";
			topic_starter.testMethod = TopicStarter;
			all.Add(topic_starter);

			//Achievement somebody_likes_you = new Achievement();
			//somebody_likes_you.Id = (int)AchievementType.SomebodyLikesYou;
			//somebody_likes_you.Name = "Somebody likes you";
			//somebody_likes_you.Description = "Write a comment which gets upvoted";
			//somebody_likes_you.testMethod = SomebodyLikesYou;
			//all.Add(somebody_likes_you);

			//Achievement brown_nose = new Achievement();
			//brown_nose.Id = (int)AchievementType.BrownNose;
			//brown_nose.Name = "Brown Nose";
			//brown_nose.Description = "Upvote a message by forum administrator or moderator";
			//brown_nose.testMethod = BrownNose;
			//all.Add(brown_nose);

			Achievement selfish = new Achievement();
			selfish.Id = AchievementType.Selfish;
			selfish.Name = "Selfish";
			selfish.Description = "Tried to upvote your own post";
			selfish.testMethod = Selfish;
			all.Add(selfish);

			Achievement something_to_say = new Achievement();
			something_to_say.Id = AchievementType.SomethingToSay;
			something_to_say.Name = "I've got something to say";
			something_to_say.Description = "Write 100 messages";
			something_to_say.testMethod = SomethingToSay;
			all.Add(something_to_say);

			Achievement free_time = new Achievement();
			free_time.Id = AchievementType.FreeTime;
			free_time.Name = "Lots of free time";
			free_time.Description = "Write 1000 messages";
			free_time.testMethod = FreeTime;
			all.Add(free_time);

			Achievement lonely = new Achievement();
			lonely.Id = AchievementType.Lonely;
			lonely.Name = "Lonely";
			lonely.Description = "Write a message on a Friday night";
			lonely.testMethod = Lonely;
			all.Add(lonely);

			//Achievement popularity = new Achievement();
			//popularity.Id = (int)AchievementType.Popularity;
			//popularity.Name = "Popularity";
			//popularity.Description = "Topic you've created has more than 1000 views";
			//popularity.testMethod = Popularity;
			return all;
		}
		#region Achievements methods
		private static bool WelcomeAbroad(int UserID)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				using (var dr = cn.ExecuteReader("SELECT * FROM ForumMessages WHERE UserID = ?", UserID))
				{
					return dr.HasRows;
				}
			}
		}

		private static bool TopicStarter(int UserID)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				using (var dr = cn.ExecuteReader("SELECT * FROM ForumTopics WHERE UserID = ?", UserID))
				{
					return dr.HasRows;
				}
			}
		}

		private static bool SomebodyLikesYou(int UserID)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				using (var dr = cn.ExecuteReader("SELECT * FROM ForumMessages WHERE UserID = ? AND Rating > 0", UserID))
				{
					return dr.HasRows;
				}
			}
		}

		private static bool BrownNose(int UserID)
		{
			return false;
		}

		private static bool Selfish(int UserID)
		{
			return false;
		}

		private static bool SomethingToSay(int UserID)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				int count = Convert.ToInt32(cn.ExecuteScalar("SELECT PostsCount FROM ForumUsers WHERE UserID = ?", UserID));
				return count >= 100;
			}
		}

		private static bool FreeTime(int UserID)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				int count = Convert.ToInt32(cn.ExecuteScalar("SELECT PostsCount FROM ForumUsers WHERE UserID = ?", UserID));
				return (count >= 1000);
			}
		}

		private static bool Lonely(int UserID)
		{
			DateTime dt = DateTime.Now;
			if (dt.DayOfWeek == DayOfWeek.Friday && dt.Hour > 18)
				return true;

			return false;
		}

		private static bool Popularity(int UserID)
		{
			return false;
		}
		#endregion
	}

	public class Achievement
	{
		public AchievementType Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string PathToImg { get; set; }
		public bool Achieved {get; set;} //if current user has this achievement, temporary filed for data binding only
		public Func<int, bool> testMethod { get; set; }

		public bool Test(int UserID)
		{
			return this.testMethod(UserID);
		}
	}

	public enum AchievementType
	{
		WelcomeAboard = 1,
		TopicStarter = 2,
		SomebodyLikesYou = 3,
		BrownNose = 4,
		Selfish = 5,
		SomethingToSay = 6,
		FreeTime = 7,
		Lonely = 8
	}
}