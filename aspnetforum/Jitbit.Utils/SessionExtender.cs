using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;

namespace Jitbit.Utils
{
	/// <summary>
	/// this class saves something to the Session object
	/// but with an EXPIRATION TIMEOUT
	/// (just like the ASP.NET Cache)
	/// (c) Jitbit 2011. Feel free to use/modify/whatever
	/// usage sample:
	///   Session.AddWithTimeout(
	///     "key",
	///     "value",
	///     TimeSpan.FromMinutes(5));
	/// </summary>
	public static class SessionExtender
	{
		public static void AddWithTimeout(this HttpSessionState session,
			string name,
			object value,
			TimeSpan expireAfter)
		{
			session[name] = value;
			session[name + "ExpDate"] = DateTime.Now.Add(expireAfter);
		}

		public static object GetWithTimeout(
			this HttpSessionState session,
			string name)
		{
			object value = session[name];
			if (value == null) return null;

			DateTime? expDate = session[name + "ExpDate"] as DateTime?;
			if (expDate == null) return null;

			if (expDate < DateTime.Now)
			{
				session.Remove(name);
				session.Remove(name + "ExpDate");
				return null;
			}

			return value;
		}
	}
}