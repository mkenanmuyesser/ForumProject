using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.Common;
using Jitbit.Utils;

namespace aspnetforum
{
	public partial class autologin : ForumPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			//brute force check
			//check if last attempt was less than 5 mins ago
			if (Application["LastTry"] != null && DateTime.Now.Subtract((DateTime)Application["LastTry"]).Minutes < 5)
			{
				Response.Write("Not yet. Please wait 5 mins from your last failed attempt.");
				Response.End();
				return;
			}

			Application["LastTry"] = DateTime.Now;

			string username = Request.QueryString["username"];
			string email = Request.QueryString["email"];
			string userHash = Request.QueryString["userHash"];
			string url;
			if (Request.QueryString["ReturnUrl"] != null)
			{
				url = Request.QueryString["ReturnUrl"];
				if ((!string.IsNullOrEmpty(url) && !url.Contains("/")) && url.Contains("%"))
					url = HttpUtility.UrlDecode(url);
			}
			else url = "default.aspx";


			if (username == null || email == null || userHash == null)
			{
				Response.Write("Invalid parameters passed. Wait 5 minutes ang try again");
				Response.End();
				return;
			}

			//logging in a user (either new or existing) with the app "shared secret"
			if (email != null && userHash != null)
			{
				if (string.IsNullOrEmpty(Utils.Settings.AutoLoginSharedSecret))
				{
					Response.Write("No shared key configured in the application settings.");
					Response.End();
					return;
				}
				string computedHash = Utils.Password.CalculateHash(username + email + Utils.Settings.AutoLoginSharedSecret);
				if (userHash.ToLower() != computedHash.ToLower())
				{
					Response.Write("Invalid parameters passed. Wait 5 minutes and try again.");
					Response.End();
					return;
				}

				int userId = Utils.User.GetUserIdByUserName(username);
				if (userId == 0) //user not found - lets add him
				{
					Utils.User.CreateUser(username, email, CryptoUtils.GenerateRandomNumericCode(), "", "", false);
					userId = Utils.User.GetUserIdByUserName(username);
				}

				Application["LastTry"] = null;
				Utils.User.Login(userId, username);
				Response.Redirect(url);
			}
		}
	}
}