<%@ Page Language="C#" AutoEventWireup="true" Codebehind="adduser.aspx.cs" Inherits="aspnetforum.adduser" MasterPageFile="AspNetForumMaster.Master" %>
<%@ Import Namespace="aspnetforum.Resources" %>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
	
	<p><%= various.ManuallyAddingUserDescription %></p>

	<table class="roundedborder noborder" cellpadding="11">
		<tr><th colspan="2"><%= various.NewUser %></th></tr>
		<tr>
			<td class="gray">
				<%= various.Username %> *
			</td>
			<td>
				<asp:TextBox ID="txUserName" runat="server"></asp:TextBox>
				<asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="txUserName" Display="Dynamic" ErrorMessage="***"></asp:RequiredFieldValidator>
			</td>
		</tr>
		<tr>
			<td class="gray">
				<%= various.Password %> *
			</td>
			<td>
				<asp:TextBox ID="txPsw" runat="server"></asp:TextBox>
				<asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ControlToValidate="txPsw" Display="Dynamic" ErrorMessage="***"></asp:RequiredFieldValidator></td>
		</tr>
		<tr>
			<td class="gray">
				<%= various.Email %> *
			</td>
			<td>
				<asp:TextBox ID="txEmail" runat="server"></asp:TextBox>
				<asp:RequiredFieldValidator ID="RequiredFieldValidator3" runat="server" ControlToValidate="txEmail" Display="Dynamic" ErrorMessage="***"></asp:RequiredFieldValidator></td>
		</tr>
		<tr>
			<td class="gray">
				<%= various.Homepage %>
			</td>
			<td>
				<asp:TextBox ID="txHomepage" runat="server"></asp:TextBox>
			</td>
		</tr>
		<tr><td colspan="2"><asp:Button CssClass="gradientbutton" ID="btnAdd" runat="server" Text="<%# various.Add %>" OnClick="btnAdd_Click" /></td></tr>
	</table>
	<asp:label id="lblError" runat="server" Visible="False" ForeColor="Red"><%= various.UserAlreadyExists %></asp:label>
	<asp:label id="lblSuccess" runat="server" Visible="False"><%= various.UserSuccessfullyCreated %></asp:label>
</asp:Content>
