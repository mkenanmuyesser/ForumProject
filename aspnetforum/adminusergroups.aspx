<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="adminusergroups.aspx.cs" Inherits="aspnetforum.adminusergroups" MasterPageFile="AspNetForumMaster.Master" %>
<%@ Register TagPrefix="cc" Namespace="aspnetforum" Assembly="aspnetforum" %>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">

<p><b><asp:Label ID="lblAvailableGroups" runat="server" EnableViewState="False" meta:resourcekey="lblAvailableGroupsResource1" Text="Available user groups:"></asp:Label></b>
	<br />
	<cc:THDataGrid CellPadding="11" id="gridGroups" Runat="server" Width="100%" AutoGenerateColumns="False" EnableViewState="False"
		ShowHeader="False" OnItemCommand="gridGroups_ItemCommand" BorderColor="Silver" meta:resourcekey="gridGroupsResource1"
		CssClass="roundedborder horizseparated" GridLines="None" CellSpacing="-1">
		<AlternatingItemStyle BackColor="Lavender" />
		<Columns>
			<asp:BoundColumn Visible="False" DataField="GroupID"></asp:BoundColumn>
			<asp:HyperLinkColumn DataNavigateUrlField="GroupID" DataTextField="Title" DataNavigateUrlFormatString="editusergroup.aspx?GroupID={0}" ItemStyle-Width="100%"></asp:HyperLinkColumn>
			<asp:TemplateColumn>
				<ItemTemplate><asp:LinkButton Text="<img src=&quot;images/delete.png&quot; alt=&quot;&quot;>" runat="server" CommandName="delete" OnClientClick="return confirm('Are you sure?');"></asp:LinkButton></ItemTemplate>
			</asp:TemplateColumn>
		</Columns>
	</cc:THDataGrid>
	<asp:Label ID="lblNoGroups" Runat="server" Visible="False" meta:resourcekey="lblNoGroupsResource1" Text="No groups found."></asp:Label>
</p>

<table width="100%" cellpadding="7" class="roundedborder noborder">
	<tr><th colspan="2"><b><asp:Label ID="lblAddNew" runat="server" EnableViewState="False" meta:resourcekey="lblAddNewResource1" Text="Add a new group:"></asp:Label></b></th></tr>
	<tbody>
	<tr>
		<td class="gray"><asp:Label ID="lblTitle" runat="server" EnableViewState="False" meta:resourcekey="lblTitleResource1" Text="Title:"></asp:Label></td>
		<td style="width:100%"><asp:TextBox id="tbGroupTitle" runat="server" Width="100%" meta:resourcekey="tbGroupTitleResource1"></asp:TextBox></td>
	</tr>
	<tr><td colspan="2"><asp:Button id="btnAddGroup" runat="server" Text="add" onclick="btnAddGroup_Click" meta:resourcekey="btnAddGroupResource1"></asp:Button></td></tr>
	</tbody>
</table>
</asp:Content>