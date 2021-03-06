﻿using Emlid.WindowsIot.Hardware.Boards.Navio;
using Emlid.WindowsIot.Hardware.Protocols.Ppm;
using System;

namespace Emlid.WindowsIot.Tools.NavioHardwareTest.Models.Tests
{
    /// <summary>
    /// UI model for testing the <see cref="INavioRCInputDevice"/>.
    /// </summary>
    public sealed class RCInputTestUIModel : TestUIModel
    {
        #region Lifetime

        /// <summary>
        /// Creates an instance.
        /// </summary>
        public RCInputTestUIModel(TestApplicationUIModel application) : base(application)
        {
            // Initialize device
            Device = Application.Board.RCInput;
            Device.ChannelsChanged += OnChannelsChanged;
        }

        #region IDisposable

        /// <summary>
        /// Frees resources owned by this instance.
        /// </summary>
        /// <param name="disposing">
        /// True when called via <see cref="IDisposable.Dispose()"/>, false when called during finalization.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                // Dispose resources when possible
                if (disposing)
                {
                    // Unhook events
                    Device.ChannelsChanged -= OnChannelsChanged;
                }
            }
            finally
            {
                // Dispose base class
                base.Dispose(disposing);
            }
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Device.
        /// </summary>
        public INavioRCInputDevice Device { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Updates the display when the <see cref="Device"/> channels change.
        /// </summary>
        private void OnChannelsChanged(object sender, PpmFrame frame)
        {
            // Dump statistics to output
            WriteOutput(frame.ToString());

            // Update display
            DoPropertyChanged(nameof(Device));
        }

        #endregion
    }
}
