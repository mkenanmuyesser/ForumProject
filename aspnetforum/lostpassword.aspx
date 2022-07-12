<%@ Page Title="Password Reminder" language="c#" Codebehind="lostpassword.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.lostpassword" MasterPageFile="AspNetForumMaster.Master" %>
<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
	<div class="location"><h2><a href="default.aspx">
		<asp:Label ID="lblHome" runat="server" EnableViewState="False" meta:resourcekey="lblHomeResource1">Home</asp:Label></a>
		&raquo;
		<asp:Label ID="lblLostPsw" runat="server" EnableViewState="False" meta:resourcekey="lblLostPswResource1">Lost Password Recovery</asp:Label>
		</h2></div>
		
	<table cellpadding="14" class="roundedborder noborder" runat="server" id="tblMain">
	<tr>
		<th colspan="2"><asp:Label ID="lblLostPsw2" runat="server" EnableViewState="False" meta:resourcekey="lblLostPswResource1">Lost Password Recovery Form</asp:Label></th>
	</tr>
	<tr><td colspan="2">
		<asp:Label CssClass="gray" ID="lblForgot" runat="server" EnableViewState="False" meta:resourcekey="lblForgotResource1">If you have forgotten your username or password, you can request to have your username and password emailed to you.</asp:Label></td>
	</tr>
	<tr>
		<td align="right" nowrap="nowrap">
			<asp:Label ID="lblEmail" runat="server" EnableViewState="False" meta:resourcekey="lblEmailResource1">Enter your Email:</asp:Label>
		</td><td>
			<asp:TextBox id="txEmail" runat="server"></asp:TextBox><br />
			<asp:Label ID="lblEmailNotFound" runat="server" meta:resourcekey="lblEmailNotFoundResource1" Visible="false" EnableViewState="false" ForeColor="Red">Email not found</asp:Label>
		</td>
	</tr>
	<tr>
		<td align="right">
			<img alt="" src="captchaimage.ashx" />
		</td><td>
			<asp:TextBox id="tbImgCode" runat="server" autocomplete="off"></asp:TextBox>
			<asp:label id="lblWrongCode" runat="server" Visible="False" ForeColor="Red" EnableViewState="false" Text="!!" Font-Bold="true"></asp:label>
		</td>
	</tr>
	<tr>
		<td></td>
		<td><asp:Button id="btnRequest" runat="server" Text="Request Username / Password" onclick="btnRequest_Click" meta:resourcekey="btnRequestResource1"></asp:Button></td>
	</tr></table>
	
	<asp:Label ID="lblOk" runat="server" EnableViewState="false" Visible="false">Thanks. A new password has been sent to your email.</asp:Label>
</asp:Content>