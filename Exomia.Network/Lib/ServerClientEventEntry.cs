﻿#region License

// Copyright (c) 2018-2019, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

namespace Exomia.Network.Lib
{
    /// <summary>
    ///     A server client event entry. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">             Generic type parameter. </typeparam>
    /// <typeparam name="TServerClient"> Type of the server client. </typeparam>
    sealed class ServerClientEventEntry<T, TServerClient>
        where T : class
        where TServerClient : ServerClientBase<T>
    {
        /// <summary>
        ///     The deserialize.
        /// </summary>
        internal readonly DeserializePacketHandler<object> _deserialize;

        /// <summary>
        ///     The data received.
        /// </summary>
        private readonly Event<ClientDataReceivedHandler<T, TServerClient>> _dataReceived;

        /// <summary>
        ///     Initializes a new instance of the &lt;see cref="ServerClientEventEntry&lt;T,
        ///     TServerClient&gt;"/&gt; class.
        /// </summary>
        /// <param name="deserialize"> The deserialize. </param>
        public ServerClientEventEntry(DeserializePacketHandler<object> deserialize)
        {
            _dataReceived = new Event<ClientDataReceivedHandler<T, TServerClient>>();
            _deserialize  = deserialize;
        }

        /// <summary>
        ///     Adds callback.
        /// </summary>
        /// <param name="callback"> The callback to remove. </param>
        public void Add(ClientDataReceivedHandler<T, TServerClient> callback)
        {
            _dataReceived.Add(callback);
        }

        /// <summary>
        ///     Removes the given callback.
        /// </summary>
        /// <param name="callback"> The callback to remove. </param>
        public void Remove(ClientDataReceivedHandler<T, TServerClient> callback)
        {
            _dataReceived.Remove(callback);
        }

        /// <summary>
        ///     Raises the event entries.
        /// </summary>
        /// <param name="server">     The server. </param>
        /// <param name="arg0">       The argument 0. </param>
        /// <param name="data">       The data. </param>
        /// <param name="responseid"> The responseid. </param>
        /// <param name="client">     The client. </param>
        public void Raise(ServerBase<T, TServerClient> server, T arg0, object data, uint responseid,
                          TServerClient                client)
        {
            for (int i = _dataReceived.Count - 1; i >= 0; --i)
            {
                if (!_dataReceived[i].Invoke(server, arg0, data, responseid, client))
                {
                    _dataReceived.Remove(i);
                }
            }
        }
    }
}