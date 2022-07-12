using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Configuration;
using Jitbit.Utils;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for register.
	/// </summary>
	public partial class register : ForumPage
	{
		protected void Page_Load(object sender, System.EventArgs e)
		{
			if (!IsPostBack)
			{
				registerTable.Visible = false;
				divTOS.Visible = true;
			}
			else
			{
				divTOS.Visible = false;
			}

			//this is for correct processing the "enter" keypress onthe form
			if (IsPostBack && registerTable.Visible)
			{
				RegisterUser();
			}
		}

		protected void Page_PreRender(object sender, System.EventArgs e)
		{
			// Create a random code and store it in the Session object.
			this.Session["CaptchaImageText"] = CryptoUtils.GenerateRandomNumericCode();
			tbImgCode.Text = "";
		}

		//preventing the "HttpRequestValidationException A potentially dangerous Request.Form value was detected from the client"
		protected void Page_Error(object sender, System.EventArgs e)
		{
			Exception ex = Server.GetLastError();
			if (ex is HttpRequestValidationException
				|| ex is ViewStateException
				|| ex.InnerException is ViewStateException)
			{
				//it's a m..f..ing BOT!!! damn spammers!!
				//let's just suppress the error msg so they don't know what's wrong
				
				//Response.Write("die spammer, die!!!");
				Response.End();
			}
		}

		protected void RegisterUser()
		{
			//antispam check
			if (!string.IsNullOrWhiteSpace(Request.Form["email"])) return; //its a bot!!

			if (tbUserName.Text.Trim().Length == 0 ||
				tbPsw1.Text.Trim().Length == 0 ||
				tbPsw2.Text.Trim().Length == 0 ||
				tbEmail.Text.Trim().Length == 0 ||
				tbEmail.Text.IndexOf("@") == -1 ||
				tbEmail.Text.IndexOf(".") == -1)
			{
				lblError.Visible = true;
				lblError.Text = "Please, fill all the fields correctly";
				return;
			}

			if (tbPsw1.Text != tbPsw2.Text)
			{
				lblError.Visible = true;
				lblError.Text = Resources.various.ErrorPasswordsDoNotMatch;
				return;
			}

			if (tbPsw1.Text.Length < Utils.Settings.MinPasswordLength)
			{
				lblError.Visible = true;
				lblError.Text = string.Format("Password is too short, {0} characters minimum", Utils.Settings.MinPasswordLength);
				return;
			}

			if (tbImgCode.Text != (string)Session["CaptchaImageText"])
			{
				lblError.Visible = true;
				lblError.Text = "Wrong code entered";
				return;
			}

			string username = tbUserName.Text.Trim();
			if (Utils.User.GetUserIdByUserName(username) != 0)
			{
				lblError.Visible = true;
				lblError.Text = string.Format("Username {0} already exists, please select another one.", username);
				return;
			}

			if (Utils.User.GetUserIdByEmail(tbEmail.Text) != 0)
			{
				lblError.Visible = true;
				lblError.Text = string.Format("Email {0} already exists, please select another one or use the password recovery form.", tbEmail.Text);
				return;
			}

			//is email confirmation enabled?
			bool bEmailConfirmation = Utils.Settings.EnableEmailActivation;

			//should we notify admins?
			bool bNewUsersNotifyAdmin = Utils.Settings.NewUsersNotifyAdmin;

			//shoud user be disabled by default?
			bool bUserDisabled = Utils.Settings.NewUsersDisabledByDefault;

			//generate activation code
			string randomCode = CryptoUtils.GenerateRandomCode(9);

			//insert user
			Cn.Open();
			int newUserId = Utils.User.CreateUser(username, tbEmail.Text, Utils.Password.CalculateHash(tbPsw1.Text), tbHomepage.Text, tbInterests.Text, bUserDisabled, randomCode);
			Cn.Close();

			lblError.Visible = false;
			lblSuccess.Visible = true;
			registerTable.Visible = false;

			//send activation email
			if (bEmailConfirmation && bUserDisabled)
			{
				Utils.User.SendActivationEmail(newUserId);
				lblSuccessEmail.Visible = true;
			}

			//send notification to admins
			if (bNewUsersNotifyAdmin)
			{
				string url = Utils.Various.ForumURL + "viewprofile.aspx?UserID=" + newUserId;
				Utils.SendNotifications.SendNewUserRegAdminNotification(url);
			}
		}

		protected void btnAgree_Click(object sender, EventArgs e)
		{
			registerTable.Visible = true;
		}

		protected void btnDisagree_Click(object sender, EventArgs e)
		{
			Response.Redirect("default.aspx");
		}
	}
}
