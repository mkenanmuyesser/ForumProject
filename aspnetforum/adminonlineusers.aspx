<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="adminonlineusers.aspx.cs" Inherits="aspnetforum.adminonlineusers" MasterPageFile="AspNetForumMaster.Master" %>

<asp:Content runat="server" ContentPlaceHolderID="AspNetForumContentPlaceHolder">

<table cellpadding="7">
	<tr>
		<th><%= aspnetforum.Resources.various.OnlineUsers %></th>
		<th>Current URL</th>
		<th>Last seen</th>
	</tr>
<asp:Repeater ID="rptUsers" runat="server">
<ItemTemplate>
    <tr>
    <td><%# Eval("UserName")%></td>
    <td><%# Eval("CurrentURL") %></td>
    <td><%# Eval("LastActivity")%></td>
    </tr>
</ItemTemplate>
</asp:Repeater>
</table>

</asp:Content>