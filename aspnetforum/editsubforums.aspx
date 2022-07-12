<%@ Page Language="C#" MasterPageFile="AspNetForumMaster.Master" AutoEventWireup="true" CodeBehind="editsubforums.aspx.cs" Inherits="aspnetforum.editsubforums" %>
<%@ Register TagPrefix="cc" Namespace="aspnetforum" Assembly="aspnetforum" %>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
	
	<p><asp:Label ID="lblHelp" runat="server" EnableViewState="False" meta:resourcekey="lblHelpResource1">A Subforum is a "parent-child" relation between two existing forums, when one forum is a subforum to another forum. For example, a "Feedback" forum can have two subforums: "General Feedback" and "Site suggestions". To make one forum a subforum of another forum, you have to create both forums and then add a "parent-child" relation using this page.</asp:Label></p>
	<p><b><asp:Label ID="lblExisting" runat="server" EnableViewState="False" meta:resourcekey="lblExistingResource1">Existing "forum-subforum" relations:</asp:Label></b>
		<br />
		<cc:THDataGrid id="gridSubForums" runat="server" AutoGenerateColumns="False" Width="100%" CellPadding="7" GridLines="None"
			OnItemCommand="gridSubForums_ItemCommand" meta:resourcekey="gridSubForumsResource1" UseAccessibleHeader="true" CssClass="roundedborder">
			<AlternatingItemStyle BackColor="Lavender" />
			<Columns>
				<asp:BoundColumn Visible="False" DataField="ParentForumID"></asp:BoundColumn>
				<asp:BoundColumn Visible="False" DataField="SubForumID"></asp:BoundColumn>
				<asp:BoundColumn DataField="ParentForumTitle" HeaderText="Parent Forum"></asp:BoundColumn>
				<asp:BoundColumn DataField="SubForumTitle" HeaderText="Sub-Forum"></asp:BoundColumn>
				<asp:ButtonColumn Text="delete this relation" CommandName="delete" meta:resourcekey="ButtonColumnResource1"></asp:ButtonColumn>
			</Columns>
		</cc:THDataGrid>
		<asp:Label ID="lblNoSubForums" Runat="server" Visible="False" meta:resourcekey="lblNoSubForumsResource1">No subforums defined yet.</asp:Label>
	</p>
	<br />
	<asp:Label ID="lblNoForumsFound" runat="server" ForeColor="Red" Visible="False" meta:resourcekey="lblNoForumsFoundResource1">No forums found. Go back and create some forums before adding subforums.</asp:Label>
	<div id="divAddSubforum" runat="server">
		<table cellpadding="7" class="roundedborder noborder">
		    <tr><th colspan="2"><asp:Label ID="lblDefine" runat="server" EnableViewState="False" meta:resourcekey="lblDefineResource1">Define a subforum:</asp:Label></th></tr>
			<tbody>
		    <tr><td colspan="2" class="gray"><asp:Label ID="lblNote" runat="server" EnableViewState="False" meta:resourcekey="lblNoteResource1">to make one forum a subforum of another forum, select a parent forum, then select a sub-forum, and click "add".</asp:Label></td></tr>
			<tr>
				<td style="white-space:nowrap" class="gray"><asp:Label ID="lblParent" runat="server" EnableViewState="False" meta:resourcekey="lblParentResource1">Parent forum:</asp:Label></td>
				<td style="width:100%;"><asp:DropDownList ID="ddlParentForum" runat="server" DataValueField="ForumID" DataTextField="Title" meta:resourcekey="ddlParentForumResource1"></asp:DropDownList></td>
			</tr>
			<tr>
				<td style="white-space:nowrap" class="gray"><asp:Label ID="lblSubForums" runat="server" EnableViewState="False" meta:resourcekey="lblSubForumsResource1">Sub forum:</asp:Label></td>
				<td style="width:100%;"><asp:DropDownList ID="ddlSubForum" runat="server" DataValueField="ForumID" DataTextField="Title" meta:resourcekey="ddlSubForumResource1"></asp:DropDownList></td>
			</tr>
			<tr><td colspan="2"><asp:Button ID="btnAdd" runat="server" Text="Add" CssClass="gradientbutton" OnClick="btnAdd_Click" meta:resourcekey="btnAddResource1" />
			<asp:Label ID="lblError" runat="server" Visible="False" ForeColor="Red" meta:resourcekey="lblErrorResource1">ERROR: You either trying to make a forum a subforum of itself OR relation already exists.</asp:Label></td></tr>
			</tbody>
		</table>
	</div>
</asp:Content>
