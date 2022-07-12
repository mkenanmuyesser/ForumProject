using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Collections;
using System.Data.Common;
using Jitbit.Utils;

namespace aspnetforum
{
	public partial class facebooklogin : ForumPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (Request.Cookies["fbsr_" + Utils.Settings.FacebookAppID] == null) return;

			string access_token = Request.QueryString["token"];
			string uid = Request.QueryString["uid"];

			string json = GetFacebookUserJson(uid, access_token);
			Hashtable jsonHash = (Hashtable)Utils.JSON.JsonDecode(json);

			string facebookName = jsonHash["name"] as string;
			string firstName = jsonHash["first_name"] as string;
			string lastName = jsonHash["last_name"] as string;
			string email = jsonHash["email"] as string;
			string facebookId = jsonHash["id"] as string;
			string url = jsonHash["link"] as string;

			int userId;
			string userName;
			Utils.User.GetUserByFacebookId(facebookId, out userId, out userName);

			//existing facebook user
			if (userId != 0)
			{
				Utils.User.Login(userId, userName);
				Response.Redirect("default.aspx");
			}
			else //we have to add a new user
			{
				if (Utils.User.GetUserIdByUserName(facebookName) != 0) //user already exists, lets show "pick username"
				{
					//todo
				}
				else
				{
					Utils.User.CreateUser(facebookName, email, CryptoUtils.GenerateRandomNumericCode(), url, "", false, string.Empty, firstName, lastName, "", "", facebookId);
					Utils.User.GetUserByFacebookId(facebookId, out userId, out userName);

					Utils.User.SetAvatarUrl(userId, string.Format("https://graph.facebook.com/{0}/picture", facebookId));

					Utils.User.Login(userId, userName);
					Response.Redirect("default.aspx");
				}
			}
		}

		private static string GetFacebookUserJson(string userid, string access_token)
		{
			string url = string.Format("https://graph.facebook.com/{0}?access_token={1}&fields=email,name,first_name,last_name,link", userid, access_token);

			WebClient wc = new WebClient();
			Stream data = wc.OpenRead(url);
			StreamReader reader = new StreamReader(data);
			string s = reader.ReadToEnd();
			data.Close();
			reader.Close();

			return s;
		}
	}
}