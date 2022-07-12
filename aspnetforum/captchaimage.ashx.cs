using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using System.Drawing.Imaging;

namespace aspnetforum
{
    public class captchaimage : IHttpHandler, IReadOnlySessionState
    {

        public void ProcessRequest(HttpContext context)
        {
            HttpSessionState session = HttpContext.Current.Session;
            HttpResponse response = HttpContext.Current.Response;

            if (session["CaptchaImageText"] == null) return;

            // Create a CAPTCHA image using the text stored in the Session object.
            Utils.CaptchaImage ci = new Utils.CaptchaImage(session["CaptchaImageText"].ToString(), 200, 50);

            // Change the response headers to output a JPEG image.
            response.Clear();
            response.ContentType = "image/jpeg";

            // Write the image to the response stream in JPEG format.
            ci.Image.Save(response.OutputStream, ImageFormat.Jpeg);

            // Dispose of the CAPTCHA image object.
            ci.Dispose();
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}
