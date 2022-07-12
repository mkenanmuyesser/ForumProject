<%@ Page Language="C#" AutoEventWireup="true" EnableViewState="false" CodeBehind="dbupgrade.aspx.cs" Inherits="aspnetforum.dbupgrade" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>AspNetForum Database upgrader</title>
    <link href="aspnetforum.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div class="forumcontent innercontainer">
    
    <div class="location"><h2>Database Upgrade Script</h2></div>
    
    This script will try to upgrade your database to the latest version, adding missing tables and columns if necessary.
    Please note that the database user should have enough permissions for the upgrade-script to work properly ("db_owner" for MS SQL).
    
    <br /><br />
    
    <asp:Button ID="btnGo" CssClass="gradientbutton" Text="Start" runat="server" OnClick="btnGo_Click" />
    
    <br /><br />
    
    <asp:Label ID="lblResult" runat="server"></asp:Label>
    
    </div>
    </form>
</body>
</html>
