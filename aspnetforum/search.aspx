<%@ Page Title="Search" language="c#" Codebehind="search.aspx.cs" AutoEventWireup="True" Inherits="aspnetforum.search" MasterPageFile="AspNetForumMaster.Master" %>

<asp:Content ContentPlaceHolderID="ContentPlaceHolderHEAD" runat="server">
<link href="http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.14/themes/ui-lightness/jquery-ui.css" type="text/css" rel="Stylesheet" />
<script src="https://ajax.googleapis.com/ajax/libs/jqueryui/1.8.16/jquery-ui.js" type="text/javascript"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" ID="AspNetForumContent" runat="server">
<script type="text/javascript">
function ValidateSearch(source, arguments)
{
	arguments.IsValid = (arguments.Value.length>2);
}
</script>

<div class="location">
	<h2><a href="default.aspx"><asp:Label ID="lblHome" runat="server" EnableViewState="False" meta:resourcekey="lblHomeResource1">Home</asp:Label></a></h2></div>
	<table class="roundedborder noborder" cellpadding="15">
	<tr><th colspan="3"><asp:Label ID="lblSearch" runat="server" EnableViewState="False" meta:resourcekey="lblSearchResource1">Search</asp:Label></th></tr>
	<tbody>
	<tr>
		<td colspan="3">
		<asp:TextBox placeholder="search..." Width="298" id="tbWords" runat="server" meta:resourcekey="tbWordsResource1" EnableViewState="false" />
		<asp:Button CssClass="gradientbutton" id="btnSearch" Font-Bold="true" runat="server" Text="search" onclick="btnSearch_Click" meta:resourcekey="btnSearchResource1" EnableViewState="false"></asp:Button>
		<br/>
		<asp:CustomValidator id="validatorLength" runat="server" ErrorMessage="3 characters minimum" ControlToValidate="tbWords" Display="Dynamic" ClientValidationFunction="ValidateSearch" meta:resourcekey="validatorLengthResource1" EnableViewState="false" />
		<asp:RequiredFieldValidator id="RequiredFieldValidator1" runat="server" ErrorMessage="nothing to search for" ControlToValidate="tbWords" Display="Dynamic" meta:resourcekey="RequiredFieldValidator1Resource1" EnableViewState="false" /></td>
	</tr>
	<tr>
		<td colspan="3">
			<asp:DropDownList ID="ddlForum" runat="server" Width="300"></asp:DropDownList>
		</td>
	</tr>
	<tr>
		<td>
			<asp:CheckBox ID="cbSearchTtitleOnly" runat="server" meta:resourcekey="cbSearchTtitleOnlyyResource1" Text="Search titles only" Checked="true" EnableViewState="false" />
		</td>
		<td align="right"><asp:Label ID="lblDateFrom" runat="server" EnableViewState="False" meta:resourcekey="lblDateFromResource1">Post date - from:</asp:Label><br/>
			<span class="gray">(yyyy-mm-dd)</span>
		</td>
		<td><asp:TextBox id="tbDateFrom" CssClass="datepick" runat="server" meta:resourcekey="tbDateFromResource1" EnableViewState="false" /><br/>
			<%--<asp:RegularExpressionValidator id="RegularExpressionValidator1" runat="server" Display="Dynamic" ControlToValidate="tbDateFrom" ErrorMessage="wrong format" ValidationExpression="^((\d{2})|(\d{4}))\-(([1-9])|(0[1-9])|(1[0-2]))\-(([0-9])|(0[1-9])|(1[0-9])|(2[0-9])|(3[0-1]))$" meta:resourcekey="RegularExpressionValidator1Resource1" EnableViewState="false" />--%>
			<asp:CustomValidator id="CustomValidatorDateFrom" runat="server" EnableViewState="false" Display="Dynamic" ControlToValidate="tbDateFrom" ErrorMessage="wrong format" meta:resourcekey="RegularExpressionValidator2Resource1" OnServerValidate="CustomValidatorDateFrom_ServerValidate" EnableClientScript="False" />
		</td>
	</tr>
	<tr>
		<td>
			<asp:RadioButton id="rbAll" runat="server" GroupName="searchmode" Checked="True" Text="Search ALL words" meta:resourcekey="lblAllResource1" EnableViewState="false" />
			<br />
			<asp:RadioButton id="rbExact" runat="server" GroupName="searchmode" meta:resourcekey="lblExactResource1" Text="Match EXACT phrase" EnableViewState="false" />
			<br />
			<asp:RadioButton id="rbAny" runat="server" GroupName="searchmode" meta:resourcekey="lblAnyResource1" Text="Match ANY of the words" EnableViewState="false" /></td>
		<td align="right"><asp:Label ID="lblDateTo" runat="server" EnableViewState="False" meta:resourcekey="lblDateToResource1">Post date - to:</asp:Label><br/>
			<span class="gray">(yyyy-mm-dd)</span></td>
		<td><asp:TextBox id="tbDateTo" CssClass="datepick" runat="server" meta:resourcekey="tbDateToResource1" EnableViewState="false" /><br/>
			<%--<asp:RegularExpressionValidator id="RegularExpressionValidator2" runat="server" Display="Dynamic" ControlToValidate="tbDateTo" ErrorMessage="wrong format" ValidationExpression="^((\d{2})|(\d{4}))\-(([1-9])|(0[1-9])|(1[0-2]))\-(([0-9])|(0[1-9])|(1[0-9])|(2[0-9])|(3[0-1]))$" meta:resourcekey="RegularExpressionValidator2Resource1" EnableViewState="false" />--%>
			<asp:CustomValidator id="CustomValidatorDateTo" runat="server" EnableViewState="false" Display="Dynamic" ControlToValidate="tbDateTo" ErrorMessage="wrong format" meta:resourcekey="RegularExpressionValidator2Resource1" OnServerValidate="CustomValidatorDateTo_ServerValidate" EnableClientScript="false" />
		</td></tr></tbody></table>
	
<%--datepicker--%>
<script type="text/javascript">
	$(function () {
		$(".datepick").datepicker({ gotoCurrent: true, dateFormat: "yy-mm-dd", firstDay: 1 });
	});
</script>

	
<br/>

<asp:repeater id="rptTopicsList" runat="server" EnableViewState="False">
<HeaderTemplate>
	<table width="100%" class="roundedborder biglist">
	<tr><th></th><th><%= aspnetforum.Resources.various.LatestPost %></th></tr>
	<tbody>
</HeaderTemplate>
<ItemTemplate>
	<tr <%# Container.ItemType == ListItemType.AlternatingItem ? " class='altItem'" : "" %>>
		<td width="70%"><h2><a href='<%# aspnetforum.Utils.Various.GetTopicURL(Eval("TopicID"), Eval("Subject")) %>'>
					<%# Eval("Subject") %>
				</a>
			</h2>
		</td>
		<td width="30%" style="white-space:nowrap" class="gray">
			<%# aspnetforum.Utils.Topic.GetTopicInfoBMessageyID(Eval("LastMessageID"), Eval("Subject"), Eval("RepliesCount") as int?, Cmd) %>
		</td>
	</tr>
</ItemTemplate>
<FooterTemplate>
	</tbody></table>
</FooterTemplate>
</asp:repeater>

<asp:Label id="lblNothingFound" runat="server" Visible="False" EnableViewState="false" meta:resourcekey="lblNothingFoundResource1">Sorry, your search did not match any results.</asp:Label>
<div class="location">
	<h2><a href="default.aspx"><asp:Label ID="lblHome2" runat="server" EnableViewState="False" meta:resourcekey="lblHomeResource1">Home</asp:Label></a>
	&raquo;
	<asp:Label ID="lblSearch3" runat="server" EnableViewState="False" meta:resourcekey="lblSearch3Resource1">Search</asp:Label></h2></div>
</asp:Content>