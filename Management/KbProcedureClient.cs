﻿using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Payloads.RoundTrip;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbProcedureClient
    {
        private readonly KbClient _client;

        public KbIndexesClient Indexes { get; set; }

        public KbProcedureClient(KbClient client)
        {
            _client = client;
            Indexes = new KbIndexesClient(client);
        }

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        /// <param name="procedure"></param>
        /// <returns></returns>
        /// <exception cref="KbAPIResponseException"></exception>
        public KbQueryProcedureExecuteReply Execute(KbProcedure procedure, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryProcedureExecute(_client.ServerConnectionId, procedure), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }
    }
}
