<%@ Page Title="View User Profile" language="c#" Codebehind="viewpostsbyuser.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.viewpostsbyuser" MasterPageFile="aspnetforummaster.master" %>
<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
    <div class="location">
	<h2><asp:Label id="lblUser" runat="server" Font-Bold="True" meta:resourcekey="lblUserResource1"></asp:Label></h2>
    
    <h1><asp:Label ID="lblAllMsgs" runat="server" EnableViewState="False" meta:resourcekey="lblAllMsgsResource1">all messages by user</asp:Label></h1></div>
	    <asp:repeater id="rptMessagesList" runat="server" EnableViewState="False">
	        <HeaderTemplate>
	            <table class="postlist roundedborder">
	        </HeaderTemplate>
		    <ItemTemplate>
			    <tr valign="top">
				    <td>
				        <span class="gray">
				        <%# ToAgoString((DateTime)Eval("CreationDate"))%><br />
				        Topic:</span>
				        <a href='<%# aspnetforum.Utils.Various.GetTopicURL(Eval("TopicID"), Eval("Subject")) %>'><strong><%# Eval("Subject") %></strong></a>
				        <br/><br/>
				        <%# aspnetforum.Utils.User.DisplayUserInfo(userID, userName, null, avatarFileName, firstName, lastName)%>
			        </td>
				    <td><%# aspnetforum.Utils.Formatting.FormatMessageHTML(Eval("Body").ToString())%></td>
			    </tr>
		    </ItemTemplate>
		    <FooterTemplate>
                </table>
				<div class="pager"><%#pagerString %></div>
		    </FooterTemplate>
	    </asp:repeater>
</asp:Content>