<%@ Page Title="" Language="C#" MasterPageFile="AspNetForumMaster.Master" AutoEventWireup="true" CodeBehind="updatedtopics.aspx.cs" Inherits="aspnetforum.updatedtopics" %>
<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" runat="server">

<div class="location">
	<h2><a href="default.aspx">
		<%= aspnetforum.Resources.various.Home %></a>
	</h2>

	<h1><%= aspnetforum.Resources.various.UnreadTopics %></h1>
</div>


<asp:repeater id="rptTopicsList" runat="server" EnableViewState="False">
	<HeaderTemplate>
		<table width="100%" class="biglist roundedborder">
		<tr><th></th><th><%= aspnetforum.Resources.various.LatestPost %></th></tr>
		<tbody>
	</HeaderTemplate>
	<ItemTemplate>
		<tr>
			<td width="70%"><h2><a href='<%# aspnetforum.Utils.Various.GetTopicURL(Eval("TopicID"), Eval("Subject")) %>'>
						<%# Eval("Subject") %>
					</a>
				</h2>
			</td>
			<td style="white-space:nowrap" class="gray">
			    <%# aspnetforum.Utils.Topic.GetTopicInfoBMessageyID(Eval("LastMessageID"), Eval("Subject"), Eval("RepliesCount") as int?, Cmd) %>
			</td>
		</tr>
	</ItemTemplate>
	<FooterTemplate>
		</tbody></table>
	</FooterTemplate>
</asp:repeater>


</asp:Content>
