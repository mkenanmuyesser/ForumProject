<%@ Page Title="" Language="C#" MaintainScrollPositionOnPostback="true" MasterPageFile="AspNetForumMaster.Master" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="adminsettings.aspx.cs" Inherits="aspnetforum.adminsettings" culture="auto" meta:resourcekey="PageResource1" %>
<%@ Register TagPrefix="cc" Namespace="aspnetforum" Assembly="aspnetforum" %>

<asp:Content ID="Content2" ContentPlaceHolderID="AspNetForumContentPlaceHolder" runat="server">
<cc:THDataGrid CellSpacing="-1" id="gridSettings" Runat="server" AutoGenerateColumns="False" EnableViewState="True" GridLines="None"
	OnEditCommand="gridSettings_EditCommand" OnUpdateCommand="gridSettings_UpdateCommand"
	OnCancelCommand="gridSettings_CancelCommand" OnItemDataBound="gridSettings_ItemDataBound" UseAccessibleHeader="true" CssClass="horizseparated roundedborder">
	<AlternatingItemStyle CssClass="altItem" />
	<Columns>
		<asp:BoundColumn Visible="False" DataField="CfgKey" ReadOnly="true"></asp:BoundColumn>
		<asp:TemplateColumn>
			<HeaderTemplate>Ayarlar ve açıklamaları</HeaderTemplate>
			<ItemTemplate><b><%# Eval("CfgKey")%></b>. <%# aspnetforum.Utils.DbAwareSettings.Current.GetDescription(Eval("CfgKey").ToString()).Replace("\n", "<br/>") %></ItemTemplate>
		</asp:TemplateColumn>
		<asp:TemplateColumn ItemStyle-Wrap="false">
			<HeaderTemplate>Değeri</HeaderTemplate>
			<ItemTemplate><b><a href="javascript:<%# Page.GetPostBackEventReference(Container.Cells[3].Controls[0]) %>"><%# GetNiceValue(Eval("CfgValue")) %></a></b></ItemTemplate>
			<EditItemTemplate>
				<asp:TextBox ID="tbValue" runat="server" />
				<asp:CheckBox ID="cbValue" runat="server" />
				<button type="button" onclick="<%# Page.GetPostBackEventReference(Container.Cells[3].Controls[0]) %>">save</button>
				<button type="button" onclick="<%# Page.GetPostBackEventReference(Container.Cells[3].Controls[2]) %>">cancel</button>
			</EditItemTemplate>
		</asp:TemplateColumn>

		<asp:EditCommandColumn Visible="false"
			CancelText="cancel"
			EditText="edit"
			UpdateText="update"></asp:EditCommandColumn>
	</Columns>
</cc:THDataGrid>

<p><asp:Button ID="btnTestEmail" runat="server" Text="Test SMTP settings" OnClick="btnTestEmail_Click" /></p>
<div runat="server" id="divTestEmail" visible="false" enableviewstate="false"></div>

</asp:Content>
