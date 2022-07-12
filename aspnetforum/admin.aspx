<%@ Page language="c#" ValidateRequest="false" Codebehind="admin.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.admin" MasterPageFile="AspNetForumMaster.Master" %>
<%@ Register TagPrefix="cc" Namespace="aspnetforum" Assembly="aspnetforum" %>
<%@ Import Namespace="aspnetforum.Resources" %>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">

<p><a href="http://www.jitbit.com/asp-net-forum/versionhistory/" style="color:Red" id="lnkUpgrade" runat="server" visible="false" target="_blank" enableviewstate="false">A newer version detected. Click here to learn more.</a></p>

<p>
	<cc:THDataGrid id="gridForums" runat="server" AutoGenerateColumns="False" Width="100%" EnableViewState="False"
		ShowHeader="true" UseAccessibleHeader="true" OnItemCommand="gridForums_ItemCommand" OnItemDataBound="gridForums_ItemDataBound" CellPadding="11"
		CssClass="roundedborder horizseparated" GridLines="None" CellSpacing="-1">
		<AlternatingItemStyle CssClass="altItem" />
		<Columns>
			<asp:BoundColumn Visible="False" DataField="ForumID" HeaderText="ForumID"></asp:BoundColumn>
			<asp:HyperLinkColumn DataTextField="Title" DataNavigateUrlField="ForumID" DataNavigateUrlFormatString="editforum.aspx?ForumID={0}" HeaderText="Forums" ItemStyle-Width="100%"></asp:HyperLinkColumn>
			<asp:ButtonColumn Text="<img src='images/up.png' alt='yukarý' title='yukarý'/>" CommandName="up"></asp:ButtonColumn>
			<asp:ButtonColumn Text="<img src='images/down.png' alt='aþaðý' title='aþaðý'/>" CommandName="down"></asp:ButtonColumn>
			<asp:TemplateColumn>
				<ItemTemplate><asp:Button runat="server" CommandName="delete" Text="<%# various.Delete %>" /></ItemTemplate>
			</asp:TemplateColumn>
		</Columns>
	</cc:THDataGrid>
	<asp:Label ID="lblNoForums" Runat="server" Visible="False"><%= various.NoForumsFound %></asp:Label>
</p>
<br />
<p>
	<table class="roundedborder" cellpadding="7">
		<tr><th colspan="2"><%= various.AddNewForum %></th></tr>
		<tbody>
		<tr>
			<td align="right" class="gray" nowrap="nowrap"><%= various.Name %>:</asp:Label></td>
			<td style="width:100%;">
				<asp:TextBox id="tbTitle" runat="server" Width="100%" MaxLength="50"></asp:TextBox></td>
		</tr>
		<tr>
			<td align="right" class="gray" nowrap="nowrap"><%= various.Description %>:</asp:Label></td>
			<td>
				<asp:TextBox id="tbDescr" runat="server" Width="100%" MaxLength="255"></asp:TextBox></td>
		</tr>
		<tr>
			<td align="right" class="gray" nowrap="nowrap"><%= various.ForumCategory %></td>
			<td>
				<span id="lblSelectGroup" class="gray" runat="server" enableviewstate="false"><%= various.SelectCategory %></span>
				<asp:DropDownList Width="130px" id="ddlForumGroup" runat="server" DataTextField="GroupName" DataValueField="GroupID"></asp:DropDownList>
				<a href="forumgroups.aspx" title="edit available forum groups" runat="server" id="lnkEditForumGroups" enableviewstate="false">.?.</a>
				&nbsp;&nbsp;&nbsp;
				<span id="lblEnterGroup" class="gray" runat="server" enableviewstate="false"><%= various.OrEnterNew %></span>
				<asp:TextBox id="tbForumGroup" runat="server"></asp:TextBox>
			</td>
		</tr>
		<tr><td></td><td><asp:Button id="btnAdd" runat="server" Text="<%# various.Add %>" onclick="btnAdd_Click"></asp:Button></td></tr>
		</tbody>
	</table>
</p>
<asp:Label ID="lblError" runat="server" Visible="False" EnableViewState="False" ForeColor="Red">Error: a forum was not created. Please fill all the fields.</asp:Label>

</asp:Content>