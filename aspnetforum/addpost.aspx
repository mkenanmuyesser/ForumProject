<%@ Page language="c#" Codebehind="addpost.aspx.cs" EnableEventValidation="false" validateRequest="false" AutoEventWireup="True" Inherits="aspnetforum.addpost" MasterPageFile="AspNetForumMaster.Master" %>
<%@ Import Namespace="aspnetforum.Resources" %>
<asp:Content ID="ContentHead" runat="server" ContentPlaceHolderID="ContentPlaceHolderHEAD">
<link href="editor.css" rel="Stylesheet" type="text/css" />
</asp:Content>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">

<div class="location">
<h2>
	<%= various.AddMessage %>:
	<asp:Label ID="lblSubjectText" runat="server" EnableViewState="false"></asp:Label>
</h2></div>
	
<asp:Label ID="lblDenied" Runat="server" Visible="False" Font-Bold="True" ForeColor="Red">Posting to this forum is restricted</asp:Label>
<div id="divMain" runat="server">
	<asp:textbox id="tbSubj" onkeydown="return ProcessSubjectKeyPress(event);" runat="server" Visible="False" MaxLength="50"></asp:textbox>
	<asp:RequiredFieldValidator ID="reqSubject" Enabled="false" EnableViewState="false" EnableClientScript="true" ValidationGroup="post" ErrorMessage="***" Display="Dynamic" ControlToValidate="tbSubj" runat="server"></asp:RequiredFieldValidator>
	<div class="richeditor">
		<div class="editbar" id="divEditbar" runat="server" enableviewstate="false">
			<button title="bold" onclick="wswgEditor.doClick('bold');" type="button"><b>B</b></button>
			<button title="italic" onclick="wswgEditor.doClick('italic');" type="button"><i>I</i></button>
			<button title="underline" onclick="wswgEditor.doClick('underline');" type="button"><u>U</u></button>
			<button title="hyperlink" onclick="wswgEditor.doLink();" type="button" style="background-image:url('images/url.gif');"></button>
			<button title="list" onclick="wswgEditor.doClick('InsertUnorderedList');" type="button" style="background-image:url('images/icon_list.gif');"></button>
			<button title="image" onclick="wswgEditor.doImage();" type="button" style="background-image:url('images/img.gif');"></button>
			<button title="color" onclick="wswgEditor.showColorGrid2('none')" type="button" style="background-image:url('images/colors.gif');"></button><span id="colorpicker201" class="colorpicker201"></span>
			<button title="smilies" onclick="OpenSmilies()" type="button" runat="server" id="btnSmilies" enableviewstate="false" style="background-image:url('images/smilies/smile.gif');"></button>
			<span id="divSmilies" style="DISPLAY:none;position:absolute;border:1px solid slategray;background:#ffffff;padding: 5px 5px 5px 5px">
				<img style="cursor:pointer" onclick="InsertSmile(':)')" src="images/smilies/smile.gif" alt=":)" />
				<img style="cursor:pointer" onclick="InsertSmile(';)')" src="images/smilies/wink.gif" alt=";)" />
				<img style="cursor:pointer" onclick="InsertSmile(':(')" src="images/smilies/upset.gif" alt=":(" />
				<img style="cursor:pointer" onclick="InsertSmile(':cool:')" src="images/smilies/1cool.gif" alt="cool" />
				<img style="cursor:pointer" onclick="InsertSmile(':\\:')" src="images/smilies/eek7.gif" alt="Whaaaaa?" />
				<br />
				<a href="javascript:void(0)" onclick="OpenMoreSmilies()"><%= various.MoreSmilies %></a>
			</span>
			<button title="quote" onclick="wswgEditor.doQuote();" type="button" style="background-image:url('images/icon_quote.png');"></button>
			<button title="youtube" onclick="wswgEditor.InsertYoutube();" type="button" style="background-image:url('images/icon_youtube.gif');"></button>
			<button title="switch to source" type="button" onclick="wswgEditor.SwitchEditor()" style="background-image:url('images/icon_html.gif');"></button>
		</div>
		<div class="container iphone-textbox">
			<asp:textbox id="tbMsg" Width="100%" runat="server" onkeydown="return ProcessCtrlEnter(event);" TextMode="MultiLine" Height="100px"></asp:textbox>
		</div>
	</div>



<div id="divCaptcha" runat="server" enableviewstate="false" visible="false">
	<img alt="" src="captchaimage.ashx" /> :: <asp:textbox id="tbImgCode" autocomplete="off" runat="server"></asp:textbox>
</div>
<asp:Label ID="lblFileSizeError" runat="server" ForeColor="red" Visible="false" EnableViewState="false"><%= various.MaxAttachSize %></asp:Label>
<asp:Label ID="lblMaxSize" runat="server" ForeColor="red" Visible="false" EnableViewState="false"></asp:Label>

<asp:button ValidationGroup="post" id="btnSave" runat="server" Font-Bold="True" onclick="btnSave_Click" OnClientClick="wswgEditor.doCheck();"></asp:button>

<span id="spanUtils" runat="server" enableviewstate="false">
	<span class="gray">(ctrl+enter)</span>
	&nbsp;&nbsp;&nbsp;
	<button type="button" id="btnPreview" runat="server" enableviewstate="false" class="greybutton" onclick="wswgEditor.doCheck();ShowPreview(); return false;"><%= various.Preview %></button>
	<button class="greybutton" onclick="history.back()" type="button"><%= various.Cancel %></button>
</span>
<asp:checkbox id="cbSubscribe" runat="server" Text="Notify me when a reply is posted" Font-Bold="false"></asp:checkbox>


<asp:Repeater ID="rptExistingFiles" runat="server" EnableViewState="false" Visible="false">
<HeaderTemplate>
	<br />
	<script type="text/javascript">
		function DelFile(ifileId) {
			if (!confirm('are you sure?')) return;
			//jquery ajax post
			$.post(
			"addpostajax.ashx", //url
			{FileID: ifileId, mode: "delfile" });
			//remove element from page
			$("#spanFileID" + ifileId).remove();
		}

		
	</script>
	<%= various.Attachments %><br />
</HeaderTemplate>
<ItemTemplate>
	<div id='spanFileID<%# Eval("FileID") %>'><a href="javascript:void(0)" onclick='DelFile(<%# Eval("FileID") %>)' title="delete attachment" style="color:Red">x</a> <%# Eval("FileName") %>; </div>
</ItemTemplate>
</asp:Repeater>

<div id="divFiles" runat="server">
	<br />
	<a href="javascript:void(0)" onclick="javascript:OpenFiles()"><%= various.AttachFiles %></a>
	<div id="divFilesContainer" style="display:none;">
		<span class="gray" style="float:right"><img src="images/error.png" alt="info" /> to insert an attachment as an inline-image into your message,<br />add this code: "[img]att1[/img]", "[img]att2[/img]" etc</span>
		<asp:FileUpload id="fileUpload" runat="server" CssClass="multi" />
	</div>
</div>


<div id="divPolls" runat="server">
	<hr />
	<a href="javascript:void(0)" onclick="OpenPolls()">
		<%= various.CreateAPoll %>
	</a>
	<div id="divPollsContainer" style="DISPLAY:none;">
		<table id="tblOptions" class="roundedborder noborder gray" cellpadding="11" style="margin-bottom:5px">
			<tr><td align="right"><b><%= various.PollQuestion %></b>:</td>
			<td><asp:TextBox ID="tbPollQuestion" MaxLength="255" Columns="80" runat="server"></asp:TextBox></td></tr>
			<tr><td align="right"><%= various.PollOption %>:</td>
			<td><input type="text" size="80" maxlength="255" name="PollOption0" /></td></tr>
		</table>
		<button onclick="javascript:AddOptionInput();" type="button"><%= various.AddAnOption %></button>
		<button onclick="javascript:ClearPoll();" type="button" class="gradientbutton"><%= various.Cancel %></button>
	</div>
</div>
<br />

<table width="100%" id="tblPreview" style="display:none">
	<tr><td><div id="divPreview"></div></td></tr>
</table>
<asp:Repeater ID="rptMessages" runat="server">
	<HeaderTemplate>
		<br /><br />
		<%= various.PreviousMessagesInTopic %>
		<table style="width:100%;" cellpadding="12">
	</HeaderTemplate>
	<ItemTemplate>
		<tr><td valign="top" style="width:120px"><b><%# Eval("UserName") %></b><br /><%# ToAgoString((DateTime)Eval("CreationDate"))%></td>
		<td><%# aspnetforum.Utils.Formatting.FormatMessageHTML(Eval("Body").ToString())%></td></tr>
	</ItemTemplate>
	<FooterTemplate></table></FooterTemplate>
</asp:Repeater>
</div>

<img src="renewsession.ashx" id="renewSession" alt="" />
<script src="jquery.MultiFile.js" type="text/javascript"></script>
<script src="jquery.placeholder.min.js" type="text/javascript"></script>
<script src="editor.js" type="text/javascript"></script>

<script type="text/javascript">
	$(function(){ $('input[placeholder]').placeholder();});

	function ShowPreview() {
		$("#tblPreview").show();
		<%-- jquery ajax post--%>
		$.post(
				"addpostajax.ashx", //url
				{messagetext: $("#<%= tbMsg.ClientID %>").val(), mode: "preview" }, //name-values to post
				function (data) { $("#divPreview").get(0).innerHTML = data; }, //callback
				"html");<%--returned datatype--%>
	}
	function InsertSmile(txt) {
		wswgEditor.InsertText(txt);
		document.getElementById('divSmilies').style.display = 'none';
	}
	function OpenSmilies() {
		OpenCloseDiv('divSmilies');
	}
	function OpenFiles() {
		OpenCloseDiv('divFilesContainer');
	}
	function OpenPolls() {
		OpenCloseDiv('divPollsContainer');
	}
	function OpenMoreSmilies() {
		window.open("smilies.htm", null, "height=200,width=400,status=yes,toolbar=no,menubar=no,location=no,scrollbars=yes");
	}
	function OpenCloseDiv(divname) {
		objDiv = document.getElementById(divname);
		if (objDiv.style.display == 'none')
			objDiv.style.display = '';
		else
			objDiv.style.display = 'none';
	}
	var numOptions = 1;
	function AddOptionInput() {
		var tblOptions = document.getElementById('tblOptions');
		var tBody = tblOptions.getElementsByTagName('tbody')[0];
		var newTR = document.createElement('tr');
		var newTD1 = document.createElement('td');
		newTD1.align = 'right';
		var newTD2 = document.createElement('td');
		var opt = document.createElement("input");
		opt.type = "text";
		opt.name = "PollOption" + numOptions;
		opt.size = 80;
		opt.maxlength = 255;
		numOptions++;
		newTD1.innerHTML = numOptions;
		newTD2.appendChild(opt);
		newTR.appendChild(newTD1);
		newTR.appendChild(newTD2);
		tBody.appendChild(newTR);
	}
	function ClearPoll() {
		$("input[name^=PollOption]").val("");
		$("[id$='tbPollQuestion']").val("");
		OpenPolls();
	}
	<%-- processes ctrl+enter--%>
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
	<%--processes TAB--%>
	function ProcessSubjectKeyPress(e) {
		<%--if TAB pressed - move focus to editor--%>
		var evt = e ? e : window.event;
		var keyCode = evt.keyCode ? evt.keyCode : evt.which;
		<%--if TAB--%>
		if (keyCode == 9) {
			try { document.getElementById('rte').contentWindow.focus(); } <%--wysiwyg--%>
			catch (e) { document.getElementById("<%=tbMsg.ClientID%>").focus(); } <%--textarea--%>

			<%--prevent processing--%>
			evt.returnValue = false; <%-- IE--%>
			if (evt.preventDefault) evt.preventDefault(); <%-- Mozilla--%>
		}
	}

	<%--renew session--%>
	window.setInterval("renewSession();", 9000);
	function renewSession() {
		try { document.images("renewSession").src = "renewsession.ashx?par1=" + Math.random() + "&par2=" + Math.random(); }
		catch (e) { }
	}

	wswgEditor.initEditor("<%=tbMsg.ClientID%>", <%= (!(Request.Browser.IsMobileDevice || IsiPhoneOrAndroid() || IsIpad())).ToString().ToLower() %>);
	$(wswgEditor.getEditorDoc()).keydown(ProcessCtrlEnter); <%--processes ctrl+enter--%>
	$("#<%=tbMsg.ClientID%>").keydown(ProcessCtrlEnter); <%--processes ctrl+enter--%>
			
	$(wswgEditor.getEditorDoc()).bind('keyup', function() { AutoResizeEditableIframe(wswgEditor.getIframe()); });
	function AutoResizeEditableIframe(oFrame) {
		var scrollHeight = oFrame.contentWindow.document.body.scrollHeight;
		if (scrollHeight > oFrame.contentWindow.document.body.clientHeight && scrollHeight > 100) { //100 - min height, hack for IE
			oFrame.style.height = (scrollHeight) + "px";
		}
	}

	<%--focus inputs onload:--%>
	var tbSubjId = '<%= tbSubj.ClientID %>';
	try { document.getElementById(tbSubjId).focus(); }
	catch (err) {
		try { document.getElementById('rte').focus(); }
		catch(err1) { }
	}
</script>
</asp:Content>