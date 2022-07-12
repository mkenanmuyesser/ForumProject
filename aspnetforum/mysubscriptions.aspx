<%@ Page language="c#" Codebehind="mysubscriptions.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.mysubscriptions" MasterPageFile="AspNetForumMaster.Master" %>
<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
<div class="location">
	<h2><a href="default.aspx"><asp:Label ID="lblHome" runat="server" EnableViewState="False" meta:resourcekey="lblHomeResource1">Home</asp:Label></a>
	&raquo;
	<a href="editprofile.aspx"><asp:Label ID="lblProfile" runat="server" EnableViewState="False" meta:resourcekey="lblProfileResource1">Profile</asp:Label></a>
	&raquo;
	<asp:Label ID="lblMySubs" runat="server" EnableViewState="False" meta:resourcekey="lblMySubsResource1">My subscriptions</asp:Label></h2></div>
<asp:Label ID="lblNotLoggedIn" runat="server" Visible="False" Font-Bold="True" ForeColor="Red">You are not signed in as a member. Please sign in to access your profile.</asp:Label>
<div id="divMain" runat="server">
	<table width="100%">
		<tr>
			<th colspan="2"><asp:Label ID="lblTopicMessages" runat="server" EnableViewState="False" meta:resourcekey="lblTopicMessagesResource1">New messages in a topic</asp:Label></th></tr>
		<tr>
			<td>
				<asp:DataGrid CellPadding="7" id="grid" runat="server" AutoGenerateColumns="False" Width="100%" EnableViewState="False" BorderColor="Silver" ShowHeader="false" OnItemCommand="grid_ItemCommand">
				    <AlternatingItemStyle BackColor="Lavender" />
					<Columns>
						<asp:BoundColumn Visible="False" DataField="TopicID"></asp:BoundColumn>
						<asp:HyperLinkColumn DataNavigateUrlField="TopicID" DataNavigateUrlFormatString="messages.aspx?TopicID={0}"
							DataTextField="Subject" HeaderText="Topic"></asp:HyperLinkColumn>
						<asp:ButtonColumn Text="unsubscribe" CommandName="delete"></asp:ButtonColumn>
					</Columns>
				</asp:DataGrid></td>
		</tr>
	</table>
	<br />
	<table width="100%">
		<tr>
			<th colspan="2"><asp:Label ID="lblForumTopics" runat="server" EnableViewState="False" meta:resourcekey="lblForumTopicsResource1">New topics in a forum</asp:Label></th></tr>
		<tr>
			<td>
				<asp:DataGrid CellPadding="7" id="gridForums" runat="server" AutoGenerateColumns="False" Width="100%" EnableViewState="False" BorderColor="Silver" ShowHeader="false" OnItemCommand="gridForums_ItemCommand">
				    <AlternatingItemStyle BackColor="Lavender" />
					<Columns>
						<asp:BoundColumn Visible="False" DataField="ForumID"></asp:BoundColumn>
						<asp:HyperLinkColumn DataNavigateUrlField="ForumID" DataNavigateUrlFormatString="topics.aspx?ForumID={0}"
							DataTextField="Title" HeaderText="Forum"></asp:HyperLinkColumn>
						<asp:ButtonColumn Text="unsubscribe" CommandName="delete"></asp:ButtonColumn>
					</Columns>
				</asp:DataGrid></td>
		</tr>
	</table>
	<br />
	<table width="100%">
		<tr>
			<th colspan="2"><asp:Label ID="lblForumMessages" runat="server" EnableViewState="False" meta:resourcekey="lblForumMessagesResource1">New POSTS in a forum</asp:Label></th></tr>
		<tr>
			<td>
				<asp:DataGrid CellPadding="7" id="girdForumPosts" runat="server" AutoGenerateColumns="False" Width="100%" EnableViewState="False" BorderColor="Silver" ShowHeader="false" OnItemCommand="girdForumPosts_ItemCommand">
				    <AlternatingItemStyle BackColor="Lavender" />
					<Columns>
						<asp:BoundColumn Visible="False" DataField="ForumID"></asp:BoundColumn>
						<asp:HyperLinkColumn DataNavigateUrlField="ForumID" DataNavigateUrlFormatString="topics.aspx?ForumID={0}"
							DataTextField="Title" HeaderText="Forum"></asp:HyperLinkColumn>
						<asp:ButtonColumn Text="unsubscribe" CommandName="delete"></asp:ButtonColumn>
					</Columns>
				</asp:DataGrid></td>
		</tr>
	</table>
</div>
</asp:Content>