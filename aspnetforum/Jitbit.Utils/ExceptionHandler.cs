using System;
using System.Configuration;
using System.Reflection;
using System.Web;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace Jitbit.Utils
{
	public static class ExceptionHandler
	{
		//writes an exception to an application log
		public static void LogException(Exception ex)
		{
			LogException("general error", ex);
		}

		private static string GetAppName()
		{
			return Assembly.GetExecutingAssembly().GetName().Name;
		}

		public static void LogException(string errorMsg, Exception ex)
		{
			if (ConfigurationManager.AppSettings["LogErrors"] == "false")
				return;

			StringBuilder err = new StringBuilder();
			err.Append("\n\n");
			err.Append("Error in " + GetAppName() + ": ");
			err.Append(errorMsg);
			err.Append("\n\n");
			err.Append(ex.ToString());

			try
			{
				EventLog.WriteEntry("Application", err.ToString(), EventLogEntryType.Warning);
			}
			catch { }
		}

		public static void LogDebugInfo(string msg)
		{
			try
			{
				EventLog.WriteEntry("Application", msg, EventLogEntryType.Information);
			}
			catch { }
		}

		//sends error report to an address specified in the web.config
		public static void SendErrorReport(Exception ex)
		{
			SendErrorReport(ex.ToString());
		}

		public static void SendErrorReport(string report, string[] emails = null)
		{
			if (ConfigurationManager.AppSettings["SendEmailErrorReports"] != "true")
				return;

			string subject = "Error in the " + GetAppName() + " application";
			string body = report;
			try {
				if (HttpContext.Current != null)
					body = "URL: " + HttpContext.Current.Request.Url +
						"\nMethod: " + HttpContext.Current.Request.HttpMethod +
						"\nReferrer: " + HttpContext.Current.Request.UrlReferrer.AbsoluteUri +
						"\n\n" + body + "\n\nDo not reply to this email. If you need help with this error, please forward this message to support@jitbit.com";
			} catch { }

			if (emails == null) emails = ConfigurationManager.AppSettings["EmailErrorTo"].Split(new[] {',', ';'}, StringSplitOptions.RemoveEmptyEntries);

			TrySend(emails, "", subject, body );
		}

		private static void TrySend(
			string[] to,
			string fromName,
			string subject,
			string body
			)
		{
			try
			{
				AsyncSendMail asm = new AsyncSendMail(
					ConfigurationManager.AppSettings["EmailErrorSMTPHost"],
					ConfigurationManager.AppSettings["EmailErrorSMTPUser"] != "",
					ConfigurationManager.AppSettings["EmailErrorSMTPUser"],
					ConfigurationManager.AppSettings["EmailErrorSMTPPassword"],
					int.Parse( ConfigurationManager.AppSettings["EmailErrorSMTPPort"] ),
					ConfigurationManager.AppSettings["EmailErrorUseSSL"] == "true",
					to,
					ConfigurationManager.AppSettings["EmailErrorFrom"],
					fromName,
					subject,
					body,
					null,
					false );
				asm.SendAsynchronously();
			}
			catch { }
		}

		/// <summary>
		/// Renders pretty error page for any exception
		/// </summary>
		/// <param name="message">Exception.message</param>
		public static void RenderErrorPage(string message, bool showSEOLinks = false)
		{
			HttpContext context = HttpContext.Current;
			if (context != null)
			{
				context.Response.Write(@"
				<!DOCTYPE html>
				<html xmlns='http://www.w3.org/1999/xhtml' xml:lang='en' lang='en'>
				<head>
					<title>Jitbit Helpdesk Error</title>
				<style>
				html,button,input,select,textarea{font-family:sans-serif;color:#222;font-size:13px;margin:0}
				body{background-image: url(data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAgAAAAIBAMAAAA2IaO4AAAAFVBMVEXv7e/f3N/w7vDw7/Df3d/n5ufv7u80BVBNAAAALUlEQVR4XiXIQQ0AIBADwX2hAA0noQpqoRbqXwIEfpOhrmkatiwuyvo3wfK8O8FECYnjynsqAAAAAElFTkSuQmCC);}
				a{color:#1566ad;text-decoration:none;-webkit-transition:background-color .15s,color .15s,border .15s;-moz-transition:background-color .15s,color .15s,border .15s;transition:background-color .15s,color .15s,border .15s}
				.outerroundedbox{border:1px solid #d2d5d7;-moz-border-radius:5px;-webkit-border-radius:5px;border-radius:5px;background-color:#fafafa;background:#fafafa;margin-bottom:20px;padding:20px;box-shadow:0 3px 6px -2px #ddd}
				.center{margin-left:auto;margin-right:auto}
				</style>
				</head>
				<body>
				<div class='outerroundedbox center' style='width:400px;margin-top:40px'>");
				context.Response.Write("<h3>" + message + "</h3>");
				if(showSEOLinks){
					context.Response.Write("<br/>Please contact the application administrator.<br /><br /><span class='grey'><a href='http://www.jitbit.com/hosted-helpdesk/'>Hosted Helpdesk</a> | <a href='http://www.jitbit.com/hosted-crm/'>Hosted CRM</a></span>");
				}
				context.Response.Write("</div></body></html>");
				context.Response.End();
			}
		}

	}
}
