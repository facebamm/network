﻿#region License

// Copyright (c) 2018-2019, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using System.Net;
using System.Net.Sockets;
using Exomia.Network.Encoding;
using Exomia.Network.Native;

namespace Exomia.Network.TCP
{
    /// <summary>
    ///     A TCP server base.
    /// </summary>
    /// <typeparam name="TServerClient"> Type of the server client. </typeparam>
    public abstract class TcpServerBase<TServerClient> : ServerBase<Socket, TServerClient>
        where TServerClient : ServerClientBase<Socket>
    {
        /// <summary>
        ///     Size of the payload.
        /// </summary>
        private protected readonly ushort _payloadSize;

        /// <summary>
        ///     Size of the maximum payload.
        /// </summary>
        private readonly ushort _maxPayloadSize;

        /// <inheritdoc />
        private protected override ushort MaxPayloadSize
        {
            get { return _maxPayloadSize; }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpServerBase{TServerClient}" /> class.
        /// </summary>
        /// <param name="expectedMaxPayloadSize"> (Optional) Size of the expected maximum payload. </param>
        private protected TcpServerBase(ushort expectedMaxPayloadSize = Constants.TCP_PAYLOAD_SIZE_MAX)
        {
            _maxPayloadSize = expectedMaxPayloadSize > 0 && expectedMaxPayloadSize < Constants.TCP_PAYLOAD_SIZE_MAX
                ? expectedMaxPayloadSize
                : Constants.TCP_PAYLOAD_SIZE_MAX;
            _payloadSize = (ushort)(PayloadEncoding.EncodedPayloadLength(_maxPayloadSize) + 1);
        }

        /// <inheritdoc />
        private protected override bool OnRun(int port, out Socket listener)
        {
            try
            {
                if (Socket.OSSupportsIPv6)
                {
                    listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true, Blocking = false, DualMode = true
                    };
                    listener.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                }
                else
                {
                    listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true, Blocking = false
                    };
                    listener.Bind(new IPEndPoint(IPAddress.Any, port));
                }
                listener.Listen(100);
                return true;
            }
            catch
            {
                listener = null;
                return false;
            }
        }

        /// <inheritdoc />
        private protected override void OnAfterClientDisconnect(TServerClient client)
        {
            try
            {
                client.Arg0?.Shutdown(SocketShutdown.Both);
                client.Arg0?.Close(CLOSE_TIMEOUT);
            }
            catch
            {
                /* IGNORE */
            }
        }

        /// <summary>
        ///     Receives.
        /// </summary>
        /// <param name="socket">           The socket. </param>
        /// <param name="buffer">           The buffer. </param>
        /// <param name="bytesTransferred"> The bytes transferred. </param>
        /// <param name="state">            The state. </param>
        private protected unsafe void Receive(Socket                  socket,
                                              byte[]                  buffer,
                                              int                     bytesTransferred,
                                              ServerClientStateObject state)
        {
            DeserializePacketInfo deserializePacketInfo;
            int                   size = state.CircularBuffer.Write(buffer, 0, bytesTransferred);
            while (state.CircularBuffer.PeekHeader(
                       0, out byte packetHeader, out deserializePacketInfo.CommandID,
                       out deserializePacketInfo.Length, out ushort checksum)
                && deserializePacketInfo.Length <= state.CircularBuffer.Count - Constants.TCP_HEADER_SIZE)
            {
                if (state.CircularBuffer.PeekByte(
                        (Constants.TCP_HEADER_SIZE + deserializePacketInfo.Length) - 1, out byte b) &&
                    b == Constants.ZERO_BYTE)
                {
                    fixed (byte* ptr = state.BufferRead)
                    {
                        state.CircularBuffer.Read(ptr, deserializePacketInfo.Length, Constants.TCP_HEADER_SIZE);
                        if (size < bytesTransferred)
                        {
                            state.CircularBuffer.Write(buffer, size, bytesTransferred - size);
                        }
                    }

                    if (Serialization.Serialization.DeserializeTcp(
                        packetHeader, checksum, state.BufferRead, state.BigDataHandler,
                        out deserializePacketInfo.Data, ref deserializePacketInfo.Length,
                        out deserializePacketInfo.ResponseID))
                    {
                        DeserializeData(socket, in deserializePacketInfo);
                    }

                    continue;
                }
                bool skipped = state.CircularBuffer.SkipUntil(Constants.TCP_HEADER_SIZE, Constants.ZERO_BYTE);
                if (size < bytesTransferred)
                {
                    size += state.CircularBuffer.Write(buffer, size, bytesTransferred - size);
                }
                if (!skipped && !state.CircularBuffer.SkipUntil(0, Constants.ZERO_BYTE)) { break; }
            }
        }

        /// <summary>
        ///     A server client state object. This class cannot be inherited.
        /// </summary>
        private protected class ServerClientStateObject
        {
            /// <summary>
            ///     The buffer read.
            /// </summary>
            public byte[] BufferRead;

            /// <summary>
            ///     Buffer for circular data.
            /// </summary>
            public CircularBuffer CircularBuffer;

            /// <summary>
            ///     The big data handler.
            /// </summary>
            public BigDataHandler BigDataHandler;
        }
    }
}