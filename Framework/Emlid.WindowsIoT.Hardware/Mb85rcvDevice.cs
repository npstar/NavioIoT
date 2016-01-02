﻿using System;
using Windows.Devices.I2c;

namespace Emlid.WindowsIot.Hardware
{
    /// <summary>
    /// Base class for MB85RC#V FRAM (Ferroelectric Random Access Memory) family of chips (hardware devices), connected via I2C.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The MB85RC#V family are FRAM (Ferroelectric Random Access Memory) chips in different configurations,
    /// providing more or less words of memory, using the ferroelectric process and silicon gate CMOS process technologies
    /// for forming the nonvolatile memory cells.
    /// </para>
    /// <para>
    /// Unlike SRAM, the MB85RC#V chips are able to retain data without using a data backup battery.
    /// </para>
    /// <para>
    /// The read/write endurance of the nonvolatile memory cells used for the MB85RC#V has improved to be at
    /// least 10^12 cycles, significantly outperforming other nonvolatile memory products in the number.
    /// </para>
    /// <para>
    /// The MB85RC#V chips do not need a polling sequence after writing to the memory such as the case of Flash
    /// memory or E2PROM.
    /// </para>
    /// <para>
    /// MB85RC04V data sheet: https://www.fujitsu.com/us/Images/MB85RC256V-DS501-00017-3v0-E.pdf
    /// MB85RC256V data sheet: https://www.fujitsu.com/us/Images/MB85RC256V-DS501-00017-3v0-E.pdf
    /// </para>
    /// </remarks>
    public abstract class Mb85rcvDevice : I2cConnectedDevice
    {
        #region Constants

        /// <summary>
        /// The upper 4 bits of the device address byte are a device type code that identifies the device type, and are
        /// fixed at “1010” for the MB85RC#V.
        /// </summary>
        /// <remarks>
        /// Shifted down 1 bit because the <see cref="I2cDevice"/> handles the read/write flag automatically (a.k.a. 7-bit addressing).
        /// </remarks>
        public const int DeviceTypeCode = 0x90 >> 1;

        /// <summary>
        /// Bit mask for the <see cref="DeviceTypeCode"/>.
        /// </summary>
        /// <remarks>
        /// Shifted down 1 bit because the <see cref="I2cDevice"/> handles the read/write flag automatically (a.k.a. 7-bit addressing).
        /// </remarks>
        public const int DeviceTypeMask = 0xf0 >> 1;

        /// <summary>
        /// Observed maximum transfer size for I2C requests on Windows IoT or Raspberry Pi 2 (not confirmed).
        /// See: https://social.msdn.microsoft.com/Forums/en-US/e938900f-b732-41dc-95f6-058a39dac31d/i2c-transfer-limit-of-16384-bytes-on-raspberry-pi-2?forum=WindowsIoT
        /// </summary>
        public const int MaximumTransferSize = 16384;

        #endregion

        #region Lifetime

        /// <summary>
        /// Creates an instance at the specified I2C <paramref name="address"/> with custom settings.
        /// </summary>
        /// <param name="address">
        /// I2C slave address of the chip.
        /// This is a physical property, not a software option.
        /// </param>
        /// <param name="fast">
        /// Set true for I2C <see cref="I2cBusSpeed.FastMode"/> or false for <see cref="I2cBusSpeed.StandardMode"/>.
        /// </param>
        /// <param name="exclusive">
        /// Set true for I2C <see cref="I2cSharingMode.Exclusive"/> or false for <see cref="I2cSharingMode.Shared"/>.
        /// </param>
        /// <param name="size">Memory size in bytes.</param>
        protected Mb85rcvDevice(int address, bool fast, bool exclusive, int size)
            : base(address, fast, exclusive)
        {
            // Initialize members
            Size = size;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Size of memory in bytes.
        /// </summary>
        public int Size { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads a single byte at the "current address" (next byte after the last operation).
        /// </summary>
        public byte ReadByte()
        {
            // Get correct I2C device for memory address and flags
            var device = GetDevice(0);

            // Read and return data
            var buffer = new byte[1];
            device.Read(buffer);
            return buffer[0];
        }

        /// <summary>
        /// Reads a single byte at the "current address" (next byte after the last operation).
        /// </summary>
        public byte[] ReadPage(int length)
        {
            // Validate
            if (length < 0 || length > Size) throw new ArgumentOutOfRangeException(nameof(length));

            // Get correct I2C device for memory address and flags
            var device = GetDevice(0);

            // Read data with chunking when transfer size exceeds limit
            var resultBuffer = new byte[length];
            var remaining = length; var offset = 0;
            do
            {
                // Check transfer size and reduce when necessary
                var transferSize = remaining;
                if (transferSize > MaximumTransferSize)
                    transferSize = MaximumTransferSize;

                // Read data
                var buffer = new byte[transferSize];
                device.Read(buffer);
                Array.ConstrainedCopy(buffer, 0, resultBuffer, offset, transferSize);

                // Next transfer when necessary...
                remaining -= transferSize;
                offset += transferSize;
            }
            while (remaining > 0);
            return resultBuffer;
        }

        /// <summary>
        /// Reads a single byte "randomly" at the specified address.
        /// </summary>
        public byte ReadByte(int address)
        {
            // Validate
            if (address < 0 || address > Size - 1) throw new ArgumentOutOfRangeException(nameof(address));

            // Get correct I2C device and data for memory address and flags
            var device = GetDevice(address);
            var addressBytes = GetMemoryAddressBytes(address);

            // Read and return data
            var buffer = new byte[1];
            device.WriteRead(addressBytes, buffer);
            return buffer[0];
        }

        /// <summary>
        /// Reads a "page" of bytes "sequentially" starting at the specified address.
        /// </summary>
        public byte[] ReadPage(int address, int length)
        {
            // Validate
            if (address < 0 || address > Size - 1) throw new ArgumentOutOfRangeException(nameof(address));
            if (length < 0 || length > Size - address) throw new ArgumentOutOfRangeException(nameof(length));

            // Get correct I2C device and data for memory address and flags
            var device = GetDevice(address);

            // Read data with chunking when transfer size exceeds limit
            var resultBuffer = new byte[length];
            var remaining = length; var offset = 0;
            do
            {
                var addressBytes = GetMemoryAddressBytes(address);
                var addressBytesLength = addressBytes.Length;

                // Check transfer size and reduce when necessary
                var transferSize = remaining;
                if (transferSize > MaximumTransferSize)
                    transferSize = MaximumTransferSize;

                // Read data
                var buffer = new byte[transferSize];
                device.WriteRead(addressBytes, buffer);
                Array.ConstrainedCopy(buffer, 0, resultBuffer, offset, transferSize);

                // Next transfer when necessary...
                remaining -= transferSize;
                offset += transferSize;
                address += transferSize;
            }
            while (remaining > 0);
            return resultBuffer;
        }

        /// <summary>
        /// Writes a single byte at the specified address.
        /// </summary>
        public void WriteByte(int address, byte data)
        {
            // Validate
            if (address < 0 || address > Size - 1) throw new ArgumentOutOfRangeException(nameof(address));

            // Get correct I2C device and data for memory address and flags
            var device = GetDevice(address);
            var addressBytes = GetMemoryAddressBytes(address);

            // Write data
            var addressBytesLength = addressBytes.Length;
            var buffer = new byte[addressBytesLength + 1];
            Array.Copy(addressBytes, buffer, addressBytesLength);
            buffer[addressBytesLength] = data;
            device.Write(buffer);
        }

        /// <summary>
        /// Writes a "page" of multiple bytes starting at the specified address.
        /// </summary>
        public void WritePage(int address, byte[] data)
        {
            // Call overloaded method
            WritePage(address, data, 0, data.Length);
        }

        /// <summary>
        /// Writes a "page" of multiple bytes starting at the specified address.
        /// </summary>
        public void WritePage(int address, byte[] data, int offset, int length)
        {
            // Validate
            if (address < 0 || address > Size - 1) throw new ArgumentOutOfRangeException(nameof(address));
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset >= data.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 1 || offset + length > data.Length || offset + length > Size - address)
                throw new ArgumentOutOfRangeException(nameof(data));

            // Get correct I2C device and data for memory address and flags
            var device = GetDevice(address);

            // Write data with chunking when transfer size exceeds limit
            var remaining = length;
            do
            {
                var addressBytes = GetMemoryAddressBytes(address);
                var addressBytesLength = addressBytes.Length;

                // Check transfer size and reduce when necessary
                var transferSize = addressBytesLength + remaining;
                if (transferSize > MaximumTransferSize)
                    transferSize = MaximumTransferSize;
                var transferDataSize = transferSize - addressBytesLength;

                // Write data
                var buffer = new byte[transferSize];
                Array.Copy(addressBytes, buffer, addressBytesLength);
                Array.ConstrainedCopy(data, offset, buffer, addressBytesLength, transferDataSize);
                device.Write(buffer);

                // Next transfer when necessary...
                remaining -= transferDataSize;
                offset += transferDataSize;
                address += transferDataSize;
            }
            while (remaining > 0);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets the <see cref="I2cDevice"/> required to communicate with a specific 
        /// memory address or access method.
        /// </summary>
        /// <param name="address">Memory address. Leave zero when performing commands which operate on the current address.</param>
        /// <remarks>
        /// Because the chip mixes sub-address bits and flags with the slave address and the Windows
        /// <see cref="I2cDevice.ConnectionSettings"/> cannot be modified after creation, it is
        /// necessary to use multiple <see cref="I2cDevice"/> instances for each.
        /// <para>
        /// This base class implementation is the least complex like newer/larger chips, only
        /// requiring different I2C addresses for the read/write flag (no memory address bits).
        /// Older chips should override this implementation to index into the additional
        /// I2C devices they need to access all memory addresses.
        /// </para>
        /// </remarks>
        [CLSCompliant(false)]
        public virtual I2cDevice GetDevice(int address)
        {
            return Hardware;
        }

        /// <summary>
        /// Gets the I2C command memory address bytes for the specified logical address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns>
        /// Byte array which can be written to request the specified memory address,
        /// assuming the correct I2C device is being used as provided by <see cref="GetDevice(int)"/>
        /// which may include the MSB in it's I2C address.
        /// </returns>
        /// <remarks>
        /// Besides being split into bytes, some older/smaller chips separate the MSB
        /// into the I2C device address.
        /// </remarks>
        public abstract byte[] GetMemoryAddressBytes(int address);

        #endregion
    }
}
