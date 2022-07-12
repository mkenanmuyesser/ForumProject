<%@ Page language="c#" Codebehind="register.aspx.cs" Title="Register a new user" AutoEventWireup="True" Inherits="aspnetforum.register" MasterPageFile="AspNetForumMaster.Master" %>
<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">

<script type="text/javascript">
function CheckUserName() {
	var textboxelement = document.getElementById("<%= tbUserName.ClientID %>");
	//jquery ajax post
	$.post(
		"ajaxutils.ashx", //url
		{username: textboxelement.value, mode: "CheckUserNameAvailability" }, //name-values to post
		function(data) {
			if (data == "1") {
				$("#imgOk").hide();
				$("#imgError").show();
			}
			else {
				$("#imgOk").show();
				$("#imgError").hide();
			}
		}, //callback
		"html");     //returned datatype
}
</script>


	<div class="location"><h2>
		<a href="default.aspx"><asp:Label ID="lbl" runat="server" EnableViewState="False" meta:resourcekey="lblResource1">Home</asp:Label></a>
		&raquo;
		<asp:Label ID="lblRegister" runat="server" EnableViewState="False" meta:resourcekey="lblRegisterResource1">Register</asp:Label>
	</h2></div>
	
	<div id="divTOS" runat="server" enableviewstate="false" visible="false">
	<p>Kayýt için gerekli kýsýmlar</p>

	<p>This forum is powered by <a href="http://www.jitbit.com/asp-net-forum/">Jitbit ASP.NET Forum Software</a>
	which is a bulletin board solution for ASP.NET.</p>

	<p>You agree not to post any abusive, obscene, vulgar, slanderous, hateful, threatening, sexually-orientated or any other material that may violate any laws be it of your country or International Law.
	Doing so may lead to you being immediately and permanently banned, with notification of your Internet Service Provider if deemed required by us.
	The IP address of all posts are recorded to aid in enforcing these conditions.
	You agree that we have the right to remove, edit, move or close any topic at any time should we see fit.
	As a user you agree to any information you have entered to being stored in a database.</p>
	
	<p align="center">
		<asp:Button ID="btnAgree" meta:resourcekey="btnAgreeResource1" CssClass="gradientbutton" Text="I agree to these terms" runat="server" Font-Bold="true" OnClick="btnAgree_Click" />
		<asp:Button ID="btnDisagree" meta:resourcekey="btnDisagreeResource1" CssClass="gradientbutton" Text="I do not agree to these terms" runat="server" OnClick="btnDisagree_Click" />
	</p>
	
	</div>
	
	<asp:label id="lblError" runat="server" Visible="False" ForeColor="Red" meta:resourcekey="lblErrorResource1"></asp:label>
	
	<table id="registerTable" runat="server" cellpadding="11" class="roundedborder noborder gray">
		<tr>
			<td align="right">* <asp:Label ID="lblUsername" runat="server" EnableViewState="False" meta:resourcekey="lblUsernameResource1">Username:</asp:Label></td>
			<td><asp:textbox id="tbUserName" runat="server"></asp:textbox>
			<a href="javascript:void(0)" onclick="CheckUserName()">
				<asp:Label ID="lblCheck" runat="server" meta:resourcekey="lblCheckResource1">check availability</asp:Label>
			</a><img style="display:none" src="images/ok.png" id="imgOk" alt="ok" /><img style="display:none" src="images/error.png" id="imgError" alt="allready taken" />
			</td>
		</tr>
		<tr>
			<td align="right">* <asp:Label ID="lblEmail" runat="server" EnableViewState="False" meta:resourcekey="lblEmailResource1">Email (NOT shared):</asp:Label></td>
			<td><asp:textbox id="tbEmail" runat="server"></asp:textbox></td>
		</tr>
		<tr>
			<td align="right">* <asp:Label ID="lblPsw" runat="server" EnableViewState="False" meta:resourcekey="lblPswResource1">Password:</asp:Label></td>
			<td><asp:textbox id="tbPsw1" runat="server" TextMode="Password"></asp:textbox></td>
		</tr>
		<tr>
			<td align="right">* <asp:Label ID="lblPswConf" runat="server" EnableViewState="False" meta:resourcekey="lblPswConfResource1">Confirm password:</asp:Label></td>
			<td><asp:textbox id="tbPsw2" runat="server" TextMode="Password"></asp:textbox></td>
		</tr>
		<tr>
			<td align="right"><img alt="" src="captchaimage.ashx" /></td>
			<td><asp:Label ID="lblCaptcha" runat="server" EnableViewState="False" meta:resourcekey="lblCaptchaResource1">Enter the code shown:</asp:Label><br />
				<asp:TextBox id="tbImgCode" runat="server" autocomplete="off"></asp:TextBox></td>
		</tr>
		<tr>
			<td align="right"><asp:Label ID="lblHomepage" runat="server" EnableViewState="False" meta:resourcekey="lblHomepageResource1">Homepage:</asp:Label></td>
			<td><asp:textbox id="tbHomepage" runat="server" MaxLength="50"></asp:textbox></td>
		</tr>
		<tr>
			<td align="right"><asp:Label ID="lblInterests" runat="server" EnableViewState="False" meta:resourcekey="lblInterestsResource1">Interests:</asp:Label></td>
			<td><asp:textbox id="tbInterests" runat="server" MaxLength="255"></asp:textbox></td>
		</tr>
		<tr>
			<td colspan="2">
				<asp:button id="btnOK" runat="server" Text="register" CssClass="gradientbutton" meta:resourcekey="btnOKResource1"></asp:button></td></tr>
	</table>
	<asp:label id="lblSuccess" runat="server" Visible="False" meta:resourcekey="lblSuccessResource1">User successfully created!</asp:label>
	<asp:label id="lblSuccessEmail" runat="server" Visible="False" meta:resourcekey="lblSuccessEmailResource1">Please check your email to activate your account.</asp:label>

	<%-- antispam honeypot fields (alternative to captcha) --%>
	<input type="text" name="email" style="position:absolute;top:-10000px;left:-10000px" autocomplete="off" />
</asp:Content>