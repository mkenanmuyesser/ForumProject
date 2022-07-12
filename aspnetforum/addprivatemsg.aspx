<%@ Page language="c#" Codebehind="addprivatemsg.aspx.cs" EnableEventValidation="false" ValidateRequest="false" AutoEventWireup="True" Inherits="aspnetforum.addprivatemsg" MasterPageFile="AspNetForumMaster.Master" %>
<%@ Import Namespace="aspnetforum.Resources" %>

<asp:Content ID="ContentHead" runat="server" ContentPlaceHolderID="ContentPlaceHolderHEAD">
<link href="editor.css" rel="Stylesheet" type="text/css" />
</asp:Content>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">

	<div class="location">
	<h2><%= various.SendAPrivateMessage %> - <%= aspnetforum.Utils.User.GetUserNameById(toUserID) %></h2></div>

	<div id="divMain" runat="server">
		<div class="richeditor">
			<div class="editbar">
				<button title="bold" onclick="wswgEditor.doClick('bold');" type="button"><b>B</b></button>
				<button title="italic" onclick="wswgEditor.doClick('italic');" type="button"><i>I</i></button> 
				<button title="underline" onclick="wswgEditor.doClick('underline');" type="button"><u>U</u></button>
				<button title="hyperlink" onclick="wswgEditor.doLink();" type="button" style="background-image:url('images/url.gif');"></button>
				<button title="list" onclick="wswgEditor.doClick('InsertUnorderedList');" type="button" style="background-image:url('images/icon_list.gif');"></button>
				<button title="image" onclick="wswgEditor.doImage();" type="button" style="background-image:url('images/img.gif');"></button>
				<button title="color" onclick="wswgEditor.showColorGrid2('none');" type="button" style="background-image:url('images/colors.gif');"></button><span id="colorpicker201" class="colorpicker201"></span>
				<button title="quote" onclick="wswgEditor.doQuote();" type="button" style="background-image:url('images/icon_quote.png');"></button>
				<button title="switch to source" type="button" onclick="wswgEditor.SwitchEditor()" style="background-image:url('images/icon_html.gif');"></button>
			</div>
			<div class="container">
				<asp:textbox id="tbMsg" Width="100%" runat="server" onkeydown="return ProcessCtrlEnter(event);" TextMode="MultiLine" height="100px"></asp:textbox>
			</div>
		</div>
		
		<asp:Label ID="lblFileSizeError" runat="server" ForeColor="red" Visible="false" EnableViewState="false">Max attachment size is </asp:Label>
		<asp:Label ID="lblMaxSize" runat="server" ForeColor="red" Visible="false" EnableViewState="false"></asp:Label>
		
		<asp:button CssClass="gradientbutton" id="btnSave" runat="server" onclick="btnSave_Click" OnClientClick="wswgEditor.doCheck();return true;" Font-Bold="true" Text="<%# various.SendMessage %>"></asp:button>
		<span class="gray">(ctrl+enter)</span>
		&nbsp;&nbsp;&nbsp;
		<button type="button" class="gradientbutton" onclick="wswgEditor.doCheck();ShowPreview(); return false;"><%= various.Preview %></button>
		<button type="button" onclick="history.back()" class="gradientbutton"><%= various.Cancel %></button>
		
		<div id="divFiles" runat="server">
			<br />
			<a href="javascript:OpenFiles()"><%= various.AttachFiles %></a>
			<div id="divFilesContainer" style="display:none;">
				<asp:FileUpload id="fileUpload" runat="server" CssClass="multi" />
			</div>
		</div>
		
		<br />
		<table width="100%" id="tblPreview" style="display:none">
			<tr><td><div id="divPreview"></div></td></tr>
		</table>
	</div>

	<asp:Label ID="lblError" runat="server" ForeColor="red" Font-Bold="true" Visible="false" EnableViewState="false"><%= various.UserNotFoundOrYouLoggedOut %></asp:Label>


	<script src="jquery.MultiFile.js" type="text/javascript"></script>
	<script src="editor.js" type="text/javascript"></script>
	<script type="text/javascript">
		function ShowPreview() {
			$("#tblPreview").show();
			//jquery ajax post
			$.post(
				"addpostajax.ashx", //url
				{messagetext: $("#<%= tbMsg.ClientID %>").val(), mode: "preview" }, //name-values to post
				function (data) { $("#divPreview").get(0).innerHTML = data; }, //callback
				"html");   //returned datatype
		}
		function OpenFiles() {
			OpenCloseDiv('divFilesContainer');
		}
		function OpenCloseDiv(divname) {
			objDiv = document.getElementById(divname);
			if (objDiv.style.display == 'none')
				objDiv.style.display = '';
			else
				objDiv.style.display = 'none';
		}
		function ProcessCtrlEnter(e) {
			var evt = e ? e : window.event;
			if (evt.ctrlKey) {
				var keyCode = evt.keyCode ? evt.keyCode : evt.which;
				if (keyCode == 13) {
					$("#<%=btnSave.ClientID%>").click();
					return false;
				}
				else
					return true;
			}
			else
				return true;
		}

		wswgEditor.initEditor("<%=tbMsg.ClientID%>", <%= (!(Request.Browser.IsMobileDevice || IsiPhoneOrAndroid() || IsIpad())).ToString().ToLower() %>);
		$(wswgEditor.getEditorDoc()).keydown(ProcessCtrlEnter); //processes ctrl+enter
		$("#<%=tbMsg.ClientID%>").keydown(ProcessCtrlEnter); //processes ctrl+enter
		
		$(wswgEditor.getEditorDoc()).bind('keyup', function() { AutoResizeEditableIframe(wswgEditor.getIframe()); });
		function AutoResizeEditableIframe(oFrame) {
			var scrollHeight = oFrame.contentWindow.document.body.scrollHeight;
			if (scrollHeight > oFrame.contentWindow.document.body.clientHeight && scrollHeight > 100) { //100 - min height, hack for IE
				oFrame.style.height = (scrollHeight) + "px";
			}
		}

		//focus inputs onload:
		try { document.getElementById('rte').focus(); }
		catch(err1) { }
	</script>
</asp:Content>