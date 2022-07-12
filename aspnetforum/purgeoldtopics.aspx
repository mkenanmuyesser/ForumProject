<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="purgeoldtopics.aspx.cs" Inherits="aspnetforum.purgeoldtopics" MasterPageFile="AspNetForumMaster.Master" %>

<asp:Content ContentPlaceHolderID="ContentPlaceHolderHEAD" runat="server">
<link href="https://ajax.googleapis.com/ajax/libs/jqueryui/1.8.14/themes/ui-lightness/jquery-ui.css" type="text/css" rel="Stylesheet" />
<script src="https://ajax.googleapis.com/ajax/libs/jqueryui/1.8.16/jquery-ui.js" type="text/javascript"></script>
</asp:Content>

<asp:Content runat="server" ContentPlaceHolderID="AspNetForumContentPlaceHolder">

<p><asp:Label ID="lblPurge" runat="server" EnableViewState="False" meta:resourcekey="lblEditForumResource1">Purge old topics</asp:Label></p>

<asp:TextBox id="tbDateFrom" CssClass="datepick" runat="server" meta:resourcekey="tbDateFromResource1" EnableViewState="false" />
<asp:Button ID="btnPurge" runat="server" Text="Purge topics older than this" OnClick="btnPurge_Click" />

<asp:Label runat="server" ID="lblRes"></asp:Label>

<%--datepicker--%>
<script type="text/javascript">
	$(function () {
		$(".datepick").datepicker({ gotoCurrent: true, dateFormat: "yy-mm-dd", firstDay: 1 });
	});
</script>


</asp:Content>