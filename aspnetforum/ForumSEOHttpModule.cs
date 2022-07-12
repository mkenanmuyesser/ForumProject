using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.IO;

namespace aspnetforum
{
    public class ForumSEOHttpModule : IHttpModule
    {
        private static bool _moduleLoaded = false;

        public ForumSEOHttpModule()
        {
        }

        public void Dispose()
        {
        }

        public static bool SEOUrlsEnabled
        {
            get { return _moduleLoaded; }
        }

        public void Init(HttpApplication context)
        {
            _moduleLoaded = true;
            //let's register our event handler
            context.PostResolveRequestCache += new EventHandler(context_PostResolveRequestCache);
        }

        void context_PostResolveRequestCache(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;         

            //the file being requested is not found
            if (!File.Exists(application.Request.PhysicalPath))
            {
                string path = application.Request.Path.ToLower();
                if (!path.EndsWith(".aspx")) return;

                int posTopic = path.IndexOf("/topic", path.LastIndexOf("/")); //if our URL has a "/topic" in the file name
                int posForum = path.IndexOf("/forum", path.LastIndexOf("/")); //if our URL has a "/forum" in the file name

                if (posTopic > -1)
                {
                    string prefix = path.Substring(0, posTopic); //prefix is needed if the forum is installed under existing site in some subfolder, e.g. "/forum" - prefix

                    //try to extract topicid
                    string topicid = path.Substring(posTopic);
                    topicid = topicid.Replace("/topic", "");

                    int dashIndex = topicid.IndexOf("-");
                    if (dashIndex < 0) return;
                    topicid = topicid.Substring(0, dashIndex);

                    int tst = 0;
                    if (int.TryParse(topicid, out tst)) //topicid extracted and parsed
                    {
                        string topicURL = prefix + "/messages.aspx?TopicID=" + topicid + "&" + application.Request.QueryString.ToString();
                        context.RewritePath(topicURL);
                    }
                }
                else if (posForum > -1)
                {
                    string prefix = path.Substring(0, posForum); //prefix is needed if the forum is installed under existing site in some subfolder, e.g. "/forum" - prefix

                    //try to extract forumid
                    string forumid = path.Substring(posForum);
                    forumid = forumid.Replace("/forum", "");

                    int dashIndex = forumid.IndexOf("-");
                    if (dashIndex < 0) return;
                    forumid = forumid.Substring(0, dashIndex);

                    int tst = 0;
                    if (int.TryParse(forumid, out tst)) //topicid extracted and parsed
                    {
                        string topicURL = prefix + "/topics.aspx?ForumID=" + forumid + "&" + application.Request.QueryString.ToString();
                        context.RewritePath(topicURL);
                    }
                }
            }
        }
    }
}
