//*********************************************************************************************************************
// File Name:      USBDeviceNotification.cs
// Description:    Implementation of the USB device notification system
//
// Copyright (C) 2023 Mike Pullen. All Rights Reserved.
// Confidential and Proprietary
//
// Revision History: 
//====================================================================================================================
// 2023/12/03 - Mike Pullen - Original implementation.
// 2026/09/04 - Mike Pullen - Released the notification filter buffer, reported registration failures and hardened
//                            the unregister path.
//*********************************************************************************************************************
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DeviceInterfaces
{
    public static class USBDeviceNotification
    {
        #region Externals

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, int Flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr Handle);

        #endregion
        #region Type definitions

        /// <summary>
        /// Structure for filling the notificationFilter parameter buffer for RegisterDeviceNotification
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NotificationFilterStr
        {
            internal int iSize;
            internal int iDeviceType;
            internal int iReserved;
            internal Guid classGuid;
            internal short iName;
        }

        #endregion
        #region Methods

        /// <summary>
        /// Registers a window to receive notifications when USB devices are connected or removed
        /// </summary>
        /// <param name="pWindowHandle">IN - Handle to the window to be notified</param>
        /// <returns>The notification handle, which must be passed to UnregisterUsbDeviceNotification</returns>
        /// <exception cref="Win32Exception">Thrown when the window could not be registered for device notifications</exception>
        public static IntPtr RegisterUsbDeviceNotification(IntPtr pWindowHandle)
        {
            // Populate the notification filter information
            NotificationFilterStr notificationFilter = new NotificationFilterStr
            {
                iDeviceType = m_iINTERFACE_TYPE,
                iReserved = 0,
                classGuid = m_USB_GUID,
                iName = 0
            };

            // Allocate the buffer to marshal the structure into
            notificationFilter.iSize = Marshal.SizeOf(notificationFilter);
            IntPtr pNotificationFilterBuffer = Marshal.AllocHGlobal(notificationFilter.iSize);

            try
            {
                // Marshal the structure into the buffer. The buffer holds uninitialized memory, so there is no
                // previously marshalled structure for StructureToPtr to release.
                Marshal.StructureToPtr(notificationFilter, pNotificationFilterBuffer, false);

                // Register the window for USB device events
                IntPtr pNotificationHandle = RegisterDeviceNotification(pWindowHandle, pNotificationFilterBuffer, m_iDEVICE_NOTIFY_WINDOW_HANDLE);
                if (pNotificationHandle == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                // Return the notification handle
                return pNotificationHandle;
            }
            finally
            {
                // Windows copies the filter during registration, so the buffer is no longer needed
                Marshal.FreeHGlobal(pNotificationFilterBuffer);
            }
        }

        /// <summary>
        /// Unregisters the window for USB device notifications
        /// </summary>
        /// <param name="pNotificationHandle">IN - Notification handle returned by RegisterUsbDeviceNotification</param>
        public static void UnregisterUsbDeviceNotification(IntPtr pNotificationHandle)
        {
            // Ignore a handle that was never registered
            if (pNotificationHandle == IntPtr.Zero)
            {
                return;
            }

            // Unregister the window using the specified notification handle
            UnregisterDeviceNotification(pNotificationHandle);
        }

        #endregion
        #region Data Members

        // Exposed constants
        public const int iDEVICE_CONNECTED = 0x8000; // Device connected wparam
        public const int iDEVICE_REMOVED = 0x8004; // Device removed wparam
        public const int iWM_DEVICECHANGE = 0x0219; // Device changed event

        // Interface constants
        private const int m_iDEVICE_NOTIFY_WINDOW_HANDLE = 0;
        private const int m_iINTERFACE_TYPE = 5; // DBT_DEVTYP_DEVICEINTERFACE
        private static readonly Guid m_USB_GUID = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");

        #endregion
    }
}
