using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
//using System.Net.Mail;
using System.Threading;
using Limilabs.Mail;
using Limilabs.Client.POP3;
using Limilabs.Client.IMAP;


namespace Jitbit.Utils
{
	public enum ServerType
	{
		POP,
		IMAP
	}

	public class EmailAttachment
	{
		public string FileName { get; set; }
		public byte[] FileData { get; set; }
		public string ContentId { get; set; }
	}

	public static class EmailProcessingHelpers
	{
		//quoted-email tools

		//---------xxxx------------ (i.e. ---Original message---)
		static Regex rOriginalMessage = new Regex(@"\n\s*---+.+---+\r?\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		//On XXX, XXX wrote:
		static Regex rOnXxxWrote = new Regex(@"\n-*\s*On\s.+\swrote:\r?\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		//[b]FROM:[/b] something (bold tags can be or not be there)
		static Regex rFromXXX = new Regex(@"^\s*(\[b\])?from(\[/b\])?:\s*.*", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

		//matches two lines in a row which start with ">"
		static Regex rBlockqoute = new Regex(@"^\s*>.*\n\s*>.*", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

		//From: Alex <jitbit@gmil.com>
		static Regex rOriginalFrom = new Regex(@"from:(\[/b\])?\s(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		//finding emails like "support@BLA.jitbit.com" (used in hosted version)
		static Regex rPusherEmail = new Regex(@"(support|crm)@.*?\.jitbit\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static Regex _rUnclosedBbCodeAtTheEnd = new Regex(@"\[[^/]+?]\s*$", RegexOptions.Compiled);
		public static string RemoveQuotedReplyFromBBCodeEmail(string input)
		{
			string body = input;

			//first - search for "reply above this line"
			int pos = body.IndexOf("--reply above this line--");
			if (pos > -1)
			{
				body = body.Substring(0, pos);
			}

			//searching for "-----Original Message------"
			Match m = rOriginalMessage.Match(body);
			if (m.Success)
			{
				body = body.Substring(0, m.Index);
			}

			//searching for "On xxxx xxxx wrote:
			m = rOnXxxWrote.Match(body);
			if (m.Success)
			{
				body = body.Substring(0, m.Index);
			}

			//searching for "[b]From:[/b] xxx [mailto:xxxxxx]"
			m = rFromXXX.Match(body);
			if (m.Success)
			{
				body = body.Substring(0, m.Index);
			}

			//removing quoted lines by searching for ">" char - removing ">blablabla" lines
			//if it finds two lines in a row that start with ">" - removes everything after it
			m = rBlockqoute.Match(body);
			if (m.Success)
			{
				body = body.Substring(0, m.Index);
			}

			//look for unclosed [*] tags, that can screw up markup, can happen after removing quoting
			body = _rUnclosedBbCodeAtTheEnd.Replace(body, "");

			//look for unclosed [table] tags explicitly, that can screw up markup, can happen after removing quoting
			var diff = StringUtils.CountOccurences("[table]", body) - StringUtils.CountOccurences("[/table]", body);
			if (diff > 0)
				for (int i = 0; i < diff; i++)
					body += "[/table]";

			//remove trailing new line
			return body.TrimEnd();
		}

		//sometimes quoting can be found in HTML-version only
		private static string RemoveQuotedReplyFromHTMLEmail(string input)
		{
			string body = input;

			//first - search for "reply above this line"
			int pos = body.IndexOf("--reply above this line--");
			if (pos > -1)
			{
				//FOUND!! so do nothing!!!! because there'll be another attempt to remove quoting from BBCODEd email and it will use the --reply above-- text
				return body;
			}

			pos = body.ToLower().IndexOf("class=\"gmail_quote\"");
			if (pos == -1)
				pos = body.ToLower().IndexOf("class=3d\"gmail_quote\"");

			if (pos > -1)
			{
				body = body.Substring(0, pos);
				pos = body.LastIndexOf("<");
				body = body.Substring(0, pos);
			}

			return body.TrimEnd();
		}

		/// <summary>
		/// this method cobines TO and CC addresses, then removes "our own addresses" (addresses checked by pop/imap, "from" addresses of the app etc)
		/// </summary>
		public static IEnumerable<string> DeriveRecipientList(IEnumerable<string> to, IEnumerable<string> cc, string ownEmail, string fromAddress)
		{
			string webconfigAddress = ConfigurationManager.AppSettings["EmailErrorFrom"] ?? "";

			var ownEmails = new[] { ownEmail, webconfigAddress, fromAddress };

			IEnumerable<string> result;

			//combine TO and CC
			if (to.Count() > 1)
				result = cc.Concat(to);
			else
				result = cc;

			//remove "our own emails"
			result = result.Except(ownEmails).Distinct();

			//remove "pusher" address (used in hosted verson) and empty lines (just in case)
			result = result.Where(email => !rPusherEmail.IsMatch(email) && !string.IsNullOrEmpty(email));

			return result;
		}

		public static string GetEmailBBCodeBody(IMail msg, bool removeQuotedText)
		{
			bool isHtml = msg.IsHtml;
			string body = isHtml ? msg.Html : msg.Text;

			if (isHtml || StringUtils.HasCommonHtmlTags(body))
			{
				if (removeQuotedText) //"#ticket#" text is found in the subject, so it is a reply.
				{
					//lets try to get rid of the quoted msg (sometimes it can be done in HTML version only)
					body = RemoveQuotedReplyFromHTMLEmail(body);
				}

				//now let's ged rid of all BBCODE-incompatible html
				body = StringUtils.HTML2BBCode(body);
			}
			else //not HTML, but le't still escape bbcode
			{
				body = StringUtils.EscapeBbCode(body);
			}

			if (removeQuotedText)
				body = RemoveQuotedReplyFromBBCodeEmail(body);

			//get rid of too many CRs (to make it look nice)
			body = StringUtils.RemoveRepeatingCRLFs(body);

			return body;
		}

		public static bool IsForwardedMessage(string subject, string bbCodeBody, out System.Net.Mail.MailAddress originalFrom)
		{
			originalFrom = null;

			if (subject == null || bbCodeBody == null) return false;

			//is forwarded?
			bool isForwarded = subject.StartsWith("fw:", StringComparison.OrdinalIgnoreCase) ||
				subject.StartsWith("fwd:", StringComparison.OrdinalIgnoreCase);

			if (isForwarded)
			{
				//searching for "From: John Smith <john@gmail.com>"
				Match m = rOriginalFrom.Match(bbCodeBody);
				if (m.Success)
				{
					string fromStr = m.Groups[2].ToString().Trim();
					if (fromStr.StartsWith("<")) fromStr = fromStr.Replace("<", "").Replace(">", ""); //sometimes it's just "<john@gmail.com>"

					try { originalFrom = new System.Net.Mail.MailAddress(fromStr); }
					catch
					{
						try
						{
							fromStr = StringUtils.StripBBCode(fromStr); //let's strip bbcode and try again
							if (fromStr.StartsWith("<")) fromStr = fromStr.Replace("<", "").Replace(">", ""); //sometimes it's just "<john@gmail.com>"
							originalFrom = new System.Net.Mail.MailAddress(fromStr);
						}
						catch { }
					}
				}
			}
			return isForwarded;
		}

		public static bool IsUselessMessage(IMail msg)
		{
			//is it a bounce email?
			var bounce = new Limilabs.Mail.Tools.Bounce();
			var result = bounce.Examine(msg);
			var useless = result.IsDeliveryFailure;

			if(!useless) useless = IsUselessMessage(msg.From[0].Address, msg.From[0].Name, msg.Subject, msg.Headers);

			return useless;


			//loop through all entities and if some entity has ContentType "message/delivery-status" - its probably an NDR
			/*if (!useless)
			{
				if (msg.AllEntities != null)
				{
					foreach (var e in msg.AllEntities)
					{
						if (e.ContentType.TypeWithSubtype == MIME_MediaTypes.Message.delivery_status)
							return true;
					}
				}
			}*/
			//temporary commented this out, to see if helps cure the missing emails problem

		}

		private static bool IsUselessMessage(string from, string fromName, string subject, Limilabs.Mail.Headers.HeaderCollection headers)
		{
			//caching
			string lSubject = (subject == null ? "" : subject.ToLower());
			string lFrom = (from == null ? "" : from.ToLower());
			string lFromName = (fromName == null ? "" : fromName.ToLower());

			bool useless = lSubject.IndexOf("delivery status") > -1 || lSubject.Contains("delivery has failed") || lSubject.Contains("out of office") || lSubject.Contains("out of the office") || lSubject.Contains("out-of-office");

			useless |= lSubject.StartsWith("automatic reply") || lSubject.StartsWith("[auto-reply]") || lSubject.StartsWith("automatische antwort") || lSubject.StartsWith("automatisch antwoord") || lSubject.StartsWith("respuesta automática");

			useless |= (lFrom.IndexOf("mailer-daemon") > -1 || lFrom.StartsWith("postmaster@"));

			useless |= (lFromName.IndexOf("mail delivery subsystem") > -1);

			useless |= (headers != null)
				&& (headers.AllKeys.Contains("X-Failed-Recipients") || headers.AllKeys.Contains("x-failed-recipients"));

			useless |= (headers != null)
				&& (headers.AllKeys.Contains("X-Autoreply") || headers.AllKeys.Contains("x-autoreply"));

			useless |= (headers != null)
				&& (headers.AllKeys.Contains("X-Autorespond") || headers.AllKeys.Contains("x-autorespond"));

			useless |= rPusherEmail.IsMatch(lFrom); //email came from the hosted helpdesk?

			//sometimes header names are converted to lowercase... AND I SPENT FRIGGIN WEEK DEBUGGIN THIS! FUCK!  .. uhm, sory
			if (!useless && headers != null)
			{
				string xmailerHeader = "";
				if (headers.AllKeys.Contains("X-Mailer"))
					xmailerHeader = headers["X-Mailer"];
				if (headers.AllKeys.Contains("x-mailer"))
					xmailerHeader = headers["x-mailer"];

				useless = xmailerHeader.ToLower().IndexOf("jitbit") > -1;
			}

			return useless;
		}

		private static Pop3 GetPopClient(string server, int port, bool useSSL, string login, string password)
		{
			var client = new Pop3();
			client.ServerCertificateValidate +=
				new Limilabs.Client.ServerCertificateValidateEventHandler(
					delegate(object sender, Limilabs.Client.ServerCertificateValidateEventArgs e)
					{
						e.IsValid = true;
					});

			client.Connect(server, port, useSSL);
			client.Login(login, password);
			return client;
		}

		private static Imap GetImapClient(string server, int port, bool useSSL, string login, string password)
		{
			var client = new Imap();
			client.ServerCertificateValidate +=
				new Limilabs.Client.ServerCertificateValidateEventHandler(
					delegate(object sender, Limilabs.Client.ServerCertificateValidateEventArgs e)
					{
						e.IsValid = true;
					});

			client.Connect(server, port, useSSL);
			client.Login(login, password);
			client.SelectInbox();
			return client;
		}

		internal static void TestPop3(string server, int port, string login, string password, bool useSSL)
		{
			var client = GetPopClient(server, port, useSSL, login, password);
			client.GetAll();
			client.Close();
		}

		internal static void TestImap(string server, int port, string login, string password, bool useSSL)
		{
			var client = GetImapClient(server, port, useSSL, login, password);
			client.Noop();
			client.Close();
		}

		/// <summary>
		/// This method contains hacks and fixes we found to use for some broken emails. Mostly from Outlook and Exchange. 
		/// </summary>
		/// <param name="message">Email message</param>
		/// <returns>Message as a byte array, which you can pass straight to Mail_Message.ParseFromByte</returns>
		public static string PrepareEmailForParsing(string message)
		{
			//making sure all line endings are the same
			message = message.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");

			//Fixes bad content-type header. More info:
			//http://www.lumisoft.ee/Forum/yaf_postsm9308_POP3-ClientMessage-ParseFromByte-problem.aspx#post9308
			message = message.Replace(";\r\nboundary=", ";boundary=");
			message = message.Replace(";\r\ntype=", ";type=");

			return message;
		}
		public static byte[] PrepareEmailForParsing(byte[] message)
		{
			return Encoding.UTF8.GetBytes(PrepareEmailForParsing(System.Text.Encoding.UTF8.GetString(message)));
		}

		public static byte[] PrepareEmailForParsing(Stream message)
		{
			var memoryStream = new MemoryStream();
			message.CopyTo(memoryStream);
			return PrepareEmailForParsing(memoryStream.ToArray());
		}

		public static List<IMail> GetNext5Emails(ServerType serverType, string server, int port, bool useSSL, string login, string password)
		{
			var messages = new List<IMail>();

			if (ConfigurationManager.AppSettings["DisableMailChecker"] != null) return messages; //debug setting from web.config... used this to move to a new server... don't mind

			if (serverType == ServerType.POP)
			{
				using (var popClient = GetPopClient(server, port, useSSL, login, password))
				{
					MailBuilder builder = new MailBuilder();
					foreach (var uid in popClient.GetAll().Take(5))
					{
						var msg = builder.CreateFromEml(popClient.GetMessageByUID(uid));
						messages.Add(msg);
						popClient.DeleteMessageByUID(uid);
					}
					popClient.Close();
				}
			}
			else //imap
			{
				using (var client = GetImapClient(server, port, useSSL, login, password))
				{
					var uids = client.Search(Flag.Unseen).Take(5);

					foreach (var uid in uids)
					{
						var msg = new MailBuilder().CreateFromEml(client.GetMessageByUID(uid));
						messages.Add(msg);
						client.DeleteMessageByUID(uid);
					}
				}
			}

			return messages;
		}

		public static List<EmailAttachment> GetAttachmentsFromMessage(IMail msg)
		{
			List<EmailAttachment> attachments = new List<EmailAttachment>();

			foreach (var attachment in msg.Attachments)
			{
				string filename = "noname";
				if (attachment.FileName != null)
					filename = attachment.SafeFileName;

				//removing Outlook garbage attachements
				Regex outlook_garbage = new Regex("ATT\\d*.+html?", RegexOptions.IgnoreCase); //like ATT00001.htm
				if (outlook_garbage.IsMatch(filename))
					continue;

				attachments.Add(new EmailAttachment() { FileName = filename, FileData = attachment.Data, ContentId = attachment.ContentId });
			}

			return attachments;
		}

		private static IDictionary<string, string> _mimemappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {

		#region Big freaking list of mime types
		// combination of values from Windows 7 Registry and 
		// from C:\Windows\System32\inetsrv\config\applicationHost.config
		// some added, including .7z and .dat
		{".323", "text/h323"},
		{".3g2", "video/3gpp2"},
		{".3gp", "video/3gpp"},
		{".3gp2", "video/3gpp2"},
		{".3gpp", "video/3gpp"},
		{".7z", "application/x-7z-compressed"},
		{".aa", "audio/audible"},
		{".aac", "audio/aac"},
		{".aaf", "application/octet-stream"},
		{".aax", "audio/vnd.audible.aax"},
		{".ac3", "audio/ac3"},
		{".aca", "application/octet-stream"},
		{".accda", "application/msaccess.addin"},
		{".accdb", "application/msaccess"},
		{".accdc", "application/msaccess.cab"},
		{".accde", "application/msaccess"},
		{".accdr", "application/msaccess.runtime"},
		{".accdt", "application/msaccess"},
		{".accdw", "application/msaccess.webapplication"},
		{".accft", "application/msaccess.ftemplate"},
		{".acx", "application/internet-property-stream"},
		{".addin", "text/xml"},
		{".ade", "application/msaccess"},
		{".adobebridge", "application/x-bridge-url"},
		{".adp", "application/msaccess"},
		{".adt", "audio/vnd.dlna.adts"},
		{".adts", "audio/aac"},
		{".afm", "application/octet-stream"},
		{".ai", "application/postscript"},
		{".aif", "audio/x-aiff"},
		{".aifc", "audio/aiff"},
		{".aiff", "audio/aiff"},
		{".air", "application/vnd.adobe.air-application-installer-package+zip"},
		{".amc", "application/x-mpeg"},
		{".application", "application/x-ms-application"},
		{".art", "image/x-jg"},
		{".asa", "application/xml"},
		{".asax", "application/xml"},
		{".ascx", "application/xml"},
		{".asd", "application/octet-stream"},
		{".asf", "video/x-ms-asf"},
		{".ashx", "application/xml"},
		{".asi", "application/octet-stream"},
		{".asm", "text/plain"},
		{".asmx", "application/xml"},
		{".aspx", "application/xml"},
		{".asr", "video/x-ms-asf"},
		{".asx", "video/x-ms-asf"},
		{".atom", "application/atom+xml"},
		{".au", "audio/basic"},
		{".avi", "video/x-msvideo"},
		{".axs", "application/olescript"},
		{".bas", "text/plain"},
		{".bcpio", "application/x-bcpio"},
		{".bin", "application/octet-stream"},
		{".bmp", "image/bmp"},
		{".c", "text/plain"},
		{".cab", "application/octet-stream"},
		{".caf", "audio/x-caf"},
		{".calx", "application/vnd.ms-office.calx"},
		{".cat", "application/vnd.ms-pki.seccat"},
		{".cc", "text/plain"},
		{".cd", "text/plain"},
		{".cdda", "audio/aiff"},
		{".cdf", "application/x-cdf"},
		{".cer", "application/x-x509-ca-cert"},
		{".chm", "application/octet-stream"},
		{".class", "application/x-java-applet"},
		{".clp", "application/x-msclip"},
		{".cmx", "image/x-cmx"},
		{".cnf", "text/plain"},
		{".cod", "image/cis-cod"},
		{".config", "application/xml"},
		{".contact", "text/x-ms-contact"},
		{".coverage", "application/xml"},
		{".cpio", "application/x-cpio"},
		{".cpp", "text/plain"},
		{".crd", "application/x-mscardfile"},
		{".crl", "application/pkix-crl"},
		{".crt", "application/x-x509-ca-cert"},
		{".cs", "text/plain"},
		{".csdproj", "text/plain"},
		{".csh", "application/x-csh"},
		{".csproj", "text/plain"},
		{".css", "text/css"},
		{".csv", "text/csv"},
		{".cur", "application/octet-stream"},
		{".cxx", "text/plain"},
		{".dat", "application/octet-stream"},
		{".datasource", "application/xml"},
		{".dbproj", "text/plain"},
		{".dcr", "application/x-director"},
		{".def", "text/plain"},
		{".deploy", "application/octet-stream"},
		{".der", "application/x-x509-ca-cert"},
		{".dgml", "application/xml"},
		{".dib", "image/bmp"},
		{".dif", "video/x-dv"},
		{".dir", "application/x-director"},
		{".disco", "text/xml"},
		{".dll", "application/x-msdownload"},
		{".dll.config", "text/xml"},
		{".dlm", "text/dlm"},
		{".doc", "application/msword"},
		{".docm", "application/vnd.ms-word.document.macroenabled.12"},
		{".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
		{".dot", "application/msword"},
		{".dotm", "application/vnd.ms-word.template.macroenabled.12"},
		{".dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template"},
		{".dsp", "application/octet-stream"},
		{".dsw", "text/plain"},
		{".dtd", "text/xml"},
		{".dtsconfig", "text/xml"},
		{".dv", "video/x-dv"},
		{".dvi", "application/x-dvi"},
		{".dwf", "drawing/x-dwf"},
		{".dwp", "application/octet-stream"},
		{".dxr", "application/x-director"},
		{".eml", "message/rfc822"},
		{".emz", "application/octet-stream"},
		{".eot", "application/octet-stream"},
		{".eps", "application/postscript"},
		{".etl", "application/etl"},
		{".etx", "text/x-setext"},
		{".evy", "application/envoy"},
		{".exe", "application/octet-stream"},
		{".exe.config", "text/xml"},
		{".fdf", "application/vnd.fdf"},
		{".fif", "application/fractals"},
		{".filters", "application/xml"},
		{".fla", "application/octet-stream"},
		{".flr", "x-world/x-vrml"},
		{".flv", "video/x-flv"},
		{".fsscript", "application/fsharp-script"},
		{".fsx", "application/fsharp-script"},
		{".generictest", "application/xml"},
		{".gif", "image/gif"},
		{".group", "text/x-ms-group"},
		{".gsm", "audio/x-gsm"},
		{".gtar", "application/x-gtar"},
		{".gz", "application/x-gzip"},
		{".h", "text/plain"},
		{".hdf", "application/x-hdf"},
		{".hdml", "text/x-hdml"},
		{".hhc", "application/x-oleobject"},
		{".hhk", "application/octet-stream"},
		{".hhp", "application/octet-stream"},
		{".hlp", "application/winhlp"},
		{".hpp", "text/plain"},
		{".hqx", "application/mac-binhex40"},
		{".hta", "application/hta"},
		{".htc", "text/x-component"},
		{".htm", "text/html"},
		{".html", "text/html"},
		{".htt", "text/webviewhtml"},
		{".hxa", "application/xml"},
		{".hxc", "application/xml"},
		{".hxd", "application/octet-stream"},
		{".hxe", "application/xml"},
		{".hxf", "application/xml"},
		{".hxh", "application/octet-stream"},
		{".hxi", "application/octet-stream"},
		{".hxk", "application/xml"},
		{".hxq", "application/octet-stream"},
		{".hxr", "application/octet-stream"},
		{".hxs", "application/octet-stream"},
		{".hxt", "text/html"},
		{".hxv", "application/xml"},
		{".hxw", "application/octet-stream"},
		{".hxx", "text/plain"},
		{".i", "text/plain"},
		{".ico", "image/x-icon"},
		{".ics", "application/octet-stream"},
		{".idl", "text/plain"},
		{".ief", "image/ief"},
		{".iii", "application/x-iphone"},
		{".inc", "text/plain"},
		{".inf", "application/octet-stream"},
		{".inl", "text/plain"},
		{".ins", "application/x-internet-signup"},
		{".ipa", "application/x-itunes-ipa"},
		{".ipg", "application/x-itunes-ipg"},
		{".ipproj", "text/plain"},
		{".ipsw", "application/x-itunes-ipsw"},
		{".iqy", "text/x-ms-iqy"},
		{".isp", "application/x-internet-signup"},
		{".ite", "application/x-itunes-ite"},
		{".itlp", "application/x-itunes-itlp"},
		{".itms", "application/x-itunes-itms"},
		{".itpc", "application/x-itunes-itpc"},
		{".ivf", "video/x-ivf"},
		{".jar", "application/java-archive"},
		{".java", "application/octet-stream"},
		{".jck", "application/liquidmotion"},
		{".jcz", "application/liquidmotion"},
		{".jfif", "image/pjpeg"},
		{".jnlp", "application/x-java-jnlp-file"},
		{".jpb", "application/octet-stream"},
		{".jpe", "image/jpeg"},
		{".jpeg", "image/jpeg"},
		{".jpg", "image/jpeg"},
		{".js", "application/x-javascript"},
		{".jsx", "text/jscript"},
		{".jsxbin", "text/plain"},
		{".latex", "application/x-latex"},
		{".library-ms", "application/windows-library+xml"},
		{".lit", "application/x-ms-reader"},
		{".loadtest", "application/xml"},
		{".lpk", "application/octet-stream"},
		{".lsf", "video/x-la-asf"},
		{".lst", "text/plain"},
		{".lsx", "video/x-la-asf"},
		{".lzh", "application/octet-stream"},
		{".m13", "application/x-msmediaview"},
		{".m14", "application/x-msmediaview"},
		{".m1v", "video/mpeg"},
		{".m2t", "video/vnd.dlna.mpeg-tts"},
		{".m2ts", "video/vnd.dlna.mpeg-tts"},
		{".m2v", "video/mpeg"},
		{".m3u", "audio/x-mpegurl"},
		{".m3u8", "audio/x-mpegurl"},
		{".m4a", "audio/m4a"},
		{".m4b", "audio/m4b"},
		{".m4p", "audio/m4p"},
		{".m4r", "audio/x-m4r"},
		{".m4v", "video/x-m4v"},
		{".mac", "image/x-macpaint"},
		{".mak", "text/plain"},
		{".man", "application/x-troff-man"},
		{".manifest", "application/x-ms-manifest"},
		{".map", "text/plain"},
		{".master", "application/xml"},
		{".mda", "application/msaccess"},
		{".mdb", "application/x-msaccess"},
		{".mde", "application/msaccess"},
		{".mdp", "application/octet-stream"},
		{".me", "application/x-troff-me"},
		{".mfp", "application/x-shockwave-flash"},
		{".mht", "message/rfc822"},
		{".mhtml", "message/rfc822"},
		{".mid", "audio/mid"},
		{".midi", "audio/mid"},
		{".mix", "application/octet-stream"},
		{".mk", "text/plain"},
		{".mmf", "application/x-smaf"},
		{".mno", "text/xml"},
		{".mny", "application/x-msmoney"},
		{".mod", "video/mpeg"},
		{".mov", "video/quicktime"},
		{".movie", "video/x-sgi-movie"},
		{".mp2", "video/mpeg"},
		{".mp2v", "video/mpeg"},
		{".mp3", "audio/mpeg"},
		{".mp4", "video/mp4"},
		{".mp4v", "video/mp4"},
		{".mpa", "video/mpeg"},
		{".mpe", "video/mpeg"},
		{".mpeg", "video/mpeg"},
		{".mpf", "application/vnd.ms-mediapackage"},
		{".mpg", "video/mpeg"},
		{".mpp", "application/vnd.ms-project"},
		{".mpv2", "video/mpeg"},
		{".mqv", "video/quicktime"},
		{".ms", "application/x-troff-ms"},
		{".msi", "application/octet-stream"},
		{".mso", "application/octet-stream"},
		{".mts", "video/vnd.dlna.mpeg-tts"},
		{".mtx", "application/xml"},
		{".mvb", "application/x-msmediaview"},
		{".mvc", "application/x-miva-compiled"},
		{".mxp", "application/x-mmxp"},
		{".nc", "application/x-netcdf"},
		{".nsc", "video/x-ms-asf"},
		{".nws", "message/rfc822"},
		{".ocx", "application/octet-stream"},
		{".oda", "application/oda"},
		{".odc", "text/x-ms-odc"},
		{".odh", "text/plain"},
		{".odl", "text/plain"},
		{".odp", "application/vnd.oasis.opendocument.presentation"},
		{".ods", "application/oleobject"},
		{".odt", "application/vnd.oasis.opendocument.text"},
		{".one", "application/onenote"},
		{".onea", "application/onenote"},
		{".onepkg", "application/onenote"},
		{".onetmp", "application/onenote"},
		{".onetoc", "application/onenote"},
		{".onetoc2", "application/onenote"},
		{".orderedtest", "application/xml"},
		{".osdx", "application/opensearchdescription+xml"},
		{".p10", "application/pkcs10"},
		{".p12", "application/x-pkcs12"},
		{".p7b", "application/x-pkcs7-certificates"},
		{".p7c", "application/pkcs7-mime"},
		{".p7m", "application/pkcs7-mime"},
		{".p7r", "application/x-pkcs7-certreqresp"},
		{".p7s", "application/pkcs7-signature"},
		{".pbm", "image/x-portable-bitmap"},
		{".pcast", "application/x-podcast"},
		{".pct", "image/pict"},
		{".pcx", "application/octet-stream"},
		{".pcz", "application/octet-stream"},
		{".pdf", "application/pdf"},
		{".pfb", "application/octet-stream"},
		{".pfm", "application/octet-stream"},
		{".pfx", "application/x-pkcs12"},
		{".pgm", "image/x-portable-graymap"},
		{".pic", "image/pict"},
		{".pict", "image/pict"},
		{".pkgdef", "text/plain"},
		{".pkgundef", "text/plain"},
		{".pko", "application/vnd.ms-pki.pko"},
		{".pls", "audio/scpls"},
		{".pma", "application/x-perfmon"},
		{".pmc", "application/x-perfmon"},
		{".pml", "application/x-perfmon"},
		{".pmr", "application/x-perfmon"},
		{".pmw", "application/x-perfmon"},
		{".png", "image/png"},
		{".pnm", "image/x-portable-anymap"},
		{".pnt", "image/x-macpaint"},
		{".pntg", "image/x-macpaint"},
		{".pnz", "image/png"},
		{".pot", "application/vnd.ms-powerpoint"},
		{".potm", "application/vnd.ms-powerpoint.template.macroenabled.12"},
		{".potx", "application/vnd.openxmlformats-officedocument.presentationml.template"},
		{".ppa", "application/vnd.ms-powerpoint"},
		{".ppam", "application/vnd.ms-powerpoint.addin.macroenabled.12"},
		{".ppm", "image/x-portable-pixmap"},
		{".pps", "application/vnd.ms-powerpoint"},
		{".ppsm", "application/vnd.ms-powerpoint.slideshow.macroenabled.12"},
		{".ppsx", "application/vnd.openxmlformats-officedocument.presentationml.slideshow"},
		{".ppt", "application/vnd.ms-powerpoint"},
		{".pptm", "application/vnd.ms-powerpoint.presentation.macroenabled.12"},
		{".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
		{".prf", "application/pics-rules"},
		{".prm", "application/octet-stream"},
		{".prx", "application/octet-stream"},
		{".ps", "application/postscript"},
		{".psc1", "application/powershell"},
		{".psd", "application/octet-stream"},
		{".psess", "application/xml"},
		{".psm", "application/octet-stream"},
		{".psp", "application/octet-stream"},
		{".pub", "application/x-mspublisher"},
		{".pwz", "application/vnd.ms-powerpoint"},
		{".qht", "text/x-html-insertion"},
		{".qhtm", "text/x-html-insertion"},
		{".qt", "video/quicktime"},
		{".qti", "image/x-quicktime"},
		{".qtif", "image/x-quicktime"},
		{".qtl", "application/x-quicktimeplayer"},
		{".qxd", "application/octet-stream"},
		{".ra", "audio/x-pn-realaudio"},
		{".ram", "audio/x-pn-realaudio"},
		{".rar", "application/octet-stream"},
		{".ras", "image/x-cmu-raster"},
		{".rat", "application/rat-file"},
		{".rc", "text/plain"},
		{".rc2", "text/plain"},
		{".rct", "text/plain"},
		{".rdlc", "application/xml"},
		{".resx", "application/xml"},
		{".rf", "image/vnd.rn-realflash"},
		{".rgb", "image/x-rgb"},
		{".rgs", "text/plain"},
		{".rm", "application/vnd.rn-realmedia"},
		{".rmi", "audio/mid"},
		{".rmp", "application/vnd.rn-rn_music_package"},
		{".roff", "application/x-troff"},
		{".rpm", "audio/x-pn-realaudio-plugin"},
		{".rqy", "text/x-ms-rqy"},
		{".rtf", "application/rtf"},
		{".rtx", "text/richtext"},
		{".ruleset", "application/xml"},
		{".s", "text/plain"},
		{".safariextz", "application/x-safari-safariextz"},
		{".scd", "application/x-msschedule"},
		{".sct", "text/scriptlet"},
		{".sd2", "audio/x-sd2"},
		{".sdp", "application/sdp"},
		{".sea", "application/octet-stream"},
		{".searchconnector-ms", "application/windows-search-connector+xml"},
		{".setpay", "application/set-payment-initiation"},
		{".setreg", "application/set-registration-initiation"},
		{".settings", "application/xml"},
		{".sgimb", "application/x-sgimb"},
		{".sgml", "text/sgml"},
		{".sh", "application/x-sh"},
		{".shar", "application/x-shar"},
		{".shtml", "text/html"},
		{".sit", "application/x-stuffit"},
		{".sitemap", "application/xml"},
		{".skin", "application/xml"},
		{".sldm", "application/vnd.ms-powerpoint.slide.macroenabled.12"},
		{".sldx", "application/vnd.openxmlformats-officedocument.presentationml.slide"},
		{".slk", "application/vnd.ms-excel"},
		{".sln", "text/plain"},
		{".slupkg-ms", "application/x-ms-license"},
		{".smd", "audio/x-smd"},
		{".smi", "application/octet-stream"},
		{".smx", "audio/x-smd"},
		{".smz", "audio/x-smd"},
		{".snd", "audio/basic"},
		{".snippet", "application/xml"},
		{".snp", "application/octet-stream"},
		{".sol", "text/plain"},
		{".sor", "text/plain"},
		{".spc", "application/x-pkcs7-certificates"},
		{".spl", "application/futuresplash"},
		{".src", "application/x-wais-source"},
		{".srf", "text/plain"},
		{".ssisdeploymentmanifest", "text/xml"},
		{".ssm", "application/streamingmedia"},
		{".sst", "application/vnd.ms-pki.certstore"},
		{".stl", "application/vnd.ms-pki.stl"},
		{".sv4cpio", "application/x-sv4cpio"},
		{".sv4crc", "application/x-sv4crc"},
		{".svc", "application/xml"},
		{".swf", "application/x-shockwave-flash"},
		{".t", "application/x-troff"},
		{".tar", "application/x-tar"},
		{".tcl", "application/x-tcl"},
		{".testrunconfig", "application/xml"},
		{".testsettings", "application/xml"},
		{".tex", "application/x-tex"},
		{".texi", "application/x-texinfo"},
		{".texinfo", "application/x-texinfo"},
		{".tgz", "application/x-compressed"},
		{".thmx", "application/vnd.ms-officetheme"},
		{".thn", "application/octet-stream"},
		{".tif", "image/tiff"},
		{".tiff", "image/tiff"},
		{".tlh", "text/plain"},
		{".tli", "text/plain"},
		{".toc", "application/octet-stream"},
		{".tr", "application/x-troff"},
		{".trm", "application/x-msterminal"},
		{".trx", "application/xml"},
		{".ts", "video/vnd.dlna.mpeg-tts"},
		{".tsv", "text/tab-separated-values"},
		{".ttf", "application/octet-stream"},
		{".tts", "video/vnd.dlna.mpeg-tts"},
		{".txt", "text/plain"},
		{".u32", "application/octet-stream"},
		{".uls", "text/iuls"},
		{".user", "text/plain"},
		{".ustar", "application/x-ustar"},
		{".vb", "text/plain"},
		{".vbdproj", "text/plain"},
		{".vbk", "video/mpeg"},
		{".vbproj", "text/plain"},
		{".vbs", "text/vbscript"},
		{".vcf", "text/x-vcard"},
		{".vcproj", "application/xml"},
		{".vcs", "text/plain"},
		{".vcxproj", "application/xml"},
		{".vddproj", "text/plain"},
		{".vdp", "text/plain"},
		{".vdproj", "text/plain"},
		{".vdx", "application/vnd.ms-visio.viewer"},
		{".vml", "text/xml"},
		{".vscontent", "application/xml"},
		{".vsct", "text/xml"},
		{".vsd", "application/vnd.visio"},
		{".vsi", "application/ms-vsi"},
		{".vsix", "application/vsix"},
		{".vsixlangpack", "text/xml"},
		{".vsixmanifest", "text/xml"},
		{".vsmdi", "application/xml"},
		{".vspscc", "text/plain"},
		{".vss", "application/vnd.visio"},
		{".vsscc", "text/plain"},
		{".vssettings", "text/xml"},
		{".vssscc", "text/plain"},
		{".vst", "application/vnd.visio"},
		{".vstemplate", "text/xml"},
		{".vsto", "application/x-ms-vsto"},
		{".vsw", "application/vnd.visio"},
		{".vsx", "application/vnd.visio"},
		{".vtx", "application/vnd.visio"},
		{".wav", "audio/wav"},
		{".wave", "audio/wav"},
		{".wax", "audio/x-ms-wax"},
		{".wbk", "application/msword"},
		{".wbmp", "image/vnd.wap.wbmp"},
		{".wcm", "application/vnd.ms-works"},
		{".wdb", "application/vnd.ms-works"},
		{".wdp", "image/vnd.ms-photo"},
		{".webarchive", "application/x-safari-webarchive"},
		{".webtest", "application/xml"},
		{".wiq", "application/xml"},
		{".wiz", "application/msword"},
		{".wks", "application/vnd.ms-works"},
		{".wlmp", "application/wlmoviemaker"},
		{".wlpginstall", "application/x-wlpg-detect"},
		{".wlpginstall3", "application/x-wlpg3-detect"},
		{".wm", "video/x-ms-wm"},
		{".wma", "audio/x-ms-wma"},
		{".wmd", "application/x-ms-wmd"},
		{".wmf", "application/x-msmetafile"},
		{".wml", "text/vnd.wap.wml"},
		{".wmlc", "application/vnd.wap.wmlc"},
		{".wmls", "text/vnd.wap.wmlscript"},
		{".wmlsc", "application/vnd.wap.wmlscriptc"},
		{".wmp", "video/x-ms-wmp"},
		{".wmv", "video/x-ms-wmv"},
		{".wmx", "video/x-ms-wmx"},
		{".wmz", "application/x-ms-wmz"},
		{".wpl", "application/vnd.ms-wpl"},
		{".wps", "application/vnd.ms-works"},
		{".wri", "application/x-mswrite"},
		{".wrl", "x-world/x-vrml"},
		{".wrz", "x-world/x-vrml"},
		{".wsc", "text/scriptlet"},
		{".wsdl", "text/xml"},
		{".wvx", "video/x-ms-wvx"},
		{".x", "application/directx"},
		{".xaf", "x-world/x-vrml"},
		{".xaml", "application/xaml+xml"},
		{".xap", "application/x-silverlight-app"},
		{".xbap", "application/x-ms-xbap"},
		{".xbm", "image/x-xbitmap"},
		{".xdr", "text/plain"},
		{".xht", "application/xhtml+xml"},
		{".xhtml", "application/xhtml+xml"},
		{".xla", "application/vnd.ms-excel"},
		{".xlam", "application/vnd.ms-excel.addin.macroenabled.12"},
		{".xlc", "application/vnd.ms-excel"},
		{".xld", "application/vnd.ms-excel"},
		{".xlk", "application/vnd.ms-excel"},
		{".xll", "application/vnd.ms-excel"},
		{".xlm", "application/vnd.ms-excel"},
		{".xls", "application/vnd.ms-excel"},
		{".xlsb", "application/vnd.ms-excel.sheet.binary.macroenabled.12"},
		{".xlsm", "application/vnd.ms-excel.sheet.macroenabled.12"},
		{".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
		{".xlt", "application/vnd.ms-excel"},
		{".xltm", "application/vnd.ms-excel.template.macroenabled.12"},
		{".xltx", "application/vnd.openxmlformats-officedocument.spreadsheetml.template"},
		{".xlw", "application/vnd.ms-excel"},
		{".xml", "text/xml"},
		{".xmta", "application/xml"},
		{".xof", "x-world/x-vrml"},
		{".xoml", "text/plain"},
		{".xpm", "image/x-xpixmap"},
		{".xps", "application/vnd.ms-xpsdocument"},
		{".xrm-ms", "text/xml"},
		{".xsc", "application/xml"},
		{".xsd", "text/xml"},
		{".xsf", "text/xml"},
		{".xsl", "text/xml"},
		{".xslt", "text/xml"},
		{".xsn", "application/octet-stream"},
		{".xss", "application/xml"},
		{".xtp", "application/octet-stream"},
		{".xwd", "image/x-xwindowdump"},
		{".z", "application/x-compress"},
		{".zip", "application/x-zip-compressed"},
		#endregion
		
		};

		public static string GetMimeType(string extension)
		{
			if (extension == null)
			{
				throw new ArgumentNullException("extension");
			}

			if (!extension.StartsWith("."))
			{
				extension = "." + extension;
			}

			string mime;

			return _mimemappings.TryGetValue(extension.ToLower(), out mime) ? mime : "application/octet-stream";
		}

	}
}