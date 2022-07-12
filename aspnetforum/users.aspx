<%@ Page Title="Users list" Language="C#" AutoEventWireup="true" CodeBehind="users.aspx.cs" Inherits="aspnetforum.users" MasterPageFile="AspNetForumMaster.Master" %>
<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
<div class="location">
	<div class="smalltoolbar"><a href="allusers.aspx"><asp:Label ID="lblAll" runat="server" EnableViewState="False" meta:resourcekey="lblAllResource1">List of all users</asp:Label></a>
	<a href="allusers.aspx?Admin=1">Administrators</a>
	<span id="spanNonActive" runat="server" enableviewstate="false"><a href="allusers.aspx?Disabled=1"><asp:Label ID="lblNonActivated" runat="server" EnableViewState="False" meta:resourcekey="lblNonActivatedResource1">NON-activated users</asp:Label></a></span>
	<a href="adminonlineusers.aspx" runat="server" id="lnkOnlineUsers" enableviewstate="false" meta:resourcekey="lnkOnlineUsers">Online users</a>
	<span id="spanAddUser" runat="server" enableviewstate="false"><a href="adduser.aspx"><asp:Label runat="server" EnableViewState="False" meta:resourcekey="lblAddUserResource1">Add user</asp:Label></a></span>
	</div>	

	<h2><a href="default.aspx"><asp:Label ID="lblHome" runat="server" EnableViewState="False" meta:resourcekey="lblHomeResource1">Home</asp:Label></a>
	&raquo;
	<asp:Label ID="lblUsers" runat="server" EnableViewState="False" meta:resourcekey="lblUsersResource1">Users</asp:Label></h2>
</div>
		
<asp:Repeater id="rptRecent" runat="server" EnableViewState="false">
	<HeaderTemplate><table style="float:left;margin-right:20px;" class="roundedborder" cellpadding="4"><thead><tr><th colspan="2"><asp:Label ID="lblRecet" runat="server" EnableViewState="False" meta:resourcekey="lblRecetResource1">Newly registered users</asp:Label></th></tr></thead></HeaderTemplate>
	<ItemTemplate>
		<tr>
		<td><a href='viewprofile.aspx?UserID=<%# Eval("UserID") %>'><img src='<%# aspnetforum.Utils.User.GetAvatarFileName(Eval("AvatarFileName")) %>' width="25" height="25" /></a></td>
		<td><a href='viewprofile.aspx?UserID=<%# Eval("UserID") %>'>
			<%# aspnetforum.Utils.User.GetUserDisplayName(Eval("UserName"), Eval("FirstName"), Eval("LastName"))%>
		</a></td></tr>
	</ItemTemplate>
	<FooterTemplate></table></FooterTemplate>
</asp:Repeater>


<asp:Repeater id="rptRecentlyActive" runat="server" EnableViewState="false">
	<HeaderTemplate><table style="float:left;margin-right:20px;" class="roundedborder" cellpadding="4"><thead><tr><th colspan="3"><asp:Label ID="lblRecet" runat="server" EnableViewState="False" meta:resourcekey="lblRecentlyActiveResource1">Last two weeks active users</asp:Label></th></tr></thead></HeaderTemplate>
	<ItemTemplate>
		<tr>
			<td><a href='viewprofile.aspx?UserID=<%# Eval("UserID") %>'><img src='<%# aspnetforum.Utils.User.GetAvatarFileName(Eval("AvatarFileName")) %>' width="25" height="25" /></a></td>
			<td><a href='viewprofile.aspx?UserID=<%# Eval("UserID") %>'>
				<%# aspnetforum.Utils.User.GetUserDisplayName(Eval("UserName"), Eval("FirstName"), Eval("LastName"))%>
			</a></td>
			<td>posts: <%# Eval("MsgCount") %></td>
		</tr>
	</ItemTemplate>
	<FooterTemplate></table></FooterTemplate>
</asp:Repeater>


<asp:Repeater id="rptMostActive" runat="server" EnableViewState="false">
	<HeaderTemplate><table cellpadding="4" class="roundedborder"><thead><tr><th colspan="3"><asp:Label ID="lblActive" runat="server" EnableViewState="False" meta:resourcekey="lblActiveResource1">Most active users</asp:Label></th></tr></thead></HeaderTemplate>
	<ItemTemplate>
		<tr>
			<td><a href='viewprofile.aspx?UserID=<%# Eval("UserID") %>'><img src='<%# aspnetforum.Utils.User.GetAvatarFileName(Eval("AvatarFileName")) %>' width="25" height="25" /></a></td>
			<td><a href='viewprofile.aspx?UserID=<%# Eval("UserID") %>'>
				<%# aspnetforum.Utils.User.GetUserDisplayName(Eval("UserName"), Eval("FirstName"), Eval("LastName"))%>
			</a></td>
			<td>posts: <%# Eval("MsgCount") %></td>
		</tr>
	</ItemTemplate>
	<FooterTemplate></table></FooterTemplate>
</asp:Repeater>

<br style="clear:both" />

</asp:Content>
