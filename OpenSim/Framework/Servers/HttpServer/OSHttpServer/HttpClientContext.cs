using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using OSHttpServer.Exceptions;
using OSHttpServer.Parser;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace OSHttpServer
{
    /// <summary>
    /// Contains a connection to a browser/client.
    /// </summary>
    /// <remarks>
    /// Remember to <see cref="Start"/> after you have hooked the <see cref="RequestReceived"/> event.
    /// </remarks>
    public class HttpClientContext : IHttpClientContext, IDisposable
    {
        const int MAXREQUESTS = 20;
        const int MAXKEEPALIVE = 60000;

        static private int basecontextID;

        private readonly byte[] m_ReceiveBuffer;
        private int m_ReceiveBytesLeft;
        private ILogWriter _log;
        private readonly IHttpRequestParser m_parser;
        private readonly int m_bufferSize;
        private HashSet<uint> requestsInServiceIDs;
        private Socket m_sock;

        public bool Available = true;
        public bool StreamPassedOff = false;

        public int LastActivityTimeMS = 0;
        public int MonitorKeepaliveStartMS = 0;
        public bool TriggerKeepalive = false;
        public int TimeoutFirstLine = 10000; // 10 seconds
        public int TimeoutRequestReceived = 30000; // 30 seconds

        // The difference between this and request received is on POST more time is needed before we get the full request.
        public int TimeoutMaxIdle = 180000; // 3 minutes
        public int m_TimeoutKeepAlive = MAXKEEPALIVE; // 400 seconds before keepalive timeout

        public int m_maxRequests = MAXREQUESTS;

        public bool FirstRequestLineReceived;
        public bool FullRequestReceived;

        private bool isSendingResponse = false;

        private HttpRequest m_currentRequest;
        private HttpResponse m_currentResponse;

        public int contextID { get; private set; }
        public int TimeoutKeepAlive
        {
            get { return m_TimeoutKeepAlive; }
            set
            {
                m_TimeoutKeepAlive = (value > MAXKEEPALIVE) ? MAXKEEPALIVE : value;
            }
        }

        public int MaxRequests
        {
            get { return m_maxRequests; }
            set
            {
                if(value <= 1)
                    m_maxRequests = 1;
                else
                   m_maxRequests = value > MAXREQUESTS ? MAXREQUESTS : value;
            }
        }

        public bool IsSending()
        {
            return isSendingResponse;
        }

        public bool StopMonitoring;

        /// <summary>
        /// Context have been started (a new client have connected)
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientContext"/> class.
        /// </summary>
        /// <param name="secured">true if the connection is secured (SSL/TLS)</param>
        /// <param name="remoteEndPoint">client that connected.</param>
        /// <param name="stream">Stream used for communication</param>
        /// <param name="parserFactory">Used to create a <see cref="IHttpRequestParser"/>.</param>
        /// <param name="bufferSize">Size of buffer to use when reading data. Must be at least 4096 bytes.</param>
        /// <exception cref="SocketException">If <see cref="Socket.BeginReceive(byte[],int,int,SocketFlags,AsyncCallback,object)"/> fails</exception>
        /// <exception cref="ArgumentException">Stream must be writable and readable.</exception>
        public HttpClientContext(bool secured, IPEndPoint remoteEndPoint,
                                    Stream stream, IRequestParserFactory parserFactory, Socket sock)
        {
            if (!stream.CanWrite || !stream.CanRead)
                throw new ArgumentException("Stream must be writable and readable.");

            RemoteAddress = remoteEndPoint.Address.ToString();
            RemotePort = remoteEndPoint.Port.ToString();
            _log = NullLogWriter.Instance;
            m_parser = parserFactory.CreateParser(_log);
            m_parser.RequestCompleted += OnRequestCompleted;
            m_parser.RequestLineReceived += OnRequestLine;
            m_parser.HeaderReceived += OnHeaderReceived;
            m_parser.BodyBytesReceived += OnBodyBytesReceived;
            m_currentRequest = new HttpRequest(this);
            IsSecured = secured;
            m_stream = stream;
            m_sock = sock;

            m_bufferSize = 8196;
            m_ReceiveBuffer = new byte[m_bufferSize];
            requestsInServiceIDs = new HashSet<uint>();

            SSLCommonName = "";
            if (secured)
            {
                SslStream _ssl = (SslStream)m_stream;
                X509Certificate _cert1 = _ssl.RemoteCertificate;
                if (_cert1 != null)
                {
                    X509Certificate2 _cert2 = new X509Certificate2(_cert1);
                    if (_cert2 != null)
                        SSLCommonName = _cert2.GetNameInfo(X509NameType.SimpleName, false);
                }
            }

            ++basecontextID;
            if (basecontextID <= 0)
                basecontextID = 1;

            contextID = basecontextID;
        }

        public bool CanSend()
        {
            if (contextID < 0)
                return false;

            if (m_stream == null || m_sock == null || !m_sock.Connected)
                return false;

            return true;
        }

        /// <summary>
        /// Process incoming body bytes.
        /// </summary>
        /// <param name="sender"><see cref="IHttpRequestParser"/></param>
        /// <param name="e">Bytes</param>
        protected virtual void OnBodyBytesReceived(object sender, BodyEventArgs e)
        {
            m_currentRequest.AddToBody(e.Buffer, e.Offset, e.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnHeaderReceived(object sender, HeaderEventArgs e)
        {
            if (string.Compare(e.Name, "expect", true) == 0 && e.Value.Contains("100-continue"))
            {
                lock (requestsInServiceIDs)
                {
                    if (requestsInServiceIDs.Count == 0)
                        Respond("HTTP/1.1", HttpStatusCode.Continue, "Please continue.");
                }
            }
            m_currentRequest.AddHeader(e.Name, e.Value);
        }

        private void OnRequestLine(object sender, RequestLineEventArgs e)
        {
            m_currentRequest.Method = e.HttpMethod;
            m_currentRequest.HttpVersion = e.HttpVersion;
            m_currentRequest.UriPath = e.UriPath;
            m_currentRequest.AddHeader("remote_addr", RemoteAddress);
            m_currentRequest.AddHeader("remote_port", RemotePort);

            FirstRequestLineReceived = true;
            TriggerKeepalive = false;
            MonitorKeepaliveStartMS = 0;
            LastActivityTimeMS = ContextTimeoutManager.EnvironmentTickCount();
        }

        /// <summary>
        /// Start reading content.
        /// </summary>
        /// <remarks>
        /// Make sure to call base.Start() if you override this method.
        /// </remarks>
        public virtual void Start()
        {
            ReceiveLoop();
            Started?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clean up context.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public virtual void Cleanup()
        {
            if (StreamPassedOff)
                return;

            if (m_stream != null)
            {
                m_stream.Close();
                m_stream = null;
                m_sock = null;
            }

            m_currentRequest?.Clear();
            m_currentRequest = null;
            m_currentResponse?.Clear();
            m_currentResponse = null;
            requestsInServiceIDs.Clear();

            FirstRequestLineReceived = false;
            FullRequestReceived = false;
            LastActivityTimeMS = 0;
            StopMonitoring = true;
            MonitorKeepaliveStartMS = 0;
            TriggerKeepalive = false;

            isSendingResponse = false;

            m_ReceiveBytesLeft = 0;

            contextID = -100;
            m_parser.Clear();
        }

        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Using SSL or other encryption method.
        /// </summary>
        [Obsolete("Use IsSecured instead.")]
        public bool Secured
        {
            get { return IsSecured; }
        }

        /// <summary>
        /// Using SSL or other encryption method.
        /// </summary>
        public bool IsSecured { get; internal set; }


        // returns the SSL commonName of remote Certificate
        public string SSLCommonName { get; internal set; }

        /// <summary>
        /// Specify which logger to use.
        /// </summary>
        public ILogWriter LogWriter
        {
            get { return _log; }
            set
            {
                _log = value ?? NullLogWriter.Instance;
                m_parser.LogWriter = _log;
            }
        }

        private Stream m_stream;

        /// <summary>
        /// Gets or sets the network stream.
        /// </summary>
        internal Stream Stream
        {
            get { return m_stream; }
            set { m_stream = value; }
        }

        /// <summary>
        /// Gets or sets IP address that the client connected from.
        /// </summary>
        internal string RemoteAddress { get; set; }

        /// <summary>
        /// Gets or sets port that the client connected from.
        /// </summary>
        internal string RemotePort { get; set; }

        /// <summary>
        /// Disconnect from client
        /// </summary>
        /// <param name="error">error to report in the <see cref="Disconnected"/> event.</param>
        public void Disconnect(SocketError error)
        {
            // disconnect may not throw any exceptions
            try
            {
                try
                {
                    if (m_stream != null)
                    {
                        if (error == SocketError.Success)
                            m_stream.Flush();
                        m_stream.Close();
                        m_stream = null;
                    }
                    m_sock = null;
                }
                catch { }

                Disconnected?.Invoke(this, new DisconnectedEventArgs(error));
            }
            catch (Exception err)
            {
                LogWriter.Write(this, LogPrio.Error, "Disconnect threw an exception: " + err);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                int bytesRead = 0;
                if (m_stream == null)
                    return;
                try
                {
                    bytesRead = m_stream.EndRead(ar);
                }
                catch (NullReferenceException)
                {
                    Disconnect(SocketError.ConnectionReset);
                    return;
                }

                if (bytesRead == 0)
                {
                    Disconnect(SocketError.ConnectionReset);
                    return;
                }

                if(m_maxRequests <= 0)
                    return;

                m_ReceiveBytesLeft += bytesRead;
                if (m_ReceiveBytesLeft > m_ReceiveBuffer.Length)
                {
                    throw new BadRequestException("HTTP header Too large: " + m_ReceiveBytesLeft);
                }

                int offset = m_parser.Parse(m_ReceiveBuffer, 0, m_ReceiveBytesLeft);
                if (m_stream == null)
                    return; // "Connection: Close" in effect.

                // try again to see if we can parse another message (check parser to see if it is looking for a new message)
                int nextOffset;
                int nextBytesleft = m_ReceiveBytesLeft - offset;

                while (offset != 0 && nextBytesleft > 0)
                {
                    nextOffset = m_parser.Parse(m_ReceiveBuffer, offset, nextBytesleft);

                    if (m_stream == null)
                        return; // "Connection: Close" in effect.

                    if (nextOffset == 0)
                        break;

                    offset = nextOffset;
                    nextBytesleft = m_ReceiveBytesLeft - offset;
                }

                // copy unused bytes to the beginning of the array
                if (offset > 0 && m_ReceiveBytesLeft > offset)
                    Buffer.BlockCopy(m_ReceiveBuffer, offset, m_ReceiveBuffer, 0, m_ReceiveBytesLeft - offset);

                m_ReceiveBytesLeft -= offset;
                if (m_stream != null && m_stream.CanRead)
                {
                    if (!StreamPassedOff)
                        Stream.BeginRead(m_ReceiveBuffer, m_ReceiveBytesLeft, m_ReceiveBuffer.Length - m_ReceiveBytesLeft, OnReceive, null);
                    else
                    {
                        _log.Write(this, LogPrio.Warning, "Could not read any more from the socket.");
                        Disconnect(SocketError.Success);
                    }
                }
            }
            catch (BadRequestException err)
            {
                LogWriter.Write(this, LogPrio.Warning, "Bad request, responding with it. Error: " + err);
                try
                {
                    Respond("HTTP/1.0", HttpStatusCode.BadRequest, err.Message);
                }
                catch (Exception err2)
                {
                    LogWriter.Write(this, LogPrio.Fatal, "Failed to reply to a bad request. " + err2);
                }
                Disconnect(SocketError.NoRecovery);
            }
            catch (IOException err)
            {
                LogWriter.Write(this, LogPrio.Debug, "Failed to end receive: " + err.Message);
                if (err.InnerException is SocketException)
                    Disconnect((SocketError)((SocketException)err.InnerException).ErrorCode);
                else
                    Disconnect(SocketError.ConnectionReset);
            }
            catch (ObjectDisposedException err)
            {
                LogWriter.Write(this, LogPrio.Debug, "Failed to end receive : " + err.Message);
                Disconnect(SocketError.NotSocket);
            }
            catch (NullReferenceException err)
            {
                LogWriter.Write(this, LogPrio.Debug, "Failed to end receive : NullRef: " + err.Message);
                Disconnect(SocketError.NoRecovery);
            }
            catch (Exception err)
            {
                LogWriter.Write(this, LogPrio.Debug, "Failed to end receive: " + err.Message);
                Disconnect(SocketError.NoRecovery);
            }
        }

        private async void ReceiveLoop()
        {
            m_ReceiveBytesLeft = 0;
            try
            {
                while(true)
                {
                    if (m_stream == null || !m_stream.CanRead)
                        return;

                    int bytesRead = await m_stream.ReadAsync(m_ReceiveBuffer, m_ReceiveBytesLeft, m_ReceiveBuffer.Length - m_ReceiveBytesLeft).ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        Disconnect(SocketError.ConnectionReset);
                        return;
                    }

                    m_ReceiveBytesLeft += bytesRead;
                    if (m_ReceiveBytesLeft > m_ReceiveBuffer.Length)
                        throw new BadRequestException("HTTP header Too large: " + m_ReceiveBytesLeft);

                    int offset = m_parser.Parse(m_ReceiveBuffer, 0, m_ReceiveBytesLeft);
                    if (m_stream == null)
                        return; // "Connection: Close" in effect.

                    // try again to see if we can parse another message (check parser to see if it is looking for a new message)
                    int nextOffset;
                    int nextBytesleft = m_ReceiveBytesLeft - offset;

                    while (offset != 0 && nextBytesleft > 0)
                    {
                        nextOffset = m_parser.Parse(m_ReceiveBuffer, offset, nextBytesleft);

                        if (m_stream == null)
                            return; // "Connection: Close" in effect.

                        if (nextOffset == 0)
                            break;

                        offset = nextOffset;
                        nextBytesleft = m_ReceiveBytesLeft - offset;
                    }

                    // copy unused bytes to the beginning of the array
                    if (offset > 0 && m_ReceiveBytesLeft > offset)
                        Buffer.BlockCopy(m_ReceiveBuffer, offset, m_ReceiveBuffer, 0, m_ReceiveBytesLeft - offset);

                    m_ReceiveBytesLeft -= offset;
                    if (StreamPassedOff)
                        return; //?
                }
            }
            catch (BadRequestException err)
            {
                LogWriter.Write(this, LogPrio.Warning, "Bad request, responding with it. Error: " + err);
                try
                {
                    Respond("HTTP/1.0", HttpStatusCode.BadRequest, err.Message);
                }
                catch (Exception err2)
                {
                    LogWriter.Write(this, LogPrio.Fatal, "Failed to reply to a bad request. " + err2);
                }
                Disconnect(SocketError.NoRecovery);
            }
            catch (IOException err)
            {
                LogWriter.Write(this, LogPrio.Debug, "Failed to end receive: " + err.Message);
                if (err.InnerException is SocketException)
                    Disconnect((SocketError)((SocketException)err.InnerException).ErrorCode);
                else
                    Disconnect(SocketError.ConnectionReset);
            }
            catch (ObjectDisposedException err)
            {
                LogWriter.Write(this, LogPrio.Debug, "Failed to end receive : " + err.Message);
                Disconnect(SocketError.NotSocket);
            }
            catch (NullReferenceException err)
            {
                LogWriter.Write(this, LogPrio.Debug, "Failed to end receive : NullRef: " + err.Message);
                Disconnect(SocketError.NoRecovery);
            }
            catch (Exception err)
            {
                LogWriter.Write(this, LogPrio.Debug, "Failed to end receive: " + err.Message);
                Disconnect(SocketError.NoRecovery);
            }
        }

        private void OnRequestCompleted(object source, EventArgs args)
        {
            TriggerKeepalive = false;
            MonitorKeepaliveStartMS = 0;
            FullRequestReceived = true;
            LastActivityTimeMS = ContextTimeoutManager.EnvironmentTickCount();

            if (m_maxRequests == 0)
                return;

            if(--m_maxRequests == 0)
                m_currentRequest.Connection = ConnectionType.Close;

            // load cookies if they exist
            if(m_currentRequest.Headers["cookie"] != null)
                m_currentRequest.SetCookies(new RequestCookies(m_currentRequest.Headers["cookie"]));

            m_currentRequest.Body.Seek(0, SeekOrigin.Begin);

            int nreqs;
            lock (requestsInServiceIDs)
            {
                nreqs = requestsInServiceIDs.Count;
                requestsInServiceIDs.Add(m_currentRequest.ID);
            }

            // for now pipeline requests need to be serialized by opensim
            RequestReceived?.Invoke(this, new RequestEventArgs(m_currentRequest));

            m_currentRequest = new HttpRequest(this);

            int nreqsnow;
            lock (requestsInServiceIDs)
            {
                nreqsnow = requestsInServiceIDs.Count;
            }
            if (nreqs != nreqsnow)
            {
                // request was not done by us
            }
        }

        public void StartSendResponse(HttpResponse response)
        {
            LastActivityTimeMS = ContextTimeoutManager.EnvironmentTickCount();
            isSendingResponse = true;
            m_currentResponse = response;
            ContextTimeoutManager.EnqueueSend(this, response.Priority);
        }

        public bool TrySendResponse(int bytesLimit)
        {
            if(m_currentResponse == null)
                return false;
            if (m_currentResponse.Sent)
                return false;

            if(!CanSend())
                return false;
 
            m_currentResponse?.SendNextAsync(bytesLimit);
            return false;
        }

        public void ContinueSendResponse(bool notThrottled)
        {
            if(m_currentResponse == null)
                return;
            ContextTimeoutManager.EnqueueSend(this, m_currentResponse.Priority, notThrottled);
        }

        public void EndSendResponse(uint requestID, ConnectionType ctype)
        {
            isSendingResponse = false;
            m_currentResponse?.Clear();
            m_currentResponse = null;

            bool doclose = ctype == ConnectionType.Close;
            lock (requestsInServiceIDs)
            {
                requestsInServiceIDs.Remove(requestID);
                if (requestsInServiceIDs.Count > 1)
                {
                }
            }

            if (doclose)
                Disconnect(SocketError.Success);
            else
            {
                LastActivityTimeMS = ContextTimeoutManager.EnvironmentTickCount();
                lock (requestsInServiceIDs)
                {
                    if (requestsInServiceIDs.Count == 0)
                        TriggerKeepalive = true;
                }
            }
        }

        /// <summary>
        /// Send a response.
        /// </summary>
        /// <param name="httpVersion">Either <see cref="HttpHelper.HTTP10"/> or <see cref="HttpHelper.HTTP11"/></param>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="reason">reason for the status code.</param>
        /// <param name="body">HTML body contents, can be null or empty.</param>
        /// <param name="contentType">A content type to return the body as, i.e. 'text/html' or 'text/plain', defaults to 'text/html' if null or empty</param>
        /// <exception cref="ArgumentException">If <paramref name="httpVersion"/> is invalid.</exception>
        public void Respond(string httpVersion, HttpStatusCode statusCode, string reason, string body, string contentType)
        {
            LastActivityTimeMS = ContextTimeoutManager.EnvironmentTickCount();

            if (string.IsNullOrEmpty(contentType))
                contentType = "text/html";

            if (string.IsNullOrEmpty(reason))
                reason = statusCode.ToString();

            byte[] buffer;
            if(string.IsNullOrEmpty(body))
                buffer = Encoding.ASCII.GetBytes(httpVersion + " " + (int)statusCode + " " + reason ?? statusCode.ToString() + "\r\n\r\n");
            else
                buffer = Encoding.UTF8.GetBytes(
                        string.Format("{0} {1} {2}\r\nContent-Type: {5}\r\nContent-Length: {3}\r\n\r\n{4}",
                                                  httpVersion, (int)statusCode, reason ?? statusCode.ToString(),
                                                  body.Length, body, contentType));
            Send(buffer);
        }

        /// <summary>
        /// Send a response.
        /// </summary>
        /// <param name="httpVersion">Either <see cref="HttpHelper.HTTP10"/> or <see cref="HttpHelper.HTTP11"/></param>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="reason">reason for the status code.</param>
        public void Respond(string httpVersion, HttpStatusCode statusCode, string reason)
        {

            Respond(httpVersion, statusCode, reason, null, null);
        }

        /// <summary>
        /// send a whole buffer
        /// </summary>
        /// <param name="buffer">buffer to send</param>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Send(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            return Send(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Send data using the stream
        /// </summary>
        /// <param name="buffer">Contains data to send</param>
        /// <param name="offset">Start position in buffer</param>
        /// <param name="size">number of bytes to send</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>

        private object sendLock = new object();

        public bool Send(byte[] buffer, int offset, int size)
        {
            if (m_stream == null || m_sock == null || !m_sock.Connected)
                return false;

            if (offset + size > buffer.Length)
                throw new ArgumentOutOfRangeException("offset", offset, "offset + size is beyond end of buffer.");

            bool ok = true;
            lock (sendLock) // can't have overlaps here
            {
                try
                {
                    m_stream.Write(buffer, offset, size);
                }
                catch
                {
                    ok = false;
                }

                if (!ok && m_stream != null)
                    Disconnect(SocketError.NoRecovery);
                return ok;
            }
        }

        public async Task<bool> SendAsync(byte[] buffer, int offset, int size)
        {
            if (m_stream == null || m_sock == null || !m_sock.Connected)
                return false;

            if (offset + size > buffer.Length)
                throw new ArgumentOutOfRangeException("offset", offset, "offset + size is beyond end of buffer.");

            bool ok = true;
            ContextTimeoutManager.ContextEnterActiveSend();
            try
            {
                await m_stream.WriteAsync(buffer, offset, size).ConfigureAwait(false);
            }
            catch
            {
                ok = false;
            }

            ContextTimeoutManager.ContextLeaveActiveSend();

            if (!ok && m_stream != null)
                Disconnect(SocketError.NoRecovery);
            return ok;
        }

        /// <summary>
        /// The context have been disconnected.
        /// </summary>
        /// <remarks>
        /// Event can be used to clean up a context, or to reuse it.
        /// </remarks>
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        /// <summary>
        /// A request have been received in the context.
        /// </summary>
        public event EventHandler<RequestEventArgs> RequestReceived;

        public HTTPNetworkContext GiveMeTheNetworkStreamIKnowWhatImDoing()
        {
            StreamPassedOff = true;
            m_parser.RequestCompleted -= OnRequestCompleted;
            m_parser.RequestLineReceived -= OnRequestLine;
            m_parser.HeaderReceived -= OnHeaderReceived;
            m_parser.BodyBytesReceived -= OnBodyBytesReceived;
            m_parser.Clear();

            return new HTTPNetworkContext() { Socket = m_sock, Stream = m_stream as NetworkStream };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (contextID >= 0)
            {
                StreamPassedOff = false;
                Cleanup();
            }
        }
    }
}