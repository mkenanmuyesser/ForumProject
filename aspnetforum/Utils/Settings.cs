using System;
using System.Collections.Generic;
using System.Web;
using System.Configuration;
using System.Data.Common;

namespace aspnetforum.Utils
{
	// !!! to add settings, 
	//     1) add one entry to _defaults, 
	//     2) if needed, add strongly typed property below 
	//     3) if needed, add _descriptions (existing ones are copied from web.config) 

	public class DbAwareSettings
	{
		// currently the instance by itself is pointless and safe
		// and is only there for the indexer - too bad C# doesn't support static indexers...
		private static readonly DbAwareSettings _instance = new DbAwareSettings();

		static DbAwareSettings()
		{
			if (ConfigurationManager.AppSettings["ResetSettingsToDefault"] == "true")
				ResetToDefaults();
		}

		private static readonly Dictionary<string, string> _defaults = new Dictionary<string, string>()
		{
			{ "MaxUploadFileSizeInBytes", "1500000" },
			{ "EnableFileUploads", "true" },
			{ "AllowGuestPosts", "false" },
			{ "AllowGuestThreads", "false" },
			{ "MailNotificationsEnabled", "true" },
			{ "EnableRating", "true" },
			{ "ForumTitle", "Acme Forum" },
			{ "TitleLink", "http://www.jitbit.com/" },
			{ "MailServer", "mail.mydomain.com" },
			{ "MailServerPort", "25" },
			{ "MailServerLogin", "robot@mydomain.com" },
			{ "MailServerPassword", "password" },
			{ "MailFromAddress", "robot@mydomain.com" },
			{ "MailUseSSL", "false" },
			{ "SendErrorReportsTo", "" },
			{ "EmailDebug", "false" },
			{ "IntegratedAuthentication", "false" },
			{ "NotifyModeratorOfNewMessages", "true" },
			{ "EnableAvatars", "true" },
			{ "MaxAvatarFileSizeInBytes", "15000" },
			{ "MaxAvatarWidthHeight", "150" },
			{ "BannedIPs", "" },
			{ "ForbiddenUploadExtensions", "exe;dll" },
			{ "PageSize", "20" },
			{ "EnableEmailActivation", "true" },
			{ "NewUsersNotifyAdmin", "false" },
			{ "AdminRoleName", "" },
			{ "AdminUserName", "" },
			{ "NewUsersDisabledByDefault", "true" },
			{ "ServerTimeOffset", "0" },
			{ "ForumURL", "" },
			{ "BadWords", "fuck;shit;cunt;cocksucker;piss" },
			{ "MsgSortDescending", "false" },
			{ "AllowSmilies", "true" },
			{ "UseSHA1InsteadOfMD5", "false" },
			{ "ShowRecentPostsOnHomepage", "false" },
			{ "EnablePrivateMessaging", "true" },
			{ "DisableRSS", "false" },
			{ "ShowFullNamesInsteadOfUsernames", "true" },
			{ "EnableOpenId", "true" },
			{ "IntegratedMembership", "false" },
			{ "DisableEditing", "false" },
			{ "AutoLoginSharedSecret", "hgsdfhdsfh" },
			{ "TwitterConsumerKey", "" },
			{ "TwitterConsumerSecret", "" },
			{ "FacebookAppID", "" },
			{ "FacebookAppSecret", "" },
			{ "EnableGravatar", "true" },
			{ "MinPasswordLength", "6" },
			{ "FilesUploadPath", "" },
			{ "DisableAchievements", "false" }
		};

		private static readonly Dictionary<string, string> _descriptions = new Dictionary<string, string>()
		{
			 { "AdminRoleName", @"This setting makes sense only if the IntegratedAuthentication flag is set to true.
		You can specify the name of the membership role (or windows-group) that will have admin-permissions in the forum.
		NOTE: if you use windows-authentication, remember to include the domain like this: DOMAIN\Username" },
			{ "AdminUserName", @"This setting makes sense only if the IntegratedAuthentication flag is set to true.
		You can specify the name of the admin user.
		NOTE: if you use windows-authentication, remember to include the domain like this: DOMAIN\Username" },
			{ "AllowGuestPosts", "This setting enables/disables guest posting (posts from unregistered users)" }, 
			{ "AllowGuestThreads", "Enables/disables creating THREADS for guest users" }, 
			{ "AllowSmilies", "Enables/disables 'smilies' - smile images" }, 
			{ "BadWords", "Comma-separated words for the 'bad words' filter. Messages with these words won't go through." }, 
			{ "EnableGravatar", "Enable Gravatar.com support for avatar-images" }, 
			{ "EnableOpenId", "Enable/disable OpenID-support" }, 
			{ "EnablePrivateMessaging", "Enable/disable private messaging for your users" }, 
			{ "FacebookAppSecret", " Facebook app secret. This is required if you want 'connect with facebook' to work. You have to register an 'app' at http://developers.facebook.com/setup/ to obtain these keys" }, 
			{ "MailFromAddress", "The forum will send email notifcation from this address" }, 
			{ "MailNotificationsEnabled", "Enable/disable email notifications for the forum" }, 
			{ "MailServer", "Outgoing SMTP-server for the email notifications" }, 
			{ "MailServerPassword", "Password for the SMTP-server" }, 
			{ "MailServerPort", "SMTP port (typically 25, 468 or 587)" }, 
			{ "MailUseSSL", "Use SLL to connect to the SMTP server" }, 
			{ "MaxAvatarFileSizeInBytes", "Maximum avatar image file size" }, 
			{ "MaxAvatarWidthHeight", "Maximum avatar image dimensions (in pixels)" }, 
			{ "MsgSortDescending", "This setting will sort all messages (in topics) in a descending order" }, 
			{ "PageSize", "Page size for messages in a topic, and topics in a forum" }, 
			{ "SendErrorReportsTo", "Administrator email (so he gets error notifications), separate with commas" }, 
			{ "ShowRecentPostsOnHomepage", "Enable/disable showing recent posts on the homepage" }, 
			{ "TitleLink", "URL that the top 'back to website' points to. Leave blank to remove that link" }, 
			{ "TwitterConsumerSecret", "Twitter secret key. This is required if you want 'login with twitter' to work. You have to register an 'app' at http://dev.twitter.com/apps to obtain these keys" }, 
			{ "MaxUploadFileSizeInBytes", "" },
			{ "EnableFileUploads", @"Enables/disables file uploads.
		PLEASE NOTE: to enable file-uploading you should grant write permissions on the 'App_Data' folder to the user-account, which your ASP.NET website runs under.
		(typically NETWORK SERVICE account, or ASPNET account" },
			{ "EnableRating", "Enable/disable post ratings and reputation" },
			{ "MailServerLogin", "Specify EMPTY serverlogin if your smtp-server does not require authentication" },
			{ "EmailDebug", "enable showing email errors/ if you have trouble sending emails, set this flag to true and repeat your last action to see the error message" },
			{ "IntegratedAuthentication", @"Makes sense only if the forum is run as a part of another bigger web-application OR the forum uses Windows-authentication. It specifies if you want the forum to attempt to recognize the parent application authenticated users (or Windows-authenticated users) and automatically register them as forum users.

<b>PLEASE NOTE: before you enable this setting, remember to specify 'AdminUserName' setting as well.</b> Or you might not be able to access the forum after saving.
		
EXAMPLE 1: your parent website runs its own database of registered users, and uses ASP.NET Forms Authentication to authenticate users (via SQL membership provider for example). AspNetForum will detect authenticated users and log them in automatically (and add them to the forum users database)

EXAMPLE 2: your parent website uses Windows Authentication to authenticate your Active Directory users. AspNetForum will detect current authenticated windows-users and add log them in automatically (and add them to the forum users database).

EXAMPLE 3: you have no parent website at all, but you have configured the forum to use windows-authentication (above). AspNetForum will detect current authenticated windows-users and add log them in automatically (and add them to the forum users database)." },
			{ "NotifyModeratorOfNewMessages", "enable/disable email notification for moderators when a new message is posted to THEIR forum(s)" },
			{ "EnableAvatars", @"Enable/disable avatars" },
			{ "BannedIPs", "Ban by IP address (semicolon separated, you can use patterns like \"192.*.*.*\")" },
			{ "ForbiddenUploadExtensions", "prohibit certain file extensions for attachments" },
			{ "EnableEmailActivation", "Enable/disable email confirmation when registering new users" },
			{ "NewUsersNotifyAdmin", "notify ALL administrators of new user registrations" },
			{ "NewUsersDisabledByDefault", @"newly registered users are created DISABLED by default, until they activated by email or activated by admin" },
			{ "ServerTimeOffset", @"Server time offset.
		For EXAMPLE: if your hosting provider is in New York-USA, but your website is French, you might want to add a 6 hours offset (cause when it's 1:00 in NY it is 7:00 in Paris)
		This can be a negative value. INTEGER." },
			{ "ForumURL", @"the forum URL (required for links in emails etc). This setting is !!OPTIONAL!! because the forum can determine its address automatically, but in case you have some weird firewall-proxy-redirecting config - uncomment and edit it.
		NOTE: the URL MUST end with /." },
			{ "UseSHA1InsteadOfMD5", @"use SHA1-hashing for passwords instead of MD5." },
			{ "DisableRSS", "enable/disable RSS links" },
			{ "ShowFullNamesInsteadOfUsernames", @"shows user's fullname (firstname+lastname) instead of his login on most pages if first/last names are both empty - the forum will show user's login" },
			{ "IntegratedMembership", @"If your parent website uses asp.net-membership, set this flag to true and the forum will try to pull user emails from the membership" },
			{ "DisableEditing", "enable/disable editing own messages" },
			{ "AutoLoginSharedSecret", @"Shared secret key for remote authentication. Please refer to the manual or leave it blank" },
			{ "TwitterConsumerKey", @"Twitter API key. This is required if you want 'login with twitter' to work.
		You have to register an 'app' at http://dev.twitter.com/apps to obtain these keys" },
			{ "FacebookAppID", @"Facebook API key. This is required if you want 'connect with facebook' to work.
		You have to register an 'app' at http://developers.facebook.com/setup/ to obtain these keys" },
			{ "MinPasswordLength", "Minimum allowed password length" },
			{ "FilesUploadPath", "Advanced setting - absolute path to the file-uploads folder. Overrides the default file-upload path (with a UNC-path for example, for server farms). Use with caution. Makes sure write permissions are granted and all the files are copied there. You can use ASP.NET tilde paths (starting with '~/')." },
			{ "DisableAchievements", "Disable user achievements" }
		};


		public static DbAwareSettings Current
		{
			get { return _instance; }
		}

		public string this[string key]
		{
			get { return Get(key); }
			set
			{
				if (value != null)
				{
					Set(key, value);
				}
			}
		}

		public string GetDescription(string key)
		{
			if (_descriptions.ContainsKey(key))
			{
				return _descriptions[key];
			}
			return "";
		}

		// initializes all settings
		internal void Preload()
		{
			DeleteUnneededDbSettings();
			foreach (string key in _defaults.Keys)
			{
				Get(key);
			}
		}

		//removes unneeded settings that might be present in DB when from older versions...
		private static void DeleteUnneededDbSettings()
		{
			string inCriteria = "";
			foreach (string key in _defaults.Keys)
			{
				inCriteria += "'" + key + "',";
			}
			inCriteria = inCriteria.Substring(0, inCriteria.Length - 1);

			using (DbCommand cmd = DB.CreateCommand())
			{
				cmd.CommandText = string.Format("DELETE FROM ForumConfig WHERE CfgKey NOT IN ({0})", inCriteria);
				cmd.Connection.Open();
				cmd.ExecuteNonQuery();
				cmd.Connection.Close();
			}
		}

		private static string MakeAppKey(string key)
		{
			return string.Format("JitBitForumConfig:{0}", key);
		}

		private static string GetNonDb(string key)
		{
			if (ConfigurationManager.AppSettings[key] != null)
			{
				return ConfigurationManager.AppSettings[key];
			}
			else if (_defaults.ContainsKey(key))
			{
				return _defaults[key];
			}
			throw new Exception("Invalid configuration setting");
		}

		private static string Get(string key)
		{
			// try cache first
			string appKey = MakeAppKey(key);
			object paramObj = HttpContext.Current.Application[appKey];
			if (paramObj != null)
			{
				return paramObj.ToString();
			}

			// if not found in cache, then go to db
			// assuming that all settings will be cached fast it makes no sense to cache the connection/etc

			string param = "";
			bool dbError = false;
			bool ignoreDbError = (HttpContext.Current.Items["IgnoreDbSettingErrors"] != null);
			using (var cn = DB.CreateOpenConnection())
			{
				try
				{
					paramObj = cn.ExecuteScalar("SELECT CfgValue FROM ForumConfig WHERE CfgKey = ?", key);
					if (null != paramObj && DBNull.Value != paramObj)
					{
						param = paramObj.ToString();
					}
					else
					{
						// not found in db
						param = GetNonDb(key);
						DbSet(key, param, cn, false);
					}
				}
				catch (Exception ex)
				{
					dbError = true;
					if (!ignoreDbError)
					{
						throw;
					}
				}
			}

			if (dbError)
			{
				param = GetNonDb(key);
			}

			HttpContext.Current.Application[appKey] = param;
			return param;
		}

		private static void Set(string key, string value)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				DbSet(key, value, cn, true);
			}
			HttpContext.Current.Application[MakeAppKey(key)] = value;
		}

		private static void ResetToDefaults()
		{
			//do not reset if it was already reset recently (to prevent infinite loop)
			if (HttpContext.Current.Application["LastResetDate"] != null)
			{
				if ((DateTime)HttpContext.Current.Application["LastResetDate"] > DateTime.Now.AddMinutes(-5))
					return;
			}
			HttpContext.Current.Application["LastResetDate"] = DateTime.Now;

			using (var cn = DB.CreateOpenConnection())
			{
				foreach (string key in _defaults.Keys)
				{
					DbSet(key, _defaults[key], cn, true);
					HttpContext.Current.Application[MakeAppKey(key)] = _defaults[key];
				}
			}
		}

		private static void DbSet(
			string key,
			string value,
			DbConnection cn,
			bool tryUpdate
			)
		{
			value = value ?? "";

			string insertText = "INSERT INTO ForumConfig (CfgValue,CfgKey) VALUES (?,?)";
			string sql = tryUpdate ? "UPDATE ForumConfig SET CfgValue = ? WHERE CfgKey = ?" : insertText;
			int results = cn.ExecuteNonQuery(sql, value, key);
			if (tryUpdate && (results == 0)) // nothing to update
			{
				cn.ExecuteNonQuery(insertText, value, key);
			}
		}
	}


	public static class Settings
	{
		public static int MaxUploadFileSize
		{
			get
			{
				int res;
				int.TryParse(DbAwareSettings.Current["MaxUploadFileSizeInBytes"], out res);
				return res;
			}
		}

		// TODO: why is this all not using bool.Parse?
		public static bool EnableFileUploads
		{
			get { return (DbAwareSettings.Current["EnableFileUploads"].ToLower() == "true"); }
		}

		public static bool AllowGuestPosts
		{
			get { return (DbAwareSettings.Current["AllowGuestPosts"].ToLower() == "true"); }
		}

		public static bool AllowGuestThreads
		{
			get { return (DbAwareSettings.Current["AllowGuestThreads"].ToLower() == "true"); }
		}

		public static bool MailNotificationsEnabled
		{
			get { return (DbAwareSettings.Current["MailNotificationsEnabled"].ToLower() == "true"); }
		}

		public static bool EnableRating
		{
			get { return (DbAwareSettings.Current["EnableRating"].ToLower() == "true"); }
		}

		public static string ForumTitle
		{
			get { return DbAwareSettings.Current["ForumTitle"]; }
		}

		public static string TitleLink
		{
			get { return DbAwareSettings.Current["TitleLink"]; }
		}

		public static string MailServer
		{
			get { return DbAwareSettings.Current["MailServer"]; }
		}
		public static int MailServerPort
		{
			get
			{
				int res;
				int.TryParse(DbAwareSettings.Current["MailServerPort"], out res);
				return res;
			}
		}
		public static string MailServerLogin
		{
			get { return DbAwareSettings.Current["MailServerLogin"]; }
		}
		public static string MailServerPassword
		{
			get { return DbAwareSettings.Current["MailServerPassword"]; }
		}
		public static string MailFromAddress
		{
			get { return DbAwareSettings.Current["MailFromAddress"]; }
		}
		public static bool MailUseSSL
		{
			get { return DbAwareSettings.Current["MailUseSSL"] == "true"; }
		}
		public static string SendErrorReportsTo
		{
			get { return DbAwareSettings.Current["SendErrorReportsTo"]; }
		}

		public static bool EmailDebug
		{
			get { return DbAwareSettings.Current["EmailDebug"] == "true"; }
		}

		public static bool IntegratedAuthentication
		{
			get
			{
				return (DbAwareSettings.Current["IntegratedAuthentication"].ToLower() == "true");
			}
		}

		public static bool NotifyModeratorOfNewMessages
		{
			get
			{
				return (DbAwareSettings.Current["NotifyModeratorOfNewMessages"].ToLower() == "true");
			}
		}

		public static bool EnableAvatars
		{
			get { return (DbAwareSettings.Current["EnableAvatars"].ToLower() == "true"); }
		}

		public static int MaxAvatarFileSizeInBytes
		{
			get
			{
				int res;
				int.TryParse(DbAwareSettings.Current["MaxAvatarFileSizeInBytes"], out res);
				return res;
			}
		}

		public static int MaxAvatarWidthHeight
		{
			get
			{
				int res;
				int.TryParse(DbAwareSettings.Current["MaxAvatarWidthHeight"], out res);
				return res;
			}
		}

		public static string[] BannedIPs
		{
			get
			{
				if (!string.IsNullOrEmpty(DbAwareSettings.Current["BannedIPs"]))
					return DbAwareSettings.Current["BannedIPs"].Split(';');
				else
					return null;
			}
		}

		public static string[] ForbiddenUploadExtensions
		{
			get
			{
				if (!string.IsNullOrEmpty(DbAwareSettings.Current["ForbiddenUploadExtensions"]))
					return DbAwareSettings.Current["ForbiddenUploadExtensions"].Split(';');
				else
					return new string[] { };
			}
		}

		public static int PageSize
		{
			get
			{
				int res;
				int.TryParse(DbAwareSettings.Current["PageSize"], out res);
				return res;
			}
		}

		public static bool EnableEmailActivation
		{
			get { return (DbAwareSettings.Current["EnableEmailActivation"].ToLower() == "true"); }
		}

		public static bool NewUsersNotifyAdmin
		{
			get { return (DbAwareSettings.Current["NewUsersNotifyAdmin"].ToLower() == "true"); }
		}

		public static string AdminRoleName
		{
			get
			{
				try { return DbAwareSettings.Current["AdminRoleName"]; }
				catch { return null; }
			}
		}

		public static string AdminUserName
		{
			get
			{
				try { return DbAwareSettings.Current["AdminUserName"]; }
				catch { return null; }
			}
		}

		public static bool NewUsersDisabledByDefault
		{
			get { return (DbAwareSettings.Current["NewUsersDisabledByDefault"].ToLower() == "true"); }
		}

		public static int ServerTimeOffset
		{
			get
			{
				int res;
				int.TryParse(DbAwareSettings.Current["ServerTimeOffset"], out res);
				return res;
			}
		}

		public static string ForumURL
		{
			get
			{
				try { return DbAwareSettings.Current["ForumURL"]; }
				catch { return null; }
			}
		}

		public static string[] BadWords
		{
			get
			{
				if (!string.IsNullOrEmpty(DbAwareSettings.Current["BadWords"]))
					return DbAwareSettings.Current["BadWords"].Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
				else
					return null;
			}
		}

		public static bool MsgSortDescending
		{
			get { return (DbAwareSettings.Current["MsgSortDescending"].ToLower() == "true"); }
		}

		public static bool AllowSmilies
		{
			get
			{
				return (DbAwareSettings.Current["AllowSmilies"].ToLower() == "true");
			}
		}

		public static bool UseSHA1InsteadOfMD5
		{
			get
			{
				return (DbAwareSettings.Current["UseSHA1InsteadOfMD5"].ToLower() == "true");
			}
		}

		public static bool ShowRecentPostsOnHomepage
		{
			get
			{
				return (DbAwareSettings.Current["ShowRecentPostsOnHomepage"].ToLower() == "true");
			}
		}

		public static bool EnablePrivateMessaging
		{
			get
			{
				return (DbAwareSettings.Current["EnablePrivateMessaging"].ToLower() == "true");
			}
		}

		public static bool DisableRSS
		{
			get
			{
				return (DbAwareSettings.Current["DisableRSS"].ToLower() == "true");
			}
		}

		public static bool ShowFullNamesInsteadOfUsernames
		{
			get
			{
				return (DbAwareSettings.Current["ShowFullNamesInsteadOfUsernames"].ToLower() == "true");
			}
		}

		public static bool EnableOpenId
		{
			get
			{
				return (DbAwareSettings.Current["EnableOpenId"].ToLower() == "true");
			}
		}

		public static bool IntegratedMembership
		{
			get
			{
				return (DbAwareSettings.Current["IntegratedMembership"].ToLower() == "true");
			}
		}

		public static bool DisableEditing
		{
			get
			{
				return (DbAwareSettings.Current["DisableEditing"].ToLower() == "true");
			}
		}

		public static string AutoLoginSharedSecret
		{
			get
			{
				try { return DbAwareSettings.Current["AutoLoginSharedSecret"]; }
				catch { return null; }
			}
		}

		public static string TwitterConsumerKey
		{
			get
			{
				return DbAwareSettings.Current["TwitterConsumerKey"] as string;
			}
		}

		public static string TwitterConsumerSecret
		{
			get
			{
				return DbAwareSettings.Current["TwitterConsumerSecret"] as string;
			}
		}

		public static string FacebookAppID
		{
			get
			{
				return DbAwareSettings.Current["FacebookAppID"] as string;
			}
		}

		public static string FacebookAppSecret
		{
			get
			{
				return DbAwareSettings.Current["FacebookAppSecret"] as string;
			}
		}

		public static bool EnableGravatar
		{
			get
			{
				return (DbAwareSettings.Current["EnableGravatar"].ToLower() == "true");
			}
		}

		public static int MinPasswordLength
		{
			get
			{
				try { return int.Parse(DbAwareSettings.Current["MinPasswordLength"]); }
				catch { return 6; }
			}
		}

		public static string FilesUploadPath
		{
			get
			{
				string filepath = DbAwareSettings.Current["FilesUploadPath"];
				if (string.IsNullOrWhiteSpace(filepath))
					return AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
				else if (filepath.StartsWith("~"))
					return System.Web.Hosting.HostingEnvironment.MapPath(filepath);
				else
					return DbAwareSettings.Current["FilesUploadPath"];
			}
		}

		public static bool DisableAchievements
		{
			get
			{
				return (DbAwareSettings.Current["DisableAchievements"].ToLower() == "true");
			}
		}
	}
}
