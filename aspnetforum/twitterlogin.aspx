<%@ Page Title="" Language="C#" MasterPageFile="AspNetForumMaster.Master" EnableViewState="false" AutoEventWireup="true" CodeBehind="twitterlogin.aspx.cs" Inherits="aspnetforum.twitterlogin" %>
<asp:Content ID="Content2" ContentPlaceHolderID="AspNetForumContentPlaceHolder" runat="server">

<asp:Label id="lblError" runat="server" EnableViewState="false" Visible="false">Twitter has returned a '401-unauthorized' error message. Please <a href='twitterlogin.aspx'>try again</a></asp:Label>

<script type="text/javascript">
	function CheckUserName() {
		var textboxelement = document.getElementById("<%= tbPickUserName.ClientID %>");
		//jquery ajax post
		$.post(
		"ajaxutils.ashx", //url
		{username: textboxelement.value, mode: "CheckUserNameAvailability" }, //name-values to post
		function (data) {
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


<div class="location"><h2>Hello, <b style="color:Red"><asp:Label ID="lblTwitterName" runat="server"></asp:Label></b>!</h2></div>

<p id="pPickUserName" runat="server" visible="false">Unfortunately this username is already taken on this forum. You will have to choose another (this is done only once).</p>
	
<table class="roundedborder noborder gray" cellpadding="10">
<tr id="trPickUserName" runat="server" visible="false">
	<td align="right"><asp:Label runat="server">Please pick a username:</asp:Label></td>
	<td>
		<asp:TextBox ID="tbPickUserName" runat="server"></asp:TextBox>
		<a href="javascript:void(0)" onclick="CheckUserName()">
			<asp:Label runat="server" meta:resourcekey="lblCheckResource1">check availability</asp:Label>
		</a><img style="display:none" src="images/ok.png" id="imgOk" alt="ok" /><img style="display:none" src="images/error.png" id="imgError" alt="allready taken" />
		<asp:RequiredFieldValidator ID="rqvUserName" runat="server" ControlToValidate="tbPickUserName" ErrorMessage="Please enter a username" Display="Dynamic"></asp:RequiredFieldValidator>
	</td>
</tr>
<tr>
	<td align="right"><asp:Label runat="server">Please enter your email (required only once):</asp:Label></td>
	<td>
		<asp:TextBox ID="tbEmail" runat="server"></asp:TextBox>
		<asp:RequiredFieldValidator ID="reqEmail" runat="server" ControlToValidate="tbEmail" ErrorMessage="Please enter email" Display="Dynamic"></asp:RequiredFieldValidator>
	</td>
</tr>
<tr>
	<td></td>
	<td><input type="submit" value="OK" name="pickusernamebtn" /></td>
</tr>
</table>

</asp:Content>
