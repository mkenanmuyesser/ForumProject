<%@ Page language="c#" Codebehind="messages.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.messages" MasterPageFile="AspNetForumMaster.Master" %>
<%@ Import Namespace="aspnetforum.Resources" %>

<asp:Content ContentPlaceHolderID="ContentPlaceHolderHEAD" ID="AspNetForumHead" runat="server">
<link rel="alternate" type="application/rss+xml" title="topics in this forum" id="rssDiscoverLink" runat="server" />
<link rel="canonical" id="lnkCanonical" runat="server" />
<% if(Request.QueryString["ratedOnly"] == "1") { %>
	<meta name="robots" content="noindex">
<% } %>
<link href="editor.css" rel="Stylesheet" type="text/css" />
</asp:Content>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
<div class="location">
	<div class="smalltoolbar">
	<asp:Label runat="server" ID="lblClosedTop" Font-Bold="true" Visible="False" EnableViewState="False" meta:resourcekey="lblClosedResource1">the topic is closed</asp:Label>
	<a id="lnkRatedOnly" rel="nofollow" title="show positively rated messages only, sorted by rating top-to-bottom" runat="server" enableviewstate="false"><asp:Label ID="lblRatedOnly" runat="server" EnableViewState="False" meta:resourcekey="lblRatedOnlyResource1">show rated messages only</asp:Label></a>
	<span id="spanSubcriptionTop" runat="server" enableviewstate="false">
		<asp:LinkButton id="btnSubscribeTop" Runat="server" Visible="False" onclick="btnSubscribe_Click" EnableViewState="False" meta:resourcekey="btnSubscribeResource1">watch this topic for replies</asp:LinkButton>
		<asp:LinkButton id="btnUnsubscribeTop" Runat="server" Visible="False" onclick="btnUnsubscribe_Click" EnableViewState="False" meta:resourcekey="btnUnsubscribeResource1">stop watching this topic</asp:LinkButton>
	</span>
	<span id="spanMoveTop" runat="server">
		<a href="javascript:void(0)" onclick="document.getElementById('spanMoveControlTop').style.display='';"><asp:Label ID="lblMoveTop" runat="server" EnableViewState="False" meta:resourcekey="lblMoveResource1">move thread to forum:</asp:Label></a>
		<span id="spanMoveControlTop" style="display:none">
		<asp:DropDownList ID="ddlForumsTop" Runat="server" DataTextField="Title" DataValueField="ForumID"></asp:DropDownList><asp:Button ID="btnMoveTop" Runat="server" Text="move" onclick="btnMoveTop_Click" CssClass="gradientbutton" EnableViewState="False" meta:resourcekey="btnMoveResource1"></asp:Button>
		</span>
	</span>
	<span id="spanMergeTop" runat="server" enableviewstate="false">
		<a href="javascript:void(0)" onclick="document.getElementById('spanMergeControlTop').style.display='';"><asp:Label ID="lblMergeTop" runat="server" meta:resourcekey="lblMergeResource1">merge with another topic</asp:Label></a>
		<span id="spanMergeControlTop" style="display:none">
		<asp:Label ID="lblMergeTopHelp" runat="server" meta:resourcekey="lblMergeHelpResource1">enter the topic ID</asp:Label>:
		<asp:TextBox ID="tbTopMergeThreadId" runat="server" EnableViewState="false"></asp:TextBox> <asp:Button ID="btnMergeTop" Runat="server" Text="merge" onclick="btnMergeTop_Click" meta:resourcekey="btnMergeResource1"></asp:Button>
		</span>
	</span>
	</div>

	<h2><a href="default.aspx"><asp:Label ID="lblHome" runat="server" EnableViewState="False" meta:resourcekey="lblHomeResource1">Home</asp:Label></a>
		&#9654;
		<asp:label id="lblCurForum" runat="server" EnableViewState="False"></asp:label>
		</h2>
		<span class="gray" id="divDescription" runat="server"></span>

	<h1><%= _topicSubject %>&nbsp;<a runat="server" id="rssLink" enableviewstate="false"><img alt="Messages in this topic - RSS" src="images/rss.png"  /></a>
		<span id="spanAddPost" class="postbutton" runat="server" enableviewstate="false">
			<a class="button" href="addpost.aspx?TopicID=<%= _topicID %>"><asp:Label ID="lblReplyTop" runat="server" EnableViewState="False" meta:resourcekey="lblReplyResource1">add reply</asp:Label></a>
		</span>
	</h1>
</div>

<div id="divPoll" runat="server" visible="false" align="center" style="padding:17px 17px 17px 17px">
	<h2><asp:Label ID="lblPollName" runat="server" EnableViewState="false"></asp:Label></h2>
	<asp:RadioButtonList CssClass="radiolist" style="margin: 5px 5px 5px 5px;" ID="rblOptions" runat="server" DataTextField="OptionText" DataValueField="OptionID" Visible="false" CellPadding="7"></asp:RadioButtonList>
	<asp:Repeater id="rptVoteResults" runat="server">
		<HeaderTemplate><table cellpadding="7" style="margin: 5px 5px 5px 5px"></HeaderTemplate>
		<ItemTemplate>
			<tr><td><b><%# Eval("OptionText") %>:</b></td><td align="left"><%# Eval("VoteCount") %> <img src="images/vote_bg.gif" height="10" width='<%# GetVotingBarWidth(Eval("VoteCount")) %>' alt="" /></td></tr>
		</ItemTemplate>
		<FooterTemplate></table></FooterTemplate>
	</asp:Repeater>
	<asp:Button ID="btnVote" runat="server" Text="vote" CssClass="gradientbutton" OnClick="btnVote_Click" meta:resourcekey="btnVoteResource1" />
</div>

<table id="tblError" runat="server" visible="false" width="100%" cellpadding="11" enableviewstate="false">
<tr><td><div id="divError" runat="server" enableviewstate="false" visible="false"></div></td></tr>
</table>

<asp:repeater id="rptMessagesList" runat="server" EnableViewState="False" OnItemDataBound="rptMessagesList_ItemDataBound" OnItemCommand="rptMessagesList_ItemCommand">
	<HeaderTemplate>
		<table style="width:100%;" class="roundedborder postlist">
		<tbody>
	</HeaderTemplate>
	<ItemTemplate>
	<tr valign="top" class="<%# Container.ItemType == ListItemType.AlternatingItem ? "altItem" : "" %> <%# GetPostClassName(Eval("AcceptedAnswer")) %> ">
		<td>
			<a name='post<%# Eval("MessageID") %>'></a>
			<%# aspnetforum.Utils.User.DisplayUserInfo(Eval("UserID"), Eval("UserName"), Eval("PostsCount"), Eval("AvatarFileName"), _forumID, Eval("FirstName"), Eval("LastName"), Eval("UseGravatar"), Eval("Email"))%>
			<br /><br />			
			<span class="gray"><%# ToAgoString((DateTime)Eval("CreationDate")) %></span>
		</td>
		<td style="border-bottom:none;">
			<div class="mobileshown">
				<%# aspnetforum.Utils.User.DisplayUserInfo(Eval("UserID"), Eval("UserName"), Eval("PostsCount"), Eval("AvatarFileName"), _forumID, Eval("FirstName"), Eval("LastName"), Eval("UseGravatar"), Eval("Email"))%>
			</div>
			<asp:Literal ID="ltrBody" runat="server"></asp:Literal>
			<%# aspnetforum.Utils.Formatting.FormatSignature(Eval("Signature").ToString())%>
			<asp:Repeater ID="rptFiles" runat="server">
			<HeaderTemplate>
				<br /><br />
				<div class="gray">
				<asp:Label ID="lblAttachments" runat="server" EnableViewState="False" meta:resourcekey="lblAttachmentsResource1">Attachments:</asp:Label><br />
			</HeaderTemplate>
			<ItemTemplate>
				<a href='getattachment.ashx?fileid=<%# Eval("FileID") %>'>
				<%# aspnetforum.Utils.Attachments.GetThumbnail(Eval("FileName").ToString(), Convert.ToInt32(Eval("UserID"))) %>
				<%# Eval("FileName") %></a><br />
			</ItemTemplate>
			<FooterTemplate></div></FooterTemplate>
			</asp:Repeater>
		</td>
	</tr>
	<tr class="utils <%# Container.ItemType == ListItemType.AlternatingItem ? " altItem" : "" %> <%# GetPostClassName(Eval("AcceptedAnswer")) %>" >
		<td>
			<%# _isModerator ? (Eval("IPAddress").ToString() != "" ? "<span class='gray'>Posted from IP:</span> <a href='http://www.geoiptool.com/en/?IP=" + Eval("IPAddress") + "'>" + Eval("IPAddress") + "</a>" : "") : ""%>
		</td>
		<td class="messageActions" align="right">
			<%# RenderMsgRating(Eval("MessageID"), Eval("Rating")) %>
			<span id="spanRate" runat="server" enableviewstate="false">
				<a href="javascript:void(0)" onclick="RatePost(<%# Eval("MessageID") %>, 1)" title="thumbs up"><img src="images/thumbsup.png" alt="thumbs up" /></a>
				<a href="javascript:void(0)" onclick="RatePost(<%# Eval("MessageID") %>, -1)" title="thumbs down"><img src="images/thumbsdown.png" alt="thumbs down" /></a>
			</span>
			<asp:LinkButton id="btnAcceptAnswer" Visible="False" Runat="server" CommandName="acceptAnswer" CommandArgument='<%# Eval("MessageID") %>' meta:resourcekey="btnAcceptAnswer">mark as "accepted answer"</asp:LinkButton>
			<a href='#post<%# Eval("MessageID") %>' onclick="ShowLink(this, '<%# Eval("MessageID") %>'); return false;"> <asp:Label ID="lblPermalink" runat="server" EnableViewState="False" meta:resourcekey="lblPermalinkResource1">link</asp:Label></a>
			<a runat="server" id="lnkQuote" visible="False"><asp:Label ID="lblQuote" runat="server" EnableViewState="False" meta:resourcekey="lblQuoteResource1">reply with quote</asp:Label></a>
			<asp:LinkButton id="btnComplain" Visible="False" Runat="server" CommandName="complain" CommandArgument='<%# Eval("MessageID") %>' meta:resourcekey="btnComplainResource1">report to moderator</asp:LinkButton>
			<a runat="server" id="lnkEdit" visible="False"><asp:Label ID="lblEdit" runat="server" EnableViewState="False" meta:resourcekey="lblEditResource1">edit</asp:Label></a>
			<asp:LinkButton id="btnModeratorApprove" Runat="server" Visible="False" CommandName="approve" CommandArgument='<%# Eval("MessageID") %>' meta:resourcekey="btnModeratorApproveResource1">|<b>approve</b></asp:LinkButton>
			<asp:LinkButton id="btnModeratorDelete" OnClientClick="if(!confirm('Are you sure?')) return false;" Runat="server" Visible="False" CommandName="delete" CommandArgument='<%# Eval("MessageID") %>' meta:resourcekey="btnModeratorDeleteResource1">delete</asp:LinkButton>
		</td></tr>
	</ItemTemplate>
	<FooterTemplate>
		</tbody>
		</table>
	</FooterTemplate>
</asp:repeater>

<div class="pager"><%= pagerString %></div>

<br /><br />
<div id="divQuickReply" runat="server" enableviewstate="false" visible="false">
<div class="richeditor">
	<div class="editbar" id="divEditbar" runat="server" enableviewstate="false">
		<button title="bold" onclick="wswgEditor.doClick('bold');" type="button"><b>B</b></button>
		<button title="italic" onclick="wswgEditor.doClick('italic');" type="button"><i>I</i></button>
		<button title="underline" onclick="wswgEditor.doClick('underline');" type="button"><u>U</u></button>
		<button title="hyperlink" onclick="wswgEditor.doLink();" type="button" style="background-image:url('images/url.gif');"></button>
		<button title="list" onclick="wswgEditor.doClick('InsertUnorderedList');" type="button" style="background-image:url('images/icon_list.gif');"></button>
		<button title="quote" onclick="wswgEditor.doQuote();" type="button" style="background-image:url('images/icon_quote.png');"></button>
		<button title="switch to source" type="button" onclick="wswgEditor.SwitchEditor()" style="background-image:url('images/icon_html.gif');"></button>
	</div>
	<div class="container iphone-textbox">
		<asp:TextBox Rows="2" TextMode="MultiLine" Height="50" Width="100%" ID="tbQuickReply" runat="server" EnableViewState="false" onkeydown="return ProcessCtrlEnter(event);"></asp:TextBox>
	</div>
</div>
<asp:Button runat="server" ID="btnQuickReply" OnClick="btnQuickReply_Click" OnClientClick="wswgEditor.doCheck();" Text="quick reply" EnableViewState="false" meta:resourcekey="btnQuickReplyResource1" />
<span class="gray">(Ctrl+Enter)</span>
</div>



<div id="divLink" style="display:none;position:absolute;background-color:#ebebeb;padding:7px" class="roundedborder gray">
share a link to this post<br />
<input type="text" id="tbLink" size="60" /><br />
<a href="javascript:void(0)" onclick="$('#divLink').hide()">close</a>
</div>

<script src="editor.js" type="text/javascript"></script>
<script type="text/javascript">
	if (document.location.href.toLowerCase().indexOf("messageid=") != -1) {
		document.location.hash = 'post' + getParameterByName("MessageID");
	}

	//init wysiwyg editor
	wswgEditor.initEditor("<%=tbQuickReply.ClientID%>", <%= (!(Request.Browser.IsMobileDevice || IsiPhoneOrAndroid() || IsIpad())).ToString().ToLower() %>);
	$(wswgEditor.getEditorDoc()).keydown(ProcessCtrlEnter); <%--processes ctrl+enter--%>
	$("#<%=tbQuickReply.ClientID%>").keydown(ProcessCtrlEnter); <%--processes ctrl+enter--%>

	function ShowLink(linkElement, msgId)
	{
		var linkPosition = $(linkElement).offset();
		$("#divLink").show().offset({ top: linkPosition.top + 25, left: linkPosition.left-$("#divLink").width() });
		$("#tbLink").val(document.URL.split('#')[0] + '#post' + msgId).focus().select();
	}

	function getParameterByName(name)
	{
		name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
		var regexS = "[\\?&]" + name + "=([^&#]*)";
		var regex = new RegExp(regexS);
		var results = regex.exec(window.location.href);
		if(results == null)
		return "";
		else
		return decodeURIComponent(results[1].replace(/\+/g, " "));
	}

	function RatePost(msgId, score) {
		//jquery ajax post
		$.ajax({
			url: "messagesajax.ashx",
			type: "POST",
			data: {Score: score, Mode: "Rate", MessageID: msgId },
			dataType: "text",
			success: function(rating) {
				if (rating!="") {
					var sign = (rating > 0) ? "+" : "";
					$("#spanRating" + msgId).html(sign + rating);
					if (rating != 0) {
						var color = (rating > 0) ? "green" : "red";
						$("#spanRating" + msgId).css("color", color);
					}
				}
			}
		});
	}
	<%-- processes ctrl+enter--%>
	function ProcessCtrlEnter(e) {
		var evt = e ? e : window.event;
		if (evt.ctrlKey) {
			var keyCode = evt.keyCode ? evt.keyCode : evt.which;
			if (keyCode == 13) {
				$("#<%=btnQuickReply.ClientID%>").click();
				return false;
			}
			else
				return true;
		}
		else
			return true;
	}
</script>
</asp:Content>