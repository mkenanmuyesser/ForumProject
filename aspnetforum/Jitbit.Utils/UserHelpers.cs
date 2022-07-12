using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Dapper;
using System.Data;
using System.Configuration;

namespace Jitbit.Utils
{
	//user utilities. This class is used accross multiple Jitbit products so it's been moved to a separate namespace

	public static class UserHelpers
	{
		//call this method before using anything!! call it on application start.
		public static void Init(string usersDbTableName, AddWindowsUserDelegate addWinUserFunc)
		{
			_usersDbTableName = usersDbTableName;
			_addWindowsUserFunc = addWinUserFunc;
		}

		//table that holds users in the DB
		private static string _usersDbTableName;

		//"add user" delegate
		public delegate int AddWindowsUserDelegate(string username, string email, string firstName, string lastName, string phone, string location, string lang, int instanceId, string company, string department, byte[] jpegPhoto);
		public static AddWindowsUserDelegate _addWindowsUserFunc;

		/// <summary>
		/// gets/sets the current user's UserID from the Users table
		/// </summary>
		/// <returns>UserID from the Users table</returns>
		public static int CurrentUserID
		{
			get
			{
				HttpContext context = HttpContext.Current;
				if (context == null) return 0;

				if (!context.User.Identity.IsAuthenticated) return 0;

				var cached = context.Items["currentUserID"];
				if (cached != null)
					return (int)cached;

				cached = context.Session["currentUserID"];
				if (cached != null)
				{
					return (int)(context.Items["currentUserID"] = (int)cached);
				}

				//ok, nothing cached in session, nothing in context = lets find the userid!!!!

				int userId;

				//check if instance expired
				if (Instance.IsCurrentInstanceExpired()) return 0;

				string userName = context.User.Identity.Name;
				userId = GetUserIDByUsername(userName, Instance.CurrentInstanceID);

				//it is  windows-authentication - lets add a new user maybe?
				if (ADUtils.IsWindowsAuthentication())
				{
					//win-user NOT found in the DB!
					if (userId == 0)
					{
						//try to get the win-user's parameters from Active Directory
						string email = "", firstName = "", lastName = "", phone = "", location = "", language = "", company = "", department = ""; byte[] jpegPhoto = null;
						ADUtils.GetUserPropertiesFromAD(userName, out email, out firstName, out lastName, out phone, out location, out language, out company, out jpegPhoto, out department);

						//now lets add the win-user to the DB
						if(_addWindowsUserFunc!=null)
							userId = _addWindowsUserFunc(userName, email, firstName, lastName, phone, location, "", Instance.CurrentInstanceID, company, department, jpegPhoto);

					}
					else if (!IsEnabled(userId)) //win-user IS found in the DB, but he's disabled
					{
						ExceptionHandler.RenderErrorPage("Access denied, disabled user.");
						return 0;
					}
				}

				context.Session["currentUserID"] = context.Items["currentUserID"] = (userId == 0 ? null : (object)userId);
				ResetCurrentUserInfoCache();


				return userId;

			}
			set
			{
				if (value == 0)
				{
					HttpContext.Current.Items["currentUserID"] = null;
					HttpContext.Current.Session["currentUserID"] = null;
					HttpContext.Current.Items["IsCurrentUserAdmin"] = null;
				}
				else
				{
					HttpContext.Current.Items["currentUserID"] = value;
					HttpContext.Current.Session["currentUserID"] = value;
				}
				ResetCurrentUserInfoCache();
			}
		}

		public static void UpdatePassword(int userID, string newPassword)
		{
			using (SqlConnection cn = DBUtils.GetNewOpenConnection())
			{
				cn.Execute(
					"UPDATE " + _usersDbTableName + " SET Password=@Password WHERE UserID=@UserID",
					new { Password = CryptoUtils.MD5Hash(newPassword), UserID = userID });
			}
		}

		public static bool GetUserIdAndPswByUsername(string username, int instanceId, out int userId, out string password)
		{
			userId = 0;
			password = null;
			using (var cn = DBUtils.GetNewOpenConnection())
			{
				var res = cn.Query("SELECT UserID, Password FROM " + _usersDbTableName + " WHERE UserName=@UserName and Disabled=0 AND InstanceID=@InstanceID",
					new { InstanceID = instanceId, UserName = username });
				if (!res.Any()) return false;

				userId = Convert.ToInt32(res.First().UserID);
				password = res.First().Password;
				return true;
			}
		}

		/// <summary>
		/// Does userID has Administrator priveleges
		/// </summary>
		/// <returns>true if the user has admin priveleges</returns>
		public static bool IsAdmin(int userID)
		{
			if (userID == 0) return false;

			return IsAdmin(userID, DBUtils.GetNewConnection());
		}
		public static bool IsAdmin(int userID, SqlConnection cn)
		{
			if (userID == 0) return false;

			bool openConnection = (cn.State == ConnectionState.Open);
			if (!openConnection) cn.Open();
			
			var res = cn.Query("SELECT UserName, IsAdministrator FROM " + _usersDbTableName + " WHERE UserID=" + userID);
			bool isAdmin = (res.Any()) ? (bool)res.First().IsAdministrator : false;

			//adding windows-admin
			if (res.Any() && !isAdmin && ADUtils.IsWindowsAuthentication()
				&& ConfigurationManager.AppSettings["WindowsAdminUsername"] != null && (string)res.First().UserName == ConfigurationManager.AppSettings["WindowsAdminUsername"])
			{
				isAdmin = true;
				cn.Execute("UPDATE " + _usersDbTableName + " SET IsAdministrator=@isAdmin WHERE UserID=@userId", new { isAdmin, userID });
			}

			if (!openConnection) cn.Close();

			return isAdmin;
		}

		//current user is admin (caches this info in session AND context)
		public static bool IsCurrentUserAdmin()
		{
			if (CurrentUserID == 0)
			{
				HttpContext.Current.Session.Remove("IsCurrentUserAdmin");
				HttpContext.Current.Items.Remove("IsCurrentUserAdmin");
				return false;
			}

			//first, lets check the context, its fasterr than session
			bool? isAdmin = HttpContext.Current.Items["IsCurrentUserAdmin"] as bool?;
			if (isAdmin != null)
				return isAdmin.Value;

			//nothing in context lets check the session
			isAdmin = HttpContext.Current.Session["IsCurrentUserAdmin"] as bool?;
			if (isAdmin != null)
			{
				HttpContext.Current.Items["IsCurrentUserAdmin"] = isAdmin.Value;
				return isAdmin.Value;
			}
			
			//finally - lets go to database
			isAdmin = IsAdmin(CurrentUserID);

			//cache the results in session
			HttpContext.Current.Items["IsCurrentUserAdmin"] = isAdmin.Value;
			HttpContext.Current.Session["IsCurrentUserAdmin"] = isAdmin.Value;

			return (bool)isAdmin;
		}

		/// <summary>
		/// returns user ID or 0 if not found
		/// </summary>
		/// <returns></returns>
		public static int GetUserIDByUsername(string username, int instanceId)
		{
			if (username == null) return 0;

			using (SqlConnection cn = DBUtils.GetNewConnection())
			{
				cn.Open();
				return cn.Query<int>(
					"SELECT UserID FROM " + _usersDbTableName + " WHERE UserName=@UserName AND InstanceID=@InstanceID",
					new { UserName = username, InstanceID = instanceId }).FirstOrDefault();
			}
		}

		public static void ResetUserInfoCacheForUserId(int userId)
		{
			if (HttpContext.Current != null && HttpContext.Current.Application != null)
				HttpContext.Current.Application["ResetUserInfo" + userId] = true; //set the flag that we should reset
		}

		public static string CurrentUserName
		{
			get
			{
				return RemoveDomainFromUsername(HttpContext.Current.User.Identity.Name);
			}
		}

		/// <summary>
		/// removes the domain part from "DOMAIN\Username"
		/// </summary>
		/// <param name="username"></param>
		/// <returns></returns>
		public static string RemoveDomainFromUsername(string username)
		{
			if (username == null) return null;
			return username.Substring(username.IndexOf("\\") + 1);
		}

		public static void ResetCurrentUserInfoCache()
		{
			HttpContext.Current.Session["UserInfo"] = null;
		}

		//gets current user details from the session cache
		//if not found in session cache - calls the "get info" method that has to be supplied
		public static T GetCurrentUserInfo<T>(Func<T> getCurrentUserInfoMethod)
		{
				//if theres a flag that we should reset cahe for this user - then reset and clear the flag
				if (HttpContext.Current.Application["ResetUserInfo" + CurrentUserID] != null)
				{
					HttpContext.Current.Application["ResetUserInfo" + CurrentUserID] = null;
					HttpContext.Current.Session["UserInfo"] = null;
				}

				if (HttpContext.Current.Session["UserInfo"] == null) //nothing found in cache, lets create the object
				{
					T userInfo = getCurrentUserInfoMethod(); //call the supplied delegate

					HttpContext.Current.Session["UserInfo"] = userInfo;
					return userInfo;
				}
				return (T)HttpContext.Current.Session["UserInfo"];
		}

		/// <summary>
		/// returns user ID or 0 if not found
		/// </summary>
		/// <returns></returns>
		public static int GetUserIDByEmail(string email, int instanceId)
		{
			if (email == null) return 0;
			if (email.Trim() == string.Empty) return 0;

			using (var cn = DBUtils.GetNewOpenConnection())
			{
				return cn.Query<int>("SELECT UserID FROM " + _usersDbTableName + " WHERE email=@email AND InstanceID=@InstanceID",
					new { email, InstanceID = instanceId }).FirstOrDefault();
			}
		}

		public static bool UpdateUserEmail(int userID, string newEmail)
		{
			bool retval;
			using (SqlCommand cmd = DBUtils.GetNewCommandObject())
			{
				cmd.CommandText = "SELECT InstanceID FROM " + _usersDbTableName + " WHERE UserID=" + userID;
				cmd.Connection.Open();
				object res = cmd.ExecuteScalar();
				if (res != null) //user found
				{
					int instanceId = Convert.ToInt32(res);

					//ensure we have unique email within one instance
					int existingUserId = GetUserIDByEmail(newEmail, instanceId);
					if (existingUserId == 0 || existingUserId == userID)
					{
						if (existingUserId != userID) //update only if email is changed
						{
							cmd.CommandText = "UPDATE " + _usersDbTableName + " SET Email=@Email WHERE UserID=" + userID;
							cmd.Parameters.Clear();
							cmd.Parameters.AddWithValue("@Email", newEmail);
							cmd.ExecuteNonQuery();
						}
						retval = true;
					}
					else
					{
						retval = false;
					}
				}
				else
				{
					retval = false;
				}
				cmd.Connection.Close();
			}
			return retval;
		}

		public static bool IsEnabled(int userID)
		{
			if (userID == 0) return false;

			bool res;
			using (SqlConnection cn = DBUtils.GetNewConnection())
			{
				cn.Open();

				res = cn.Query<bool>("SELECT Disabled FROM " + _usersDbTableName + " WHERE UserID=" + userID).FirstOrDefault();
			}
			return !res;
		}

		public static void Disable(int userId)
		{
			using (var cn = DBUtils.GetNewOpenConnection())
			{
				cn.Execute("UPDATE " + _usersDbTableName + " SET Disabled=1 WHERE UserID=" + userId);
			}
		}
		public static void Enable(int userId)
		{
			using (var cn = DBUtils.GetNewOpenConnection())
			{
				cn.Execute("UPDATE " + _usersDbTableName + " SET Disabled=0 WHERE UserID=" + userId);
			}
		}
	}
}