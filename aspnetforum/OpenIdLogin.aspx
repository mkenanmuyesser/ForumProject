<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="OpenIdLogin.aspx.cs" MasterPageFile="AspNetForumMaster.Master" Inherits="aspnetforum.OpenIdLogin" %>


<asp:Content runat="server" ContentPlaceHolderID="ContentPlaceHolderHEAD">
<link href="openid.css" type="text/css" rel="Stylesheet" />
</asp:Content>


<asp:Content ContentPlaceHolderID="AspNetForumContentPlaceHolder" runat="server">

<script type="text/javascript">
function CheckUserName() {
    var textboxelement = document.getElementById("<%= tbPickUserName.ClientID %>");
    //jquery ajax post
    $.post(
        "ajaxutils.ashx", //url
        {username: textboxelement.value, mode: "CheckUserNameAvailability" }, //name-values to post
        function(data) {
            if (data == "1") {
                $("#imgOk").hide();
                $("#imgError").show();
            }
            else {
                $("#imgOk").show();
                $("#imgError").hide();
            }
        }, //callback
        "html");     //returned datatype
}
</script>

<div id="divLogin" class="openid" runat="server" enableviewstate="false">			
    <div class="location"><h2>Log in with Open ID:</h2></div>
    <div><ul>
    <li class="openid" title="OpenID"><img src="images/openid/openidW.png" alt="Custom URL" /><span><strong>http://{your-openid-url}</strong></span></li>
    <li class="direct" title="Google"><img src="images/openid/googleW.png" alt="Google" /><span>https://www.google.com/accounts/o8/id</span></li>
    <li class="direct" title="Yahoo"><img src="images/openid/yahooW.png" alt="Yahoo" /><span>http://yahoo.com/</span></li>
    <li class="username" title="AOL screen name"><img src="images/openid/aolW.png" alt="aol" /><span>http://openid.aol.com/<strong>username</strong></span></li>
    <li class="username" title="MyOpenID user name"><img src="images/openid/myopenid.png" alt="myopenid" /><span>http://<strong>username</strong>.myopenid.com/</span></li>
    <li class="username" title="Flickr user name"><img src="images/openid/flickr.png" alt="Flickr" /><span>http://flickr.com/<strong>username</strong>/</span></li>
    <li class="username" title="Technorati user name"><img src="images/openid/technorati.png" alt="Technorati" /><span>http://technorati.com/people/technorati/<strong>username</strong>/</span></li>
    <li class="username" title="Wordpress blog name"><img src="images/openid/wordpress.png" alt="Wordpress" /><span>http://<strong>username</strong>.wordpress.com</span></li>
    <li class="username" title="Blogger blog name"><img src="images/openid/blogger.png" alt="Blogger.com" /><span>http://<strong>username</strong>.blogspot.com/</span></li>
    <li class="username" title="LiveJournal blog name"><img src="images/openid/livejournal.png" alt="LiveJournal" /><span>http://<strong>username</strong>.livejournal.com</span></li>
    <li class="username" title="ClaimID user name"><img src="images/openid/claimid.png" alt="ClaimID" /><span>http://claimid.com/<strong>username</strong></span></li>
    <li class="username" title="Vidoop user name"><img src="images/openid/vidoop.png" alt="Vidoop" /><span>http://<strong>username</strong>.myvidoop.com/</span></li>
    <li class="username" title="Verisign user name"><img src="images/openid/verisign.png" alt="verisign" /><span>http://<strong>username</strong>.pip.verisignlabs.com/</span></li>
    </ul></div>
    <fieldset> 
    <label for="openid_username">Enter your <span>Provider user name</span></label> 
    <div><span></span><input type="text" name="openid_username" /><span></span>
    <input type="submit" value="login" name="loginbtn" /></div>
    </fieldset> 
    <fieldset> 
    <label for="openid_identifier">Enter your <a class="openid_logo" href="http://openid.net">OpenID</a></label> 
    <div><input type="text" name="openid_identifier" />
    <input type="submit" value="login" name="loginbtn" /></div> 
    </fieldset>
    
</div>

<div id="divPickLogin" runat="server" visible="false">
    <div class="location"><h2>Hello, <b style="color:Red"><asp:Label ID="lblOpenId" runat="server"></asp:Label></b>!</h2></div>
    
    <table class="roundedborder noborder gray" cellpadding="10">
    <tr>
        <td align="right">Your OpenID:</td>
        <td><b><asp:Label ID="lblOpenId2" runat="server" ForeColor="Black"></asp:Label></b></td>
    </tr>
    <tr>
        <td align="right"><asp:Label ID="lblPickUserName" runat="server">Pick a friendly username (required only once):</asp:Label></td>
        <td>
            <asp:TextBox ID="tbPickUserName" runat="server"></asp:TextBox>
            <a href="javascript:void(0)" onclick="CheckUserName()">
                <asp:Label ID="lblCheck" runat="server" meta:resourcekey="lblCheckResource1">check availability</asp:Label>
            </a><img style="display:none" src="images/ok.png" id="imgOk" alt="ok" /><img style="display:none" src="images/error.png" id="imgError" alt="allready taken" />
            <asp:RequiredFieldValidator ID="rqvUserName" runat="server" ControlToValidate="tbPickUserName" ErrorMessage="Please enter a username" Display="Dynamic"></asp:RequiredFieldValidator>
        </td>
    </tr>
    <tr>
        <td align="right"><asp:Label ID="lblEmail" runat="server">Your email:</asp:Label></td>
        <td>
            <asp:TextBox ID="tbEmail" runat="server"></asp:TextBox>
            <asp:RequiredFieldValidator ID="reqEmail" runat="server" ControlToValidate="tbEmail" ErrorMessage="Please enter email" Display="Dynamic"></asp:RequiredFieldValidator>
        </td>
    </tr>
    <tr>
        <td></td>
        <td><input type="submit" value="go" name="pickusernamebtn" /></td>
    </tr>
    </table>
</div>

<script type="text/javascript" src="jquery.openid.js"></script>
<script type="text/javascript"> $(function() { $("form").openid(); });</script>

</asp:Content>