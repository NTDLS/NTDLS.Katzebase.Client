﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Management;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.ReliableMessaging;
using System.Diagnostics;

namespace NTDLS.Katzebase.Client
{
    public class KbClient : IDisposable
    {
        public delegate void ConnectedEvent(KbClient sender, KbSessionInfo sessionInfo);
        public event ConnectedEvent? OnConnected;

        public delegate void DisconnectedEvent(KbClient sender, KbSessionInfo sessionInfo);
        public event DisconnectedEvent? OnDisconnected;

        public delegate void CommunicationExceptionEvent(KbClient sender, KbSessionInfo sessionInfo, Exception ex);
        public event CommunicationExceptionEvent? OnCommunicationException;

        private TimeSpan _queryTimeout = TimeSpan.FromSeconds(30);

        public TimeSpan QueryTimeout
        {
            get { return _queryTimeout; }
            set
            {
                _queryTimeout = value;
                if (Connection?.IsConnected == true)
                {
                    Connection.QueryTimeout = _queryTimeout;
                }
            }
        }

        internal RmClient? Connection { get; private set; }

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string ClientName { get; private set; }
        public ulong ProcessId { get; set; }
        public Guid ServerConnectionId { get; private set; }

        public KbDocumentClient Document { get; private set; }
        public KbSchemaClient Schema { get; private set; }
        public KbServerClient Server { get; private set; }
        public KbTransactionClient Transaction { get; private set; }
        public KbQueryClient Query { get; private set; }
        public KbProcedureClient Procedure { get; private set; }

        public KbClient()
        {
            ClientName = Process.GetCurrentProcess().ProcessName;

            Document = new KbDocumentClient(this);
            Schema = new KbSchemaClient(this);
            Server = new KbServerClient(this);
            Transaction = new KbTransactionClient(this);
            Query = new KbQueryClient(this);
            Procedure = new KbProcedureClient(this);
        }

        public KbClient(string hostName, int serverPort, string clientName = "")
        {
            ClientName = clientName;

            Document = new KbDocumentClient(this);
            Schema = new KbSchemaClient(this);
            Server = new KbServerClient(this);
            Transaction = new KbTransactionClient(this);
            Query = new KbQueryClient(this);
            Procedure = new KbProcedureClient(this);

            Connect(hostName, serverPort, clientName);
        }

        public void Connect(string hostname, int port, string clientName = "")
        {
            Host = hostname;
            Port = port;
            ClientName = clientName;

            if (string.IsNullOrWhiteSpace(ClientName))
            {
                ClientName = Process.GetCurrentProcess().ProcessName;
            }

            if (Connection?.IsConnected == true)
            {
                throw new KbGenericException("The client is already connected.");
            }

            try
            {
                Connection = new RmClient(new RmConfiguration
                {
                    QueryTimeout = _queryTimeout
                });

                Connection.OnException += (RmContext? context, Exception ex, IRmPayload? payload) =>
                {
                    var sessionInfo = new KbSessionInfo
                    {
                        ConnectionId = ServerConnectionId,
                        ProcessId = ProcessId
                    };

                    OnCommunicationException?.Invoke(this, sessionInfo, ex);
                };

                Connection.OnDisconnected += (RmContext context) =>
                {
                    var sessionInfo = new KbSessionInfo
                    {
                        ConnectionId = ServerConnectionId,
                        ProcessId = ProcessId
                    };

                    OnDisconnected?.Invoke(this, sessionInfo);

                    Connection = null;
                    ServerConnectionId = Guid.Empty;
                    ProcessId = 0;
                };

                Connection.Connect(hostname, port);

                var reply = Server.StartSession();
                ServerConnectionId = reply.ConnectionId;
                ProcessId = reply.ProcessId;

                var sessionInfo = new KbSessionInfo
                {
                    ConnectionId = ServerConnectionId,
                    ProcessId = ProcessId
                };
                OnConnected?.Invoke(this, sessionInfo);
            }
            catch
            {
                Connection = null;
                ServerConnectionId = Guid.Empty;
                throw;
            }
        }

        void Disconnect()
        {
            try
            {
                try
                {
                    if (Connection?.IsConnected == true)
                    {
                        Server.CloseSession();
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    Connection?.Disconnect();
                    Connection = null;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                Connection = null;
                ServerConnectionId = Guid.Empty;
                ProcessId = 0;
            }
        }

        internal T ValidateTaskResult<T>(Task<T> task)
        {
            if (task.IsCompletedSuccessfully == false)
            {
                throw new KbAPIResponseException(task.Exception?.GetRoot()?.Message ?? "Unspecified api error has occurred.");
            }
            return task.Result;
        }

        #region IDisposable.

        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                Disconnect();
            }

            disposed = true;

        }

        #endregion
    }
}
