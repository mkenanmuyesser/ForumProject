using System;
using System.Web;
using System.Data;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Collections.Specialized;
using System.Data.Common;
using System.Web.Security;
using System.Configuration;
using System.Web.UI;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using aspnetforum.Utils;
using Jitbit.Utils;

namespace aspnetforum
{
	public class ForumPage : Page
	{
		private string _forumTitle;

		private readonly HtmlMeta _metaDescription = new HtmlMeta();
		private readonly HtmlMeta _metaKeywords = new HtmlMeta();

		public DbConnection Cn;
		public DbCommand Cmd;

		public string MetaDescription
		{
			set
			{
				try { _metaDescription.Content = value; }
				catch { }
			}
			get { return _metaDescription.Content; }
		}

		public string MetaKeywords
		{
			set
			{
				//replacing all non-alphanumeric chars with commas
				string keywords = Regex.Replace(value, @"\W", ",");
				keywords = Regex.Replace(keywords, @",{2,}", ","); //just replacing repeating commas
				try { _metaKeywords.Content = keywords; }
				catch { }
			}
			get { return _metaKeywords.Content; }
		}

		public int PageSize { get; private set; }

		//if the current user is an administrator (//todo: move this crap to the "user" class)
		private bool? _isAdministrator = null;
		public bool IsAdministrator
		{
			get
			{
				if (!_isAdministrator.HasValue)
				{
					if (CurrentUserID == 0) _isAdministrator = false;
					else
					{
						_isAdministrator = Utils.User.IsAdministrator(CurrentUserID);
					}
				}
				return _isAdministrator.Value;
			}
		}

		protected void SendOutRssAndQuit(string rssXml)
		{
			Response.Clear();
			Response.ContentType = "text/xml";
			Response.Write(rssXml);
			Response.End();
		}

		public ForumPage() : base()
		{
			Cmd = DB.CreateCommand();
			Cn = Cmd.Connection;

			PageSize = Settings.PageSize;
			_forumTitle = Settings.ForumTitle;
			_metaKeywords.Content = _forumTitle;
			_metaDescription.Content = _forumTitle;
			_metaDescription.Name = "description";
			_metaKeywords.Name = "keywords";
		}

		//returns "xx minutes ago" from datetime
		public static string ToAgoString(DateTime date)
		{
			return date.ToAgoString(Resources.various.SecondsAgo, Resources.various.MinutesAgo, Resources.various.HoursAgo, Resources.various.DaysAgo, "d", Utils.Various.GetCurrTime());
		}

		//this property returns the current user's ID
		public int CurrentUserID
		{
			get
			{
				//if not set OR SET TO ZERO i.e. NOT AUTHENTICATED (important - because a user can e 0 st some point and then he clicks "login" and becomes authenticated)
				if (!_currentUserId.HasValue || _currentUserId.Value == 0)
				{
					_currentUserId = Utils.User.CurrentUserID;
				}

				return _currentUserId.Value;
			}
		}
		private int? _currentUserId; //chache the user id here to save performance


		/// <summary>
		/// Is it iPhone?
		/// </summary>
		/// <returns></returns>
		protected bool IsiPhoneOrAndroid()
		{
			if (Request.QueryString["mobile"] == "0" || Session["AlwaysFullVersion"] != null)
			{
				Session["AlwaysFullVersion"] = true;
				return false;
			}

			//return true; //for testing
			if (Request.UserAgent != null)
			{
				string userAgent = Request.UserAgent.ToLower();
				return userAgent.Contains("iphone") || userAgent.Contains("ipod") || userAgent.Contains("android") || userAgent.Contains("blackberry");
			}
			return false;
		}

		protected bool IsIpad()
		{
			//return true; //for testing
			return Request.UserAgent != null && Request.UserAgent.ToLower().Contains("ipad");
		}

		private string GetCurrentPageName()
		{
			string sPath = Request.CurrentExecutionFilePath;
			return System.IO.Path.GetFileName(sPath);
		}

		public bool IsNonLoginPostBack { get; private set; }

		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);

			IsNonLoginPostBack = IsPostBack;

			bool isiPhone = IsiPhoneOrAndroid();
			if (isiPhone && Request.QueryString["rss"] != "1") //it's an iPhone
			{
				string currentPage = GetCurrentPageName();
				if (!currentPage.ToLower().Contains("-iphone")) //it's not an iPhone version of the page
				{
					string iphonePage = currentPage.Substring(0, currentPage.IndexOf(".")) + "-iphone.aspx";
					if (File.Exists(Request.MapPath(iphonePage))) //iPhone verison exists - for example "topics-iphone.aspx"
					{
						Server.Execute(iphonePage, Response.Output, true);
						Response.End();
						return;
					}
				}
			}


			//if we're Hebrew or Arabic - let's make it right-to-left
			if (!isiPhone)
			{
				string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
				if (culture == "ar" || culture == "he")
					Page.Form.Attributes.Add("dir", "rtl");
			}


			//adding application-wide error handler
			//we do it here because we have no Global.asax
			//this method has known issues in ASP.NET 2.0
			//if (!_isAppErrorHandlerSet)
			{
				//_isAppErrorHandlerSet = true;
				HttpApplication app = Context.ApplicationInstance;
				app.Error += new EventHandler(ApplicationInstance_Error);
			}

			//prevent client caching
			HttpContext.Current.Response.Cache.SetAllowResponseInBrowserHistory(false);
			HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);
			HttpContext.Current.Response.Cache.SetNoStore();
			Response.Cache.SetExpires(DateTime.Now);
			Response.Cache.SetValidUntilExpires(true);


			//check if IP is banned
			if (Utils.Settings.BannedIPs != null)
			{
				string clientIp = Utils.Various.GetUserIpAddress(Request);
				foreach (string ip in Utils.Settings.BannedIPs)
				{
					if (StringUtils.IpAddressMatchesPattern(clientIp, ip))
					{
						Response.Write("Looks like your IP-address " + clientIp + " has been banned by the forum administrator.");
						Response.End();
					}
				}
			}

			//show-hide RSS links
			if (Utils.Settings.DisableRSS)
			{
				if (Master is AspNetForumMaster)
				{
					ContentPlaceHolder cnt = ((AspNetForumMaster)Master).MainPlaceHolder;
					if (cnt != null)
					{
						HtmlAnchor rssLink = cnt.FindControl("rssLink") as HtmlAnchor;
						if (rssLink != null) rssLink.Visible = false;
					}
				}
			}

			bool integratedAuthEnabled = Utils.Settings.IntegratedAuthentication;

			//if the current forum user is UNDETERMINED
			if (CurrentUserID == 0)
			{
				//if login btn was pressed
				if (IsPostBack
					&& Request.Form["LoginName"] != null
					&& Request.Form["LoginName"] != ""
					&& Request.Form["Password"] != null
					&& Request.Form["Password"] != ""
					&& Request.Form["loginbutton"] != null)
				{
					IsNonLoginPostBack = false;

					bool passwordOk, awaitsEmailActivation;
					int userId;
					Utils.User.ProcessLogin(Request.Form["LoginName"], Request.Form["Password"], out passwordOk, out awaitsEmailActivation, out userId);
					
					if (!passwordOk)
					{
						if (Master is AspNetForumMaster && ((AspNetForumMaster)Master).LoginErrorLabel != null)
						{
							((AspNetForumMaster)Master).LoginErrorLabel.Visible = true;
						}
					}
					else if (awaitsEmailActivation)
					{
						Session["InvalidLoginUserId"] = userId;
						Response.Redirect("notactivated.aspx");
					}
					else //login successful - let's redirect to "default.aspx" but only for certain pages, like "activate.aspx" to not confuse the user
					{
						if (this is activate) Response.Redirect("default.aspx");
					}
				}
				else //user is not logged-in and it's not a postback to log him in
				{
					//if asp.net detects an authenticated user
					//and the appropriate setting in the web.config is enabled ("IntegratedAuthentication")
					if (integratedAuthEnabled)
					{
						if (Page.User.Identity.IsAuthenticated)
							Utils.User.ProcessMembershipLogin(User.Identity.Name);
					}
					//if nothing of the above is true, BUT 
					//if "remember me" cookie was found
					else
					{
						if (Request.Cookies["aspnetforumUID"] != null && Request.Cookies["aspnetforumUID"].Value != "")
						{
							Utils.User.ProcessCookieLogin();
						}
					}
				}
			}
			else //if the user IS logged in
			{
				if (integratedAuthEnabled)
				{
					//if integrated auth in enabled, but the user in not authed - let's log him out
					if (!User.Identity.IsAuthenticated)
					{
						Utils.User.Logout();
						return;
					}
					//if integrated auth in enabled, but the usernames are different - let's re-login him
					if (Session["aspnetforumUserName"].ToString() != User.Identity.Name)
					{
						Utils.User.ProcessMembershipLogin(User.Identity.Name);
					}
				}

				//lets update the user's LastLogonDate
				Utils.User.UpdateCurrentUserLastLogonDate();
			}
		}


		//application-global error handler
		void ApplicationInstance_Error(object sender, EventArgs e)
		{
			Exception ex = Server.GetLastError();

			//now here's a tricky part
			//pay attention
			//if its a TypeInitializationException it means that there was an error in some static constructor
			//which means that most likely user has forgot to run the database upgrade script or wrong connection string or smth similar
			//but even if he runs it afterwards, the exception will still "stay" there,
			//because TypeInitializationException is thrown only once. By the runtime.
			//So, let's unload/restart our app, so next time when it will be accessed -
			//it will RE-RUN static conctructors
			if (ex is TypeInitializationException)
			{
				HttpRuntime.UnloadAppDomain();
				return;
			}

			if (ex == null) return;
			if (ex is ViewStateException || ex.InnerException is ViewStateException) return;

			//send error report
			try
			{
				string errorDescr = ex.ToString();

				if (HttpContext.Current != null && HttpContext.Current.Request != null)
					errorDescr = HttpContext.Current.Request.Url + "\n\n" + errorDescr + "\n\n" + HttpContext.Current.Request.UserAgent + "\n" + HttpContext.Current.Request.UserHostAddress;

				SendNotifications.SendEmail(!string.IsNullOrEmpty(Settings.SendErrorReportsTo) ? Settings.SendErrorReportsTo.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries) : new string[] { null },
					"error in the Forum application", errorDescr, async: true);
			}
			catch { } //do nothing, we failed to send an error report
		}

		public bool IsModerator(int forumid)
		{
			if(CurrentUserID==0) return false;

			return Utils.User.IsModerator(forumid, CurrentUserID);
		}

		private static Regex _regexBody = new Regex(@"<body.*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		
		protected override void Render(HtmlTextWriter writer)
		{
			using (StringWriter stringWriter = new StringWriter())
			{
				using (HtmlTextWriter htmlWriter = new HtmlTextWriter(stringWriter))
				{
					base.Render(htmlWriter);

					string html = stringWriter.ToString();

#if TRIAL
					if (Session["HideTrialBar"] == null || ((DateTime)Session["HideTrialBar"]).AddMinutes(5) < DateTime.Now)
					{
						string text = @"<div style='padding: 5px;text-align: center;background:#FFF7B7;color:#000;font-weight:bold' id='trialdiv'>
Powered by <a style='color:#768696' href='http://www.jitbit.com/asp-net-forum/' style='color:blue' rel='nofollow'>Jitbit .Net Forum</a> free trial version.
<a href='javascript:void(0)' style='float:right;color:#768696' onclick='$(""#trialdiv"").slideUp();$.post(""ajaxutils.ashx"", {mode: ""HideTrialBar""})'>dismiss</a></div>";
						html = _regexBody.Replace(html, "$0" + text);
					}
#endif

					writer.Write(html);
				}
			}
		}

		protected override void OnPreRender(EventArgs e)
		{
			//count users
			Utils.User.UpdateOnlineUsersCount();

			base.OnPreRender(e);
			
			//now adding meta tags
			if (Header != null)
			{
				//only if there're no meta tags already added explicitly
				if (FindControlByTypeRecursive<HtmlMeta>(Page) == null)
				{
					Header.Controls.Add(_metaDescription);
					Header.Controls.Add(_metaKeywords);
				}
			}

			//title
			Title += (string.IsNullOrEmpty(Title) ? "" : " - ") + _forumTitle;
		}

		public static void AssignButtonTextboxEnterKey(TextBox textbox, Button button)
		{
			AssignButtonTextboxEnterKey(textbox, button.ClientID);
		}

		public static void AssignButtonTextboxEnterKey(TextBox textbox, string buttonClientId)
		{
			string script = "if(event.which || event.keyCode){if ((event.which == 13) || (event.keyCode == 13)) {document.getElementById('" + buttonClientId + "').click();return false;}} else {return true};";
			textbox.Attributes.Add("onkeypress", script);
		}

		private static T FindControlByTypeRecursive<T>(Control parent) where T : Control
		{
			if (parent is T) return (T)parent;

			foreach (Control c in parent.Controls)
			{
				T found = FindControlByTypeRecursive<T>(c);
				if (found != null) return found;
			}
			return null;
		}

		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);

			//cleanup
			Cmd.Dispose();
			Cn.Dispose();
		}
	}
}
