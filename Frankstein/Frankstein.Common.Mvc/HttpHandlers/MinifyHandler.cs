﻿using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.Caching;
using Microsoft.Ajax.Utilities;

namespace Frankstein.Common.Mvc.HttpHandlers
{
    public class MinifyHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            string file = context.Server.MapPath(context.Request.CurrentExecutionFilePath);

            if (!File.Exists(file))
                return;

            string ext = Path.GetExtension(file);

            if (context.IsDebuggingEnabled)
            {
                context.Response.TransmitFile(file);
            }
            else
            {
                Minify(context.Response, file, ext);
            }

            SetHeaders(context.Response, file, ext);
        }

        private static void SetHeaders(HttpResponse response, string file, string ext)
        {
            response.ContentType = ext == ".css" ? "text/css" : "text/javascript";

            response.Cache.SetLastModified(File.GetLastWriteTimeUtc(file));
            response.Cache.SetValidUntilExpires(true);
            response.Cache.SetExpires(DateTime.Now.AddYears(1));
            response.Cache.SetCacheability(HttpCacheability.ServerAndPrivate);
            response.Cache.SetVaryByCustom("Accept-Encoding");

            response.AddCacheDependency(new CacheDependency(file));
        }

        private static void Minify(HttpResponse response, string file, string ext)
        {
            string content = File.ReadAllText(file);
            Minifier minifier = new Minifier();

            if (ext == ".css")
            {
                Trace.TraceInformation("Applying Css Minification to '{0}'", file);
                CssSettings settings = new CssSettings() { CommentMode = CssComment.None };
                response.Write(minifier.MinifyStyleSheet(content, settings));
            }
            else if (ext == ".js")
            {
                Trace.TraceInformation("Applying Javascript Minification to '{0}'", file);
                CodeSettings settings = new CodeSettings() { PreserveImportantComments = false };
                response.Write(minifier.MinifyJavaScript(content, settings));
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}