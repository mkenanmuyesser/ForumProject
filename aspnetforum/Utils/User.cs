using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Threading;
using System.Data.Common;
using System.IO;
using System.Web.SessionState;
using System.Web.Security;
using System.Data;
using System.Linq;
using Jitbit.Utils;

namespace aspnetforum.Utils
{
	public static class User
	{
		private static bool _dontQueryMembershipForUserInfo = false;

		public static string DisplayUserInfo(object uID, object uname, object postsCount, object avatarFileName)
		{
			return DisplayUserInfo(uID, uname, postsCount, avatarFileName, null, null, null, null, null);
		}
		public static string DisplayUserInfo(object uID, object uname, object postsCount, object avatarFileName, object currentForumId)
		{
			return DisplayUserInfo(uID, uname, postsCount, avatarFileName, null, null, null, null, null);
		}
		public static string DisplayUserInfo(object uID, object uname, object postsCount, object avatarFileName, object firstName, object lastName)
		{
			return DisplayUserInfo(uID, uname, postsCount, avatarFileName, null, firstName, lastName, null, null);
		}
		public static string DisplayUserInfo(object uID, object uname, object postsCount, object avatarFileName, object currentForumId, object firstName, object lastName)
		{
			return DisplayUserInfo(uID, uname, postsCount, avatarFileName, null, firstName, lastName, null, null);
		}
		/// <summary>
		/// this method returns HTML with short user info (shown on the left of the MESSAGE-list) 
		/// username, posts count, link to a profile etc)
		/// if uid=0 then it displays GUEST as username (required for FFA forums)
		/// </summary>
		public static string DisplayUserInfo(object uID, object uname, object postsCount, object avatarFileName, object currentForumId, object firstName, object lastName, object useGravatar, object email)
		{
			StringBuilder output = new StringBuilder();

			string unameStr = GetUserDisplayName(uname, firstName, lastName);

			bool bAvatarsEnabled = Settings.EnableAvatars;
			output.Append("<span class='userinfo'>");
			if (bAvatarsEnabled)
			{
				output.Append("<div><a href=\"viewprofile.aspx?UserID=");
				output.Append(uID);
				output.Append("\">");
				output.Append("<img class=\"avatar\" src=\"");
				output.Append(GetAvatarFileName(avatarFileName, useGravatar, email));
				output.Append("\" onerror=\"this.src='images/guestavatar.gif'\" alt=\"");
				output.Append(unameStr);
				output.Append("\"/></a></div>");
			}

			if (Convert.ToInt32(uID) != 0)
			{
				output.Append("<strong><a href=\"viewprofile.aspx?UserID=");
				output.Append(uID);
				output.Append("\">");
				output.Append(unameStr);
				output.Append("</a></strong>");

				bool isAdmin = IsAdministrator(Convert.ToInt32(uID));
				if (isAdmin)
				{
					output.Append("<div class='mobilehidden' class=\"gray\">Administrator</div>");
				}

				if (currentForumId != null && !isAdmin && IsModerator(Convert.ToInt32(currentForumId), Convert.ToInt32(uID)))
				{
					output.Append("<div class='mobilehidden' class=\"gray\">Moderator</div>");
				}

				if (postsCount != null)
				{
					output.Append("<div class='mobilehidden'><span  class=\"gray2\">Posts:</span> ");
					output.Append("<span>" + postsCount + "</span></div>");
				}
			}
			else
			{
				output.Append("<strong>"); output.Append(Resources.various.Guest); output.Append("</strong>");
			}
			output.Append("</span><div style='clear:both' class='mobileshown'></div>");
			return output.ToString();
		}
		
		/// <summary>
		/// gets a user's fullname OR username (depends on the web.config setting)
		/// </summary>
		public static string GetUserDisplayName(object username, object firstName, object lastName)
		{
			string unameStr;

			if (Settings.ShowFullNamesInsteadOfUsernames &&
				((firstName != null && firstName.ToString() != "") || (lastName != null && lastName.ToString() != "")))
				unameStr = firstName + " " + lastName;
			else
			{
				unameStr = username.ToString();
				//remove domain from username (in case its windows auth)
				unameStr = unameStr.Substring(unameStr.IndexOf("\\") + 1);
			}

			return unameStr;
		}

		public static string GetCurrUserAvatarImagePath(DbConnection cn)
		{
			//Cn.Open();
			var session = HttpContext.Current.Session;
			string retVal = "images/guestavatar.gif";
			
			if (session["AvatarPath"] != null)
				retVal = session["AvatarPath"].ToString();
			else
			{
				int userID = User.CurrentUserID;
				DbDataReader dr = cn.ExecuteReader("SELECT AvatarFileName, UseGravatar, Email FROM ForumUsers WHERE UserID=" + userID);
				if (dr.Read())
				{
					retVal = Utils.User.GetAvatarFileName(dr["AvatarFileName"], dr["UseGravatar"], dr["Email"]);
					session["AvatarPath"] = retVal;
				}
				dr.Close();
			}

			return retVal;
			//Cn.Close();
		}

		/// <summary>
		/// returns a relative path to the avatar file
		/// </summary>
		public static string GetAvatarFileName(object avatarFileName) { return GetAvatarFileName(avatarFileName, null, null); }
		public static string GetAvatarFileName(object avatarFileName, object useGravatar, object email)
		{
			if (useGravatar != null && !(useGravatar is DBNull) && Convert.ToBoolean(useGravatar) && email != null && Settings.EnableGravatar)
			{
				return GetGravatarUrl(email.ToString());
			}
			else
			{
				if (avatarFileName == null || avatarFileName.ToString() == "")
				{
					return "images/guestavatar.gif";
				}
				else
				{
					//if default avatar
					if (avatarFileName.ToString().StartsWith("AspNetForumAvatar"))
					{
						return "images/" + avatarFileName;
					}

					if (avatarFileName.ToString().StartsWith("http://") || avatarFileName.ToString().StartsWith("https://"))
					{
						return avatarFileName.ToString();
					}

					return "getavatar.ashx?avatar=" + HttpUtility.UrlEncode(avatarFileName.ToString());
				}
			}
		}

		public static string GetGravatarUrl(string email)
		{
			string emailHash = Password.CalculateMD5Hash(email.ToLower().Trim()).ToLower();
			string defaultGravatarImg;
			return "https://www.gravatar.com/avatar/" + emailHash + "?d=mm&s=64";
		}

		//this list "caches" administrators list, to prevent going to the database every time
		private static List<int> _adminsList = null;
		private static List<int> AdminsList
		{
			get
			{
				InitAdminList();
				return _adminsList;
			}
		}

		public static void SendPM(int toUserID, string message)
		{
			if (string.IsNullOrWhiteSpace(message)) return;
			string msg = message.Trim();
			msg = msg.Replace("<", "&lt;").Replace(">", "&gt;");

			using (var cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery(@"INSERT INTO ForumPersonalMessages (FromUserID, ToUserID, Body, CreationDate) VALUES (?, ?, ?, ?)",
					CurrentUserID, toUserID, msg, Various.GetCurrTime());

				//get last message's ID
				object res = cn.ExecuteScalar("SELECT MAX(MessageID) FROM ForumPersonalMessages WHERE FromUserID=" + CurrentUserID + " AND ToUserID=" + toUserID);
				int messageId = (res == null || res == DBNull.Value) ? 0 : Convert.ToInt32(res);

				//save attachments
				Attachments.SaveAttachments(messageId, true, cn);
			}

			//sending notifications
			if (Settings.MailNotificationsEnabled)
			{
				string url = Various.ForumURL + "privateinbox.aspx";
				SendNotifications.SendPersonalNotificationEmails(toUserID, url, msg);
			}
		}

		public static bool IsAdministrator(int userId)
		{
			return AdminsList.Contains(userId);
		}

		//is moderator ANYWHERE
		public static bool IsCurrentUserModerator()
		{
			int currUserID = CurrentUserID;
			if (currUserID == 0) return false;

			if (IsAdministrator(currUserID)) return true;

			if (_moderatorsList == null)
				CreateModeratorsList();
			
			foreach (var modersList in _moderatorsList.Values)
			{
				if (modersList.Contains(currUserID)) return true;
			}
			return false;
		}

		private static object _adminListLock = new object();
		private static void InitAdminList()
		{
			lock (_adminListLock)
			{
				if (_adminsList == null) //filling the admins list
				{
					_adminsList = new List<int>();
					using (var cn = DB.CreateOpenConnection())
					{
						DbDataReader dr = cn.ExecuteReader("SELECT UserID FROM ForumAdministrators");
						while (dr.Read())
						{
							_adminsList.Add(Convert.ToInt32(dr["UserID"]));
						}
						dr.Close();
					}
				}
			}
		}

		//this list "caches" moderators list, to prevent going to the database every time we need it
		//the structure is: "forumid - moderators list"
		private static Dictionary<int, List<int>> _moderatorsList = null;
		public static bool IsModerator(int forumId, int userId)
		{
			if (_moderatorsList == null)
				CreateModeratorsList();

			if (IsAdministrator(userId)) return true;

			if(_moderatorsList.ContainsKey(forumId))
				return _moderatorsList[forumId].Contains(userId);

			return false;
		}

		private static object _moderatorListLock = new object();
		private static void CreateModeratorsList()
		{
			//this method can be called from multiple threads
			lock (_moderatorListLock)
			{
				if (_moderatorsList != null) return; //another thread has already been here

				_moderatorsList = new Dictionary<int, List<int>>();
				int tmpForumId = -1;

				using (var cn = DB.CreateOpenConnection())
				{
					DbDataReader dr = cn.ExecuteReader("SELECT UserID, ForumID FROM ForumModerators ORDER BY ForumID");
					while (dr.Read())
					{
						if (tmpForumId != Convert.ToInt32(dr["ForumID"]))
						{
							tmpForumId = Convert.ToInt32(dr["ForumID"]);
							List<int> forumModerators = new List<int>();

							_moderatorsList.Add(tmpForumId, forumModerators);
						}

						_moderatorsList[tmpForumId].Add(Convert.ToInt32(dr["UserID"]));
					}
				}
			}
		}

		public static void AddForumModerator(int forumId, int userId)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("DELETE FROM ForumModerators WHERE UserID=? and ForumID=?", userId, forumId);
				cn.ExecuteNonQuery("INSERT INTO ForumModerators (UserID, ForumID) VALUES (?, ?)", userId, forumId);
				_moderatorsList = null; //to reset the "caching" list
			}
		}

		public static void MakeAdmin(int userId)
		{
			if (IsAdministrator(userId)) return; //already admin

			using (var cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("INSERT INTO ForumAdministrators (UserID) VALUES (?)", userId);
			}

			if(!AdminsList.Contains(userId))
				AdminsList.Add(userId);
		}

		public static void RevokeAdmin(int userId)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("DELETE FROM ForumAdministrators WHERE UserID = ?", userId);
			}

			InitAdminList();
			if (AdminsList.Contains(userId))
				AdminsList.Remove(userId);
		}

		public static DateTime GetCurrentUserPreviousLoginDate()
		{
			var logondate = HttpContext.Current.Session["PreviousLogonDate"] as DateTime?;
			if (logondate.HasValue)
				return logondate.Value;

			//get logon date from DB and cache it in session
			using (var cn = DB.CreateOpenConnection())
			{
				logondate = GetLastLogonDateFromDB(CurrentUserID, cn);
			}

			HttpContext.Current.Session["PreviousLogonDate"] = logondate;
			return logondate.Value;
		}

		private static DateTime GetLastLogonDateFromDB(int userId, DbConnection openConnection)
		{
			var logondate = openConnection.ExecuteScalar("SELECT LastLogonDate FROM ForumUsers WHERE UserID=?", CurrentUserID) as DateTime?;
			//never logged on yet - return "10 years ago"
			if (logondate == null)
				logondate = DateTime.Now.AddYears(-10);
			return logondate.Value;
		}

		public static void UpdateCurrentUserLastLogonDate()
		{
			//lets save performance and see if was already saved in the last 2 mins
			var lastLogonDate = HttpContext.Current.Session["LastLogonDate"] as DateTime?;
			if (lastLogonDate.HasValue && DateTime.Now.Subtract(lastLogonDate.Value).TotalMinutes < 3) //was saved less than 2 minutes ago
				return;

			using (var cn = DB.CreateOpenConnection())
			{
				//calculate "previousLogonDate" if it's not been set yet
				if (HttpContext.Current.Session["PreviousLogonDate"] == null) //new session
				{
					var previousLogonDate = GetLastLogonDateFromDB(CurrentUserID, cn);
					HttpContext.Current.Session["PreviousLogonDate"] = previousLogonDate;
				}

				cn.ExecuteNonQuery("UPDATE ForumUsers SET LastLogonDate=? WHERE UserID=?", DateTime.Now, CurrentUserID);
				HttpContext.Current.Session["LastLogonDate"] = DateTime.Now;
			}
		}

		//this property returns the current user's ID
		public static int CurrentUserID
		{
			get
			{
				//if user logged out by asp.net auth - let's log him out here.
				if (Settings.IntegratedAuthentication && !HttpContext.Current.User.Identity.IsAuthenticated)
				{
					Logout();
					return 0;
				}
				else
				{
					HttpContext context = HttpContext.Current;
					HttpSessionState session = context.Session;
					if (session["aspnetforumUserID"] == null)
						return 0;
					else
						return Convert.ToInt32(session["aspnetforumUserID"]);
				}
			}
			set { HttpContext.Current.Session["aspnetforumUserID"] = value; }
		}

		public static void Logout()
		{
			HttpContext context = HttpContext.Current;
			if (context == null) return;
			if (context.Session == null) return;

			context.Session["aspnetforumUserID"] = null;
			context.Session["aspnetforumUserName"] = null;
			context.Session["AvatarPath"] = null;

			//clear "remember me" cookies
			if (context.Response != null)
			{
				HttpCookie cookieUID = new HttpCookie("aspnetforumUID", "");
				cookieUID.Expires = DateTime.Now.AddYears(-1); //expires = negative value
				HttpCookie cookiePSW = new HttpCookie("aspnetforumPSW", "");
				cookiePSW.Expires = DateTime.Now.AddYears(-1); //expires = negative value
				context.Response.Cookies.Add(cookieUID);
				context.Response.Cookies.Add(cookiePSW);
			}
		}

		/// <summary>
		/// logs in a user - stores his id in the session, counts users etc.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="username"></param>
		public static void Login(int userId, string username)
		{
			//_onlineRegisteredUsersCount++;
			HttpContext context = HttpContext.Current;
			context.Session["aspnetforumUserID"] = userId;
			context.Session["aspnetforumUserName"] = username;
		}

		private static bool IsTooManyInvalidLogins(HttpContext context)
		{
			HttpSessionState session = context.Session;

			//too many invalid logins???
			if (session["InvalidLoginsCount"] != null
				&& (int)session["InvalidLoginsCount"] > 5)
			{
				//yes it is!
				//lets check how much time passed since the last login attempt?

				if (session["LastInvalidLoginTime"] == null)
					session["LastInvalidLoginTime"] = DateTime.Now;

				if (DateTime.Now.Subtract((DateTime)session["LastInvalidLoginTime"]) < TimeSpan.FromMinutes(5))
				{
					context.Response.Write("5 invalid login attempts were made. Please wait 5 minutes and try again");
					context.Response.End();
					return true;
				}
				else
				{
					session["InvalidLoginsCount"] = 0;
					session["LastInvalidLoginTime"] = null;
					return false;
				}
			}
			else
				return false;
		}

		//log an invalid login attempt
		private static void LogInvaligLoginAttempt(HttpContext context)
		{
			if (context.Session["InvalidLoginsCount"] == null)
				context.Session["InvalidLoginsCount"] = 1;
			else
				context.Session["InvalidLoginsCount"] = ((int)context.Session["InvalidLoginsCount"]) + 1;
		}

		public static void ProcessLogin(string username, string password, out bool passwordOk, out bool awaitsEmailActivation, out int userId)
		{
			passwordOk = awaitsEmailActivation = false;
			userId = 0;

			if (password == "") return;

			HttpContext context = HttpContext.Current;
			if(IsTooManyInvalidLogins(context)) return;

			string hashedPsw = Utils.Password.CalculateHash(password);

			IEnumerable<UserInfo> res;
			using(DbConnection cn = DB.CreateOpenConnection())
			{
				res = cn.ExecuteOrm<UserInfo>(
					"SELECT UserID, UserName, OpenIdUserName, Disabled, ActivationCode FROM ForumUsers WHERE UserName=? AND (Password=? OR Password=?)",
					username, password, hashedPsw);

				passwordOk = res.Any() && res.First().OpenIdUserName == ""; //to prevent hacking via "none" password and openid username

				if (passwordOk)
				{
					userId = Convert.ToInt32(res.First().UserID);
					awaitsEmailActivation = Convert.ToBoolean(res.First().Disabled) && (res.First().ActivationCode != "");
				}
			}

			if (passwordOk)
			{
				if (!awaitsEmailActivation)
				{
					Login(res.First().UserID, res.First().UserName);

					// adding "remember me" cookies even there's no "remember me" (we make it non-persisstent)
					HttpCookie cookieUID = new HttpCookie("aspnetforumUID", CurrentUserID.ToString()) {Path = "/"};
					HttpCookie cookiePSW = new HttpCookie("aspnetforumPSW", hashedPsw) {Path = "/"};
					if (context.Request.Form["rememberme"] == "1")
					{
						cookieUID.Expires = DateTime.Now.AddMonths(1);
						cookiePSW.Expires = DateTime.Now.AddMonths(1);
					}
					context.Response.Cookies.Add(cookieUID);
					context.Response.Cookies.Add(cookiePSW);
				}
			}
			else //password not ok log an invalid attempt
			{
				LogInvaligLoginAttempt(context);
			}
		}

		public static void ProcessMembershipLogin(string username)
		{
			HttpContext context = HttpContext.Current;

			//username = username.Replace("'", "''");
			string email = "none";
			string firstName = "", lastName = "", phone = "", office = "", lang = "", company = ""; byte[] jpegPhoto = null;

			//IF it's a windows user - lets try to get email from AD
			if (context.User is System.Security.Principal.WindowsPrincipal)
			{
				ADUtils.GetUserPropertiesFromAD(username, out email, out firstName, out lastName, out phone, out office, out lang, out company, out jpegPhoto);
				if (string.IsNullOrEmpty(email)) email = "none"; //because the db does not allow empty emails
			}
			else
			{
				if (Settings.IntegratedMembership && !_dontQueryMembershipForUserInfo)
				{
					//lets try to get the user's email, first/last name from asp.net membership
					try
					{
						MembershipUser u = Membership.GetUser(username);
						if (u != null)
						{
							email = u.Email;
							if (string.IsNullOrEmpty(email)) email = "none"; //because the db does not allow empty emails
						}
						else
							_dontQueryMembershipForUserInfo = true; //dont call it again to save performncae
					}
					catch
					{
						//dont call it again to save performncae
						_dontQueryMembershipForUserInfo = true;
					}
				}


				//lets try to get the user's email, first/last name from asp.net profile/session
				try
				{
					if (HttpContext.Current.Session["FirstName"] != null)
					{
						email = HttpContext.Current.Session["Email"].ToString();
						firstName = HttpContext.Current.Session["FirstName"].ToString();
						lastName = HttpContext.Current.Session["LastName"].ToString();
					}
					else
					{
						System.Web.Profile.ProfileBase profile = HttpContext.Current.Profile;
						if (profile != null)
						{
							if (email == "none")
							{
								if (System.Web.Profile.ProfileBase.Properties["Email"] != null)
									email = profile.GetPropertyValue("Email").ToString();
								else if (System.Web.Profile.ProfileBase.Properties["email"] != null)
									email = profile.GetPropertyValue("email").ToString();
								else if (System.Web.Profile.ProfileBase.Properties["mail"] != null)
									email = profile.GetPropertyValue("mail").ToString();
							}

							if (System.Web.Profile.ProfileBase.Properties["FirstName"] != null)
								firstName = profile.GetPropertyValue("FirstName").ToString();
							if (System.Web.Profile.ProfileBase.Properties["LastName"] != null)
								lastName = profile.GetPropertyValue("LastName").ToString();
						}
					}
				}
				catch { }
			}

			//lets try to find the user in db
			int userid;
			using (var cn = DB.CreateOpenConnection())
			{
				DbDataReader dr = cn.ExecuteReader("SELECT UserID, UserName, Disabled FROM ForumUsers WHERE UserName=?", username);
				if (dr.Read()) //user IS found in the db (already exists)
				{
					userid = Convert.ToInt32(dr[0]);
					if (!Convert.ToBoolean(dr["Disabled"])) //if not disabled
					{
						Login(userid, dr[1].ToString());
					}
					dr.Close();

					//let's update his email, if it has been changed by asp.net membership
					if (email != "none" && email != "")
					{
						cn.ExecuteNonQuery("UPDATE ForumUsers SET Email=? WHERE UserID=?", email, userid);
					}
				}
				else //user DOES NOT exist YET - let's add him to our ForumUsers table
				{
					dr.Close();

					userid = CreateUser(username, email, "none", string.Empty, string.Empty, false, string.Empty, firstName, lastName, "", "", "");
					Login(userid, username);
				}
			}

			//try to see if this user is admin
			SyncIntegratedAdmin(userid);
		}

		public static int CreateUser(string userName, string email, string password, string homepage, string interests, bool disabled,
			string activationCode = "", string firstName = "", string lastName = "", string openIdUsername = "", string twitterUserName = "", string facebookId = "")
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery(@"INSERT INTO ForumUsers
					(UserName, Email, [Password], Homepage, Interests, RegistrationDate, Disabled, ActivationCode, FirstName, LastName, OpenIdUserName, UseGravatar, TwitterUserName, FacebookID)
					VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
					userName.Left(50), email.Left(50), password, homepage.Left(255), interests.Left(255), Various.GetCurrTime(), disabled,
					activationCode, firstName.Left(100), lastName.Left(100), openIdUsername, true, twitterUserName, facebookId);

				int userId = Convert.ToInt32(cn.ExecuteScalar("SELECT UserID FROM ForumUsers WHERE UserName=?", userName.Left(50)));

				return userId;
			}
		}

		//the method sends activation email to a newly registered user
		public static void SendActivationEmail(int userId)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				var res = cn.ExecuteOrm<UserInfo>(
					"SELECT UserName, Email, ActivationCode FROM ForumUsers WHERE UserID=? AND Disabled=?",
					userId, true);
				if (!res.Any()) return;

				string username = res.First().UserName;
				string code = res.First().ActivationCode;
				string email = res.First().Email;

				if (string.IsNullOrWhiteSpace(code)) return; //do not send anything if there's no code in DB

				string url = Various.ForumURL + "activate.aspx?user=" + HttpUtility.UrlEncode(username) + "&code=" + code;

				string body = Resources.various.ActivationEmailBody + "\r\n\r\n" + url;
				string subject = Settings.ForumTitle + " - " + Resources.various.ActivationEmailSubject;

				string[] recipients = new string[1];
				recipients[0] = email;

				SendNotifications.SendEmail(recipients, subject, body);
			}
		}

		public static void SetAvatarUrl(int userId, string avatarUrl, bool useGravatar = false)
		{
			//delete previous avatar if exists
			DeletePreviousAvatar(userId);

			//save avatar
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("UPDATE ForumUsers SET UseGravatar=?, AvatarFileName=? WHERE UserID=?", useGravatar, avatarUrl, userId);
			}

			if (HttpContext.Current != null && HttpContext.Current.Session != null)
			{
				HttpContext.Current.Session["AvatarPath"] = null; //reset cache
			}
		}

		private static void DeletePreviousAvatar(int userId)
		{
			//delete previous avatar if exists

			string uploadDir = Utils.Attachments.GetAvatarsDirAbsolutePath();

			using (var cn = DB.CreateOpenConnection())
			{
				var res = cn.ExecuteScalar("SELECT AvatarFileName FROM ForumUsers WHERE UserID=?", userId);
				if (!res.ToString().StartsWith("AspNetForum"))
				{
					if (res.ToString() != "" && File.Exists(uploadDir + res.ToString())) //deleting file
					{
						File.Delete(uploadDir + res.ToString());
					}

					cn.ExecuteNonQuery("UPDATE ForumUsers SET UseGravatar=?, AvatarFileName=NULL WHERE UserID=?", false, userId);
				}
			}
		}

		private static void SyncIntegratedAdmin(int userId)
		{
			//try to see if this user is admin
			try
			{
				string adminRole = Settings.AdminRoleName;
				string adminUserName = Settings.AdminUserName;

				if (!string.IsNullOrEmpty(adminUserName))
				{
					if (adminUserName.ToLower() == GetUserNameById(userId).ToLower())
					{
						MakeAdmin(userId);
						return;
					}
				}

				if (!string.IsNullOrEmpty(adminRole))
				{
					if (HttpContext.Current.User.IsInRole(adminRole))
						MakeAdmin(userId);
					else
						RevokeAdmin(userId);
				}
			}
			catch { }
		}

		public static void GetUserByFacebookId(string facebookId, out int userId, out string userName)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				DbDataReader dr = cn.ExecuteReader("SELECT UserID, UserName FROM ForumUsers WHERE FacebookID=?", facebookId);
				userId = 0;
				userName = null;
				if (dr.Read())
				{
					userId = Convert.ToInt32(dr["UserID"]);
					userName = dr["UserName"].ToString();
				}
				dr.Close();
			}
		}

		public static void GetUserByTwitterNameId(string twitterUserName, out int userId, out string userName)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				DbDataReader dr = cn.ExecuteReader("SELECT UserID, UserName FROM ForumUsers WHERE TwitterUserName=?", twitterUserName);
				userId = 0;
				userName = null;
				if (dr.Read())
				{
					userId = Convert.ToInt32(dr["UserID"]);
					userName = dr["UserName"].ToString();
				}
				dr.Close();
			}
		}

		public static void ProcessCookieLogin()
		{
			HttpContext context = HttpContext.Current;

			if (IsTooManyInvalidLogins(context)) return;

			using (DbConnection cn = DB.CreateOpenConnection())
			{
				DbDataReader dr = cn.ExecuteReader("SELECT UserID, Password, UserName FROM ForumUsers WHERE UserID=" + Convert.ToInt32(context.Request.Cookies["aspnetforumUID"].Value) + " AND Disabled=0");
				if (dr.Read())
				{
					if (dr[1].ToString() == context.Request.Cookies["aspnetforumPSW"].Value
					    || Password.CalculateHash(dr[1].ToString()) == context.Request.Cookies["aspnetforumPSW"].Value)
					{
						Login(Convert.ToInt32(dr[0]), dr[2].ToString());
					}
					else
					{
						Logout(); //to clear the invalid cookies
						LogInvaligLoginAttempt(context);
					}
				}
				dr.Close();
			}
		}

		public static void DisableUser(int userId)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery(
					"update ForumUsers set Disabled=?, ActivationCode='' where UserID=?",
					true, userId);
			}
		}

		public static void EnableUser(int userId, bool sendWelcomeEmail)
		{
			string email = null;
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("update ForumUsers set Disabled=?, ActivationCode='' where UserID=?", false, userId);

				if (sendWelcomeEmail)
				{
					email = cn.ExecuteScalar("select Email from ForumUsers where UserID=?", userId).ToString();
				}
			}

			if (sendWelcomeEmail)
			{
				//send email
				string url = Utils.Various.ForumURL;
				SendNotifications.SendWelcomeEmail(email, url);
			}
		}

		/// <summary>
		/// returns the GroupID for "_ALL_USERS" built-in group
		/// </summary>
		public static int GetAllUsersGroupId()
		{
			//check the cache first
			if (HttpRuntime.Cache["AllUsersGroupId"] != null)
				return (int)HttpRuntime.Cache["AllUsersGroupId"];

			object res = null;

			using (DbConnection cn = DB.CreateOpenConnection())
			{
				res = cn.ExecuteScalar("SELECT GroupID FROM ForumUserGroups WHERE Title='_ALL_USERS'");

				if (res == null)
				{
					cn.ExecuteNonQuery("INSERT INTO ForumUserGroups (Title) VALUES ('_ALL_USERS')");
					res = cn.ExecuteScalar("SELECT GroupID FROM ForumUserGroups WHERE Title='_ALL_USERS'");
				}
			}

			int groupId = Convert.ToInt32(res);

			//save it to cache
			HttpRuntime.Cache.Add("AllUsersGroupId", groupId, null, DateTime.Now.AddMinutes(30),
					System.Web.Caching.Cache.NoSlidingExpiration,
					System.Web.Caching.CacheItemPriority.Normal,
					null);

			return groupId;
		}

		public static IEnumerable<int> GetUserIdsInGroup(int groupId)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				//if it's a built-in "ALLUSERS" group - return all users
				if (groupId == GetAllUsersGroupId())
					return cn.ExecuteOrm<int>("SELECT UserID FROM ForumUsers");
				else
					return cn.ExecuteOrm<int>("SELECT UserID FROM ForumUsersInGroup WHERE GroupID=?", groupId);
			}
		}

		public static IEnumerable<int> GetGroupIdsForUser(int userId)
		{
			if (userId == 0) return Enumerable.Empty<int>();

			using (DbConnection cn = DB.CreateOpenConnection())
			{
				var res = cn.ExecuteOrm<int>("SELECT GroupID FROM ForumUsersInGroup WHERE UserID=?", userId);

				//now adding "all users" group
				var allusers = new[] {GetAllUsersGroupId()};
				res = res.Concat(allusers);

				return res;
			}
		}

		public static void AddUserToGroup(int userId, int groupId)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("INSERT INTO ForumUsersInGroup (UserID, GroupID) VALUES(?, ?)", userId, groupId);
			}
		}

		public static void RemoveUserFromGroup(int userId, int groupId)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("DELETE FROM ForumUsersInGroup WHERE UserID=? AND GroupID=?", userId, groupId);
			}
		}

		public static void DeleteUser(int userId)
		{
			object avatar = null;
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("DELETE FROM ForumAdministrators WHERE UserID=" + userId);
				cn.ExecuteNonQuery("DELETE FROM ForumModerators WHERE UserID=" + userId);
				cn.ExecuteNonQuery("UPDATE ForumMessages SET UserID=0 WHERE UserID=" + userId);
				cn.ExecuteNonQuery("UPDATE ForumTopics SET UserID=0 WHERE UserID=" + userId);
				cn.ExecuteNonQuery("DELETE FROM ForumPersonalMessages WHERE ToUserID=" + userId + " OR FromUserID=" + userId);
				cn.ExecuteNonQuery("DELETE FROM ForumSubscriptions WHERE UserID=" + userId);
				cn.ExecuteNonQuery("DELETE FROM ForumNewTopicSubscriptions WHERE UserID=" + userId);
				cn.ExecuteNonQuery("DELETE FROM ForumUsersInGroup WHERE UserID=" + userId);
				cn.ExecuteNonQuery("DELETE FROM ForumUploadedFiles WHERE UserID=" + userId);
				
				var avatarData = cn.ExecuteOrm<string>("SELECT AvatarFileName FROM ForumUsers WHERE UserID=" + userId);
				avatar = avatarData.Any() ? avatarData.First() : null;
				
				cn.ExecuteNonQuery("DELETE FROM ForumUsers WHERE UserID=" + userId);
			}

			//delete user's upload folder & user's avatar
			HttpRequest request = HttpContext.Current.Request;
			string uploaddir = Attachments.GetUploadDirAbsolutePath() + userId;
			if (Directory.Exists(uploaddir)) Directory.Delete(uploaddir, true);
			uploaddir = Attachments.GetUploadDirAbsolutePathOLDVersion() + userId;
			if (Directory.Exists(uploaddir)) Directory.Delete(uploaddir, true);

			if (avatar != null)
			{
				string avatarFileName = avatar.ToString();
				if (avatarFileName != "" && avatarFileName.IndexOf("AspNetForumAvatar") < 0) //not a default avatar
				{
					avatarFileName = Attachments.GetAvatarsDirAbsolutePath() + "\\" + avatarFileName;
					if (File.Exists(avatarFileName)) File.Delete(avatarFileName);
				}
			}
		}

		public static int GetUserIdByUserName(string username)
		{
			if (string.IsNullOrEmpty(username)) return 0;

			using (DbConnection cn = Utils.DB.CreateOpenConnection())
			{
				object res = cn.ExecuteScalar("SELECT UserID FROM ForumUsers WHERE UserName=?", username);
				int userId = (res == null) ? 0 : Convert.ToInt32(res);
				return userId;
			}
		}

		public static string GetUserNameById(int userId)
		{
			using (DbConnection cn = Utils.DB.CreateOpenConnection())
			{
				return cn.ExecuteScalar("SELECT UserName FROM ForumUsers WHERE UserID=?", userId) as string;
			}
		}

		public static int GetUserIdByEmail(string email)
		{
			using (DbConnection cn = Utils.DB.CreateOpenConnection())
			{
				object res = cn.ExecuteScalar("SELECT UserID FROM ForumUsers WHERE Email=?", email);
				int userId = (res == null) ? 0 : Convert.ToInt32(res);
				return userId;
			}
		}

		public static string GetUserEmail(int userId)
		{
			using (DbConnection cn = Utils.DB.CreateOpenConnection())
			{
				return cn.ExecuteScalar("SELECT Email FROM ForumUsers WHERE UserID=?", userId) as string;
			}
		}

		public static void DeleteAllPosts(int userId)
		{
			using (DbConnection cn = Utils.DB.CreateOpenConnection())
			{
				cn.ExecuteNonQuery("DELETE FROM ForumUploadedFiles WHERE UserID=" + userId);
				cn.ExecuteNonQuery("DELETE FROM ForumMessages WHERE UserID=" + userId);
				cn.ExecuteNonQuery("DELETE FROM ForumTopics WHERE UserID=" + userId);
				cn.ExecuteNonQuery("UPDATE ForumUsers SET PostsCount=0 WHERE UserID=" + userId);
			}

			Forum.ClearAllCache();
		}

		#region Online Users Count
		/*online users*/
		private const int CLEANUP_TIMEOUT_MINUTES = 1;
		private static int _onlineRegisteredUsersCount = 0;
		public static int OnlineRegisteredUsersCount { get { return _onlineRegisteredUsersCount; } }
		private static int _onlineUsersCount = 0;
		public static int OnlineUsersCount { get { return _onlineUsersCount; } }
		
		//this class holds session parameters: last active, and whether the session user is a member.
		public class SessionParameters
		{
			public DateTime LastActivity { get; private set; }
			public bool IsMember { get; set; }
			public string UserName { get; private set; }
			public string CurrentURL { get; private set; }
			public SessionParameters(DateTime lastActvivity, bool isMember, string userName, string currentURL)
			{
				LastActivity = lastActvivity;
				IsMember = isMember;
				UserName = userName;
				CurrentURL = currentURL;
			}
		}
		//this collection holds sessionIds and its parameters
		private static Dictionary<string, SessionParameters> _onlineUsersSessions = new Dictionary<string, SessionParameters>();
		public static Dictionary<string, SessionParameters> OnlineUsersSessions { get { return _onlineUsersSessions; } }
		
		private static Timer _timer = null; //timer to clean up once a while
		public static void UpdateOnlineUsersCount() //this method is called from pages' base class
		{
			lock (_onlineUsersSessions)
			{
				HttpContext context = HttpContext.Current;
				if (context == null) return;
				if (context.Session == null) return;

				//init timer (if not already)
				//timer calls back every 5 minutes, and kills inactive users (sessions) from the list
				if (_timer == null)
					_timer = new Timer(new TimerCallback(CleanUpDeadSessionsCallback),
						context,
						CLEANUP_TIMEOUT_MINUTES * 60000,
						CLEANUP_TIMEOUT_MINUTES * 60000);

				//lets just put something into the session,
				//because otherwise the sessionid changes with each new request
				context.Session["ThisSessionIsLogged"] = true;

				SessionParameters sp = new SessionParameters(DateTime.Now, (CurrentUserID != 0), context.Session["aspnetforumUserName"] as string, context.Request.Url.ToString());

				string sessionId = context.Session.SessionID;

				if (_onlineUsersSessions.ContainsKey(sessionId))
				{
					SessionParameters oldsp = _onlineUsersSessions[sessionId];
					if (oldsp.IsMember != sp.IsMember) //the user has chnaged his status (logged out or logged in)
					{
						if (!oldsp.IsMember)
							_onlineRegisteredUsersCount++;
						else
							_onlineRegisteredUsersCount--;
					}
					_onlineUsersSessions[sessionId] = sp;
				}
				else
				{
					_onlineUsersCount++;
					_onlineUsersSessions.Add(sessionId, sp);
					if (sp.IsMember)
						_onlineRegisteredUsersCount++;
				}
			}
		}
		private static void CleanUpDeadSessionsCallback(object sender)
		{
			//kills sessions from the dictionary if its been idle for more than _timeout
			lock (_onlineUsersSessions) //to prevent multi-run
			{
				DateTime now = DateTime.Now;
				List<string> keysToDelete = new List<string>(_onlineUsersSessions.Count);
				foreach (KeyValuePair<string, SessionParameters> pair in _onlineUsersSessions)
				{
					//if its been more that X minutes since the session was active - kill it
					if (now.Subtract(pair.Value.LastActivity).Minutes > CLEANUP_TIMEOUT_MINUTES)
					{
						keysToDelete.Add(pair.Key);
						if(pair.Value.IsMember)
							_onlineRegisteredUsersCount--;
					}
				}
				//actual deleting
				foreach (string key in keysToDelete)
				{
					_onlineUsersSessions.Remove(key);
				}

				_onlineUsersCount = _onlineUsersSessions.Count;
			}
		}
		#endregion
	}

	public class UserInfo
	{
		public int UserID { get; set; }
		public string Email { get; set; }
		public string UserName { get; set; }
		public string OpenIdUserName{get;set;}
		public bool Disabled{get;set;}
		public string ActivationCode { get; set; }
	}
}
