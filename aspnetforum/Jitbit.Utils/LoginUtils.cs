using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace Jitbit.Utils
{
	public class LoginUtils
	{
		public static bool IsBruteForce(HttpContext context, bool applicationWide = false, int maxAttempts = 5)
		{
			//brute force check
			//check if last attempt was less than 5 mins ago
			if (!applicationWide)
			{
				if (context.Session["LastTry"] != null
					&& DateTime.Now.Subtract((DateTime)context.Session["LastTry"]).Minutes < 5
					&& context.Session["InvalidLogins"] != null
					&& Convert.ToInt32(context.Session["InvalidLogins"]) > maxAttempts)
				{
					ExceptionHandler.RenderErrorPage(maxAttempts + " invalid login attempts. Please wait 5 mins from your last attempt.");
					return true;
				}

				context.Session["LastTry"] = DateTime.Now;
				return false;
			}
			else
			{
				string ip = context.Request.UserHostAddress;
				if (HttpRuntime.Cache["InvalidLogins" + ip] != null
					&& Convert.ToInt32(HttpRuntime.Cache["InvalidLogins" + ip]) > maxAttempts)
				{
					ExceptionHandler.RenderErrorPage(maxAttempts + "5 invalid login attempts. Please wait 5 mins from your last attempt.");
					return true;
				}

				return false;
			}
		}

		public static bool VerifyAutoLogin(string username, string pswHash, string email, string userHash, string sharedSecret, out string result, Func<int> addUserMethod)
		{
			result = "";

			if (LoginUtils.IsBruteForce(System.Web.HttpContext.Current, true)) return false;

			if (username == null) //username not passed - get out
			{
				LoginUtils.LogInvalidLoginAttempt(System.Web.HttpContext.Current, true);
				return false;
			}

			if (pswHash == null && (email == null || userHash == null)) //pswHash not passwed AND email/userHash not passed - get out
			{
				LoginUtils.LogInvalidLoginAttempt(System.Web.HttpContext.Current, true);
				return false;
			}

			//logging in an existing user with his password hash
			if (pswHash != null)
			{
				int userId;
				string password;
				if (UserHelpers.GetUserIdAndPswByUsername(username, Instance.CurrentInstanceID, out userId, out password))
				{
					if (CryptoUtils.MD5Hash(password).ToLower() == pswHash.ToLower() || password.ToLower() == pswHash.ToLower())
					{
						UserHelpers.CurrentUserID = userId;
						LoginUtils.ResetBruteForceCounter(System.Web.HttpContext.Current, true);
						LoginUtils.FormsAuthLogin(username, false, System.Web.HttpContext.Current);
						return true;
					}
					else
					{
						result = "Invalid parameters passed. Wait 5 minutes and try again.";
					}
				}
				else
				{
					result = "Invalid parameters passed. Wait 5 minutes and try again.";
				}
				LoginUtils.LogInvalidLoginAttempt(System.Web.HttpContext.Current, true);
				return false;
			}

			//logging in a user (either new or existing) with the app "shared secret"
			if (email != null && userHash != null)
			{
				if (string.IsNullOrEmpty(sharedSecret))
				{
					result = "No shared key specified.";
					return false;
				}
				string computedHash = CryptoUtils.MD5Hash(username + email + sharedSecret);
				if (userHash.ToLower() != computedHash.ToLower())
				{
					LoginUtils.LogInvalidLoginAttempt(System.Web.HttpContext.Current, true);
					result ="Invalid parameters passed. Wait 5 minutes and try again.";
					return false;
				}

				int userId = UserHelpers.GetUserIDByUsername(username, Instance.CurrentInstanceID);
				if (userId == 0) //user not found - lets add him (call delegate)
				{
					try
					{
						userId = addUserMethod();
					}
					catch (Exception ex)
					{
						result = ex.Message;
						return false;
					}
				}

				UserHelpers.CurrentUserID = userId;
				LoginUtils.ResetBruteForceCounter(System.Web.HttpContext.Current, true);
				LoginUtils.FormsAuthLogin(username, false, System.Web.HttpContext.Current);
				return true;
			}

			return false;
		}

		public static void LogInvalidLoginAttempt(HttpContext context, bool applicationWide = false)
		{
			if (!applicationWide)
			{
				if (context.Session["InvalidLogins"] == null)
					context.Session["InvalidLogins"] = 1;
				else
					context.Session["InvalidLogins"] = Convert.ToInt32(context.Session["InvalidLogins"]) + 1;
			}
			else //log an application-wide login attempt (for bots that have no cookies) using ip-address as a key
			{
				string ip = context.Request.UserHostAddress;
				if (HttpRuntime.Cache["InvalidLogins" + ip] == null)
					HttpRuntime.Cache.Add("InvalidLogins" + ip, 1, null, DateTime.Now.AddMinutes(5), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
				else
					HttpRuntime.Cache["InvalidLogins" + ip] = Convert.ToInt32(HttpRuntime.Cache["InvalidLogins" + ip]) + 1;
			}
		}

		public static void ResetBruteForceCounter(HttpContext context, bool applicationWide = false)
		{
			if (!applicationWide)
			{
				context.Session["LastTry"] = null;
				context.Session["InvalidLogins"] = null;
			}
			else
			{
				context.Application["LastTry"] = null;
				context.Application["InvalidLogins"] = null;
			}
		}

		/// <summary>
		/// assigns the auth-cookie to user
		/// </summary>
		public static void FormsAuthLogin(string userName, bool rememberMe, HttpContext context)
		{
			LoginUtils.ResetBruteForceCounter(context);

			if (!rememberMe)
			{
				FormsAuthentication.SetAuthCookie(userName, false);
			}
			else
			{
				FormsAuthentication.Initialize();
				DateTime expires = DateTime.Now.AddDays(20);
				FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1,
					userName,
					DateTime.Now,
					expires, // value of time out property
					true, // Value of IsPersistent property
					String.Empty,
					FormsAuthentication.FormsCookiePath);

				string encryptedTicket = FormsAuthentication.Encrypt(ticket);

				HttpCookie authCookie = new HttpCookie(
							FormsAuthentication.FormsCookieName,
							encryptedTicket);
				authCookie.Expires = expires;

				HttpContext.Current.Response.Cookies.Add(authCookie);
			}
		}

		/// <summary>
		/// assigns the auth-cookie to user AND REDIRECTS
		/// </summary>
		public static void FormsAuthLoginAndRedirectToReturnUrl(string userName, bool rememberMe, HttpContext context, string defaultReturnUrl)
		{
			FormsAuthLogin(userName, rememberMe, context);

			string returnUrl = FormsAuthentication.GetRedirectUrl(userName, true);
			if (string.IsNullOrEmpty(returnUrl)) returnUrl = defaultReturnUrl;
			HttpContext.Current.Response.Redirect(returnUrl, false);
		}
	}
}