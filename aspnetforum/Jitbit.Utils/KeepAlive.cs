using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Net;

namespace Jitbit.Utils
{
	public class KeepAlive
	{
		private static KeepAlive instance;
		private static object sync = new object();
		private string _applicationUrl;
		private string _cacheKey;
		public static int PingCount { get; private set; }

		private KeepAlive(string applicationUrl)
		{
			_applicationUrl = applicationUrl;
			_cacheKey = Guid.NewGuid().ToString();
			instance = this;
			PingCount = 0;
		}

		public static void ResetPingCount()
		{
			PingCount = 0;
		}

		public static bool IsKeepingAlive
		{
			get
			{
				lock (sync)
				{
					return instance != null;
				}
			}
		}

		public static void Start(string applicationUrl)
		{
			if (IsKeepingAlive)
			{
				return;
			}
			lock (sync)
			{
				instance = new KeepAlive(applicationUrl);
				instance.Insert();
			}
		}

		public static void Stop()
		{
			lock (sync)
			{
				HttpRuntime.Cache.Remove(instance._cacheKey);
				instance = null;
			}
		}

		private void Callback(string key, object value, CacheItemRemovedReason reason)
		{
			if (reason == CacheItemRemovedReason.Expired)
			{
				FetchApplicationUrl();
				Insert();
			}
		}

		private void Insert()
		{
			HttpRuntime.Cache.Add(_cacheKey,
				this,
				null,
				Cache.NoAbsoluteExpiration,
				new TimeSpan(0, 0, 10),
				CacheItemPriority.Normal,
				this.Callback);
		}

		public static void FetchApplicationUrl()
		{
			if (PingUrl(instance._applicationUrl))
				PingCount++;
		}

		private static bool PingUrl(string url)
		{
			try
			{
				HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
				using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
				{
					HttpStatusCode status = response.StatusCode;
					//log status
				}
				return true;
			}
			catch (Exception ex)
			{
				//log exception
				return false;
			}
		}
	}
}