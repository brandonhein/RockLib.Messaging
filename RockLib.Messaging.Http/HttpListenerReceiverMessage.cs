﻿using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RockLib.Messaging.Http
{
    /// <summary>
    /// An implementation of IReceiverMessage for use by the <see cref="HttpListenerReceiver"/>
    /// class.
    /// </summary>
    public class HttpListenerReceiverMessage : ReceiverMessage
    {
        private readonly Regex _pathRegex;
        private readonly IReadOnlyCollection<string> _pathTokens;

        internal HttpListenerReceiverMessage(HttpListenerContext context, IHttpResponseGenerator httpResponseGenerator, Regex pathRegex, IReadOnlyCollection<string> pathTokens)
            : base(() => GetPayload(context))
        {
            Context = context;
            HttpResponseGenerator = httpResponseGenerator;
            _pathRegex = pathRegex;
            _pathTokens = pathTokens;
        }

        /// <summary>
        /// Gets the <see cref="HttpListenerContext"/> for the current request/response.
        /// </summary>
        public HttpListenerContext Context { get; }

        /// <summary>
        /// Gets the object that determines the http response that is returned to clients,
        /// depending on whether the message is acknowledged, rejected, or rolled back.
        /// </summary>
        public IHttpResponseGenerator HttpResponseGenerator { get; }

        /// <inheritdoc />
        protected override void AcknowledgeMessage()
        {
            var response = HttpResponseGenerator.GetAcknowledgeResponse(this);
            WriteResponse(response);
        }

        /// <inheritdoc />
        protected override void RollbackMessage()
        {
            var response = HttpResponseGenerator.GetRollbackResponse(this);
            WriteResponse(response);
        }

        /// <inheritdoc />
        protected override void RejectMessage()
        {
            var response = HttpResponseGenerator.GetRejectResponse(this);
            WriteResponse(response);
        }

        private void WriteResponse(HttpResponse response)
        {
            Context.Response.StatusCode = response.StatusCode;

            if (response.StatusDescription != null)
                Context.Response.StatusDescription = response.StatusDescription;

            switch (response.Content)
            {
                case string stringContent:
                    Context.Response.ContentEncoding = Encoding.UTF8;
                    var buffer = Encoding.UTF8.GetBytes(stringContent);
                    Context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    break;
                case byte[] binaryContent:
                    Context.Response.OutputStream.Write(binaryContent, 0, binaryContent.Length);
                    break;
            }

            foreach (var header in response.Headers)
                Context.Response.Headers.Add(header.Key, header.Value);

            Context.Response.Close();
        }

        /// <inheritdoc />
        protected override void InitializeHeaders(IDictionary<string, object> headers)
        {
            foreach (var key in Context.Request.Headers.AllKeys)
                headers.Add(key, Context.Request.Headers[key]);

            foreach (var key in Context.Request.QueryString.AllKeys)
                headers.Add(key, Context.Request.QueryString[key]);

            if (_pathTokens.Count > 0)
            {
                var match = _pathRegex.Match(Context.Request.Url.AbsolutePath);
                foreach (var token in _pathTokens)
                    headers.Add(token, match.Groups[token].Value);
            }
        }

        private static byte[] GetPayload(HttpListenerContext context)
        {
            var buffer = new byte[context.Request.ContentLength64];
            context.Request.InputStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}