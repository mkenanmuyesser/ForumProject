using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using aspnetforum.Utils;
using aspnetforum.Utils.openid.RelyingParty;
using aspnetforum.Utils.openid;
using System.Data.Common;
using Jitbit.Utils;

namespace aspnetforum
{
	public partial class OpenIdLogin : ForumPage
	{
/*this code uses a modified version of the free opensource DotNetOpenId library ver 2.6
Here is the copyright notice of the authors:

Copyright (c) 2007, Andrew Arnott, Scott Hanselman, Jason Alexander, et. al. All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
* Neither the name of the DotNetOpenId nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.*/


		protected void Page_Load(object sender, EventArgs e)
		{
			if (!Utils.Settings.EnableOpenId)
			{
				Response.End();
				return;
			}

			if (Master is AspNetForumMaster)
			{
				((AspNetForumMaster)Master).ShowLoginTable = false;
			}

			//if its post back
			if (IsPostBack)
			{
				divLogin.Visible = false;
				if (Request.Form["openid_identifier"] != null) //user entered the openid URL and pressed "login"
				{
					try
					{
						DoLogin(Request.Form["openid_identifier"]);
					}
					catch (OpenIdException oex)
					{
						Response.Write(oex.ToString());
						Response.End();
						return;
					}
				}
				else if (Request.Form[tbPickUserName.UniqueID] != null)
				{
					AssignUsername();
				}
			}
			else
			{
				OpenIdRelyingParty openid = new OpenIdRelyingParty();
				var response = openid.Response;
				if (response != null)
				{
					ProcessOpenIdResponse(response);
				}
			}
		}

		protected void AssignUsername()
		{
			Page.Validate();
			if (!IsValid) return;

			string openid = Session["OpenIdUserName"] as string;
			Session.Remove("OpenIdUserName");

			if (Utils.User.GetUserIdByUserName(tbPickUserName.Text) == 0)
			{
				if (Utils.User.GetUserIdByEmail(tbEmail.Text) == 0)
				{
					Utils.User.CreateUser(tbPickUserName.Text, tbEmail.Text, CryptoUtils.GenerateRandomNumericCode(), string.Empty, string.Empty, false, string.Empty, string.Empty, string.Empty, openid, "", "");

					int userId = 0;
					string userName;
					GetUserByOpenId(openid, out userId, out userName);
					Utils.User.Login(userId, userName);

					Response.Redirect("default.aspx");
				}
				else
				{
					Response.Write(string.Format("Email {0} already exists, please select another or use the password recovery form. <a href='OpenIdLogin.aspx'>Try again</a>.", tbEmail.Text));
					Response.End();
				}
			}
			else
			{
				Response.Write(string.Format("Username {0} already exists, please select another. <a href='OpenIdLogin.aspx'>Try again</a>.", tbPickUserName.Text));
				Response.End();
			}
		}

		private void DoLogin(string login)
		{
			if (!Identifier.IsValid(login)) return;

			OpenIdRelyingParty openid = new OpenIdRelyingParty();
			IAuthenticationRequest request = openid.CreateRequest(login);

			// Send your visitor to their Provider for authentication.
			request.RedirectToProvider();

		}

		private void ProcessOpenIdResponse(IAuthenticationResponse response)
		{
			switch (response.Status)
			{
				case AuthenticationStatus.Authenticated:
					string openId = response.ClaimedIdentifier;
					int userId;
					string userName;
					GetUserByOpenId(openId, out userId, out userName);
					//int userId = BusinessLayer.User.GetUserIDByOpenIdUsername(response.ClaimedIdentifier);
					if (userId != 0)
					{
						Utils.User.Login(userId, userName);
						Response.Redirect("default.aspx");
					}
					else //we have to add a new user
					{
						Session["OpenIdUserName"] = openId;
						divLogin.Visible = false;
						divPickLogin.Visible = true;
						lblOpenId.Text = openId;
						lblOpenId2.Text = openId;
					}
					break;
			}
		}

		private void GetUserByOpenId(string openId, out int userId, out string userName)
		{
			Cn.Open();
			DbDataReader dr = Cn.ExecuteReader("SELECT UserID, UserName FROM ForumUsers WHERE OpenIdUserName=?", openId);
			userId = 0;
			userName = null;
			if (dr.Read())
			{
				userId = Convert.ToInt32(dr["UserID"]);
				userName = dr["UserName"].ToString();
			}
			dr.Close();
			Cn.Close();
		}
	}
}
