using System;
using System.Collections.Generic;
using System.Web;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace aspnetforum
{
    public class imgthumbnail : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            HttpResponse response = context.Response;
            HttpRequest request = context.Request;

            string image = request.QueryString["Image"];

            if (image == null)
            {
                ErrorResult(response);
                return;
            }

            string sSize = request["Size"];
            int size = 64;
            if (sSize != null)
                size = int.Parse(sSize);

            string path;
            path = Utils.Attachments.GetUploadDirAbsolutePath() + image;
            if (!File.Exists(path)) //for old version compatibility
            {
                path = context.Server.MapPath(request.Path);
                path = path.Substring(0, path.IndexOf("imgthumbnail.ashx"));
                path = path + "upload\\" + image;
            }

            Bitmap bmp = CreateThumbnail(path, size, size);

            if (bmp == null)
            {
                ErrorResult(response);
                return;
            }

            // Put user code to initialize the page here
            response.ContentType = "image/jpeg";
            bmp.Save(response.OutputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            bmp.Dispose();

        }

        private void ErrorResult(HttpResponse response)
        {
            response.Clear();
			response.TrySkipIisCustomErrors = true;
            response.StatusCode = 404;
            response.End();
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

        /// Creates a resized bitmap from an existing image on disk.
        /// Call Dispose on the returned Bitmap object
        /// 
        /// 
        /// Bitmap or null
        private static Bitmap CreateThumbnail(string lcFilename, int lnWidth, int lnHeight)
        {
            System.Drawing.Bitmap bmpOut = null;
            try
            {
                Bitmap loBMP = new Bitmap(lcFilename);
                ImageFormat loFormat = loBMP.RawFormat;

                decimal lnRatio;
                int lnNewWidth = 0;
                int lnNewHeight = 0;

                //*** If the image is smaller than a thumbnail just return it
                if (loBMP.Width < lnWidth && loBMP.Height < lnHeight)
                    return loBMP;

                /*if (loBMP.Width > loBMP.Height)
                {
                    lnRatio = (decimal)lnWidth / loBMP.Width;
                    lnNewWidth = lnWidth;
                    decimal lnTemp = loBMP.Height * lnRatio;
                    lnNewHeight = (int)lnTemp;
                }
                else
                {*/
                    lnRatio = (decimal)lnHeight / loBMP.Height;
                    lnNewHeight = lnHeight;
                    decimal lnTemp = loBMP.Width * lnRatio;
                    lnNewWidth = (int)lnTemp;
                //}

                // System.Drawing.Image imgOut = 
                //      loBMP.GetThumbnailImage(lnNewWidth,lnNewHeight,
                //                              null,IntPtr.Zero);

                // *** This code creates cleaner (though bigger) thumbnails and properly
                // *** and handles GIF files better by generating a white background for
                // *** transparent images (as opposed to black)
                bmpOut = new Bitmap(lnNewWidth, lnNewHeight);
                Graphics g = Graphics.FromImage(bmpOut);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
                g.FillRectangle(Brushes.White, 0, 0, lnNewWidth, lnNewHeight);
                g.DrawImage(loBMP, 0, 0, lnNewWidth, lnNewHeight);

                loBMP.Dispose();
            }
            catch
            {
                return null;
            }

            return bmpOut;
        }
    }
}
