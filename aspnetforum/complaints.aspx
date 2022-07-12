<%@ Page Title="Complaints" Language="C#" MasterPageFile="AspNetForumMaster.Master" AutoEventWireup="true" CodeBehind="complaints.aspx.cs" Inherits="aspnetforum.complaints" %>
<%@ Import Namespace="aspnetforum.Utils" %>

<asp:Content ID="Content2" ContentPlaceHolderID="AspNetForumContentPlaceHolder" runat="server">
    <div class="location">
		<h2>Moderator &raquo; Complaints (<%= ModeratorStats.GetComplaintsCount() %>) | <a href="unapprovedposts.aspx">Unapproved posts (<%= ModeratorStats.GetUnapprovedMsgsCount() %>)</a></h2>
	</div>
	
	<asp:repeater id="rptMessagesList" runat="server" EnableViewState="False" 
        OnItemCommand="rptMessagesList_ItemCommand" 
        onitemdatabound="rptMessagesList_ItemDataBound">
	    <HeaderTemplate>
	        <table width="100%" cellpadding="5">
	    </HeaderTemplate>
		<ItemTemplate>
			<tr>
			    <th style="width:120px"></th>
				<th>
					<div style="float:left"><%# ToAgoString((DateTime)Eval("CreationDate"))%></div>
				    <div style="float:right">
					    <asp:LinkButton id="btnModeratorUnflag" Runat="server" CommandName="remove" CommandArgument='<%# Eval("MessageID") %>' meta:resourcekey="btnModeratorDeleteResource1">&bull; flag as complete (remove from the list)</asp:LinkButton>
				    </div>
				</th>
			</tr>
			<tr valign="top">
				<td>
					topic: <a href='<%# aspnetforum.Utils.Various.GetTopicURL(Eval("TopicID"), Eval("Subject")) %>'>
						<b><%# Eval("Subject") %></b></a>
					<br/><br/>
					<%# aspnetforum.Utils.User.DisplayUserInfo(Eval("UserID"), Eval("UserName"), Eval("PostsCount"), Eval("AvatarFileName"), Eval("FirstName"), Eval("LastName"))%>
				</td>
				<td>
				    <span style="color:Red">reported by:
				    <b>
				    <asp:Repeater ID="rptComplainUsers" runat="server">
				        <ItemTemplate><%# Eval("UserName")%></ItemTemplate>
				    </asp:Repeater>
				    </b>
				    </span>
				    <hr />
					<%# aspnetforum.Utils.Formatting.FormatMessageHTML(Eval("Body").ToString())%>
				</td>
			</tr>
		</ItemTemplate>
		<FooterTemplate>
		    <tr>
			<th colspan="2">
				<asp:Label ID="lblPages" runat="server" EnableViewState="False" meta:resourcekey="lblPagesResource1">pages:</asp:Label>
				<%# pagerString %>
			</th></tr>
		    </table>
		</FooterTemplate>
	</asp:repeater>
	
	<div id="divNothingFound" runat="server" visible="false" enableviewstate="false">
	No messages.
	</div>	
</asp:Content>
