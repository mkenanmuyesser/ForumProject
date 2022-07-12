<%@ Page Title="Unapproved posts" Language="C#" MasterPageFile="AspNetForumMaster.Master" AutoEventWireup="true" CodeBehind="unapprovedposts.aspx.cs" Inherits="aspnetforum.unapprovedposts" %>
<%@ Import Namespace="aspnetforum.Utils" %>

<asp:Content ID="Content2" ContentPlaceHolderID="AspNetForumContentPlaceHolder" runat="server">
	<div class="location">
		<h2>Moderator &raquo; Unapproved posts (<%= ModeratorStats.GetUnapprovedMsgsCount() %>) | <a href="complaints.aspx">Complaints (<%= ModeratorStats.GetComplaintsCount() %>)</a></h2>
	</div>
	
	<asp:repeater id="rptMessagesList" runat="server" EnableViewState="False" OnItemCommand="rptMessagesList_ItemCommand">
		<HeaderTemplate>
			<table width="100%" cellpadding="19">
		</HeaderTemplate>
		<ItemTemplate>
			<tr valign="top" <%# Container.ItemType == ListItemType.AlternatingItem ? " class='altItem'" : "" %>>
			<td style="width:120px" rowspan="2">
				<span class="gray"><%# ToAgoString((DateTime)Eval("CreationDate")) %>
				<br />Topic:</span>
				<a href='<%# aspnetforum.Utils.Various.GetTopicURL(Eval("TopicID"), Eval("Subject")) %>'><b><%# Eval("Subject") %></b></a>
				<br /><br />
				<%# aspnetforum.Utils.User.DisplayUserInfo(Eval("UserID"), Eval("UserName"), Eval("PostsCount"), Eval("AvatarFileName"), Eval("LastName"))%>
			</td>
			<td style="border-bottom:none;">
				<%# aspnetforum.Utils.Formatting.FormatMessageHTML(Eval("Body").ToString())%>
			</td>
		</tr>
		<tr <%# Container.ItemType == ListItemType.AlternatingItem ? " class='altItem'" : "" %>>
		<td valign="bottom" align="right" class="gray" style="border-top:none;">
			<asp:LinkButton id="btnModeratorApprove" Runat="server" CommandName="approve" CommandArgument='<%# Eval("MessageID") %>' meta:resourcekey="btnModeratorApproveResource1">&bull; <strong>approve</strong></asp:LinkButton>
			<asp:LinkButton id="btnModeratorDelete" Runat="server" CommandName="delete" CommandArgument='<%# Eval("MessageID") %>' meta:resourcekey="btnModeratorDeleteResource1">&bull; delete</asp:LinkButton>
		</td></tr>
		</ItemTemplate>
		<FooterTemplate>
			</table>
			<div class="smalltoolbar gray">
				<asp:Label ID="lblPages" runat="server" EnableViewState="False" meta:resourcekey="lblPagesResource1">pages:</asp:Label>
				<%# pagerString %>
			</div>
		</FooterTemplate>
	</asp:repeater>
	
	<div id="divNothingFound" runat="server" visible="false" enableviewstate="false">
	No messages.
	</div>
</asp:Content>
