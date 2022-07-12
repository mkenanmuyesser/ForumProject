using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Net;
using System.Web;
using System.IO;

namespace Jitbit.Utils
{
	public interface IFileAttachment
	{
		string FileName { get; set; }
		byte[] FileData { get; set; }
	}

	public class AsyncSendMail
	{
		public delegate void ErrorCallback(Exception ex); //error callback

		string _smtpServerAddress;
		string _smtpLogin;
		string _smtpPassword;
		int _smtpPort;
		bool _smtpAuthentication = false;
		bool _smtpUseSSL = false;
		string[] _recipients;
		string _subject;
		string _fromEmail;
		string _fromName;
		string _body;
		string _replyto;
		bool _htmlBody;
		IEnumerable<IFileAttachment> _attachments;
		bool _separateEmailToEachRecipient;
		string _plainTextBody;

		//constructor
		public AsyncSendMail(
			string smtpServerAddress, bool smtpAuthentication, string smtpLogin, string smtpPassword, int smtpPort, bool smtpUseSSL, string[] recipients,
			string from, string fromName, string subject, string body, string replyto, bool htmlBody, IEnumerable<IFileAttachment> attachments = null, bool separateEmailToEachRecipient = true,
			string plainTextBody = null)
		{
			_smtpServerAddress = smtpServerAddress;
			_smtpAuthentication = smtpAuthentication;
			_smtpLogin = smtpLogin;
			_smtpPassword = smtpPassword;
			_smtpUseSSL = smtpUseSSL;
			_recipients = recipients;
			_subject = subject;
			_fromEmail = from;
			_fromName = fromName;
			_body = body;
			_smtpPort = smtpPort;
			_replyto = replyto;
			_htmlBody = htmlBody;
			_attachments = attachments;
			_separateEmailToEachRecipient = separateEmailToEachRecipient;
			_plainTextBody = plainTextBody;
		}

		public void SendAsynchronously(ErrorCallback onError = null, bool suppressSMTPErrorNotification = false)
		{
			Thread thread = new Thread(() => SendThreadProc(onError, suppressSMTPErrorNotification));
			thread.Priority = ThreadPriority.Normal;
			thread.Start();
		}

		public void SendSynchronously(bool suppressSMTPErrorNotification = false)
		{
			if (!_separateEmailToEachRecipient)
				SendEmailsInternal(_recipients, null, suppressSMTPErrorNotification, calledAsynchronously: false);
			else
			{
				foreach (var rec in _recipients)
				{
					SendEmailsInternal(new[] { rec }, null, suppressSMTPErrorNotification, calledAsynchronously: false);
				}
			}
		}

		//thread proc (for async send)
		private void SendThreadProc(ErrorCallback onError = null, bool suppressSMTPErrorNotification = false)
		{
			if (!_separateEmailToEachRecipient)
				SendEmailsInternal(_recipients, onError, suppressSMTPErrorNotification, calledAsynchronously: true);
			else
			{
				foreach (var rec in _recipients)
				{
					SendEmailsInternal(new[] { rec }, onError, suppressSMTPErrorNotification, calledAsynchronously: true);
				}
			}
		}

		//internal proc - sends emails
		private void SendEmailsInternal(string[] recipients, ErrorCallback onError = null, bool suppressSMTPErrorNotification = false, bool calledAsynchronously = false)
		{
			MailMessage msg = ConstructMailMessage(recipients);
			if (msg == null) return; //message constructing error. means incorrect TO-address format or something. fuck it.

			using (SmtpClient smtp = new SmtpClient())
			{
				try
				{
					if (_smtpAuthentication)
					{
						smtp.Credentials = new NetworkCredential(_smtpLogin, _smtpPassword);
					}

					smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
					smtp.Host = _smtpServerAddress;
					smtp.Port = _smtpPort;
					smtp.EnableSsl = _smtpUseSSL;
				}
				catch
				{
					//faulty properties (invalid port, empty hostname etc.)
					return;
				}

				ServicePointManager.ServerCertificateValidationCallback = delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };

				try
				{
					smtp.Send(msg);
					smtp.Dispose();
					//msg.Dispose();
				}
				catch (Exception ex) //ERROR
				{
					if (calledAsynchronously) //lets save this msg, to try again later
					{
						UnsentMsg m = new UnsentMsg(msg, _smtpServerAddress, _smtpLogin, _smtpPassword, _smtpPort, _smtpAuthentication, _smtpUseSSL, !suppressSMTPErrorNotification);
						_failedMessagesList.Add(m);

						if (onError != null)
							onError(ex);
					}
					else
					{
						Exception mailex = new Exception(string.Format("Error sending email from server {0}, port {1}, login {2}", _smtpServerAddress, _smtpPort, _smtpLogin), ex);
						ExceptionHandler.LogException("", mailex);
						if (!suppressSMTPErrorNotification)
							ExceptionHandler.SendErrorReport(mailex);

						throw (ex); //throw exception anyway - we're being called SYNCHRONOUSLY
					}
				}
			}
		}

		private MailMessage ConstructMailMessage(string[] recipients)
		{
			//try to parse the from address
			try
			{
				if (_fromName == null)
					new MailAddress(_fromEmail);
				else
					new MailAddress(_fromEmail, _fromName);
			}
			catch { return null; } //address parsing error

			//if empty recipients list
			if (recipients.Length == 0) return null;

			MailMessage msg = new MailMessage();

			foreach (string to in recipients)
			{
				if (to.Trim().Length == 0) continue;
				if (MailAddressTryParse(to))
					msg.To.Add(to);
			}

			if (msg.To.Count == 0) return null; //if empty recipients list

			if (!string.IsNullOrEmpty(_fromName))
				msg.From = new MailAddress(_fromEmail, _fromName);
			else
				msg.From = new MailAddress(_fromEmail); ;

			//msg.To = _fromEmail;
			msg.Subject = _subject;
			msg.SubjectEncoding = Encoding.UTF8; 

			msg.BodyEncoding = Encoding.UTF8;
			msg.Headers.Add("X-Mailer", "JitbitHelpdesk");
			msg.Headers.Add("X-Auto-Response-Suppress", "all");

			//adding plain text part anyway
			if (_plainTextBody == null) _plainTextBody = Jitbit.Utils.StringUtils.StripHTML(_body).Trim();
			AlternateView plainView = AlternateView.CreateAlternateViewFromString("--reply above this line--\r\n\r\n" + _plainTextBody, null, "text/plain");
			msg.AlternateViews.Add(plainView);

			//adding html part if needed. Done via Alternate view for proper MIME type
			if (_htmlBody)
			{
				string htmlText = "<div style='color:grey;font-size:8pt'>--reply above this line--</div><br/>" + _body;
				AlternateView html = AlternateView.CreateAlternateViewFromString(htmlText, null, "text/html");
				msg.AlternateViews.Add(html);
			}

			if (_attachments != null)
			{
				foreach (var att in _attachments)
				{
					if (att.FileData != null)
					{
						var stream = new MemoryStream();
						stream.Write(att.FileData, 0, att.FileData.Length);
						stream.Position = 0;
						msg.Attachments.Add(new Attachment(stream, att.FileName));
					}
				}
			}

			if (_replyto != null && _replyto.Trim().Length > 0)
			{
				try
				{
					MailAddress replyToAddress = new MailAddress(_replyto);
					msg.ReplyTo = replyToAddress;
				}
				catch { } //address parsing error - do nothing
			}

			return msg;
		}

		public static bool MailAddressTryParse(string address)
		{
			try
			{
				MailAddress toAddr = new MailAddress(address);
				return true;
			}
			catch
			{
				return false; //mail address parsing error
			}
		}

		public class UnsentMsg
		{
			public MailMessage Message { get; private set; }
			public int NumberOfFailures { get; set; }
			public string SmtpServerAddress { get; private set; }
			public string SmtpLogin { get; private set; }
			public string SmtpPassword { get; private set; }
			public int SmtpPort { get; private set; }
			public bool SmtpAuthentication { get; private set; }
			public bool SmtpUseSSL { get; private set; }
			public bool SendErrorNotificationIfFails { get; private set; }

			public UnsentMsg(MailMessage message, string smtpServerAddress, string smtpLogin, string smtpPassword, int smtpPort, bool smtpAuthentication, bool smtpUseSSL, bool sendErrorNotificationIfFails = true)
			{
				SmtpServerAddress = smtpServerAddress;
				SmtpAuthentication = smtpAuthentication;
				SmtpLogin = smtpLogin;
				SmtpPassword = smtpPassword;
				SmtpUseSSL = smtpUseSSL;
				SmtpPort = smtpPort;
				Message = message;
				SendErrorNotificationIfFails = sendErrorNotificationIfFails;
			}
		}

		public static List<UnsentMsg> _failedMessagesList = new List<UnsentMsg>();
		private static Timer _resendTimer = new Timer(new TimerCallback(ResendFailed), null, 60000, 120000);
		private static object _lockObject = new Object();
		public static void ResendFailed(object state)
		{
			lock (_lockObject)
			{
				for (int i = _failedMessagesList.Count-1; i >= 0; i--)
				{
					//try to resnd
					var m = _failedMessagesList[i];
					using (SmtpClient smtp = new SmtpClient())
					{
						if (m.SmtpAuthentication)
						{
							smtp.Credentials = new NetworkCredential(m.SmtpLogin, m.SmtpPassword);
						}

						smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
						smtp.Host = m.SmtpServerAddress;
						smtp.Port = m.SmtpPort;
						smtp.EnableSsl = m.SmtpUseSSL;

						try
						{
							smtp.Send(m.Message);
							_failedMessagesList.RemoveAt(i);
							m.Message.Dispose();
						}
						catch (Exception ex) //ERROR - lets increase the error counter
						{
							m.NumberOfFailures++;
							if (m.NumberOfFailures > 5)
							{
								if (m.SendErrorNotificationIfFails) //send error report
								{
									Exception mailex = new Exception(string.Format("Error sending email from server (tried 5 times!) {0}, port {1}, login {2}", m.SmtpServerAddress, m.SmtpPort, m.SmtpLogin), ex);
									ExceptionHandler.LogException("", mailex);
									ExceptionHandler.SendErrorReport(mailex);
								}

								//remove the msg
								_failedMessagesList.RemoveAt(i);
							}
						}
					}
				}
			}
		}
	}
}