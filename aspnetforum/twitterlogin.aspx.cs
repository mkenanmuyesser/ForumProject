using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.Data.Common;
using Jitbit.Utils;
using aspnetforum.Utils;

namespace aspnetforum
{
	public partial class twitterlogin : ForumPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			oAuthTwitter oAuth = new Utils.oAuthTwitter();
			oAuth.ConsumerKey = Utils.Settings.TwitterConsumerKey;
			oAuth.ConsumerSecret = Utils.Settings.TwitterConsumerSecret;

			if (!IsPostBack)
			{
				if (Request["oauth_token"] == null)
				{
					//Redirect the user to Twitter for authorization.
					//Using oauth_callback for local testing.
					oAuth.CallBackUrl = Request.Url.ToString();
					try
					{
						Response.Redirect(oAuth.AuthorizationLinkGet());
					}
					catch
					{
						lblError.Visible = true;
						return;
					}
				}
				else
				{
					//Get the access token and secret.
					try
					{
						oAuth.AccessTokenGet(Request["oauth_token"], Request["oauth_verifier"]);
					}
					catch
					{
						lblError.Visible = true;
						return;
					}

					if (oAuth.TokenSecret.Length > 0)
					{
						//We now have the credentials, so make a call to the Twitter API.
						string url = "https://api.twitter.com/1.1/account/verify_credentials.json";
						string json = oAuth.oAuthWebRequest(oAuthTwitter.Method.GET, url, String.Empty);

						var jss = new JavaScriptSerializer();
						var data = jss.Deserialize<dynamic>(json);

						TwitterUserInfo twitterUser = new TwitterUserInfo();
						twitterUser.twitterUsername = data["screen_name"];
						twitterUser.twitterHomepage = data["url"];
						twitterUser.twitterBio = data["description"];
						twitterUser.twitterImageURL = data["profile_image_url"];

						int userId;
						string userName;
						Utils.User.GetUserByTwitterNameId(twitterUser.twitterUsername, out userId, out userName);

						//existing twitter user
						if (userId != 0)
						{
							Utils.User.Login(userId, userName);
							Response.Redirect("default.aspx");
						}
						else //we have to add a new user
						{
							lblTwitterName.Text = twitterUser.twitterUsername;
							Session["TwitterUser"] = twitterUser;
							if (Utils.User.GetUserIdByUserName(twitterUser.twitterUsername) != 0) //user already exists, lets show "pick username"
							{
								trPickUserName.Visible = pPickUserName.Visible = true;
							}
						}
					}
				}
			}
			else if (Request.Form["pickusernamebtn"] != null)//user picked the username and clicked the btn
			{
				AssignUsernameAndEmail();
			}
		}

		private void SaveAvatarFromTwitter(int userId, string avatarUrl)
		{
			//save avatar from twitter
			Cn.Open();
			Cn.ExecuteNonQuery("UPDATE ForumUsers SET UseGravatar=?, AvatarFileName=? WHERE UserID=?", false, avatarUrl, userId);
			Cn.Close();
		}

		protected void AssignUsernameAndEmail()
		{
			Page.Validate();
			if (!IsValid) return;

			if (Session["TwitterUser"] == null) return;

			TwitterUserInfo twitterUser = (TwitterUserInfo) Session["TwitterUser"];
			Session.Remove("TwitterUser");

			if (Request.Form[tbPickUserName.UniqueID] != null && Utils.User.GetUserIdByUserName(tbPickUserName.Text) != 0)
			{
				Response.Write(string.Format("Username {0} already exists, please select another. <a href='twitterlogin.aspx'>Try again</a>.", tbPickUserName.Text));
				Response.End();
				return;
			}

			if (Utils.User.GetUserIdByEmail(tbEmail.Text) != 0)
			{
				Response.Write(string.Format("Email {0} already exists, please select another or use the password recovery form. <a href='twitterlogin.aspx'>Try again</a>.", tbEmail.Text));
				Response.End();
				return;
			}

			string username = (Request.Form[tbPickUserName.UniqueID] != null) ? tbPickUserName.Text : twitterUser.twitterUsername;
			Utils.User.CreateUser(username, tbEmail.Text, CryptoUtils.GenerateRandomNumericCode(), twitterUser.twitterHomepage, twitterUser.twitterBio, false, string.Empty, string.Empty, string.Empty, "", twitterUser.twitterUsername, "");

			int userId = 0;
			string userName;
			Utils.User.GetUserByTwitterNameId(twitterUser.twitterUsername, out userId, out userName);

			SaveAvatarFromTwitter(userId, twitterUser.twitterImageURL);

			Utils.User.Login(userId, userName);
			Response.Redirect("default.aspx");
		}

		struct TwitterUserInfo
		{
			public string twitterUsername;
			public string twitterHomepage;
			public string twitterBio;
			public string twitterImageURL;
		}
	}
}