<%@ Page Language="C#" MasterPageFile="AspNetForumMaster.Master" AutoEventWireup="true"
    Codebehind="faq.aspx.cs" Inherits="aspnetforum.faq" Title="FAQ" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolderHEAD" runat="server">
    <style type="text/css">
div.column1 {
	width: 49%;
	float: left;
	}
div.column2 {
	width: 50%;
	float: right;
	}
</style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="AspNetForumContentPlaceHolder" runat="server">
    <h1>Forum Frequently Asked Questions</h1>
    <br />
    <br />
    <div id="faqlinks">
        <div class="column1">
            <dl>
                <dt><strong>Login and Registration Issues</strong></dt><dd><a href="#f00">Why can't
                    I login?</a></dd><dd><a href="#f01">Why do I need to register at all?</a></dd><dd><a
                        href="#f02">Why do I get logged off automatically?</a></dd><dd><a href="#f04">I've lost
                            my password!</a></dd><dd><a href="#f05">I registered but cannot login!</a></dd><dd><a
                                href="#f06">I registered in the past but cannot login any more?!</a></dd></dl>
            <dl>
                <dt><strong>User Preferences and settings</strong></dt><dd><a href="#f10">How do I change
                    my settings?</a></dd><dd><a href="#f14">How do I show an image next to my username?</a></dd></dl>
            <dl>
                <dt><strong>Posting Issues</strong></dt><dd><a href="#f20">How do I post a topic in
                    a forum?</a></dd><dd><a href="#f21">How do I edit or delete a post?</a></dd><dd><a
                        href="#f26">Why can't I access a forum?</a></dd><dd><a href="#f27">Why can't I add attachments?</a></dd><dd><a
                            href="#f211">Why does my post need to be approved?</a></dd></dl>
            <dl>
                <dt><strong>Formatting and Topic Types</strong></dt><dd><a href="#f31">Can I use HTML?</a></dd><dd><a
                    href="#f32">What are Smilies?</a></dd><dd><a href="#f33">Can I post images?</a></dd><dd><a
                        href="#f36">What are sticky topics?</a></dd><dd><a href="#f37">What are closed topics?</a></dd></dl>
        </div>
        <div class="column2">
            <dl>
                <dt><strong>Rating and reputation</strong></dt><dd><a href="#f91">What are ratings?</a></dd><dd><a
                    href="#f92">How do I rate posts?</a></dd><dd><a href="#f93">How do I filter unrated posts?</a></dd></dl>
            <dl>
                <dt><strong>User Levels and Groups</strong></dt><dd><a href="#f40">What are Administrators?</a></dd><dd><a
                    href="#f41">What are Moderators?</a></dd><dd><a href="#f42">What are usergroups?</a></dd></dl>
            <dl>
                <dt><strong>Private Messaging</strong></dt><dd><a href="#f50">I cannot send private
                    messages!</a></dd><dd><a href="#f52">I have received a spamming or abusive e-mail from
                        someone on this forum!</a></dd></dl>
            <dl>
                <dt><strong>Searching the Forums</strong></dt><dd><a href="#f70">How can I search a
                    forum or forums?</a></dd><dd><a href="#f74">How can I find my own or someone else's
                        posts and topics?</a></dd></dl>
            <dl>
                <dt><strong>Topic Subscriptions and Bookmarks</strong></dt><dd><a href="#f81">How do
                    I subscribe to specific forums or topics?</a></dd><dd><a href="#f82">How do I remove
                        my subscriptions?</a></dd></dl>
            <dl>
                <dt><strong>Attachments</strong></dt><dd><a href="#f90">What is maximum attachment size
                    allowed on this forum?</a></dd></dl>
        </div>
    </div>
    <div style="clear: both">
        
        <hr />
        <h2>
            Login and Registration Issues</h2>
        <dl>
            <dt id="f00"><strong>Why can't I login?</strong></dt><dd>Make sure your username and
                password are correct. If they are, contact the forum administrator to make sure
                your account hasn't been banned or removed. It is also possible the website owner
                has a configuration error on their end, and they would need to fix it.</dd><dd><a
                    href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f01"><strong>Why do I need to register at all?</strong></dt><dd>You may not
                have to, it is up to the administrator of the forum as to whether you need to register
                in order to post messages. However; registration will give you access to additional
                features not available to guest users such as avatar images, private messaging
                etc. It only takes a few moments to register.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f02"><strong>Why do I get logged off automatically?</strong></dt><dd>If you
                do not check the <em>Remember me</em> box when you log in, the forum will only keep
                you logged in for a preset time. This prevents misuse of your account by anyone
                else. To stay logged in, check the box during login. This is not recommended if
                you access the forum from a public computer, e.g. internet cafe etc.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f04"><strong>I've lost my password!</strong></dt><dd>Click the <em>lost password</em>
                link and follow the instructions.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f05"><strong>I registered but I cannot login!</strong></dt><dd>Make sure your
                username and password are correct. If they are, then the forum requires new registrations
                to be activated, either by yourself or by an administrator before you can logon.
                You were sent an e-mail, please follow the instructions in the email to activate
                your account. If you did not receive an e-mail, you may have provided an incorrect
                e-mail address or the e-mail may have been picked up by a spam filer. If you are
                sure the e-mail address you provided is correct, try contacting the forum administrator.</dd><dd><a
                    href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f06"><strong>I registered in the past but cannot login any more?!</strong></dt><dd>Attempt
                to locate the e-mail sent to you when you first registered, check your username
                and password and try again. It is possible an administrator has deactivated or deleted
                your account for some reason. Also, many forums periodically remove users who have
                not posted for a long time to reduce the size of the database. If this has happened,
                try registering again and being more involved in discussions.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        
        <hr />
        <h2>
            User Preferences and settings</h2>
        <dl>
            <dt id="f10"><strong>How do I change my settings?</strong></dt><dd>If you are a registered
                user, all your settings are stored in the forum database. To alter them, click your
                username in the upper right corner while logged in.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f14"><strong>How do I show an image next to my username?</strong></dt><dd>It is
                up to the forum administrator to enable avatars and to choose the way in which avatars
                can be made available. If you are unable to use avatars, contact a forum administrator
                and ask him for his reasons.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <hr />
        <h2>
            Posting Issues</h2>
        <dl>
            <dt id="f20"><strong>How do I post a topic in a forum?</strong></dt><dd>To post a new
                topic in a forum, click the relevant button on either the forum or topic screens.
                You may need to register before you can post a message.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f21"><strong>How do I edit or delete a post?</strong></dt><dd>Unless you are
                a forum administrator or moderator, you can only edit or delete your own posts.
                You can edit a post by clicking the "edit" button for the relevant post.</dd><dd><a
                    href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f26"><strong>Why can't I access a forum?</strong></dt><dd>Some forums may be
                limited to certain users or groups. To view, read, post or perform another action
                you may need special permissions. Contact a moderator or the forum administrator to
                grant you access.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f27"><strong>Why can't I add attachments?</strong></dt><dd>The forum administrator
                may not have allowed attachments. Contact the forum administrator if you are unsure
                about why you are unable to add attachments.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f211"><strong>Why does my post need to be approved?</strong></dt><dd>The forum
                administrator may have decided that posts in the forum you are posting to require
                review before submission.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <hr />
        <h2>
            Formatting and Topic Types</h2>
        <dl>
            <dt id="f31"><strong>Can I use HTML?</strong></dt><dd>No. It is not possible to post
                HTML on this forum and have it rendered as HTML. Most formatting which can be carried
                out using HTML can be applied using the formatting toolbar when posting a message.</dd><dd><a
                    href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f32"><strong>What are Smilies?</strong></dt><dd>Smilies, or Emoticons, are small
                images which can be used to express a feeling using a short code, e.g. :) denotes
                happy, while :( denotes sad. The full list of emoticons can be seen in the posting
                form. Try not to overuse smilies, however, as they can quickly render a post unreadable
                and a moderator may edit them out or remove the post altogether. The forum administrator
                may also have set a limit to the number of smilies you may use within a post.</dd><dd><a
                    href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f33"><strong>Can I post images?</strong></dt><dd>Yes, images can be shown in
                your posts. If the administrator has allowed attachments, you may be able to upload
                the image to the forum. Otherwise, you must link to an image stored on a publicly
                accessible web server, e.g. http://www.example.com/my-picture.gif. You cannot link
                to pictures stored on your own PC (unless it is a publicly accessible webserver)
                nor images stored behind authentication mechanisms, e.g. hotmail or yahoo mailboxes,
                password protected sites, etc. To display the image use the appropriate button on
                the formatting toolbar which adds an [img] tag.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f36"><strong>What are sticky topics?</strong></dt><dd>Sticky topics within the
                forum appear on top of other topics in a forum. They are often quite important so
                you should read them whenever possible. Topics are made sticky by Administrators
                and Moderators.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f37"><strong>What are closed topics?</strong></dt><dd>Closed topics are topics
                where users can no longer reply. Topics may be locked for many reasons and were
                set this way by either the forum moderator or forum administrator.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <hr />
        <h2>
            User Levels and Groups</h2>
        <dl>
            <dt id="f40"><strong>What are Administrators?</strong></dt><dd>Administrators are members
                assigned with the highest level of control over the entire forum. These members
                can control all facets of forum operation, including setting permissions, banning
                users, creating usergroups or moderators, etc., dependent upon the forum founder
                and what permissions he or she has given the other administrators. They may also
                have full moderator capabilities in all forums.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f41"><strong>What are Moderators?</strong></dt><dd>Moderators are individuals
                (or groups of individuals) who look after the forums from day to day. They have
                the authority to edit or delete posts and lock, unlock, move, delete topics in the
                forum they moderate. Generally, moderators are present to prevent users from going
                off-topic or posting abusive or offensive material.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <dl>
            <dt id="f42"><strong>What are usergroups?</strong></dt><dd>Usergroups are created and
                handled by Administrators. Usergroups are groups of users that divide the community
                into manageable sections forum administrators can work with. Each user can belong
                to several groups and each group can be assigned individual permissions. This provides
                an easy way for administrators to change permissions for many users at once, such
                as changing moderator permissions or granting users access to a private forum.</dd><dd><a
                    href="#faqlinks">Top</a></dd></dl>
        
        <hr />
        <h2>
            Private Messaging</h2>
        <dl>
            <dt id="f50"><strong>I cannot send private messages!</strong></dt><dd>You are not registered
            and/or not logged on. Please register and log in to be able to send PMs.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        <hr />
        <dl>
            <dt id="f52"><strong>I have received a spamming or abusive message from someone on this
                forum!</strong></dt><dd>Please e-mail the forum administrator with a full copy of the
                    message you received. The forum administrator can then take action.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        
        <hr />
        <h2>Searching the Forums</h2>
        <dl>
            <dt id="f70"><strong>How can I search a forum or forums?</strong></dt><dd>Click the
                "Search" link, enter a search term in the search box. Click the "search" button.</dd><dd><a
                    href="#faqlinks">Top</a></dd></dl>
        <dl>
            <dt id="f74"><strong>How can I find my own or someone else's posts and topics?</strong></dt><dd>Click
                a username in the topic you have answered to, and view your profile as other's see
                it.</dd><dd><a href="#faqlinks">Top</a></dd></dl>

        <hr />
        <h2>
            Topic Subscriptions and Bookmarks</h2>
        <dl>
            <dt id="f81"><strong>How do I subscribe to specific forums or topics?</strong></dt><dd>To
                subscribe to a specific forum, click the "watch this forum for new topics" link
                upon entering the forum. To subscribe to a topic, reply to the topic with the subscribe
                checkbox checked or click the "watch this topic for replies" link within the topic
                itself.</dd><dd><a href="#faqlinks">Top</a></dd></dl>
        <dl>
            <dt id="f82"><strong>How do I remove my subscriptions?</strong></dt><dd>To remove your
                subscriptions, go to your profile (by clicking your username on the top) and click
                "My subscriptions".</dd><dd><a href="#faqlinks">Top</a></dd></dl>

        <hr />
        <h2>Attachments</h2>
        <dl>
            <dt id="f90"><strong>What is maximum attachment size allowed on this forum?</strong></dt><dd>Each
                forum administrator can set up a maximum attachment size. If you are unsure what
                is it, contact the forum administrator for assistance.</dd><dd><a href="#faqlinks">Top</a></dd></dl>

        <hr />
        <h2>Ratings and reputations</h2>
        <dl>
            <dt id="f91"><strong>What are ratings?</strong></dt><dd>User can rate messages up and down.
            This gives users "reputation" and makes finding valuable information easier. Administrator can disable rating feature.
            </dd><dd><a href="#faqlinks">Top</a></dd></dl>
        <dl>
            <dt id="f92"><strong>How do I rate posts?</strong></dt><dd>To rate  a post
                simply click the thumbs-up or thumbs-down icon next to it. the icon is displayed to registered users only.
                You can vote only once.
            </dd><dd><a href="#faqlinks">Top</a></dd></dl>
        <dl>
            <dt id="f93"><strong>How do I filter unrated posts?</strong></dt><dd>Each topic has a
                "hide unrate posts" link on top. You can hide unrated "noise" and read the valuable information only.
            </dd><dd><a href="#faqlinks">Top</a></dd></dl>
    </div>
</asp:Content>
