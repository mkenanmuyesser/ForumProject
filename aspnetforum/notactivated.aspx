<%@ Page Title="" Language="C#" MasterPageFile="AspNetForumMaster.Master" AutoEventWireup="true" CodeBehind="notactivated.aspx.cs" Inherits="aspnetforum.notactivated" %>
<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" runat="server">

<br />

It appears that your user account is not activated yet.

<br /><br />

<asp:Button ID="btnResend" runat="server" Text="Resend the activation email" OnClick="btnResend_Click" />

</asp:Content>
