using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using System.Web.WebPages;
using System.Net.NetworkInformation;

namespace Jitbit.Utils
{
	public static class WebHelpers
	{
		/// <summary>
		/// Is it iOS or Android? (ipads return true on this one!!)
		/// </summary>
		/// <returns></returns>
		[Obsolete("Use IsIosOrAndroid")]
		public static bool IsiPhoneOrAndroid(this HttpRequestBase request)
		{
			return IsIosOrAndroid(request);
		}

		public static bool IsSelfHostedTrial(this HtmlHelper htmlHelper)
		{
#if TRIAL
			return true;
#else
			return false;
#endif
		}

		/// <summary>
		/// Is it iOS or Android? (ipads return true on this one!!)
		/// </summary>
		/// <returns></returns>
		public static bool IsIosOrAndroid(this HttpRequestBase request)
		{
			//return true; //for testing
			if (request.UserAgent != null)
			{
				string userAgent = request.UserAgent.ToLower();
				return userAgent.Contains("iphone") || userAgent.Contains("ipod") || userAgent.Contains("android") || userAgent.Contains("ipad");
			}
			return false;
		}

		private static bool? _pingOk = null; //cache in a static var
		public static bool IsGoogleAvailable()
		{
			if (!_pingOk.HasValue)
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.google.com/");
				request.Timeout = 2000;
				try
				{
					request.GetResponse();
					_pingOk = true;
				}
				catch
				{
					_pingOk = false;
				}
			}
			return _pingOk.Value;
		}

		//this method is used for responsive web design primarely. For now the same as above excluding iPad 
		public static bool IsSmartphone(this HttpRequestBase request)
		{
			var cached = request.RequestContext.HttpContext.Items["mobile"];
			if (cached != null)
				return (bool)cached;

			bool result = false;

			if (request.Browser != null && request.Browser.IsMobileDevice && request.Browser.MobileDeviceModel != "IPad")
				result = true;
			else if (request.UserAgent != null)
			{
				string userAgent = request.UserAgent.ToLower();
				result = userAgent.Contains("iphone") || userAgent.Contains("ipod") || userAgent.Contains("android");
			}

			//cache
			request.RequestContext.HttpContext.Items["mobile"] = result;

			return result;
		}

		//renders Ations link WITH HTML inside it
		public static MvcHtmlString RawActionLink(this AjaxHelper ajaxHelper, string linkText, string actionName, string controllerName, object routeValues, AjaxOptions ajaxOptions, object htmlAttributes)
		{
			var repID = Guid.NewGuid().ToString();
			var lnk = ajaxHelper.ActionLink(repID, actionName, controllerName, routeValues, ajaxOptions, htmlAttributes);
			return MvcHtmlString.Create(lnk.ToString().Replace(repID, linkText));
		}

		//return strribute string from dictionary
		public static string ToAttributeString(object htmlAttributes)
		{
			string resultFormat = "{0}=\"{1}\" ";
			StringBuilder sb = new StringBuilder();
			var attributeHash = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
			foreach (string attribute in attributeHash.Keys)
			{
				sb.AppendFormat(resultFormat, attribute, attributeHash[attribute]);
			}
			return sb.ToString();
		}

		/// <summary>
		/// get a local recose using virtual path. If not found - this means it's first launch, we need to simulate a call to a root page
		/// </summary>
		public static IHtmlString Resource(this HtmlHelper htmlHelper, string virtualPath, string resourceKey)
		{
			string retval = "";
			try
			{
				retval = HttpContext.GetLocalResourceObject(virtualPath, resourceKey).ToString();
			}
			catch (NullReferenceException ex)
			{
				throw new Exception(resourceKey + " not found", ex);
			}
			catch (InvalidOperationException ex)
			{
				//resource not found. maybe its because they have not been compiled yet.
				//so, to force compilation lets launch a dumb page request
				try
				{
					string url = new Uri(HttpContext.Current.Request.Url, VirtualPathUtility.ToAbsolute("~/default.aspx")).ToString();
					WebClient wc = new WebClient();
					wc.DownloadData(url);
				}
				catch { } //fuck it

				//second try
				try
				{
					retval = HttpContext.GetLocalResourceObject(virtualPath, resourceKey).ToString();
				}
				catch { } //ok, fuck it
			}
			return MvcHtmlString.Create(retval); //to prevent encoding
		}

		/// <summary>
		/// adds script to a partial view ONLY ONCE
		/// </summary>
		/// <param name="path">script path</param>
		public static IHtmlString AddScriptFile(this HtmlHelper htmlHelper, string path)
		{
			string scriptkey = "js" + CryptoUtils.MD5Hash(path);
			if (htmlHelper.ViewContext.HttpContext.Items[scriptkey] != null) //already exists on page
				return MvcHtmlString.Create("");
			else
			{
				htmlHelper.ViewContext.HttpContext.Items.Add(scriptkey, true);
				return MvcHtmlString.Create("<script type='text/javascript' src='" + path + "'></script>");
			}
		}

		/// <summary>
		/// adds CSS to a partial view ONLY ONCE
		/// requires adding "@Html.CssStyles().Render()" to the <head> in _layout
		/// </summary>
		public static CssHelper CssStyles(this HtmlHelper htmlHelper)
		{
			return CssHelper.GetInstance(htmlHelper);
		}

		public class CssHelper
		{
			private List<string> _cssUrls;

			private CssHelper()
			{
				_cssUrls = new List<string>();
			}

			public static CssHelper GetInstance(HtmlHelper htmlHelper)
			{
				var instanceKey = "AssetsHelperInstance";

				var context = htmlHelper.ViewContext.HttpContext;
				if (context == null) return null;

				var cssHelper = (CssHelper) context.Items[instanceKey];

				if (cssHelper == null)
					context.Items.Add(instanceKey, cssHelper = new CssHelper());

				return cssHelper;
			}

			public void AddStyle(string url)
			{
				if (!_cssUrls.Contains(url))
					_cssUrls.Add(url);
			}

			public IHtmlString Render()
			{
				StringBuilder sb = new StringBuilder();
				foreach (var u in _cssUrls)
				{
					sb.Append("<link href=\"" + u + "\" type=\"text/css\" rel=\"Stylesheet\" />");
				}
				return MvcHtmlString.Create(sb.ToString());
			}
		}

		//this is a workaround for razor helpers see http://stackoverflow.com/questions/4710853/using-mvc-htmlhelper-extensions-from-razor-declarative-views
		public static HtmlHelper GetPageHelper(this System.Web.WebPages.Html.HtmlHelper html)
		{
			return ((WebViewPage)WebPageContext.Current.Page).Html;
		}

		//get absolute url for an action
		public static string AbsoluteAction(this UrlHelper url, string actionName, string controllerName, object routeValues)
		{
			return url.Action(actionName, controllerName, routeValues, url.RequestContext.HttpContext.Request.Url.Scheme);
		}
	}
}