<%@ Page Language="C#" MasterPageFile="AspNetForumMaster.Master" AutoEventWireup="true" CodeBehind="forumgroups.aspx.cs" Inherits="aspnetforum.forumgroups" Title="Untitled Page" %>
<%@ Register TagPrefix="cc" Namespace="aspnetforum" Assembly="aspnetforum" %>

<asp:Content ID="Content2" ContentPlaceHolderID="AspNetForumContentPlaceHolder" runat="server">
	<p>
	<asp:Label ID="lblEditForumGroups" runat="server" EnableViewState="False">Editing forum categories</asp:Label></p>
	
	<cc:THDataGrid id="gridForumGroups" runat="server" AutoGenerateColumns="False" Width="100%" UseAccessibleHeader="true" ShowHeader="false"
		GridLines="None" CellPadding="11" CssClass="roundedborder" OnEditCommand="gridForumGroups_EditCommand" OnCancelCommand="gridForumGroups_CancelCommand"
		OnUpdateCommand="gridForumGroups_UpdateCommand" OnItemCommand="gridForumGroups_ItemCommand" OnItemDataBound="gridForumGroups_ItemDataBound">
		<AlternatingItemStyle BackColor="Lavender" />
		<Columns>
			<asp:BoundColumn ReadOnly="true" DataField="GroupID" HeaderText="GroupID" Visible="false"></asp:BoundColumn>
			<asp:TemplateColumn HeaderText="GroupName" ItemStyle-Width="100%">
				<ItemTemplate><%# Eval("GroupName") %></ItemTemplate>
				<EditItemTemplate><asp:TextBox runat="server" Text='<%# Eval("GroupName") %>' MaxLength="50" /></EditItemTemplate>
			</asp:TemplateColumn>
			<asp:ButtonColumn Text="<img src='images/up.png' alt='move up' title='move up'/>" CommandName="up"></asp:ButtonColumn>
			<asp:ButtonColumn Text="<img src='images/down.png' alt='move down' title='move down'/>" CommandName="down"></asp:ButtonColumn>
			<asp:EditCommandColumn CancelText="Cancel" EditText="Edit" UpdateText="Update"></asp:EditCommandColumn>
			<asp:ButtonColumn Text="delete" CommandName="delete"></asp:ButtonColumn>
		</Columns>
	</cc:THDataGrid>
	<br />
	<asp:TextBox id="tbForumGroup" runat="server"></asp:TextBox>
	<asp:Button runat="server" ID="btnAdd" Text="+" onclick="btnAdd_Click" />
</asp:Content>
