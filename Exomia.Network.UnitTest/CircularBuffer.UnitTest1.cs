﻿#region MIT License

// Copyright (c) 2018 exomia - Daniel Bätz
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#endregion

using System;
using System.Linq;
using Exomia.Network.Native;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Exomia.Network.UnitTest
{
    [TestClass]
    public unsafe class CircularBufferUnitTest1
    {
        [TestMethod]
        [DataRow(1024)]
        [DataRow(4096)]
        [DataRow(8192)]
        public void InitTest_CircularBuffer_Initialize_With_PowerOfTwo_ShouldPass(int count)
        {
            CircularBuffer t1 = new CircularBuffer(count);
            Assert.IsNotNull(t1);
            Assert.AreEqual(t1.Count, 0);
            Assert.AreEqual(t1.Capacity, count);
            Assert.IsTrue(t1.IsEmpty);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(int.MinValue)]
        [DataRow(0x7FFFFFFF)]
        public void InitTest_CircularBuffer_Initialize_With_InvalidNumbers_ShouldFail(int count)
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () =>
                {
                    CircularBuffer t1 = new CircularBuffer(count);
                });
        }

        [TestMethod]
        public void CircularBuffer_Initialize_With_898_Capacity_ShouldBe_1024()
        {
            CircularBuffer t1 = new CircularBuffer(898);
            Assert.IsNotNull(t1);
            Assert.AreEqual(t1.Count, 0);
            Assert.AreEqual(t1.Capacity, 1024);
            Assert.IsTrue(t1.IsEmpty);
        }

        [TestMethod]
        public void CircularBuffer_Initialize_With_3000_Capacity_ShouldBe_4096()
        {
            CircularBuffer t1 = new CircularBuffer(3000);
            Assert.IsNotNull(t1);
            Assert.AreEqual(t1.Count, 0);
            Assert.AreEqual(t1.Capacity, 4096);
            Assert.IsTrue(t1.IsEmpty);
        }

        [TestMethod]
        public void SafeWriteTest()
        {
            CircularBuffer cb = new CircularBuffer(1024);
            byte[] buffer = { 45, 48, 72, 15 };
            cb.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(cb.Count, 4);

            cb.Write(buffer, 2, 2);

            Assert.AreEqual(cb.Count, 6);

            cb.Write(buffer, 1, 2);

            Assert.AreEqual(cb.Count, 8);
        }

        [TestMethod]
        public void UnsafeWriteTest()
        {
            CircularBuffer cb = new CircularBuffer(1024);
            byte[] buffer = { 45, 48, 72, 15 };
            fixed (byte* src = buffer)
            {
                cb.Write(src, 0, 4);

                Assert.AreEqual(cb.Count, 4);

                cb.Write(src, 2, 2);

                Assert.AreEqual(cb.Count, 6);

                cb.Write(src, 1, 2);

                Assert.AreEqual(cb.Count, 8);
            }
        }

        [TestMethod]
        public void SafeReadTest()
        {
            CircularBuffer cb = new CircularBuffer(1024);

            byte[] buffer = { 45, 48, 72, 15 };
            cb.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(cb.Count, 4);

            byte[] readBuffer = new byte[4];
            cb.Read(readBuffer, 0, readBuffer.Length, 0);

            Assert.AreEqual(cb.Count, 0);
            Assert.IsTrue(cb.IsEmpty);

            Assert.IsTrue(readBuffer.SequenceEqual(buffer));

            Assert.ThrowsException<InvalidOperationException>(
                () =>
                {
                    cb.Read(readBuffer, 0, readBuffer.Length, 0);
                });

            byte[] buffer2 = { 45, 48, 72, 1, 4, 87, 95 };
            cb.Write(buffer2, 0, buffer2.Length);

            byte[] readBuffer2 = new byte[buffer2.Length];
            cb.Read(readBuffer2, 0, buffer2.Length - 2, 2);

            Assert.IsTrue(readBuffer2.Take(buffer2.Length - 2).SequenceEqual(buffer2.Skip(2)));

            Assert.AreEqual(cb.Count, 0);
            Assert.IsTrue(cb.IsEmpty);
        }

        [TestMethod]
        public void UnsafeReadTest()
        {
            CircularBuffer cb = new CircularBuffer(1024);

            byte[] buffer = { 45, 48, 72, 15 };
            cb.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(cb.Count, 4);

            byte[] readBuffer = new byte[4];
            fixed (byte* dest = readBuffer)
            {
                cb.Read(dest, 0, readBuffer.Length, 0);
            }

            Assert.AreEqual(cb.Count, 0);
            Assert.IsTrue(cb.IsEmpty);

            Assert.IsTrue(readBuffer.SequenceEqual(buffer));

            Assert.ThrowsException<InvalidOperationException>(
                () =>
                {
                    fixed (byte* dest = readBuffer)
                    {
                        cb.Read(dest, 0, readBuffer.Length, 0);
                    }
                });

            byte[] buffer2 = { 45, 48, 72, 1, 4, 87, 95 };
            cb.Write(buffer2, 0, buffer2.Length);

            byte[] readBuffer2 = new byte[buffer2.Length];
            fixed (byte* dest = readBuffer2)
            {
                cb.Read(dest, 0, buffer2.Length - 2, 2);
            }

            Assert.IsTrue(readBuffer2.Take(buffer2.Length - 2).SequenceEqual(buffer2.Skip(2)));

            Assert.AreEqual(cb.Count, 0);
            Assert.IsTrue(cb.IsEmpty);
        }

        [TestMethod]
        public void SafeWriteTest_With_Overflow()
        {
            Random rnd = new Random(1337);

            CircularBuffer cb = new CircularBuffer(128);

            byte[] buffer = new byte[77];
            rnd.NextBytes(buffer);

            cb.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(cb.Count, 77);

            cb.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(cb.Count, 128);

            cb.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(cb.Count, 128);
        }

        [TestMethod]
        public void UnsafeWriteTest_With_Overflow()
        {
            Random rnd = new Random(1337);

            CircularBuffer cb = new CircularBuffer(128);

            byte[] buffer = new byte[77];
            rnd.NextBytes(buffer);

            fixed (byte* src = buffer)
            {
                cb.Write(src, 0, buffer.Length);

                Assert.AreEqual(cb.Count, 77);

                cb.Write(src, 0, buffer.Length);

                Assert.AreEqual(cb.Count, 128);

                cb.Write(src, 0, buffer.Length);

                Assert.AreEqual(cb.Count, 128);
            }
        }

        [TestMethod]
        public void SafeReadTest_With_Overflow()
        {
            Random rnd = new Random(1337);

            CircularBuffer cb = new CircularBuffer(16);

            byte[] buffer = new byte[9];
            rnd.NextBytes(buffer);

            cb.Write(buffer, 0, buffer.Length);
            cb.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(cb.Count, 16);

            byte[] readBuffer2 = new byte[16];
            cb.Read(readBuffer2, 0, readBuffer2.Length, 0);

            Assert.AreEqual(cb.Count, 0);
            Assert.IsTrue(cb.IsEmpty);

            Assert.IsTrue(readBuffer2.Take(7).SequenceEqual(buffer.Skip(2)));

            byte[] shouldbe = buffer.Skip(2).Concat(buffer).ToArray();

            Assert.IsTrue(readBuffer2.SequenceEqual(shouldbe));

            cb.Dispose();

            cb = new CircularBuffer(16);
            cb.Write(buffer, 0, buffer.Length);
            cb.Write(buffer, 0, buffer.Length);
            byte[] readBuffer3 = new byte[1];
            cb.Read(readBuffer3, 0, readBuffer3.Length, 15);

            Assert.AreEqual(cb.Count, 0);
            Assert.IsTrue(cb.IsEmpty);

            Assert.IsTrue(readBuffer3.SequenceEqual(buffer.Skip(8)));
        }

        [TestMethod]
        public void UnsafeReadTest_With_Overflow()
        {
            Random rnd = new Random(1337);

            CircularBuffer cb = new CircularBuffer(16);

            byte[] buffer = new byte[9];
            rnd.NextBytes(buffer);

            fixed (byte* src = buffer)
            {
                cb.Write(src, 0, buffer.Length);
                cb.Write(src, 0, buffer.Length);
            }

            Assert.AreEqual(cb.Count, 16);

            byte[] readBuffer = new byte[16];

            fixed (byte* dest = readBuffer)
            {
                cb.Read(dest, 0, readBuffer.Length, 0);

                Assert.AreEqual(cb.Count, 0);
                Assert.IsTrue(cb.IsEmpty);

                Assert.IsTrue(readBuffer.Take(7).SequenceEqual(buffer.Skip(2)));

                byte[] shouldbe = buffer.Skip(2).Concat(buffer).ToArray();

                Assert.IsTrue(readBuffer.SequenceEqual(shouldbe));
            }

            cb.Dispose();

            cb = new CircularBuffer(16);
            cb.Write(buffer, 0, buffer.Length);
            cb.Write(buffer, 0, buffer.Length);
            byte[] readBuffer3 = new byte[1];
            fixed (byte* dest = readBuffer3)
            {
                cb.Read(dest, 0, readBuffer3.Length, 15);
            }
            Assert.AreEqual(cb.Count, 0);
            Assert.IsTrue(cb.IsEmpty);

            Assert.IsTrue(readBuffer3.SequenceEqual(buffer.Skip(8)));
        }

        [TestMethod]
        public void SafePeekTest()
        {
            CircularBuffer cb = new CircularBuffer(1024);

            byte[] buffer = { 45, 48, 72, 15 };
            cb.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(cb.Count, 4);

            byte[] peekBuffer = new byte[4];
            cb.Peek(peekBuffer, 0, peekBuffer.Length, 0);

            Assert.AreEqual(cb.Count, 4);
            Assert.IsFalse(cb.IsEmpty);

            Assert.IsTrue(peekBuffer.SequenceEqual(buffer));

            Assert.ThrowsException<InvalidOperationException>(
                () =>
                {
                    cb.Peek(peekBuffer, 0, 8, 0);
                });

            byte[] buffer2 = { 45, 48, 72, 1, 4, 87, 95 };
            cb.Write(buffer2, 0, buffer2.Length);

            byte[] peekBuffer2 = new byte[buffer2.Length];
            cb.Peek(peekBuffer2, 0, buffer2.Length - 2, 4 + 2);

            Assert.IsTrue(peekBuffer2.Take(buffer2.Length - 2).SequenceEqual(buffer2.Skip(2)));

            Assert.AreEqual(cb.Count, 11);
            Assert.IsFalse(cb.IsEmpty);
        }

        [TestMethod]
        public void UnsafePeekTest()
        {
            CircularBuffer cb = new CircularBuffer(1024);

            byte[] buffer = { 45, 48, 72, 15 };
            cb.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(cb.Count, 4);

            byte[] peekBuffer = new byte[4];

            fixed (byte* dest = peekBuffer)
            {
                cb.Peek(dest, 0, peekBuffer.Length, 0);
            }
            Assert.AreEqual(cb.Count, 4);
            Assert.IsFalse(cb.IsEmpty);

            Assert.IsTrue(peekBuffer.SequenceEqual(buffer));

            Assert.ThrowsException<InvalidOperationException>(
                () =>
                {
                    fixed (byte* dest = peekBuffer)
                    {
                        cb.Peek(dest, 0, 8, 0);
                    }
                });

            byte[] buffer2 = { 45, 48, 72, 1, 4, 87, 95 };
            cb.Write(buffer2, 0, buffer2.Length);

            byte[] peekBuffer2 = new byte[buffer2.Length];

            fixed (byte* dest = peekBuffer2)
            {
                cb.Peek(peekBuffer2, 0, buffer2.Length - 2, 4 + 2);
            }
            Assert.IsTrue(peekBuffer2.Take(buffer2.Length - 2).SequenceEqual(buffer2.Skip(2)));

            Assert.AreEqual(cb.Count, 11);
            Assert.IsFalse(cb.IsEmpty);
        }

        [TestMethod]
        public void PeekByteTest()
        {
            CircularBuffer cb = new CircularBuffer(1024);

            byte[] buffer = { 45, 48, 72, 15 };
            cb.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(cb.PeekByte(0), 45);
            Assert.AreEqual(cb.PeekByte(1), 48);
            Assert.AreEqual(cb.PeekByte(2), 72);
            Assert.AreEqual(cb.PeekByte(3), 15);

            Assert.ThrowsException<InvalidOperationException>(
                () =>
                {
                    return cb.PeekByte(4);
                });
        }

        [TestMethod]
        public void SkipUntilTest()
        {
            CircularBuffer cb = new CircularBuffer(1024);
            Assert.IsFalse(cb.SkipUntil(0));

            byte[] buffer = { 45, 48, 72, 15 };
            cb.Write(buffer, 0, buffer.Length);

            Assert.IsFalse(cb.SkipUntil(0));

            byte[] peekBuffer = new byte[4];
            cb.Peek(peekBuffer, 0, 4, 0);

            Assert.IsTrue(peekBuffer.SequenceEqual(buffer));

            Assert.IsTrue(cb.SkipUntil(48));

            Assert.AreEqual(cb.Count, 2);

            cb.Peek(peekBuffer, 0, 2, 0);

            Assert.IsTrue(peekBuffer.Take(2).SequenceEqual(buffer.Skip(2)));

            Assert.IsFalse(cb.SkipUntil(0));
        }

        [TestMethod]
        public void PeekHeaderTest()
        {
            CircularBuffer cb = new CircularBuffer(16);

            byte[] buffer = { 12, 200, 4, 45, 177, 78, 147 };

            Assert.IsFalse(cb.PeekHeader(0, out byte h, out uint c1, out int d, out ushort c2));
            cb.Write(buffer, 0, buffer.Length); // 7

            Assert.IsTrue(
                cb.PeekHeader(0, out byte packetHeader, out uint commandID, out int dataLength, out ushort checksum));

            Assert.AreEqual(packetHeader, buffer[0]);

            Assert.AreEqual(commandID, (uint)((buffer[4] << 8) | buffer[3]));
            Assert.AreEqual(dataLength, (buffer[2] << 8) | buffer[1]);
            Assert.AreEqual(checksum, (ushort)((buffer[6] << 8) | buffer[5]));

            cb.Write(buffer, 0, buffer.Length); // 14
            cb.Write(buffer, 0, buffer.Length); // 16

            Assert.IsTrue(cb.PeekHeader(2 + 7, out packetHeader, out commandID, out dataLength, out checksum));

            Assert.AreEqual(packetHeader, buffer[0]);

            Assert.AreEqual(commandID, (uint)((buffer[4] << 8) | buffer[3]));
            Assert.AreEqual(dataLength, (buffer[2] << 8) | buffer[1]);
            Assert.AreEqual(checksum, (ushort)((buffer[6] << 8) | buffer[5]));

            cb.Write(buffer, 0, buffer.Length); // 16

            Assert.IsTrue(cb.PeekHeader(2 + 7, out packetHeader, out commandID, out dataLength, out checksum));

            Assert.AreEqual(packetHeader, buffer[0]);

            Assert.AreEqual(commandID, (uint)((buffer[4] << 8) | buffer[3]));
            Assert.AreEqual(dataLength, (buffer[2] << 8) | buffer[1]);
            Assert.AreEqual(checksum, (ushort)((buffer[6] << 8) | buffer[5]));
        }
    }
}