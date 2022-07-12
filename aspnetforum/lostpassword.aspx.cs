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
using aspnetforum.Utils;
using Jitbit.Utils;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for lostpassword.
	/// </summary>
	public partial class lostpassword : ForumPage
	{
		protected void Page_Load(object sender, System.EventArgs e)
		{
		}

		//preventing the "HttpRequestValidationException A potentially dangerous Request.Form value was detected from the client"
		protected void Page_Error(object sender, System.EventArgs e)
		{
			Exception ex = Server.GetLastError().GetBaseException();
			if (ex is HttpRequestValidationException || ex is ViewStateException)
			{
				Response.Write("no spam please");
				Server.ClearError();
				Response.End();
			}
		}

		protected void btnRequest_Click(object sender, System.EventArgs e)
		{
			if (txEmail.Text.Trim() != "")
			{
				if (tbImgCode.Text == (string)Session["CaptchaImageText"])
				{
					this.Cn.Open();
					object res = Cn.ExecuteScalar("SELECT UserName FROM ForumUsers WHERE Email=?", txEmail.Text.Trim());
					if (res == null)
					{
						Cn.Close();
						lblEmailNotFound.Visible = true;
						return; //no user found
					}

					string newPsw = CryptoUtils.GenerateRandomCode(7);
					string newPswHash = Utils.Password.CalculateHash(newPsw);

					Cn.ExecuteNonQuery("UPDATE ForumUsers SET [Password]=? WHERE Email=?", newPswHash, txEmail.Text.Trim());
					this.Cn.Close();

					SendPsw(txEmail.Text.Trim(), res.ToString(), newPsw);

					tblMain.Visible = false;
					lblOk.Visible = true;
				}
				else
				{
					lblWrongCode.Visible = true;
				}
			}
		}

		private void SendPsw(string email, string username, string psw)
		{
			string forum = Utils.Settings.ForumTitle;

			string[] recipients = new string[1];
			recipients[0] = email;

			string url = Utils.Various.ForumURL;
			string body = string.Format(
				Resources.various.LostPasswordEmailBody,
				forum, username, psw, url);

			Utils.SendNotifications.SendEmail(recipients, "Password reminder", body);
		}

		protected void Page_PreRender(object sender, System.EventArgs e)
		{
			// Create a random code and store it in the Session object.
			this.Session["CaptchaImageText"] = CryptoUtils.GenerateRandomNumericCode();
			tbImgCode.Text = "";
		}
	}
}
