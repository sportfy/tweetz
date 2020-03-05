﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace tweetz.core.Services
{
    public static class LongUrlService
    {
        private const int maxCacheSize = 100;
        private static readonly ConcurrentDictionary<string, string> UrlCache = new ConcurrentDictionary<string, string>();

        public static async Task<string> TryGetLongUrl(string link)
        {
            try
            {
                if (UrlCache.TryGetValue(link, out var longUrl))
                {
                    return longUrl;
                }

                var request = WebRequest.Create(new Uri(link));
                request.Method = "HEAD";
                const int timeoutInMilliseconds = 2000;
                request.Timeout = timeoutInMilliseconds;

                using var response = await request.GetResponseAsync();
                var uri = response.ResponseUri.AbsoluteUri;

                if (!string.IsNullOrWhiteSpace(uri))
                {
                    if (UrlCache.Count > maxCacheSize)
                    {
                        UrlCache.Clear();
                    }

                    UrlCache.TryAdd(link, uri);
                    return uri;
                }
            }
            catch (WebException ex)
            {
                // not fatal, eat it
                Trace.TraceWarning(ex.Message);
            }
            catch (Exception ex)
            {
                // also not fatal
                Trace.TraceError(ex.Message);
            }
            return link;
        }

        public static void HyperlinkToolTipOpeningHandler(object sender, ToolTipEventArgs args)
        {
            if (sender is Hyperlink hyperlink)
            {
                // Refresh tooltip now to prevent showing old link due to VirtualizingPanel.VirtualizationMode="Recycling"
                hyperlink.ToolTip = hyperlink.CommandParameter ?? string.Empty;

                // Fire and forget pattern
                HyperlinkToolTipOpeningHandlerAsync(hyperlink).ConfigureAwait(false);
            }
        }

        private static async Task HyperlinkToolTipOpeningHandlerAsync(Hyperlink hyperlink)
        {
            var link = await TryGetLongUrl((string)hyperlink.CommandParameter);
            hyperlink.ToolTip = link;
        }
    }
}