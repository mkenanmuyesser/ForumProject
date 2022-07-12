<%@ Page language="c#" Codebehind="viewprofile.aspx.cs" EnableViewState="false" AutoEventWireup="True" Inherits="aspnetforum.viewprofile" MasterPageFile="aspnetforummaster.master" %>
<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
	<div class="location">
	<h2><asp:Label id="lblUser" runat="server" meta:resourcekey="lblUserResource1"></asp:Label>
		-
		<asp:Label ID="lblProfile" runat="server" meta:resourcekey="lblProfileResource1">profile</asp:Label></h2>
	</div>

	<div style="float:left">
	<table cellpadding="14" class="roundedborder noborder" width="400px">
		<tr>
			<td rowspan="7" valign="top">
				<img id="imgAvatar" runat="server" class="avatar" src="images/guestavatar.gif" alt="User avatar" />
			</td>
			<td><asp:Label ID="lblUsernameTitle" CssClass="gray" runat="server" meta:resourcekey="lblUsernameTitleResource1">UserName:</asp:Label></td>
			<td>
				<asp:Label id="lblUserName" runat="server"></asp:Label>
				<asp:Label id="lblFullName" runat="server"></asp:Label>
			</td>
		</tr>
		<tr>
			<td><asp:Label ID="lblTotalPosts" CssClass="gray" runat="server" meta:resourcekey="lblTotalPostsResource1">Total posts:</asp:Label></td>
			<td><a id="lnkViewPosts" runat="server"></a></td>
		</tr>
		<tr id="trRating" runat="server">
			<td><asp:Label ID="lblRating" CssClass="gray" runat="server" meta:resourcekey="lblRatingResource1">Reputation:</asp:Label></td>
			<td><asp:Label ID="lblRatingValue" runat="server"></asp:Label></td>
		</tr>
		<tr>
			<td><asp:Label ID="lblReggedSince" CssClass="gray" runat="server" meta:resourcekey="lblReggedSinceResource1">Registered since:</asp:Label></td>
			<td><asp:Label id="lblRegistrationDate" runat="server"></asp:Label></td>
		</tr>
		<tr>
			<td><asp:Label ID="lblLastLogonDate" CssClass="gray" runat="server" meta:resourcekey="lblLastLogonDateResource1">Last logon date:</asp:Label></td>
			<td><asp:Label id="lblLastLogonDateValue" runat="server"></asp:Label></td>
		</tr>
		<tr>
			<td><asp:Label ID="lblInterestsTitle" CssClass="gray" runat="server" meta:resourcekey="lblInterestsTitleResource1">Interests:</asp:Label></td>
			<td><asp:Label id="lblInterests" runat="server"></asp:Label></td>
		</tr>
		<tr>
			<td><asp:Label ID="lblHomepage" CssClass="gray" runat="server" meta:resourcekey="lblHomepageResource1">Homepage:</asp:Label></td>
			<td><asp:HyperLink id="homepage" Target="_blank" runat="server" rel="nofollow"></asp:HyperLink></td>
		</tr>
	</table>
	
	<div id="divAchievements" runat="server">
	<table cellpadding="9" class="gray roundedborder" style="width:400px;margin-bottom:15px">
		<tr><th colspan="2">Achievements</th></tr>
		<asp:Repeater id="rptAchievements" runat="server" EnableViewState="False">
			<ItemTemplate>
				<tr><td><%# (bool)Eval("Achieved") == true? "<img src='images/ok.png' alt='You got it!' />":"" %></td>
				<td><b><%#Eval("Name") %></b><br />
				<%# Eval("Description") %>
				</td>
				</tr>
			</ItemTemplate>
		</asp:Repeater>
	</table>
	</div>

	<br />
	<div id="divGroups" runat="server">
	<span class="gray">Member of groups:</span>
	<asp:GridView CellPadding="11" id="gridGroups" Runat="server" Width="100%" AutoGenerateColumns="False"
		ShowHeader="False" BorderColor="Silver" CssClass="roundedborder" CellSpacing="-1" GridLines="None">
		<AlternatingRowStyle BackColor="Lavender" />
		<Columns>
			<asp:HyperLinkField DataTextField="Title" DataNavigateUrlFields="GroupID" DataNavigateUrlFormatString="editusergroup.aspx?GroupID={0}"></asp:HyperLinkField>
		</Columns>
	</asp:GridView>
	</div>

	</div>

	<div class="usertools">
		<asp:Button id="btnEditUser" runat="server" Text="edit..." meta:resourcekey="btnEditUserResource1"></asp:Button>
		<% if(aspnetforum.Utils.User.CurrentUserID!=0 && aspnetforum.Utils.Settings.EnablePrivateMessaging) { %>
			<button type="button" onclick="document.location='addprivatemsg.aspx?ToUserID=<%= _userId %>'"><asp:Label ID="Label5" runat="server" meta:resourcekey="Label5Resource1">Send a private message</asp:Label>...</button>
		<% } %>
		<asp:Button CssClass="greybutton" id="btnDelUser" runat="server" Text="delete this user" onclick="btnDelUser_Click" OnClientClick="return confirm('are you sure?')" meta:resourcekey="btnDelUserResource1"></asp:Button>
		<asp:Button CssClass="greybutton" id="btnActivateUser" runat="server" Text="activate this user" meta:resourcekey="btnActivateUserResource1" OnClick="btnActivateUser_Click"></asp:Button>
		<asp:Button CssClass="greybutton" id="btnResendActivaton" runat="server" Text="resend activation email" meta:resourcekey="btnResendActivatonResource1" OnClick="btnResendActivaton_Click"></asp:Button>
		<asp:Button CssClass="greybutton" id="btnDisableUser" runat="server" Text="disable this user" meta:resourcekey="btnDisableUserResource1" OnClick="btnDisableUser_Click"></asp:Button>
		<asp:Button CssClass="greybutton" id="btnMakeAdmin" runat="server" Text="grant Administrator permissions" meta:resourcekey="btnMakeAdminResource1" OnClick="btnMakeAdmin_Click"></asp:Button>
		<asp:Button CssClass="greybutton" id="btnRevokeAdmin" runat="server" Text="revoke Administrator permissions" meta:resourcekey="btnRevokeAdminResource1" OnClick="btnRevokeAdmin_Click"></asp:Button>
		<asp:Button CssClass="greybutton" id="btnDeleteAllPostsAndTopics" runat="server" Text="delete all posts from this user" OnClientClick="return confirm('are you sure?')" OnClick="btnDeleteAllPostsAndTopics_Click"></asp:Button>
		<asp:Button CssClass="greybutton" id="btnDeleteAllPostsAndDelete" runat="server" Text="delete all posts and delete user" OnClientClick="return confirm('are you sure?')" OnClick="btnDeleteAllPostsAndDelete_Click"></asp:Button>
	</div>
</asp:Content>