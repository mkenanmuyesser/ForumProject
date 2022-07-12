<%@ Page language="c#" Codebehind="privateinbox.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.privateinbox" MasterPageFile="AspNetForumMaster.Master" %>
<%@ Import Namespace="aspnetforum.Resources" %>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
<div class="location">
	<h2>◀ <a href="default.aspx"><%= various.Home %></a>
	&raquo;
	<a href="editprofile.aspx"><%= various.MyProfile %></a>
	&raquo;
	<a href="privateinbox.aspx">PM</a>
	</h2></div>

<asp:Label ID="lblNotLoggedIn" runat="server" Visible="False" Font-Bold="True" ForeColor="Red">You are not signed in as a member. Please sign in to access your private messages.</asp:Label>

<asp:repeater id="rptConversationsList" runat="server" EnableViewState="False" OnItemCommand="rptConversationsList_ItemCommand">
	<HeaderTemplate>
		<table width="100%" class="roundedborder biglist">
		<tr>
			<th></th>
			<th nowrap="nowrap"><%= various.From %></th>
			<th><%= various.LatestPost %></th>
			<th><%= various.Posts %></th>
		</tr>
		<tbody>
	</HeaderTemplate>
	<ItemTemplate>
		<tr <%# Container.ItemType == ListItemType.AlternatingItem ? " class='altItem'" : "" %>>
			<td><img alt="" class="avatar" height="32" width="32" src="<%# aspnetforum.Utils.User.GetAvatarFileName(Eval("AvatarFileName"), Eval("UseGravatar"), Eval("Email")) %>" /></td>
			<td style="position:relative;width:70%;border-left:none">
				<%# Convert.ToInt32(Eval("New"))==2 ? "<img src='images/topic_unread.png' title='unread' alt='unread' style='position:absolute;left:-25px' />" : "" %>
				<span class="topictools">
					<asp:LinkButton OnClientClick="if(!confirm('Are you sure?')) return false;" ToolTip="delete" id="btnModeratorDelete" Runat="server" CommandName="delete" CommandArgument='<%# Eval("UserID") %>'><img src="images/delete-topic.png" /></asp:LinkButton>
				</span>
				<h2>
					<a href='privateinbox.aspx?UserID=<%# Eval("UserID") %>&lastpage=1'>
						<%# aspnetforum.Utils.User.GetUserDisplayName(Eval("UserName"),Eval("FirstName"), Eval("LastName"))%>
					</a>
				</h2>
			</td>
			<td class="gray2" style="text-align: center"><%# Eval("LastMessageDate") %></td>
			<td class="gray2" style="text-align: center"><%# Eval("Posts") %></td>
		</tr>
	</ItemTemplate>
	<FooterTemplate>
		</tbody></table>
	</FooterTemplate>
</asp:repeater>

<asp:repeater id="rptMessagesList" runat="server" EnableViewState="False" 
	OnItemCommand="rptMessagesList_ItemCommand" 
	onitemdatabound="rptMessagesList_ItemDataBound">
	<HeaderTemplate><table width="100%" cellpadding="19" class="roundedborder postlist"></HeaderTemplate>
	<ItemTemplate>
		<tr valign="top" <%# Container.ItemType == ListItemType.AlternatingItem ? " class='altItem'" : "" %>>
			<td>
				<span id="span1" runat="server" style="color:#FFA767;font-weight:bold" visible='<%# Convert.ToBoolean(Eval("New")) %>'>Unread</span>
				<span class="gray"><%# ToAgoString((DateTime)Eval("CreationDate")) %></span>
				<br /><br />
				<%# aspnetforum.Utils.User.DisplayUserInfo(Eval("UserID"), Eval("UserName"), null, Eval("AvatarFileName"), Eval("FirstName"), Eval("LastName"))%>
			</td>
			<td style="border-bottom:none">
				<div class="mobileshown">
					<%# aspnetforum.Utils.User.DisplayUserInfo(Eval("UserID"), Eval("UserName"), null, Eval("AvatarFileName"), Eval("FirstName"), Eval("LastName"))%>
				</div>
				<%# aspnetforum.Utils.Formatting.FormatMessageHTML(Eval("Body").ToString())%>
				<%# aspnetforum.Utils.Formatting.FormatSignature(Eval("Signature").ToString())%>
				<asp:Repeater ID="rptFiles" runat="server">
				<HeaderTemplate>
					<br /><br />
					<div class="gray">
						<%# various.Attachments %><br />
				</HeaderTemplate>
				<ItemTemplate>
					<a href='getattachment.ashx?personal=1&fileid=<%# Eval("FileID") %>'>
					<%# aspnetforum.Utils.Attachments.GetThumbnail(Eval("FileName").ToString(), Convert.ToInt32(Eval("UserID"))) %>
					<%# Eval("FileName") %></a><br />
				</ItemTemplate>
				<FooterTemplate></div></FooterTemplate>
				</asp:Repeater>
			</td>
		</tr>
		<tr class="utils <%# Container.ItemType == ListItemType.AlternatingItem ? " altItem" : "" %>">
			<td></td>
			<td class="messageActions" align="right">
				<a href='addprivatemsg.aspx?ToUserID=<%# _userId %>&amp;Quote=<%# Eval("MessageID") %>'>
				<%# various.ReplyWithQuote%></a>
				&bull;
				<asp:LinkButton id="btnDelete" runat="server" OnClientClick="if(!confirm('Are you sure?')) return false;" CommandName="delete" CommandArgument='<%# Eval("MessageID") %>'><%#various.Delete %></asp:LinkButton>
			</td>
		</tr>
	</ItemTemplate>
	<FooterTemplate>
		</table>
		<div class="pager">
			<%# pagerString %>
		</div>
	</FooterTemplate>
</asp:repeater>
</asp:Content>