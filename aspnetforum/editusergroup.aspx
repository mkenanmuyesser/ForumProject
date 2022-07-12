<%@ Page language="c#" Codebehind="editusergroup.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.editusergroup" MasterPageFile="aspNetForumMaster.Master" %>
<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
	
	<p><asp:Label ID="lblClick" runat="server" EnableViewState="False" meta:resourcekey="lblClickResource1">Click on a username to add/remove a user to/from the group:</asp:Label></p>
	<table width="100%" style="height:400px" cellpadding="3">
		<tr>
			<th align="center" style="width:50%">
				<asp:Label ID="lblUsersIn" runat="server" EnableViewState="False" meta:resourcekey="lblUsersInResource1">Users in this group</asp:Label></th>
			<th align="center">
			    <asp:Label ID="lblUsersNotIn" runat="server" EnableViewState="False" meta:resourcekey="lblUsersNotInResource1">Users not in this group</asp:Label></th>
		</tr>
		<tr valign="top">
			<td height="100%">
				<div style="overflow:auto;height:100%">
				<asp:Label ID="lblNoUsersInGroup" Runat="server" Visible="False" meta:resourcekey="lblNoUsersInGroupResource1" Text="No users found"></asp:Label>
				<asp:Repeater id="rptAllowed" runat="server" EnableViewState="False" OnItemCommand="rptAllowed_ItemCommand">
					<ItemTemplate>
					    <asp:linkbutton runat="server" id="btnRemoveUser" CommandName="remove" CommandArgument='<%# Eval("UserID") %>' meta:resourcekey="btnRemoveUserResource1"><%# Eval("Username") %></asp:linkbutton> <b>;</b> </ItemTemplate>
				</asp:Repeater>
				</div>
			</td>
			<td>
			    <div style="overflow:auto;height:100%">
				<asp:Repeater id="rptDenied" runat="server" EnableViewState="False" OnItemCommand="rptDenied_ItemCommand">
					<ItemTemplate>
					    <asp:linkbutton runat="server" id="btnAddUser" CommandName="add" CommandArgument='<%# Eval("UserID") %>' meta:resourcekey="btnAddUserResource1"><%# Eval("Username") %></asp:linkbutton> <b>;</b> </ItemTemplate>
				</asp:Repeater>
				</div>
			</td>
		</tr>
	</table>
</asp:Content>