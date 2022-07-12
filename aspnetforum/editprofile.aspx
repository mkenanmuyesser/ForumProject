<%@ Page language="c#" EnableViewState="false" Codebehind="editprofile.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.editprofile" MasterPageFile="aspNetForumMaster.Master" ValidateRequest="false" %>
<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
<div class="location">
	<h2><a href="default.aspx"><asp:Label ID="lblHome" runat="server" meta:resourcekey="lblHomeResource1">Home</asp:Label></a>
	&raquo;
	<asp:Label ID="lblProfile" runat="server" meta:resourcekey="lblProfileResource1">Profile</asp:Label></h2>
	<asp:HyperLink ID="lblMySubs" NavigateUrl="mysubscriptions.aspx" runat="server" meta:resourcekey="lblMySubsResource1">My subscriptions</asp:HyperLink> |
	<asp:HyperLink ID="lblInbox" NavigateUrl="privateinbox.aspx" runat="server" meta:resourcekey="lblInboxResource1">Personal messages - Inbox</asp:HyperLink>
</div>
<asp:Label ID="lblNotLoggedIn" runat="server" Visible="False" Font-Bold="True" ForeColor="Red" meta:resourcekey="lblNotLoggedInResource1">You are not signed in as a member. Please sign in to access your profile.</asp:Label>
<div id="divMain" runat="server">
<div><asp:Label id="lblResult" runat="server" EnableViewState="false" Font-Bold="True" ForeColor="Red" meta:resourcekey="lblResultResource1"></asp:Label></div>

<table cellpadding="9" style="width:500px;float:left;margin-right:15px;margin-bottom:15px" class="noborder gray roundedborder">
	<tr>
		<th colspan="2"><asp:Label ID="lblMyProfile" runat="server" meta:resourcekey="lblMyProfileResource1">My Profile</asp:Label></th></tr>
	<tr>
		<td width="50%" align="right" class="gray"><b style="color:red">*</b> <asp:Label ID="lblUsername" runat="server" meta:resourcekey="lblUsernameResource1">UserName:</asp:Label></td>
		<td><asp:TextBox id="tbUsername" runat="server" Width="100%"></asp:TextBox></td>
	</tr>
	<tr>
		<td align="right"><b style="color:red">*</b> <asp:Label ID="lblEmail" runat="server" meta:resourcekey="lblEmailResource1">Email (NOT shared):</asp:Label></td>
		<td><asp:TextBox id="tbEmail" runat="server" Width="100%"></asp:TextBox></td>
	</tr>
	<tr>
		<td align="right"><asp:Label ID="lblFirstName" runat="server" meta:resourcekey="lblFirstNameResource1">First Name:</asp:Label></td>
		<td><asp:TextBox id="tbFirstName" runat="server" Width="100%"></asp:TextBox></td>
	</tr>
	<tr>
		<td align="right"><asp:Label ID="lblLastName" runat="server" meta:resourcekey="lblLastNameResource1">Last Name:</asp:Label></td>
		<td><asp:TextBox id="tbLastName" runat="server" Width="100%"></asp:TextBox></td>
	</tr>
	<tr>
		<td align="right"><asp:Label ID="lblHomepage" runat="server" meta:resourcekey="lblHomepageResource1">Homepage:</asp:Label></td>
		<td><asp:TextBox id="tbHomepage" runat="server" MaxLength="50" Width="100%"></asp:TextBox></td>
	</tr>
	<tr>
		<td align="right"><asp:Label ID="lblInterests" runat="server" meta:resourcekey="lblInterestsResource1">Interests:</asp:Label></td>
		<td><asp:TextBox id="tbInterests" runat="server" MaxLength="255" Width="100%"></asp:TextBox></td>
	</tr>
	<tr>
		<td align="right">
			<asp:Label ID="lblSignature" runat="server" meta:resourcekey="lblSignatureResource1">Signature:</asp:Label>
			<br />
			(No tags. "BBCode" only - [url][/url], [img][/img], [b][/b], [i][/i])
		</td>
		<td><asp:TextBox id="tbSignature" runat="server" TextMode="MultiLine" Rows="3" Width="100%"></asp:TextBox></td>
	</tr>
	<tr>
		<td></td>
		<td><asp:CheckBox ID="cbHidePresence" runat="server" Text="Hide me from the user lists (not recommended)" meta:resourcekey="cbHidePresenceResource1" /></td>
	</tr>
	<tr>
		<td></td><td>
			<asp:Button CssClass="gradientbutton" id="btnSave" runat="server" Text="save" onclick="btnSave_Click" meta:resourcekey="btnSaveResource1"></asp:Button></td></tr>
</table>




<table cellpadding="9" style="width:300px;margin-bottom:15px" id="tblChangePsw" runat="server" class="noborder roundedborder">
	<tr>
		<th colspan="2"><asp:Label ID="lblChangePsw" runat="server" meta:resourcekey="lblChangePswResource1">Change Password</asp:Label></th></tr>
	<tr>
		<td width="50%" align="right" class="gray"><asp:Label ID="lblOldPsw" runat="server" meta:resourcekey="lblOldPswResource1">Old password:</asp:Label></td>
		<td><asp:TextBox id="tbOldPsw" runat="server" TextMode="Password" Width="100%"></asp:TextBox></td>
	</tr>
	<tr>
		<td align="right" class="gray"><asp:Label ID="lblNewPsw" runat="server" meta:resourcekey="lblNewPswResource1">New password:</asp:Label></td>
		<td><asp:TextBox id="tbNewPsw1" runat="server" TextMode="Password" Width="100%"></asp:TextBox></td>
	</tr>
	<tr>
		<td align="right" class="gray"><asp:Label ID="lblConfPsw" runat="server" meta:resourcekey="lblConfPswResource1">Confirm new password:</asp:Label></td>
		<td><asp:TextBox id="tbNewPsw2" runat="server" TextMode="Password" Width="100%"></asp:TextBox></td>
	</tr>
	<tr>
		<td></td><td>
			<asp:Button CssClass="gradientbutton" id="btnChangePsw" runat="server" Text="change" onclick="btnChangePsw_Click" meta:resourcekey="btnChangePswResource1"></asp:Button>
		</td></tr>
</table>



<table cellpadding="9" id="tblGroups" class="gray roundedborder" runat="server" style="width:300px;margin-bottom:15px" visible="false">
<tr><th colspan="2">Groups</th></tr>
<tr><td>Member of:</td><td>Not a member of:</td></tr>
<tr valign="top">
	<td height="100%">
		<asp:Repeater id="rptMember" runat="server" EnableViewState="False" OnItemCommand="rptMember_ItemCommand">
			<ItemTemplate>
				<asp:linkbutton runat="server" id="btnRemoveUser" CommandName="remove" CommandArgument='<%# Eval("GroupID") %>'><%# Eval("Title")%></asp:linkbutton> <b>;</b> </ItemTemplate>
		</asp:Repeater>
	</td>
	<td>
		<asp:Repeater id="rptNotMember" runat="server" EnableViewState="False" OnItemCommand="rptNotMember_ItemCommand">
			<ItemTemplate>
				<asp:linkbutton runat="server" id="btnAddUser" CommandName="add" CommandArgument='<%# Eval("GroupID") %>'><%# Eval("Title")%></asp:linkbutton> <b>;</b> </ItemTemplate>
		</asp:Repeater>
	</td>
</tr>
</table>




<table cellpadding="9" id="tblAvatar" runat="server" style="width:500px;clear:both" class="noborder roundedborder">
	<tr><th colspan="2"><asp:Label ID="lblAvatar" runat="server" meta:resourcekey="lblAvatarResource1">Avatar picture</asp:Label></th></tr>
	<tr><td colspan="2"><img id="imgAvatar" class="avatar" runat="server" alt="" /></td></tr>
	<tr valign="top">
		<td class="gray" align="right">
			<asp:Label ID="lblAvatar2" runat="server" meta:resourcekey="lblAvatarResource1">Avatar picture</asp:Label>
			<br />
			(max <asp:Label ID="lblMaxSize" runat="server" meta:resourcekey="lblMaxSizeResource1"></asp:Label> bytes,<br />
			max <asp:Label ID="lblMaxDimenstions" runat="server" meta:resourcekey="lblMaxDimenstionsResource1"></asp:Label> pixels<br />
			for uploaded files)<br />
			<asp:Label ID="lblLeaveBlank" runat="server" meta:resourcekey="lblLeaveBlankResource1">leave blank to remove your current avatar</asp:Label>:
		</td>
		<td>
			<asp:TextBox runat="server" ID="tbAvatarURL" Width="100%" placeholder="URL including https:// prefix"></asp:TextBox>
			<br />or<br />
			<asp:FileUpload ID="avatarUpload" runat="server" Width="100%" />
		</td>
	</tr>
	<tr id="trGravatar" runat="server">
		<td class="gray" align="right"><a href="http://www.gravatar.com" target="_blank">Gravatar...</a></td>
		<td class="gray">
			<div style="float:left;padding: 10px 10px 10px 10px;text-align:center;">
				<label for="rbGravatar"><img class="avatar" src="<%= GetGravatarUrl() %>" alt="avatar" /></label><br />
				<input type="radio" name="DefaultAvatarInput" id="rbGravatar" value="GRAVATAR" />
			</div>
		</td></tr>
	<tr valign="top">
		<td class="gray" align="right">Predefined avatars:</td>
		<td style="width:60%">
			<asp:Repeater ID="rptDefaultAvatars" runat="server">
			<ItemTemplate>
				<div style="float:left;padding: 10px 10px 10px 10px;text-align:center">
				<label for="rbAvatar<%# Container.DataItem %>"><img class="avatar" src="images/<%# Container.DataItem %>" alt="avatar" /></label><br />
				<input type="radio" name="DefaultAvatarInput" id="rbAvatar<%# Container.DataItem %>" value="<%# Container.DataItem %>" />
				</div>
			</ItemTemplate>
			</asp:Repeater>
			<asp:Label ID="lblAvatarsNote" CssClass="gray" runat="server">Administrators: you can add more predefined avatars by adding images to the "images" folder, naming them "AspNetForumAvatar1.gif", ""AspNetForumAvatar2.jpg" etc. The file extension shoud be one of "jpg/gif/png".</asp:Label>
			<br /><br />
		</td>
	</tr>
	<tr>
		<td></td>
		<td>
			<asp:Button CssClass="gradientbutton" ID="btnSaveAvatar" runat="server" Text="OK" OnClick="btnSaveAvatar_Click" />
		</td>
	</tr>
</table>
</div>
</asp:Content>