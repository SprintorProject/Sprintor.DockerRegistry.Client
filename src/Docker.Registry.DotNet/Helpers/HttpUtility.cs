using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

using Docker.Registry.DotNet.Registry;

using JetBrains.Annotations;

namespace Docker.Registry.DotNet.Helpers
{
    internal static class HttpUtility
    {
        internal static Uri BuildUri(this Uri baseUri, string path, IQueryString queryString)
        {
            if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));

            var builder = new UriBuilder(baseUri);

            if (!string.IsNullOrEmpty(path)) builder.Path += path;

            if (queryString != null) builder.Query = queryString.GetQueryString();

            return builder.Uri;
        }

        /// <summary>
        ///     Attempts to retrieve the value of a response header.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="name"></param>
        /// <returns>The first value if one is found, null otherwise.</returns>
        public static string GetHeader([NotNull] this RegistryApiResponse response, string name)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            return response.Headers
                .FirstOrDefault(h => h.Key == name).Value?.FirstOrDefault();
        }

        /// <summary>
        ///     Attempts to retrieve the value of a response header.
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="name"></param>
        /// <returns>The first value if one is found, null otherwise.</returns>
        public static string[] GetHeaders(
            this IEnumerable<KeyValuePair<string, string[]>> headers,
            string name)
        {
            return headers
                .IfNullEmpty()
                .Where(h => h.Key == name)
                .Select(h => h.Value?.FirstOrDefault())
                .ToArray();
        }

        public static void AddRange(
            [NotNull] this HttpRequestHeaders header,
            IEnumerable<KeyValuePair<string, string>> headers)
        {
            if (header == null) throw new ArgumentNullException(nameof(header));

            foreach (var item in headers.IfNullEmpty()) header.Add(item.Key, item.Value);
        }

        public static AuthenticationHeaderValue GetHeaderBySchema(
            [NotNull] this HttpResponseMessage response,
            string schema)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            return response.Headers.WwwAuthenticate.FirstOrDefault(s => s.Scheme == schema);
        }

        public static int? GetContentLength([NotNull] this HttpResponseHeaders responseHeaders)
        {
            if (responseHeaders == null) throw new ArgumentNullException(nameof(responseHeaders));

            if (!responseHeaders.TryGetValues("Content-Length", out var values)) return null;

            var raw = values.FirstOrDefault();

            if (int.TryParse(raw, out var parsed)) return parsed;

            return null;
        }

        public static string GetString([NotNull] this HttpResponseHeaders responseHeaders, string name)
        {
            if (responseHeaders == null) throw new ArgumentNullException(nameof(responseHeaders));

            if (!responseHeaders.TryGetValues(name, out var values)) return null;

            return values.FirstOrDefault();
        }

        public static Dictionary<string, string> ParseQueryString(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException("url");
            }
            var uri = new Uri(url);
            if (string.IsNullOrWhiteSpace(uri.Query))
            {
                return new Dictionary<string, string>();
            }

            var dic = uri.Query.Substring(1)
                    .Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(param => param.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries))
                    .GroupBy(part => part[0], part => part.Length > 1 ? part[1] : string.Empty)
                    .ToDictionary(group => group.Key, group => string.Join(",", group));

            return dic;
        }
    }
}