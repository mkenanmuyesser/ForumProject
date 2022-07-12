using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data.Common;
using System.Configuration;
using System.IO;
using System.Collections.Generic;
using aspnetforum.Utils;
using Jitbit.Utils;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for addpost.
	/// </summary>
	public partial class addpost : ForumPage
	{
		int _topicID = 0;
		int _forumID = 0;
		bool _addTopic;
		bool _changeTopic;
		bool _isEditing;
		int _messageId;
		int _messageAuthorID;
		bool _premoderated;
		bool _allowGuestPosts;
		bool _allowFileUploads;
		bool _isIPhoneOrAndroid;

		protected void Page_PreInit(object sender, System.EventArgs e)
		{
			_isIPhoneOrAndroid = IsiPhoneOrAndroid();
		}        

		protected void Page_PreRender(object sender, System.EventArgs e)
		{
			if (CurrentUserID == 0 && _allowGuestPosts)
			{
				// Create a random code and store it in the Session object.
				Session["CaptchaImageText"] = CryptoUtils.GenerateRandomNumericCode();
				tbImgCode.Text = "";
				divCaptcha.Visible = true;
			}
			else
				divCaptcha.Visible = false;
		}

		protected void Page_Load(object sender, System.EventArgs e)
		{
			//firefox html3.2 rendering fix
			tbSubj.Style.Add("width", "100%");
			tbMsg.Style.Add("width", "100%");
			tbSubj.Attributes["placeholder"] = Resources.various.Subject;
			btnSave.Text = Resources.various.AddMessage;
			cbSubscribe.Text = Resources.various.NotifyMeOnReply;

			_allowFileUploads = Utils.Settings.EnableFileUploads;
			divFiles.Visible = _allowFileUploads && (CurrentUserID!=0) && !_isIPhoneOrAndroid;

			_allowGuestPosts = Utils.Settings.AllowGuestPosts;

			if (Request.QueryString["TopicID"] != null)
				_topicID = int.Parse(Request.QueryString["TopicID"]);

			if (Request.QueryString["ForumID"] != null)
				_forumID = int.Parse(Request.QueryString["ForumID"]);

			if (_forumID == 0 && _topicID == 0)
			{
				Response.Write("Either Topic or Forum must be specified");
				Response.End();
			}

			//if we have an unauthorized user
			if (CurrentUserID == 0 && !_allowGuestPosts)
			{
				Response.Write("Sorry, posting and editing is allowed only for authenticated users");
				Response.End();
			}

			cbSubscribe.Visible = Utils.Settings.MailNotificationsEnabled && (CurrentUserID != 0) && !_isIPhoneOrAndroid;

			btnSmilies.Visible = Utils.Settings.AllowSmilies && !_isIPhoneOrAndroid;

			spanUtils.Visible = divEditbar.Visible = btnPreview.Visible = !_isIPhoneOrAndroid;

			Cn.Open();

			// Figure out if we're editing or quoting a message, and extract the ID.
			_messageId = 0;
			if (Request.QueryString["Edit"]!=null)
			{
				_messageId = int.Parse(Request.QueryString["Edit"]);
				_isEditing = true;
				btnSave.Text = "update message";

				//check if it's the first msg in a topic - to see if we should allow changing the topic text
				object res = Cn.ExecuteScalar("SELECT MIN(MessageID) FROM ForumMessages WHERE TopicID=" + _topicID);
				_changeTopic = (Convert.ToInt32(res) == _messageId);
			}

			if (Request.QueryString["Quote"]!=null)
			{
				_messageId = int.Parse(Request.QueryString["Quote"]);
				_isEditing = false;
			}

			if (_forumID == 0) //we're NOT adding a new topic to a forum, we're adding msg to an existing
			{
				_addTopic = false;
				bool isTopicClosed = false;
				DbDataReader dr = Cn.ExecuteReader("SELECT Forums.ForumID, Forums.Title, Forums.Premoderated, ForumTopics.IsClosed, ForumTopics.Subject FROM Forums INNER JOIN ForumTopics ON Forums.ForumID=ForumTopics.ForumID WHERE ForumTopics.TopicID=" + _topicID);
				if (dr.Read())
				{
					_forumID = Convert.ToInt32(dr["ForumID"]);
					_premoderated = Convert.ToBoolean(dr["Premoderated"]);
					isTopicClosed = Convert.ToBoolean(dr["IsClosed"]);
					if (_changeTopic)
					{
						if (!IsPostBack)
							tbSubj.Text = dr["Subject"].ToString();
					}
					else
						lblSubjectText.Text = dr["Subject"].ToString(); //let's hsow the subj when replying
				}
				dr.Close();

				if (isTopicClosed && !_isEditing)
				{
					Cn.Close();
					Response.End();
					return;
				}
			}
			else //we're adding a NEW TOPIC to a forum
			{
				_addTopic = true;
				DbDataReader dr = Cn.ExecuteReader("SELECT Forums.ForumID, Forums.Title, Forums.Premoderated FROM Forums WHERE Forums.ForumID=" + _forumID);
				if (dr.Read())
				{
					_premoderated = Convert.ToBoolean(dr["Premoderated"]);
				}
				dr.Close();
			}

			divPolls.Visible = _addTopic && !_isIPhoneOrAndroid;

			if (!Utils.Forum.CheckForumPostPermissions(_forumID, CurrentUserID))
			{
				lblDenied.Visible=true;
				divMain.Visible=false;
			}

			if (_addTopic || _changeTopic)
			{
				tbSubj.Visible = true;
				reqSubject.Enabled = true;
			}

			if(!_addTopic)
			{
				if(!IsPostBack)
				{
					//set the "subscribe me" checkbox
					if (cbSubscribe.Visible)
					{
						var res = Cn.ExecuteScalar("SELECT UserID FROM ForumSubscriptions WHERE UserID=" + CurrentUserID + " AND TopicID=" + _topicID);
						cbSubscribe.Checked = (res != null);
					}

					if (!_isIPhoneOrAndroid)
					{
						//display previous messages in a topic
						var dr = Cn.ExecuteReader(
							@"SELECT ForumMessages.Body, ForumUsers.UserName, ForumMessages.CreationDate
							FROM ForumMessages LEFT JOIN ForumUsers ON ForumUsers.UserID=ForumMessages.UserID
							WHERE ForumMessages.TopicID=" + _topicID + " and ForumMessages.Visible=? ORDER BY ForumMessages.CreationDate DESC", true);
						rptMessages.DataSource = dr;
						rptMessages.DataBind();
						dr.Close();
					}
					else
						rptMessages.Visible = false;
				}
			}

			//if we-re quoting or editing
			if (_messageId != 0)
			{
				//get the author of the edited message
				object res = Cn.ExecuteScalar("SELECT UserID FROM ForumMessages WHERE MessageID=" + _messageId);
				_messageAuthorID = (res == null ? -1 : Convert.ToInt32(res));

				//IF not PostBack - lets pre-fill the body field with the message text and show attachments
				if (!IsPostBack)
				{
					DbDataReader dr;

					//show attachments
					if (_isEditing)
					{
						dr = Cn.ExecuteReader("SELECT FileID, FileName FROM ForumUploadedFiles WHERE MessageID=" + _messageId);
						rptExistingFiles.DataSource = dr;
						rptExistingFiles.DataBind();
						rptExistingFiles.Visible = (rptExistingFiles.Items.Count > 0);
						dr.Close();
					}

					dr = Cn.ExecuteReader("SELECT ForumMessages.Body, ForumUsers.UserName, ForumUsers.FirstName, ForumUsers.LastName, ForumMessages.UserID FROM ForumMessages LEFT OUTER JOIN ForumUsers ON ForumUsers.UserID=ForumMessages.UserID WHERE ForumMessages.MessageID=" + _messageId);
					if (dr.Read())
					{
						string body = dr["Body"].ToString().Replace("<br>", "\r\n").Replace("<br/>", "\r\n").Replace("<br />", "\r\n");
						body = System.Text.RegularExpressions.Regex.Replace(body, @"<\S[^>]*>", "");
						//if its quoting
						if (!_isEditing)
						{
							//remove domain from username (in case its windows auth)
							string uname = Utils.User.GetUserDisplayName(dr["UserName"].ToString(), dr["FirstName"].ToString(), dr["LastName"].ToString());
							
							tbMsg.Text = "[quote=" + uname + "]" + body + "[/quote]\r\n\r\n";
						}
						else //if its editing
						{
							tbMsg.Text = body;
						}
					}
					dr.Close();
				}
			}
			Cn.Close();
		}

		protected void btnSave_Click(object sender, System.EventArgs e)
		{
			if (CurrentUserID == 0 && _allowGuestPosts)
				if (tbImgCode.Text != (string)Session["CaptchaImageText"])
					return;

			string msg = tbMsg.Text.Trim();
			if (msg == "") return;
			msg = msg.Replace("<", "&lt;").Replace(">", "&gt;");

			bool isModer = IsModerator(_forumID);
			bool shouldItBeVisible = (!_premoderated) || isModer;

			if (!Utils.Attachments.CheckAttachmentsSize())
			{
				lblMaxSize.Text = Utils.Settings.MaxUploadFileSize / 1000 + " Kb";
				lblMaxSize.Visible = lblFileSizeError.Visible = true;
				return;
			}
			else
			{
				lblMaxSize.Visible = lblFileSizeError.Visible = false;
			}

			Cn.Open();

			if(_addTopic || _changeTopic) //creating a new topic or editing topic title
			{
				string subj = tbSubj.Text.Trim();
				if (subj == "") { Cn.Close(); return; }
				subj = subj.Replace("<", "&lt;").Replace(">", "&gt;");

				if (_addTopic)
				{
					_topicID = Utils.Topic.CreateTopic(Cn, _forumID, CurrentUserID, subj, msg, shouldItBeVisible);

					//CREATE A POLL (if specified)
					string pollQuestion = tbPollQuestion.Text.Trim().Replace("<", "&lt;").Replace(">", "&gt;");
					if (pollQuestion.Length > 0)
					{
						//add poll
						Utils.Topic.CreatePoll(Cn, _topicID, pollQuestion, GetPollOptionsFromRequestForm());
					}
				}
				else if (_changeTopic) //edit topic subj
				{
					Utils.Topic.ChangeTopicSubject(Cn, _topicID, subj);
				}
			}

			//saving notifications settings
			Utils.SendNotifications.UpdateTopicNotificationSettings(CurrentUserID, _topicID, cbSubscribe.Checked, Cn);

			// MESSAGE: Inserting or updating?
			if (_isEditing)
			{
				//if moderatro, admin or message author
				if (isModer || _messageAuthorID == CurrentUserID)
				{
					Utils.Message.UpdateMessageText(Cn, _messageId, msg, shouldItBeVisible);
					Utils.Attachments.SaveAttachments(_messageId, false, Cn);
				}
			}
			else //inserting
			{
				_messageId = Utils.Message.AddMessage(Cn, _topicID, msg, shouldItBeVisible, Utils.Various.GetUserIpAddress(Request), _addTopic);

				Utils.Attachments.SaveAttachments(_messageId, false, Cn);
			}

			if (_premoderated && !isModer)
			{
				Cn.Close();
				Response.Redirect("premoderatedmessage.aspx");
			}
			else
			{
				//count messages to compute the number of pages
				//(needed to get the user redirected to the last page)
				int numMessages = Convert.ToInt32(
					Cn.ExecuteScalar("SELECT COUNT(MessageID) FROM ForumMessages WHERE Visible=? AND TopicID=" + _topicID, true));
				int numPages = (numMessages - 1) / PageSize;
				Cn.Close();

				string subject = (_changeTopic || _addTopic) ? tbSubj.Text : lblSubjectText.Text;
				string url = Utils.Various.GetTopicURL(_topicID, subject);
				string sep = url.IndexOf("?") > -1 ? "&" : "?";
				url = (numPages > 0) ? url + sep + "Page=" + numPages : url;
				url += sep + "MessageID=" + _messageId;
				Response.Redirect(url);
			}
		}

		private List<string> GetPollOptionsFromRequestForm()
		{
			List<string> options = new List<string>();
			int i = 0;
			while (Request.Form["PollOption" + i] != null && Request.Form["PollOption" + i].Trim().Length > 0)
			{
				//add option
				options.Add(Request.Form["PollOption" + i].Replace("<", "&lt;").Replace(">", "&gt;"));
				i++;
			}
			return options;
		}
	}
}
