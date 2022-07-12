<%@ Page Title="Users list" language="c#" Codebehind="allusers.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.allusers" MasterPageFile="AspNetForumMaster.Master" %>
<%@ Import Namespace="aspnetforum.Resources" %>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">

<div class="location">
	<div class="smalltoolbar">
		<span id="spanNonActive" runat="server" enableviewstate="false"><a href="allusers.aspx?Disabled=1"><%= various.DisabledUsers %></a></span>
		<span id="spanActive" runat="server" enableviewstate="false"><a href="allusers.aspx"><%= various.EnabledUsers %></a></span>
		<% if(IsAdministrator) { %><a href="adminonlineusers.aspx"><%= various.OnlineUsers %>&nbsp;</a> <% } %>
		<a href="adduser.aspx" title="add user" runat="server" id="lnkAdd" enableviewstate="false">Add user...</a>
	</div>
	<h2><a href="default.aspx"><%= various.Home %></a> &raquo; <%= various.Users %></h2>
</div>
<div>
	<span style="float:left">
		<asp:TextBox ID="tbUsername" placeholder="username or email" runat="server"></asp:TextBox> &nbsp;
		<button type="button" id="btnSearch" onclick="Search();"><%= various.Search %></button>
	</span>
</div>

<br style="clear:both" /><br />
<table width="100%" cellpadding="4" class="roundedborder"><tr>
<th></th>
<th><a href="allusers.aspx"><%= various.Username %></a></th>
<th><a href="allusers.aspx?order=email"><%= various.Email %></a></th>
<th><a href="allusers.aspx?order=regdate"><%= various.RegisteredSince %></a></th>
<th><a href="allusers.aspx?order=posts"><%= various.Posts %></a></th>
<th><a href="allusers.aspx?order=logondate"><%= various.LastLogonDate %>Last logon date</asp:Label></a></th>
<th>
	<input type="checkbox" name="cbDelAll" id="cbDelAll" onclick="CheckUncheckAllUsers();" />
</th></tr><tbody></tbody>
<asp:repeater id="rptUsersList" runat="server" EnableViewState="False">
<ItemTemplate>
<tr>
	<td><a href='viewprofile.aspx?UserID=<%# Eval("UserID") %>'><img src='<%# aspnetforum.Utils.User.GetAvatarFileName(Eval("AvatarFileName")) %>' width="25" height="25" alt="<%# Eval("Username") %>" /></a></td>
	<td><a href='viewprofile.aspx?UserID=<%# Eval("UserID") %>'>
		<%# aspnetforum.Utils.User.GetUserDisplayName(Eval("UserName"), Eval("FirstName"), Eval("LastName"))%>
	</a></td>
	<td><%# ShowEmail(Eval("Email")) %></td>
	<td><%# Eval("RegistrationDate") %></td>
	<td><a href='viewpostsbyuser.aspx?UserID=<%# Eval("UserID") %>'><%# Eval("PostsCount") %></a></td>
	<td><%# Eval("LastLogonDate") %></td>
	<td align="center">
		<% if (IsAdministrator)
		{ %>
			<input type="checkbox" name="cbDel<%# Eval("UserID") %>" />
		<% } %>
	</td>
</tr>
</ItemTemplate>
</asp:repeater>
</tbody>
<tr><th colspan="6"></th>
	<th style="white-space:nowrap">
		<asp:ImageButton ID="btnDel" ToolTip="delete selected" runat="server" ImageUrl="images/delete.png" OnClick="btnDel_Click" OnClientClick="return confirm('are you sure?')"></asp:ImageButton>
		<asp:ImageButton ID="btnDisable" ToolTip="disable selected" runat="server" ImageUrl="images/delete_bw.png" OnClick="btnDisable_Click" OnClientClick="return confirm('are you sure?')"></asp:ImageButton>
	</th>
	</tr>
</table>
<div class="pager"><%= pagerString %></div>

<script type="text/javascript">
	function CheckUncheckAllUsers() {
		var chk = false;
		if (document.getElementById("cbDelAll").checked)
			chk = true;
		$("INPUT[type='checkbox'][@id ^= 'cbDel' ]").attr('checked', chk);
	   }
	function getParameterByName(name) {
		name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
		var regexS = "[\\?&]" + name + "=([^&#]*)";
		var regex = new RegExp(regexS);
		var results = regex.exec(window.location.href);
		if (results == null)
			return "";
		else
			return decodeURIComponent(results[1].replace(/\+/g, " "));
	}
	function Search() {
		var tbId = '<%= tbUsername.ClientID %>';
		document.location.href = 'allusers.aspx?q=' + document.getElementById(tbId).value + '&Disabled=' + getParameterByName("Disabled");
	}
</script>
	
</asp:Content>