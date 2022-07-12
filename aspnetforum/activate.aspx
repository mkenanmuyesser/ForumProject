<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="activate.aspx.cs" Inherits="aspnetforum.activate" MasterPageFile="AspNetForumMaster.Master" %>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">

	<div class="location">
		<strong><a href="default.aspx">Home</a> &raquo; Activation </strong>
	</div>
	<asp:Label ID="lblSuccess" runat="server" Visible="false"><%= aspnetforum.Resources.various.ActivationSuccess %></asp:Label>
	<asp:Label ID="lblError" runat="server" Visible="false"><%= aspnetforum.Resources.various.ActivationError %></asp:Label>
</asp:Content>