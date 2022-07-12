<%@ Page language="c#" Codebehind="topics.aspx.cs" EnableViewState="false" AutoEventWireup="True" Inherits="aspnetforum.topics" MasterPageFile="AspNetForumMaster.Master" %>
<%@ Import Namespace="aspnetforum.Resources" %>

<asp:Content ContentPlaceHolderID="ContentPlaceHolderHEAD" ID="AspNetForumHead" runat="server">
<link rel="alternate" type="application/rss+xml" title="topics in this forum" id="rssDiscoverLink" runat="server" />
</asp:Content>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">

<div class="location">
	<span class="smalltoolbar">
		<span id="spanRegister" runat="server" enableviewstate="false">
			<%= various.PleaseLoginRegisterToCreateTopic %>
		</span>
		<span class="mobilehidden" id="spanSubscribe" runat="server" enableviewstate="false">
			<asp:LinkButton id="btnSubscribe" Runat="server" Visible="False" onclick="btnSubscribe_Click" EnableViewState="False"><%= various.WatchForTopics %></asp:LinkButton>
			<asp:LinkButton id="btnUnsubscribe" Runat="server" Visible="False" onclick="btnUnsubscribe_Click" EnableViewState="False"><%= various.StopWatchingForum %></asp:LinkButton> 
			<asp:LinkButton id="btnSubscribeMsgs" Runat="server" Visible="False" onclick="btnSubscribeMsgs_Click" EnableViewState="False"><%= various.WatchForPosts %></asp:LinkButton>
			<asp:LinkButton id="btnUnsubscribeMsgs" Runat="server" Visible="False" onclick="btnUnsubscribeMsgs_Click" EnableViewState="False"><%= various.StopWatchingForumPosts %></asp:LinkButton>
		</span>
	</span>
	<h2>◀ <a href="default.aspx"><%= various.Home %></a></h2>

	<h1><asp:Label id="lblCurForum" runat="server" EnableViewState="False"></asp:Label>
		<span id="spanAddTopic" class="postbutton" runat="server" enableviewstate="false">
			<a class="button" href="addpost.aspx?ForumID=<%= _forumID %>"><%= various.NewTopic %></a>
		</span>

	</h1>

	<a runat="server" id="rssLink" enableviewstate="false"><img alt="topics in this forum - RSS" src="images/rss.png" /></a>
	<span id="divDescription" class="gray" runat="server"></span>
</div>


<asp:repeater id="rptSubForumsList" runat="server" EnableViewState="False">
	<HeaderTemplate>
		<table width="100%" class="biglist roundedborder"><tr><th colspan="3"><%= various.SubForums %></th></tr>
		<tbody>
	</HeaderTemplate>
	<ItemTemplate>
		<tr <%# Container.ItemType == ListItemType.AlternatingItem ? " class='altItem'" : "" %> >
			<td align="center" style="width:10%;border-right:none;"><img alt="" src="<%# aspnetforum.forums.GetForumIcon(Eval("IconFile")) %>" height="32" width="32" /></td>
			<td style="width:55%;border-left:none"><h2>
				<a href='<%# aspnetforum.Utils.Various.GetForumURL(Eval("ForumID"), Eval("Title")) %>'><%# Eval("Title") %></a>
				</h2>
				<span class="gray2"><%# Eval("Description") %></span>
			</td>
			<td width="50" class="gray2" style="text-align: center">
				<%# Eval("Topics") %></td>
			<td style="white-space:nowrap" class="gray2">
				<%# aspnetforum.Utils.Topic.GetTopicInfoBMessageyID(Eval("LatestMessageID"), Cmd)%></td>
		</tr>
	</ItemTemplate>
	<FooterTemplate>
		</tbody></table>
		<br />
	</FooterTemplate>
</asp:repeater>

<asp:Label ID="lblDenied" Runat="server" ForeColor="Red" Font-Bold="True" Visible="False" EnableViewState="False">	<%= various.AccessToForumDenied %></asp:Label>
<div id="divMain" runat="server" enableviewstate="false">
	
	<table id="tblError" runat="server" visible="false" width="100%" cellpadding="11" enableviewstate="false">
	<tr><td><div id="divError" runat="server" enableviewstate="false" visible="false"></div></td></tr>
	</table>
	
	<asp:repeater id="rptTopicsList" runat="server" EnableViewState="False" OnItemDataBound="rptTopicsList_ItemDataBound" OnItemCommand="rptTopicsList_ItemCommand">
		<HeaderTemplate>
			<table width="100%" class="roundedborder biglist">
			<tr>
				<th></th>
				<th nowrap="nowrap"><%= various.LatestPost %></th>
				<th class="mobilehidden"><%= various.Views %></th>
				<th><%= various.Posts %></th>
			</tr>
			<tbody>
		</HeaderTemplate>
		<ItemTemplate>
			<tr <%# Container.ItemType == ListItemType.AlternatingItem ? " class='altItem'" : "" %>>
				<td style="position:relative;width:70%">
					<img alt="topic" id="imgTopic" runat="server" enableviewstate="false" style="position:absolute;left:-25px" />
					<span class="topictools mobilehidden">
						<asp:LinkButton id="btnModeratorApprove" Runat="server" Visible="False" CommandName="approve" CommandArgument='<%# Eval("TopicID") %>'><img src="images/approve.png" /></asp:LinkButton>
						<asp:LinkButton ID="btnModeratorStick" runat="server" Visible="False" ToolTip="stick" CommandName="stick" CommandArgument='<%# Eval("TopicID") %>'><img src="images/stick.png" /></asp:LinkButton>
						<asp:LinkButton ID="btnModeratorUnStick" runat="server" Visible="False" ToolTip="unstick" CommandName="unstick" CommandArgument='<%# Eval("TopicID") %>'><img src="images/stick.png" /></asp:LinkButton>
						<asp:LinkButton ID="btnModeratorClose" runat="server" Visible="False" ToolTip="close" CommandName="close" CommandArgument='<%# Eval("TopicID") %>'><img src="images/lock.png" /></asp:LinkButton>
						<asp:LinkButton ID="btnModeratorReopen" runat="server" Visible="False" CommandName="reopen" CommandArgument='<%# Eval("TopicID") %>'><img src="images/lock.png" /></asp:LinkButton>
						<asp:LinkButton OnClientClick="if(!confirm('Are you sure?')) return false;" ToolTip="delete" id="btnModeratorDelete" Runat="server" Visible="False" CommandName="delete" CommandArgument='<%# Eval("TopicID") %>'><img src="images/delete-topic.png" /></asp:LinkButton>
					</span>
					<h2>
						<a href='<%# aspnetforum.Utils.Various.GetTopicURL(Eval("TopicID"), Eval("Subject")) %>'>
							<%# Eval("Subject") %>
						</a>
					</h2>
					<span class="gray"><%# ShowPageLinks(Eval("Messages"), Eval("TopicID"), Eval("Subject"))%></span>
					<span class="gray2">
							<%= various.From %>
						<%# DisplayUserName(Eval("UserName"), Eval("UserID"), Eval("FirstName"), Eval("LastName"))%></span>
				</td>
				<td style="white-space:nowrap;text-align: center" class="gray">
					<%# aspnetforum.Utils.Message.FormatMessageInfo(Eval("LastMessageID"), Eval("TopicID"), Eval("LastUserID"), Eval("LastUserName"), Eval("LastFirstName"), Eval("LastLastName"), Eval("Subject"), Eval("CreationDate"), Eval("Messages") as int?, Eval("Body"))%></td>
				<td class="gray2 mobilehidden" style="text-align: center">
					<%# Eval("ViewsCount") %></td>
				<td class="gray2" style="text-align: center">
					<%# Eval("Messages") %></td>
			</tr>
		</ItemTemplate>
		<FooterTemplate>
			</tbody></table>
		</FooterTemplate>
	</asp:repeater>

	<div class="pager"><%= pagerString %></div>
	
</div>
</asp:Content>