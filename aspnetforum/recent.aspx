<%@ Page Title="Recent messages" language="c#" Codebehind="recent.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.recent" MasterPageFile="AspNetForumMaster.Master" %>
<%@ Register TagPrefix="cc" TagName="RecentPosts" Src="recentposts.ascx" %>

<asp:Content ContentPlaceHolderID="ContentPlaceHolderHEAD" ID="AspNetForumHead" runat="server">
<link rel="alternate" type="application/rss+xml" title="recent posts" href="recent.aspx?rss=1" />
</asp:Content>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
<div class="location">
	<h2><a href="default.aspx"><%= aspnetforum.Resources.various.Home %></a></h2>
</div>

<cc:RecentPosts id="recentPosts" runat="server"></cc:RecentPosts>

</asp:Content>