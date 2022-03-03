using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;


namespace BoardWriteServer
{
    class CP2112Device : I2CProgrammer
    {
        public CP2112Device()
        {
            device_id = "CP2112";
        }

        public override bool GPIO_SUPPORT { get => false; protected set => GPIO_SUPPORT = false; }

        public override GPIOStatus PullGPIOStatus()
        {
            throw new NotImplementedException();
        }

        #region Device Init/Close

        IntPtr device = new IntPtr();

        public override bool InitializeDevice()
        {
            try
            {
                int status = HidSmbus_Open(ref device, device_index, 0, 0);
                if (status != HID_SMBUS_SUCCESS) { updateDeviceStatus(status); return false; }
            }
            catch (Exception e)
            {
                BoardLogger.Log("ERROR: Cannot open CP2112 device. The device is likely already open " +
                                               "or no longer exists. Please replug board and try again.");
                return false;
            }

            return true;
        }

        public override void CloseDevice()
        {
            try
            {
                HidSmbus_Close(device);
            }
            catch (Exception e)
            {
                // Show user a message that device cannot be closed for
                // some reason
                BoardLogger.Log("ERROR: CP2112 device cannot be closed. The device likely no longer " +
                                               "exists, was never opened, or has already been closed.");
            }
        }

        #endregion

        #region R/W Configuration Definitions
        public const ushort XGIGA_DEVICE_VID = 0x10C4;
        public const ushort XGIGA_DEVICE_PID = 0xEA90;

        private const ushort TIMEOUT_LENGTH = 0x20;
        private const ushort NUM_RETRIES = 1;
        #endregion

        #region Return Code Definitions

        private const byte HID_SMBUS_READ_TIMED_OUT = 0x12;
        private const byte HID_SMBUS_SUCCESS = 0x00;
        private const byte HID_SMBUS_S0_IDLE = 0x00;
        private const byte HID_SMBUS_S0_BUSY = 0x01;
        private const byte HID_SMBUS_S0_COMPLETE = 0x02;

        #endregion

        #region Exported Library Functions
        /// <summary>
        /// Get the number of connected cp2112 devices
        /// </summary>
        /// <param name="numDevices"></param>
        /// <param name="vid"></param>
        /// <param name="pid"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_GetNumDevices(ref uint numDevices, ushort vid, ushort pid);

        /// <summary>
        /// get the attributes of a connected cp2112 device
        /// </summary>
        /// <param name="deviceNum"></param>
        /// <param name="vid"></param>
        /// <param name="pid"></param>
        /// <param name="deviceVid"></param>
        /// <param name="devicePid"></param>
        /// <param name="deviceReleaseNumber"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_GetAttributes(uint deviceNum, ushort vid, ushort pid, ref ushort deviceVid, ref ushort devicePid, ref ushort deviceReleaseNumber);

        /// <summary>
        /// open communications with a connected cp2112 device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="deviceNum"></param>
        /// <param name="vid"></param>
        /// <param name="pid"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_Open(ref IntPtr device, uint deviceNum, ushort vid, ushort pid);

        /// <summary>
        /// close communications with an open cp2112 device
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_Close(IntPtr device);

        /// <summary>
        /// request a read from a specified address on the the I2C bus
        /// </summary>
        /// <param name="device"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="numBytesToRead"></param>
        /// <param name="targetAddressSize"></param>
        /// <param name="targetAddress"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_AddressReadRequest(IntPtr device, byte slaveAddress, ushort numBytesToRead, byte targetAddressSize, byte[] targetAddress);

        /// <summary>
        /// force the cp2112 device to complete a read request and send a response
        /// </summary>
        /// <param name="device"></param>
        /// <param name="numBytesToRead"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_ForceReadResponse(IntPtr device, ushort numBytesToRead);

        /// <summary>
        /// pull the I2C read data from the device communication bus
        /// </summary>
        /// <param name="device"></param>
        /// <param name="status"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <param name="numBytesRead"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_GetReadResponse(IntPtr device, ref byte status, byte[] buffer, byte bufferSize, ref byte numBytesRead);

        /// <summary>
        /// request a write from a specified address on the the I2C bus
        /// </summary>
        /// <param name="device"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="buffer"></param>
        /// <param name="numBytesToWrite"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_WriteRequest(IntPtr device, byte slaveAddress, byte[] buffer, byte numBytesToWrite);

        /// <summary>
        /// request the status of a read/write transaction on the bus
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_TransferStatusRequest(IntPtr device);

        /// <summary>
        /// pull the status of a read/write transaction on the bus
        /// </summary>
        /// <param name="device"></param>
        /// <param name="status"></param>
        /// <param name="detailedStatus"></param>
        /// <param name="numRetries"></param>
        /// <param name="bytesRead"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_GetTransferStatusResponse(IntPtr device, ref byte status, ref byte detailedStatus, ref ushort numRetries, ref ushort bytesRead);

        /// <summary>
        /// cancel all pending I/O operations on the device
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_CancelIo(IntPtr device);

        /// <summary>
        /// configure the communication settings on a cp2112 device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="bitRate"></param>
        /// <param name="address"></param>
        /// <param name="autoReadRespond"></param>
        /// <param name="writeTimeout"></param>
        /// <param name="readTimeout"></param>
        /// <param name="sclLowTimeout"></param>
        /// <param name="transferRetries"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SLABHIDtoSMBus.dll")]
        private static extern int HidSmbus_SetSmbusConfig(IntPtr device, uint bitRate, byte address, int autoReadRespond, ushort writeTimeout, ushort readTimeout, int sclLowTimeout, ushort transferRetries);
        #endregion

        #region Read & Write Procedures

        /// <summary>
        /// Searches connected SiLabs devices for the indicies of connected devices with a specific vid and pid.
        /// </summary>
        /// <returns>array of device indicies</returns>
        public override uint[] searchDevices()
        {
            //initiate by reading the number of devices
            uint num_devices = 0;
            int status = HidSmbus_GetNumDevices(ref num_devices, 0, 0);


            //search through devices for compatible product
            ushort vid_read = 0;
            ushort pid_read = 0;
            ushort device_id = 0;
            List<uint> device_index = new List<uint>();

            for (uint i = 0; i < num_devices; i++)
            {
                status = HidSmbus_GetAttributes(i, 0, 0, ref vid_read, ref pid_read, ref device_id);
                if ((pid_read == XGIGA_DEVICE_PID) && (vid_read == XGIGA_DEVICE_VID) && (status == HID_SMBUS_SUCCESS))
                {
                    device_index.Add(i);
                }
            }
            return device_index.ToArray();
        }

        /// <summary>
        /// Continuously polls a SiLabs device until the device enters an IDLE state.
        /// </summary>
        /// <param name="device">A device pointer, as created by the SiLabs "Open" function</param>
        private static void waitUntilIdle(IntPtr device)
        {
            byte xfer_status = 0x01;
            byte xfer_status_d = 0x0;
            ushort num_read = 0x0;
            ushort num_retry = 0;
            int status = 0;
            int i = 0;

            while ((xfer_status == HID_SMBUS_S0_BUSY) && (status == HID_SMBUS_SUCCESS) && (i < 500))
            {
                status = HidSmbus_TransferStatusRequest(device);
                status = HidSmbus_GetTransferStatusResponse(device, ref xfer_status, ref xfer_status_d, ref num_retry, ref num_read);
                i++;
            }
        }

        /// <summary>
        /// Reads in byte values from a SiLabs board.
        /// </summary>
        /// <param name="table_addr">Address of table to be read</param>
        /// <param name="read_size">number of bytes to be read</param>
        /// <param name="read_index">starting index of values to be read</param>
        /// <returns>Byte array containing values read from a connected SiLabs board</returns>
        protected override byte[] BoardRead(uint table_addr, uint read_size, uint read_index)
        {
            byte[] output_array = new byte[read_size];
            // IntPtr device = new IntPtr();
            byte[] target_addr = new byte[1];
            target_addr[0] = (byte)read_index;

            //open device
            // int status = HidSmbus_Open(ref device, device_index, 0, 0);
            // if (status != HID_SMBUS_SUCCESS) { updateDeviceStatus(status); return null; }

            //configure device
            int status = HidSmbus_SetSmbusConfig(device, BIT_RATE, 0x02, 0, TIMEOUT_LENGTH, TIMEOUT_LENGTH, TIMEOUT_LENGTH, NUM_RETRIES);
            if (status != HID_SMBUS_SUCCESS) { updateDeviceStatus(status); return null; }

            //request a read
            status = HidSmbus_AddressReadRequest(device, (byte)table_addr, (ushort)read_size, 1, target_addr);
            if (status != HID_SMBUS_SUCCESS) { updateDeviceStatus(status); return null; }

            //wait for complete read
            status = HidSmbus_ForceReadResponse(device, (ushort)read_size);
            if (status != HID_SMBUS_SUCCESS) { updateDeviceStatus(status); return null; }

            //get read data
            byte xfer_status = 0x0;
            byte[] out_buffer = new byte[0x80]; //the buffer must be large to avoid overflow. Buffer size != Output array size.
            byte num_read = 0x0;
            int total_read = 0;
            while ((total_read < read_size) && (status != HID_SMBUS_READ_TIMED_OUT))
            {
                status = HidSmbus_GetReadResponse(device, ref xfer_status, out_buffer, Convert.ToByte(out_buffer.Length), ref num_read);
                //add read bytes to output array
                for (int i = total_read; i < num_read + total_read; i++)
                {
                    if (i < output_array.Length)
                    {
                        output_array[i] = out_buffer[i - total_read];
                    }
                }
                total_read += num_read;
            }

            Thread.Sleep(READ_WRITE_DELAY);
            // HidSmbus_Close(device);
            updateDeviceStatus(status);
            if (status != HID_SMBUS_SUCCESS)
                return null;
            else
                return output_array;
        }

        /// <summary>
        /// Writes a binary array to a SiLabs board
        /// </summary>
        /// <param name="table_addr">address of table to write to</param>
        /// <param name="start_index">index of first byte to write to</param>
        /// <param name="write_buf">Array of bytes to write</param>
        /// <returns>returns boolean FALSE if the procedure failed and TRUE if write was successful</returns>
        protected override bool BoardWrite(uint table_addr, uint start_index, byte[] input_buf)
        {
            // IntPtr device = new IntPtr();
            byte[] write_buf = new byte[input_buf.Length + 1];

            write_buf[0] = (byte)start_index;
            //add the input values to the write buffer
            for (int i = 0; i < input_buf.Length; i++)
            {
                write_buf[i + 1] = input_buf[i];
            }

            //open device
            //int status = HidSmbus_Open(ref device, device_index, 0, 0);
            //if (status != HID_SMBUS_SUCCESS) { updateDeviceStatus(status); return false; }

            //Wait for device bus to be available
            waitUntilIdle(device);

            //configure device
            int status = HidSmbus_SetSmbusConfig(device, BIT_RATE, 0x02, 1, TIMEOUT_LENGTH, TIMEOUT_LENGTH, TIMEOUT_LENGTH, NUM_RETRIES);
            if (status != HID_SMBUS_SUCCESS) { updateDeviceStatus(status); return false; }

            //request a write
            byte write_len = 0x0;
            if (write_buf.Length < 0x3D)
            {
                write_len = Convert.ToByte(write_buf.Length);
            }
            else
            {
                write_len = 0x3D;
            }
            status = HidSmbus_WriteRequest(device, (byte)table_addr, write_buf, write_len);
            if (status != HID_SMBUS_SUCCESS) { updateDeviceStatus(status); return false; }

            //wait for write to complete
            waitUntilIdle(device);
            Thread.Sleep(READ_WRITE_DELAY);

            // HidSmbus_Close(device);
            updateDeviceStatus(status);
            return true;
        }

        /// <summary>
        /// Decoding function for Read/Write return codes.
        /// </summary>
        /// <param name="status_in">return code from CP2112 read/write operation</param>
        /// <returns>String indicating the return status of the operation</returns>
        private void updateDeviceStatus(int status_in)
        {
            switch (status_in)
            {
                case 0x00:
                    DEVICE_STATUS = DeviceMaster.operation_success;
                    break;
                case 0x01:
                    DEVICE_STATUS = "Device not Found";
                    break;
                case 0x02:
                case 0x03:
                    DEVICE_STATUS = "Invalid Device";
                    break;
                case 0x04:
                    DEVICE_STATUS = "Invalid Parameter";
                    break;
                case 0x05:
                    DEVICE_STATUS = "Invalid Request Length";
                    break;
                case 0x10:
                    DEVICE_STATUS = "Component Read Error";
                    break;
                case 0x11:
                    DEVICE_STATUS = "Component Write Error";
                    break;
                case 0x12:
                    DEVICE_STATUS = "Component Read Timeout";
                    break;
                case 0x13:
                    DEVICE_STATUS = "Component Write Timeout";
                    break;
                case 0x14:
                    DEVICE_STATUS = "Device IO Failure (check board connection)";
                    break;
                case 0x15:
                    DEVICE_STATUS = "Device Access Error (try re-selecting device)";
                    break;
                case 0x16:
                    DEVICE_STATUS = "Device Not Supported";
                    break;
                default:
                    DEVICE_STATUS = "Read/Write Failure";
                    break;
            }
        }
        #endregion
    }

    class LTUSBXpressDevice : I2CProgrammer
    {
        public LTUSBXpressDevice()
        {
            device_id = "USBXpress";
        }

        public override bool GPIO_SUPPORT { get => false; protected set => GPIO_SUPPORT = false; }

        public override GPIOStatus PullGPIOStatus()
        {
            throw new NotImplementedException();
        }

        #region Device Init/Close

        public override bool InitializeDevice()
        {
            try
            {
                int status = SI_Open(device_index, ref device);
                if (status != USBX_SUCCESS) { updateDeviceStatus(status); return false; }
            }
            catch (Exception e)
            {
                BoardLogger.Log("ERROR: Cannot open USBXpress device. The device is likely already open " +
                                               "or no longer exists. Please replug board and try again.");
                return false;
            }

            return true;
        }

        public override void CloseDevice()
        {
            try
            {
                SI_Close(device);
            }
            catch (Exception e)
            {
                // Show user a message that device cannot be closed for
                // some reason
                BoardLogger.Log("ERROR: USBXpress device cannot be closed. The device likely no longer " +
                                               "exists, was never opened, or has already been closed.");
            }
        }

        #endregion

        #region R/W Configuration Definitions

        public const ushort LT_DEVICE_VID = 0x1057;
        public const ushort LT_DEVICE_PID = 0x5444;

        public const byte LT_USB_TOKEN = 0xFE;
        public const byte LT_USB_HANDSHAKE = 0xFD;

        public const byte LT_USB_WRITE_CMD = 0x02;
        public const byte LT_USB_READ_CMD = 0x03;

        public const byte LT_USB_RETURN_HDR_LEN = 0x04;
        public const byte LT_USB_RETURN_FTR_LEN = 0x02;

        private static IntPtr device = new IntPtr();

        private const ushort NUM_RETRIES = 3;

        private const int SI_MAX_DEVICE_STRLEN = 256;

        private static BackgroundWorker si_open_worker;
        #endregion

        #region Return Code Definitions

        private const byte USBX_READ_TIMED_OUT = 0x12;
        private const byte USBX_SUCCESS = 0x00;
        private const byte USBX_S0_IDLE = 0x00;
        private const byte USBX_S0_BUSY = 0x01;
        private const byte USBX_S0_COMPLETE = 0x02;

        #endregion

        #region Exported Library Functions
        /// <summary>
        /// get the number of connected SiLabs devices
        /// </summary>
        /// <param name="numDevices"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_GetNumDevices(ref uint numDevices);

        /// <summary>
        /// get the description of a connected SiLabs device
        /// </summary>
        /// <param name="dwDeviceNum"></param>
        /// <param name="lpvDeviceString"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        public static extern int SI_GetProductString(UInt32 dwDeviceNum, StringBuilder lpvDeviceString, Int32 dwFlags);

        /// <summary>
        /// open communication with a connected SiLabs device
        /// </summary>
        /// <param name="devNum"></param>
        /// <param name="devHandle"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_Open(UInt32 devNum, ref IntPtr devHandle);

        /// <summary>
        /// close communication with a connected SiLabs device
        /// </summary>
        /// <param name="devNum"></param>
        /// <param name="devHandle"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_Close(IntPtr device);

        /// <summary>
        /// Read from the USB of a SiLabs device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="Buffer"></param>
        /// <param name="NumBytesToRead"></param>
        /// <param name="NumBytesReturned"></param>
        /// <param name="overlapped"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_Read(IntPtr device, byte[] Buffer, uint NumBytesToRead, ref uint NumBytesReturned, IntPtr overlapped);

        /// <summary>
        /// Write to the USB of a SiLabs device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="Buffer"></param>
        /// <param name="NumBytesToRead"></param>
        /// <param name="NumBytesReturned"></param>
        /// <param name="overlapped"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_Write(IntPtr device, byte[] Buffer, UInt32 NumBytesToWrite, out UInt32 NumBytesWritten, IntPtr overlapped);

        /// <summary>
        /// Cancel all I/O operations with a SiLabs device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="status"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <param name="numBytesRead"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_CancelIo(IntPtr device, ref byte status, byte[] buffer, byte bufferSize, ref byte numBytesRead);

        /// <summary>
        /// Clear the communication buffers of a SiLabs device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="buffer"></param>
        /// <param name="numBytesToWrite"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_FlushBuffers(IntPtr device, byte slaveAddress, byte[] buffer, byte numBytesToWrite);

        /// <summary>
        /// set communication timeout for a SiLabs device
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_SetTimeouts(IntPtr device);

        /// <summary>
        /// get communication timeout status for a SiLabs device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="status"></param>
        /// <param name="detailedStatus"></param>
        /// <param name="numRetries"></param>
        /// <param name="bytesRead"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_GetTimeouts(IntPtr device, ref byte status, ref byte detailedStatus, ref ushort numRetries, ref ushort bytesRead);

        /// <summary>
        /// Check the status of the device RX queue
        /// </summary>
        /// <param name="device"></param>
        /// <param name="NumBytesInQueue"></param>
        /// <param name="QueueStatus"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_CheckRXQueue(IntPtr device, ref int NumBytesInQueue, ref int QueueStatus);

        /// <summary>
        /// Set control values for the connected device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="IOControlCode"></param>
        /// <param name="InBuffer"></param>
        /// <param name="BytesToRead"></param>
        /// <param name="OutBuffer"></param>
        /// <param name="BytesToWrite"></param>
        /// <param name="BytesSucceeded"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "SiUSBXp.dll")]
        private static extern int SI_DeviceIOControl(IntPtr device, uint IOControlCode, ref byte[] InBuffer, uint BytesToRead, ref byte[] OutBuffer, uint BytesToWrite, ref uint BytesSucceeded);
        #endregion

        #region Read & Write Procedures

        /// <summary>
        /// Searches connected SiLabs for the indicies of connected devices with a specific vid and pid.
        /// </summary>
        /// <returns>array of device indicies</returns>
        public override uint[] searchDevices()
        {
            //initiate by reading the number of devices
            uint num_devices = 0;
            int status = SI_GetNumDevices(ref num_devices);

            //search through devices for compatible product
            int vid_read = 0;
            int pid_read = 0;
            StringBuilder device_string = new StringBuilder(SI_MAX_DEVICE_STRLEN * SI_MAX_DEVICE_STRLEN);

            List<uint> device_index = new List<uint>();

            for (uint i = 0; i < num_devices; i++)
            {
                status = SI_GetProductString(i, device_string, 0x3); //return VID
                vid_read = Convert.ToInt32(device_string.ToString(), 16);
                status = SI_GetProductString(i, device_string, 0x4); //return PID
                pid_read = Convert.ToInt32(device_string.ToString(), 16);
                if ((pid_read == LT_DEVICE_PID) && (vid_read == LT_DEVICE_VID))
                {
                    device_index.Add(i);
                }
            }
            return device_index.ToArray();
        }

        /// <summary>
        /// Continuously polls a SiLabs device until the device enters an IDLE state.
        /// </summary>
        /// <param name="device">A device pointer, as created by the SiLabs "Open" function</param>
        private static void waitUntilIdle(IntPtr device)
        {
            int xfer_status = 0x01;
            int bytes_in_queue = 0x0;
            int status = 0;

            while ((bytes_in_queue != 0) && (status == 0x00))
            {
                status = SI_CheckRXQueue(device, ref bytes_in_queue, ref xfer_status);
            }
        }

        /// <summary>
        /// function used to open communications with a board
        /// </summary>
        /// <returns>True if successful</returns>
        public void BoardOpen()
        {
            si_open_worker = new BackgroundWorker();
            si_open_worker.DoWork += BoardOpenAsync;
            si_open_worker.RunWorkerAsync();
        }

        /// <summary>
        /// Asynchronous command for opening communications to the board. 
        /// This is done to prevent the open command from stalling the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoardOpenAsync(object sender, DoWorkEventArgs e)
        {
            int status = SI_Open(device_index, ref device);
        }

        /// <summary>
        /// function used to close communications with a board
        /// </summary>
        /// <returns>True if successful</returns>
        public static bool BoardClose()
        {
            //open device
            int status = SI_Close(device);
            return (status != USBX_SUCCESS);
        }

        /// <summary>
        /// Method for generating an SI Write command.
        /// </summary>
        /// <param name="base_addr">byte indicating the table to be written to</param>
        /// <param name="start_idx">byte indicating the index of the first byte to be written to</param>
        /// <param name="write_array">byte values to be written to the SI USB</param>
        /// <returns>byte array containing a valid SI Write command</returns>
        private static byte[] buildWriteCommand(byte base_addr, byte start_idx, byte[] write_array)
        {
            byte[] write_command = new byte[9 + write_array.Length];
            //the first and last bytes are always the same
            write_command[0] = LT_USB_TOKEN;
            write_command[write_command.Length - 1] = LT_USB_HANDSHAKE;
            //byte 1 is the length of the command excluding this byte and the start byte
            write_command[1] = Convert.ToByte(write_command.Length - 2);
            //byte 2 indicates either a read, write, or idle operation
            write_command[2] = LT_USB_WRITE_CMD;
            //byte 3 is always 0
            write_command[3] = 0x00;
            //byte 4 is the size of the read operation (1 byte for sending the read size) or the size of the write array + 1
            write_command[4] = Convert.ToByte(write_array.Length + 1);
            //byte 5 is the read/write table
            write_command[5] = base_addr;
            //byte 6 is the read/write index
            write_command[6] = start_idx;
            //bytes 7 to n-2 contain the data to be written to the optic
            for (int i = 0; i < write_array.Length; i++)
            {
                write_command[7 + i] = write_array[i];
            }
            //byte n-2 is the checksum of bytes 1 to n-2
            byte command_checksum = 0;
            for (int i = 1; i < write_array.Length + 7; i++)
            {
                command_checksum += write_command[i];
            }

            write_command[write_array.Length + 7] = command_checksum;
            return write_command;
        }

        /// <summary>
        /// Method for generating an SI Read command.
        /// </summary>
        /// <param name="base_addr">byte indicating the table to be written to</param>
        /// <param name="start_idx">byte indicating the index of the first byte to be written to</param>
        /// <param name="read_length">number of bytes to read</param>
        /// <returns>byte array containing a valid SI Read command</returns>
        private static byte[] buildReadCommand(byte base_addr, byte start_idx, ushort read_length)
        {
            byte[] write_command = new byte[10];
            //the first and last bytes are always the same
            write_command[0] = LT_USB_TOKEN;
            write_command[9] = LT_USB_HANDSHAKE;
            //byte 1 is the length of the command excluding this byte and the start byte
            write_command[1] = Convert.ToByte(write_command.Length - 2);
            //byte 2 indicates either a read, write, or idle operation
            write_command[2] = LT_USB_READ_CMD;
            //byte 3 is always 0
            write_command[3] = 0x00;
            //byte 4 is the size of the read operation (1 byte for sending the read size) or the size of the write array + 1
            write_command[4] = 0x01;
            //byte 5 is the read/write table
            write_command[5] = base_addr;
            //byte 6 is the read/write index
            write_command[6] = start_idx;
            //byte 7 contains the read size
            write_command[7] = (byte)read_length;
            //byte 8 is the checksum of bytes 1 to 7
            byte command_checksum = 0;
            for (int i = 1; i < write_command.Length - 2; i++)
            {
                command_checksum += write_command[i];
            }
            write_command[8] = command_checksum;

            return write_command;
        }

        /// <summary>
        /// Reads in binary values from a SiLabs board.
        /// </summary>
        /// <param name="table_addr">Address of table to be read</param>
        /// <param name="read_size">number of bytes to be read</param>
        /// <param name="read_index">starting index of values to be read</param>
        /// <returns>Byte array containing values read from a connected SiLabs board</returns>
        protected override byte[] BoardRead(uint table_addr, uint read_size, uint read_index)
        {
            byte[] output_array = new byte[read_size];
            byte[] target_addr = new byte[1];
            target_addr[0] = (byte)read_index;

            // int status = SI_Open(device_index, ref device);
            // if (status != USBX_SUCCESS) { updateDeviceStatus(status); return null; }

            //Wait for device bus to be available
            waitUntilIdle(device);

            //request a read
            byte[] rd_req_command = buildReadCommand((byte)table_addr, (byte)read_index, (ushort)read_size);

            uint num_bytes_returned = 0;
            int status = SI_Write(device, rd_req_command, Convert.ToUInt32(rd_req_command.Length), out num_bytes_returned, IntPtr.Zero);
            if (status != USBX_SUCCESS) { updateDeviceStatus(status); return null; }

            //get read data
            byte[] output_buffer = new byte[read_size + LT_USB_RETURN_HDR_LEN + LT_USB_RETURN_FTR_LEN];
            num_bytes_returned = 0;
            status = SI_Read(device, output_buffer, Convert.ToUInt32(output_buffer.Length), ref num_bytes_returned, IntPtr.Zero);
            if (status != USBX_SUCCESS) { updateDeviceStatus(status); return null; }

            output_array = new byte[output_buffer.Length - LT_USB_RETURN_FTR_LEN - 1];
            Array.Copy(output_buffer, LT_USB_RETURN_HDR_LEN, output_array, 0, output_buffer.Length - LT_USB_RETURN_FTR_LEN - 1);

            // SI_Close(device);
            Thread.Sleep(READ_WRITE_DELAY);
            updateDeviceStatus(status);
            return output_array;
        }

        /// <summary>
        /// Writes a binary array to a SiLabs board
        /// </summary>
        /// <param name="table_addr">address of table to write to</param>
        /// <param name="start_index">index of first byte to write to</param>
        /// <param name="write_buf">Array of bytes to write</param>
        /// <returns>returns boolean FALSE if the procedure failed and TRUE if write was successful</returns>
        protected override bool BoardWrite(uint table_addr, uint start_index, byte[] input_buf)
        {
            byte[] write_buf = new byte[input_buf.Length + 1];

            //int status = SI_Open(device_index, ref device);
            //if (status != USBX_SUCCESS) { updateDeviceStatus(status); return false; }

            //Wait for device bus to be available
            waitUntilIdle(device);

            //request a write
            byte[] wr_req_command = buildWriteCommand((byte)table_addr, (byte)start_index, input_buf);
            uint num_bytes_returned;

            int status = SI_Write(device, wr_req_command, Convert.ToUInt32(wr_req_command.Length), out num_bytes_returned, IntPtr.Zero);
            //Retry if retriies are enabled
            int i = 0;
            while ((i < NUM_RETRIES) && (status != USBX_SUCCESS))
            {
                status = SI_Write(device, wr_req_command, Convert.ToUInt32(wr_req_command.Length), out num_bytes_returned, IntPtr.Zero);
                Thread.Sleep(READ_WRITE_DELAY);
                i++;
            }
            if (status != USBX_SUCCESS) { updateDeviceStatus(status); return false; }

            //retrieve write response
            byte[] output_buffer = new byte[LT_USB_RETURN_HDR_LEN + LT_USB_RETURN_FTR_LEN];
            status = SI_Read(device, output_buffer, Convert.ToUInt32(output_buffer.Length), ref num_bytes_returned, IntPtr.Zero);

            // SI_Close(device);
            Thread.Sleep(READ_WRITE_DELAY);
            updateDeviceStatus(status);
            return (status == USBX_SUCCESS);
        }

        /// <summary>
        /// Decoding function for Read/Write return codes.
        /// </summary>
        /// <param name="status_in">return code from USBXpress read/write operation</param>
        /// <returns>String indicating the return status of the operation</returns>
        private void updateDeviceStatus(int status_in)
        {
            switch (status_in)
            {
                case 0x00:
                    DEVICE_STATUS = DeviceMaster.operation_success;
                    break;
                case 0xFF:
                    DEVICE_STATUS = "Device not Found";
                    break;
                case 0x01:
                    DEVICE_STATUS = "Invalid Device";
                    break;
                case 0x02:
                    DEVICE_STATUS = "Component Read Error";
                    break;
                case 0x04:
                    DEVICE_STATUS = "Component Write Error";
                    break;
                case 0x05:
                    DEVICE_STATUS = "Reset Error";
                    break;
                case 0x06:
                    DEVICE_STATUS = "Invalid Parameter";
                    break;
                case 0x07:
                    DEVICE_STATUS = "Invalid Request Length";
                    break;
                case 0x08:
                    DEVICE_STATUS = "Device IO Failure (check board connection)";
                    break;
                case 0x09:
                    DEVICE_STATUS = "Invalid Baudrate";
                    break;
                case 0x0A:
                    DEVICE_STATUS = "Function Not Supported";
                    break;
                case 0x0B:
                    DEVICE_STATUS = "Global Data Error";
                    break;
                case 0x0C:
                    DEVICE_STATUS = "System Error Code";
                    break;
                case 0x0D:
                    DEVICE_STATUS = "Component Read Timeout";
                    break;
                case 0x0E:
                    DEVICE_STATUS = "Component Write Timeout";
                    break;
                case 0x0F:
                    DEVICE_STATUS = "I/O Pending";
                    break;
                default:
                    DEVICE_STATUS = "Read/Write Failure";
                    break;
            }
        }
        #endregion
    }

    class FT4222ADevice : I2CProgrammer
    {
        public FT4222ADevice()
        {
            device_id = "FT4222-A";
        }

        public override bool GPIO_SUPPORT { get => false; protected set => GPIO_SUPPORT = false; }

        public override GPIOStatus PullGPIOStatus()
        {
            throw new NotImplementedException();
        }

        #region Device Init/Close

        IntPtr device = new IntPtr();

        public override bool InitializeDevice()
        {
            try
            {
                int status = FT_Open(device_index, ref device);
                if (status != FT_SUCCESS)
                {
                    UpdateDeviceStatus(status);
                    return false;
                }
                status = FT4222_I2CMaster_Init(device, BIT_RATE);
                if (status != FT_SUCCESS)
                {
                    UpdateDeviceStatus(status);
                    return false;
                }
            }
            catch (Exception e)
            {
                BoardLogger.Log("ERROR: Cannot open FT4222A device. The device is likely already open " +
                                               " or no longer exists. Please replug board and try again.");
            }

            return true;
        }

        public override void CloseDevice()
        {
            try
            {
                FT_Close(device);
            }
            catch (Exception e)
            {
                BoardLogger.Log("ERROR: Cannot close FT4222A device. The device likely no longer " +
                                               "exists, was never opened, or has already been closed.");
            }
        }

        #endregion

        #region Return Code Definitions
        private const byte FT_SUCCESS = 0x00;
        #endregion

        #region Exported Library Functions
        /// <summary>
        /// Create a list of device information for connected FTDI 4222 chips 
        /// </summary>
        /// <param name="numDevices"></param>
        /// <returns></returns>
        [DllImport("ftd2xx.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT_CreateDeviceInfoList(ref uint numDevices);

        /// <summary>
        /// Get detailed information on connected FTDI 4222 chips
        /// </summary>
        /// <param name="device_idx"></param>
        /// <param name="flags"></param>
        /// <param name="device_type"></param>
        /// <param name="device_id"></param>
        /// <param name="location_id"></param>
        /// <param name="serial_no"></param>
        /// <param name="description"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport("ftd2xx.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT_GetDeviceInfoDetail(uint device_idx, ref ulong flags, ref ulong device_type, ref ulong device_id, ref ulong location_id, ref char[] serial_no, ref char[] description, ref IntPtr device);

        /// <summary>
        /// Open communication with an FTDI 4222 device
        /// </summary>
        /// <param name="device_index"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport("ftd2xx.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT_Open(uint device_index, ref IntPtr device);

        /// <summary>
        /// Close communication with an FTDI 4222 device
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport("ftd2xx.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT_Close(IntPtr device);

        /// <summary>
        /// Uninitialize an FTDI 4222 device
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport("LibFT4222.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT4222_Uninitialize(IntPtr device);

        /// <summary>
        /// Initialize an FTDI 4222 device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="data_rate_kbps"></param>
        /// <returns></returns>
        [DllImport("LibFT4222.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FT4222_I2CMaster_Init(IntPtr device, uint data_rate_kbps);

        /// <summary>
        /// Set the BAUD rate for I2C communication
        /// </summary>
        /// <param name="device"></param>
        /// <param name="rate_idx"></param>
        /// <returns></returns>
        [DllImport("LibFT4222.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT4222_SetClock(IntPtr device, uint rate_idx);

        /// <summary>
        /// Write to a device's I2C bus
        /// </summary>
        /// <param name="device"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="buffer"></param>
        /// <param name="numBytesToWrite"></param>
        /// <param name="numBytesWritten"></param>
        /// <returns></returns>
        [DllImport("LibFT4222.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FT4222_I2CMaster_Write(IntPtr device, byte slaveAddress, byte[] buffer, uint numBytesToWrite, ref uint numBytesWritten);

        /// <summary>
        /// Write to a device's I2C bus (includes flagging)
        /// </summary>
        /// <param name="device"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="flag"></param>
        /// <param name="buffer"></param>
        /// <param name="numBytesToWrite"></param>
        /// <param name="numBytesWritten"></param>
        /// <returns></returns>
        [DllImport("LibFT4222.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FT4222_I2CMaster_WriteEx(IntPtr device, byte slaveAddress, byte flag, byte[] buffer, uint numBytesToWrite, ref uint numBytesWritten);

        /// <summary>
        /// Read from a device's I2C bus
        /// </summary>
        /// <param name="device"></param>
        /// <param name="slave_address"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <param name="numBytesRead"></param>
        /// <returns></returns>
        [DllImport("LibFT4222.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FT4222_I2CMaster_Read(IntPtr device, ushort slave_address, byte[] buffer, uint bufferSize, ref uint numBytesRead);
        #endregion

        #region Device Read/Write

        public override uint[] searchDevices()
        {
            uint numDevices = 0;
            int status = FT_CreateDeviceInfoList(ref numDevices);

            List<uint> device_indices = new List<uint>();

            for (uint i = 0; i < numDevices / 2; i++)
            {
                IntPtr dev = new IntPtr();
                status = FT_Open(i, ref dev);
                if (status == FT_SUCCESS)
                {
                    status = FT4222_I2CMaster_Init(dev, BIT_RATE);
                    if (status == FT_SUCCESS)
                    {
                        uint indexSelWritten = 0;
                        byte[] devInfo = new byte[23];
                        FT4222_I2CMaster_Write(dev, 0x57, new byte[] { 0x00 }, 1, ref indexSelWritten);
                        FT4222_I2CMaster_Read(dev, 0x57, devInfo, 23, ref indexSelWritten);
                        string devInfoString = Encoding.ASCII.GetString(devInfo);
                        string devProdName = devInfoString.Substring(13, 3);
                        if (devProdName != "SXQ")
                        {
                            device_indices.Add(i);
                        }
                    }
                }
                FT_Close(dev);
            }
            return device_indices.ToArray();
        }

        protected override byte[] BoardRead(uint table_addr, uint read_size, uint read_index)
        {
            table_addr = Convert.ToByte(table_addr / 2);
            byte[] outputArray = new byte[read_size];
            int status = FT_SUCCESS;

            // Set R/W starting byte index
            uint indexSelWritten = 0;
            byte[] devInfo = new byte[23];
            while (indexSelWritten < 1)
            {
                FT4222_I2CMaster_Write(device, (byte)table_addr, new byte[] { (byte)read_index }, 1, ref indexSelWritten);
            }

            // Get read data
            byte[] outBuffer = new byte[4 * read_size]; // large buffer to avoid overflow
            uint numRead = 0;
            uint totalRead = 0;
            while ((totalRead < read_size) && (status == FT_SUCCESS))
            {
                status = FT4222_I2CMaster_Read(device, (byte)table_addr, outBuffer, read_size, ref numRead);
                for (uint i = totalRead; i < numRead + totalRead; i++)
                {
                    if (i < outputArray.Length)
                    {
                        outputArray[i] = outBuffer[i - totalRead];
                    }
                }
                totalRead += numRead;
            }

            Thread.Sleep(READ_WRITE_DELAY);

            UpdateDeviceStatus(status);

            if (status != FT_SUCCESS)
                return null;
            else return outputArray;
        }

        protected override bool BoardWrite(uint table_addr, uint start_index, byte[] input_buf)
        {
            byte[] writeBuf = new byte[input_buf.Length + 1];
            table_addr = Convert.ToByte(table_addr / 2);
            writeBuf[0] = (byte)start_index;

            // Add input values to write buffer
            for (int i = 0; i < input_buf.Length; i++)
            {
                writeBuf[i + 1] = input_buf[i];
            }

            int status = FT_SUCCESS;

            byte writeLen = 0;
            if (writeBuf.Length < 0x3D)
            {
                writeLen = Convert.ToByte(writeBuf.Length);
            }
            else
            {
                writeLen = 0x3D;
            }

            uint numSent = 0;
            uint totalSent = 0;

            while (totalSent < writeBuf.Length && status == FT_SUCCESS)
            {
                status = FT4222_I2CMaster_WriteEx(device, (byte)table_addr, 6, writeBuf, (uint)writeBuf.Length, ref numSent);
                totalSent += numSent;
            }

            if (status != FT_SUCCESS)
            {
                UpdateDeviceStatus(status);
                return false;
            }

            Thread.Sleep(READ_WRITE_DELAY);
            UpdateDeviceStatus(status);
            return status == FT_SUCCESS;
        }

        void UpdateDeviceStatus(int statusIn)
        {
            switch (statusIn)
            {
                case 0x00:
                    DEVICE_STATUS = DeviceMaster.operation_success;
                    break;
                case 0x01:
                    DEVICE_STATUS = "Invalid Device Handle";
                    break;
                case 0x02:
                    DEVICE_STATUS = "Device Not Found";
                    break;
                case 0x03:
                    DEVICE_STATUS = "Device Not Opened";
                    break;
                case 0x04:
                    DEVICE_STATUS = "I//O Error";
                    break;
                case 0x05:
                    DEVICE_STATUS = "Insufficient Resources";
                    break;
                case 0x6:
                    DEVICE_STATUS = "Invalid Input parameter";
                    break;
                case 0x7:
                    DEVICE_STATUS = "Invalid Baud Rate";
                    break;
                case 0x8:
                    DEVICE_STATUS = "Device Not Opened For Erase";
                    break;
                case 0x9:
                    DEVICE_STATUS = "Device Not Opened For Write";
                    break;
                case 0xA:
                    DEVICE_STATUS = "Failed To Write Device";
                    break;
                case 0xB:
                    DEVICE_STATUS = "EEPROM Read Fail";
                    break;
                case 0xC:
                    DEVICE_STATUS = "EEPROM Write Fail";
                    break;
                case 0xD:
                    DEVICE_STATUS = "EEPROM Erase Fail";
                    break;
                case 0xE:
                    DEVICE_STATUS = "EEPROM Not Present";
                    break;
                case 0xF:
                    DEVICE_STATUS = "EEPROM Not Programmed";
                    break;
                case 0x10:
                    DEVICE_STATUS = "Invalid R/W Arguments";
                    break;
                case 0x11:
                    DEVICE_STATUS = "Device Not Supported";
                    break;
                case 0x12:
                    DEVICE_STATUS = "Device Not Supported";
                    break;
                default:
                    DEVICE_STATUS = "Read/Write Failure";
                    break;
            }
        }

        #endregion
    }

    class FT4222BDevice : I2CProgrammer
    {
        public FT4222BDevice()
        {
            device_id = "FT4222-B";
        }

        public const byte SFP_LED_ADDR = 0x68;
        public const byte XFP_LED_ADDR = 0x69;
        public const byte QSFP_LED_ADDR = 0x6A;
        public const byte AUX_LED_ADDR = 0x6B;

        public override bool GPIO_SUPPORT { get => true; protected set => GPIO_SUPPORT = true; }

        public override GPIOStatus PullGPIOStatus()
        {
            GPIOStatus gpioStatus = new GPIOStatus();

            InitializeDevice();

            bool port_empty = false;

            FT4222_GPIO_Read(device, 0, ref port_empty);
            gpioStatus.MOD_ABS_SFP = port_empty;

            FT4222_GPIO_Read(device, 4, ref port_empty);
            gpioStatus.MOD_ABS_XFP = port_empty;

            FT4222_GPIO_Read(device, 12, ref port_empty);
            gpioStatus.MOD_ABS_QSFP = port_empty;

            CloseDevice();

            return gpioStatus;
        }

        #region Device Init/Close

        IntPtr device = new IntPtr();

        public override bool InitializeDevice()
        {
            //open device
            try
            {
                int status = FT_Open(device_index, ref device);
                if (status != FT_SUCCESS)
                {
                    updateDeviceStatus(status);
                    return false;
                }
                status = FT4222_I2CMaster_Init(device, BIT_RATE);
                if (status != FT_SUCCESS)
                {
                    updateDeviceStatus(status);
                    return false;
                }
            }
            catch (Exception e)
            {
                BoardLogger.Log("ERROR: Cannot open FT4222B device. The device is likely already open " +
                                               "or no longer exists. Please replug board and try again.");
                return false;
            }

            return true;
        }

        public override void CloseDevice()
        {
            try
            {
                FT_Close(device);
            }
            catch (Exception e)
            {
                // Show user a message that device cannot be closed for
                // some reason
                BoardLogger.Log("ERROR: FT4222B device cannot be closed. The device likely no longer " +
                                               "exists, was never opened, or has already been closed.");
            }
        }

        #endregion

        #region Return Code Definitions
        private const byte FT_SUCCESS = 0x00;
        #endregion

        #region Exported Library Functions
        /// <summary>
        /// Create a list of device information for connected FTDI 4222 chips
        /// </summary>
        /// <param name="numDevices"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "ftd2xx.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT_CreateDeviceInfoList(ref uint numDevices);

        /// <summary>
        /// Get detailed information on connected FTDI 4222 chips
        /// </summary>
        /// <param name="device_idx"></param>
        /// <param name="flags"></param>
        /// <param name="device_type"></param>
        /// <param name="device_id"></param>
        /// <param name="location_id"></param>
        /// <param name="serial_no"></param>
        /// <param name="description"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "ftd2xx.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT_GetDeviceInfoDetail(uint device_idx, ref ulong flags, ref ulong device_type, ref ulong device_id, ref ulong location_id, ref char[] serial_no, ref char[] description, ref IntPtr device);

        /// <summary>
        /// Open communication with an FTDI 4222 device
        /// </summary>
        /// <param name="device_index"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "ftd2xx.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT_Open(uint device_index, ref IntPtr device);

        /// <summary>
        /// Close communication with an FTDI 4222 device
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "ftd2xx.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT_Close(IntPtr device);

        /// <summary>
        /// Uninitialize an FTDI 4222 device
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "LibFT4222.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT4222_Uninitialize(IntPtr device);

        /// <summary>
        /// Initialize an FTDI 4222 device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="data_rate_kbps"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "LibFT4222.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FT4222_I2CMaster_Init(IntPtr device, uint data_rate_kbps);

        /// <summary>
        /// Set the BAUD rate for I2C communication
        /// </summary>
        /// <param name="device"></param>
        /// <param name="rate_idx"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "LibFT4222.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT4222_SetClock(IntPtr device, uint rate_idx);

        /// <summary>
        /// Write to a device's I2C bus
        /// </summary>
        /// <param name="device"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="buffer"></param>
        /// <param name="numBytesToWrite"></param>
        /// <param name="numBytesWritten"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "LibFT4222.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FT4222_I2CMaster_Write(IntPtr device, byte slaveAddress, byte[] buffer, uint numBytesToWrite, ref uint numBytesWritten);

        /// <summary>
        /// Write to a device's I2C bus (includes flagging)
        /// </summary>
        /// <param name="device"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="flag"></param>
        /// <param name="buffer"></param>
        /// <param name="numBytesToWrite"></param>
        /// <param name="numBytesWritten"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "LibFT4222.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FT4222_I2CMaster_WriteEx(IntPtr device, byte slaveAddress, byte flag, byte[] buffer, uint numBytesToWrite, ref uint numBytesWritten);

        /// <summary>
        /// Read from a device's I2C bus
        /// </summary>
        /// <param name="device"></param>
        /// <param name="slave_address"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <param name="numBytesRead"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "LibFT4222.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FT4222_I2CMaster_Read(IntPtr device, ushort slave_address, byte[] buffer, uint bufferSize, ref uint numBytesRead);

        /// <summary>
        /// set interrupt trigger
        /// </summary>
        /// <param name="device"></param>
        /// <param name="gpio_port"></param>
        /// <param name="gpio_trigger"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "LibFT4222.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT4222_GPIO_SetInputTrigger(IntPtr device, int gpio_port, int gpio_trigger);

        /// <summary>
        /// get GPIO status
        /// </summary>
        /// <param name="device"></param>
        /// <param name="gpio_port"></param>
        /// <param name="port_val"></param>
        /// <returns></returns>
        [DllImport(DeviceMaster.dll_location + "LibFT4222.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int FT4222_GPIO_Read(IntPtr device, int gpio_port, ref bool port_val);
        #endregion

        #region Read & Write Procedures

        /// <summary>
        /// function for reading GPIO status lines for connected optics
        /// </summary>
        /// <returns></returns>
        /*public byte[] PullGPIOStatus()
        {
            //read GPIO status
            byte[] gpio_status = BoardRead(0x44, 0x3, 0x80);
            if (gpio_status == null)
                return null;

            //set GPIO tx disabled
            byte[] disable_cmd = new byte[] { Convert.ToByte(gpio_status[0] & 0xDD) };
            BoardWrite(0x44, 0x04, disable_cmd);
            disable_cmd = new byte[] { Convert.ToByte(gpio_status[1] & 0xF7) };
            BoardWrite(0x44, 0x05, disable_cmd);

            return gpio_status;
        }*/

        /// <summary>
        /// Sets an I2C RGB LED to specified brightness values defined by 'red', 'green', and 'blue'.
        /// The LED is held steady after the values are set.
        /// </summary>
        /// <param name="device_addr">Device address of the LED to be modified.</param>
        /// <param name="red">brightness value for the color red</param>
        /// <param name="blue">brightness value for the color blue</param>
        /// <param name="green">brightness value for the color green</param>
        private void SetLEDSolid(byte device_addr, byte red, byte blue, byte green)
        {
            //open device
            IntPtr device = new IntPtr();
            int status = FT_Open(device_index, ref device);
            if (status != FT_SUCCESS) { updateDeviceStatus(status); return; }

            //configure device
            status = FT4222_I2CMaster_Init(device, BIT_RATE);
            if (status != FT_SUCCESS) { updateDeviceStatus(status); return; }

            //setup write commands
            byte[] reset_cmd = { 0x2F, 0x00 }; //reset command
            byte[] shutdown_cmd = { 0x00, 0x20 }; //shutdown command
            byte[] mode_cmd = { 0x02, 0x00 }; //mode select command
            byte[] current_cmd = { 0x03, 0x10 }; //current select command
            byte[] green_cmd = { 0x04, green }; //green brightness command
            byte[] red_cmd = { 0x05, red }; //red brightness command
            byte[] blue_cmd = { 0x06, blue }; //blue brightness command
            byte[] load_rgb_cmd = { 0x07, 0x00 }; //load brightness commands
            byte[] pulse_1_cmd = { 0x10, 0x00 }; //pulse setting command
            byte[] pulse_2_cmd = { 0x11, 0x00 }; //pulse setting command
            byte[] pulse_3_cmd = { 0x12, 0x00 }; //pulse setting command
            byte[] pulse_4_cmd = { 0x16, 0x00 }; //pulse setting command
            byte[] pulse_5_cmd = { 0x17, 0x00 }; //pulse setting command
            byte[] pulse_6_cmd = { 0x18, 0x00 }; //pulse setting command
            byte[] load_pulse_cmd = { 0x1C, 0x00 }; //load pulse setting command

            //write
            uint num_transferred = 0x0;
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, reset_cmd, Convert.ToUInt32(reset_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, shutdown_cmd, Convert.ToUInt32(shutdown_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, mode_cmd, Convert.ToUInt32(mode_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, current_cmd, Convert.ToUInt32(current_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, green_cmd, Convert.ToUInt32(green_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, red_cmd, Convert.ToUInt32(red_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, blue_cmd, Convert.ToUInt32(blue_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, load_rgb_cmd, Convert.ToUInt32(load_rgb_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_1_cmd, Convert.ToUInt32(pulse_1_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_2_cmd, Convert.ToUInt32(pulse_2_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_3_cmd, Convert.ToUInt32(pulse_3_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_4_cmd, Convert.ToUInt32(pulse_4_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_5_cmd, Convert.ToUInt32(pulse_5_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_6_cmd, Convert.ToUInt32(pulse_6_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, load_pulse_cmd, Convert.ToUInt32(load_pulse_cmd.Length), ref num_transferred);
            FT4222_Uninitialize(device);
            FT_Close(device);
        }

        /// <summary>
        /// Sets an I2C RGB LED to specified brightness values defined by 'red', 'green', and 'blue'.
        /// The LED is pulsed after the values are set.
        /// </summary>
        /// <param name="device_addr">Device address of the LED to be modified.</param>
        /// <param name="red">brightness value for the color red</param>
        /// <param name="blue">brightness value for the color blue</param>
        /// <param name="green">brightness value for the color green</param>
        private void SetLEDPulse(byte device_addr, byte red, byte blue, byte green, byte speed)
        {
            //pulse speed has inverse relationship with input value (when speed is high, pulse is slow)
            //this subtraction changes this to a direct relationship
            speed = Convert.ToByte(0xFF - speed);
            //open device
            IntPtr device = new IntPtr();
            int status = FT_Open(device_index, ref device);
            if (status != FT_SUCCESS) { updateDeviceStatus(status); return; }

            //configure device
            status = FT4222_I2CMaster_Init(device, BIT_RATE);
            if (status != FT_SUCCESS) { updateDeviceStatus(status); return; }

            //setup write commands
            byte[] reset_cmd = { 0x2F, 0x00 }; //reset command
            byte[] shutdown_cmd = { 0x00, 0x20 }; //shutdown command
            byte[] mode_cmd = { 0x02, 0x20 }; //mode select command
            byte[] current_cmd = { 0x03, 0x10 }; //current select command
            byte[] green_cmd = { 0x04, green }; //green brightness command
            byte[] red_cmd = { 0x05, red }; //red brightness command
            byte[] blue_cmd = { 0x06, blue }; //blue brightness command
            byte[] load_rgb_cmd = { 0x07, 0x00 }; //load brightness commands
            byte[] pulse_1_cmd = { 0x10, speed }; //pulse setting command
            byte[] pulse_2_cmd = { 0x11, speed }; //pulse setting command
            byte[] pulse_3_cmd = { 0x12, speed }; //pulse setting command
            byte[] pulse_4_cmd = { 0x16, speed }; //pulse setting command
            byte[] pulse_5_cmd = { 0x17, speed };  //pulse setting command
            byte[] pulse_6_cmd = { 0x18, speed }; //pulse setting command
            byte[] load_pulse_cmd = { 0x1C, 0x00 }; //load pulse setting command

            //write
            uint num_transferred = 0x0;
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, reset_cmd, Convert.ToUInt32(reset_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, shutdown_cmd, Convert.ToUInt32(shutdown_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, mode_cmd, Convert.ToUInt32(mode_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, current_cmd, Convert.ToUInt32(current_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, green_cmd, Convert.ToUInt32(green_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, red_cmd, Convert.ToUInt32(red_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, blue_cmd, Convert.ToUInt32(blue_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, load_rgb_cmd, Convert.ToUInt32(load_rgb_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_1_cmd, Convert.ToUInt32(pulse_1_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_2_cmd, Convert.ToUInt32(pulse_2_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_3_cmd, Convert.ToUInt32(pulse_3_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_4_cmd, Convert.ToUInt32(pulse_4_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_5_cmd, Convert.ToUInt32(pulse_5_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_6_cmd, Convert.ToUInt32(pulse_6_cmd.Length), ref num_transferred);
            status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, load_pulse_cmd, Convert.ToUInt32(load_pulse_cmd.Length), ref num_transferred);
            FT4222_Uninitialize(device);
            FT_Close(device);
        }

        /// <summary>
        /// Sets an I2C RGB LED to specified brightness values defined by 'red', 'green', and 'blue'.
        /// The LED is held steady after the values are set.
        /// </summary>
        /// <param name="device_addr">Device address of the LED to be modified.</param>
        /// <param name="red">brightness value for the color red</param>
        /// <param name="blue">brightness value for the color blue</param>
        /// <param name="green">brightness value for the color green</param>
        private static void SetLEDPoll(IntPtr device, byte device_addr, byte red, byte blue, byte green)
        {
            //int status = 0;
            ////setup write commands
            //byte[] reset_cmd      = { 0x2F, 0x00 }; //reset command
            //byte[] shutdown_cmd   = { 0x00, 0x20 }; //shutdown command
            //byte[] mode_cmd       = { 0x02, 0x00 }; //mode select command
            //byte[] current_cmd    = { 0x03, 0x10 }; //current select command
            //byte[] green_cmd      = { 0x04, green}; //green brightness command
            //byte[] red_cmd        = { 0x05, red  }; //red brightness command
            //byte[] blue_cmd       = { 0x06, blue }; //blue brightness command
            //byte[] load_rgb_cmd   = { 0x07, 0x00 }; //load brightness commands
            //byte[] pulse_1_cmd    = { 0x10, 0x00 }; //pulse setting command
            //byte[] pulse_2_cmd    = { 0x11, 0x00 }; //pulse setting command
            //byte[] pulse_3_cmd    = { 0x12, 0x00 }; //pulse setting command
            //byte[] pulse_4_cmd    = { 0x16, 0x00 }; //pulse setting command
            //byte[] pulse_5_cmd    = { 0x17, 0x00 }; //pulse setting command
            //byte[] pulse_6_cmd    = { 0x18, 0x00 }; //pulse setting command
            //byte[] load_pulse_cmd = { 0x1C, 0x00 }; //load pulse setting command

            ////write
            //uint num_transferred = 0x0;
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, reset_cmd     , Convert.ToUInt32(reset_cmd     .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, shutdown_cmd  , Convert.ToUInt32(shutdown_cmd  .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, mode_cmd      , Convert.ToUInt32(mode_cmd      .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, current_cmd   , Convert.ToUInt32(current_cmd   .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, green_cmd     , Convert.ToUInt32(green_cmd     .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, red_cmd       , Convert.ToUInt32(red_cmd       .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, blue_cmd      , Convert.ToUInt32(blue_cmd      .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, load_rgb_cmd  , Convert.ToUInt32(load_rgb_cmd  .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_1_cmd   , Convert.ToUInt32(pulse_1_cmd   .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_2_cmd   , Convert.ToUInt32(pulse_2_cmd   .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_3_cmd   , Convert.ToUInt32(pulse_3_cmd   .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_4_cmd   , Convert.ToUInt32(pulse_4_cmd   .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_5_cmd   , Convert.ToUInt32(pulse_5_cmd   .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, pulse_6_cmd   , Convert.ToUInt32(pulse_6_cmd   .Length), ref num_transferred);
            //status = FT4222_I2CMaster_WriteEx(device, device_addr, 6, load_pulse_cmd, Convert.ToUInt32(load_pulse_cmd.Length), ref num_transferred);
        }

        private void pollOpticPlugs()
        {
            int status = FT_SUCCESS;
            IntPtr device = new IntPtr();
            bool port_empty = false;
            while (status == FT_SUCCESS)
            {
                //open device
                status = FT_Open(device_index, ref device);
                //configure device
                status = FT4222_I2CMaster_Init(device, BIT_RATE);

                status = FT4222_GPIO_Read(device, 0, ref port_empty);  //SFP inserted
                if (!port_empty)
                    SetLEDPoll(device, SFP_LED_ADDR, 0x80, 0x00, 0x80);
                else
                    SetLEDPoll(device, SFP_LED_ADDR, 0x00, 0x00, 0x00);

                status = FT4222_GPIO_Read(device, 4, ref port_empty);  //XFP inserted
                if (!port_empty)
                    SetLEDPoll(device, XFP_LED_ADDR, 0x80, 0x00, 0x80);
                else
                    SetLEDPoll(device, XFP_LED_ADDR, 0x00, 0x00, 0x00);

                status = FT4222_GPIO_Read(device, 12, ref port_empty); //QSFP inserted
                if (!port_empty)
                    SetLEDPoll(device, QSFP_LED_ADDR, 0x80, 0x00, 0x80);
                else
                    SetLEDPoll(device, QSFP_LED_ADDR, 0x00, 0x00, 0x00);

                FT_Close(device);

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Searches connected devices for the indicies of connected devices matching a description.
        /// </summary>
        /// <returns>array of device indicies</returns>
        public override uint[] searchDevices()
        {
            //initiate by reading the number of devices
            uint num_devices = 0;
            int status = FT_CreateDeviceInfoList(ref num_devices);

            //search through devices for compatible product
            List<uint> device_index = new List<uint>();

            for (uint i = 0; i < num_devices / 2; i++)
            {
                //open device
                IntPtr device = new IntPtr();
                status = FT_Open(i, ref device);
                if (status == FT_SUCCESS)
                {
                    //configure device
                    status = FT4222_I2CMaster_Init(device, BIT_RATE);
                    if (status == FT_SUCCESS)
                    {
                        //Check for device type
                        uint idx_select_written = 0;
                        byte[] deviceInfo = new byte[23];
                        FT4222_I2CMaster_Write(device, 0x57, new byte[] { 0x00 }, 1, ref idx_select_written);
                        FT4222_I2CMaster_Read(device, 0x57, deviceInfo, 23, ref idx_select_written);
                        string deviceInfoString = Encoding.ASCII.GetString(deviceInfo);
                        string deviceProductName = deviceInfoString.Substring(13, 3);
                        if (deviceProductName == "SXQ")
                        {
                            //System.Threading.Thread pollingThread = new System.Threading.Thread(pollOpticPlugs);
                            //pollingThread.Start();
                            device_index.Add(i);
                        }
                    }
                }

                FT_Close(device);
            }
            return device_index.ToArray();
        }

        /// <summary>
        /// Reads in binary values from a FTDI board.
        /// </summary>
        /// <param name="table_addr">Address of table to be read (usually either 0xA0 or 0xA2</param>
        /// <param name="read_size">number of bytes to be read</param>
        /// <returns>Byte array containing values read from a connected FTDI board</returns>
        protected override byte[] BoardRead(uint table_addr, uint read_size, uint read_index)
        {
            table_addr = Convert.ToByte(table_addr / 2);
            byte[] output_array = new byte[read_size];
            //IntPtr device = new IntPtr();

            //open device
            //int status = FT_Open(device_index, ref device);
            //if (status != FT_SUCCESS) { updateDeviceStatus(status); return null; }

            //configure device
            uint idx_select_written = 0;
            int status = FT4222_I2CMaster_Init(device, BIT_RATE);
            if (status != FT_SUCCESS) { updateDeviceStatus(status); return null; }
            FT4222_I2CMaster_Write(device, 0x22, new byte[] { 0x8C, 0x5D, 0x0F, 0x7C }, 4, ref idx_select_written);
            if (status != FT_SUCCESS) { updateDeviceStatus(status); return null; }

            //Set R/W starting byte index
            idx_select_written = 0;
            while (idx_select_written < 1)
            {
                FT4222_I2CMaster_Write(device, (byte)table_addr, new byte[] { (byte)read_index }, 1, ref idx_select_written);
            }

            //get read data
            byte[] out_buffer = new byte[0x80]; //the buffer must be large to avoid overflow. Buffer size != Output array size.
            uint num_read = 0x0;
            uint total_read = 0;
            while ((total_read < read_size) && (status == FT_SUCCESS))
            {
                status = FT4222_I2CMaster_Read(device, (byte)table_addr, out_buffer, read_size, ref num_read);
                //add read bytes to output array
                for (uint i = total_read; i < num_read + total_read; i++)
                {
                    if (i < output_array.Length)
                    {
                        output_array[i] = out_buffer[i - total_read];
                    }
                }
                total_read += num_read;
            }

            Thread.Sleep(READ_WRITE_DELAY);

            // FT_Close(device);
            updateDeviceStatus(status);
            if (status != FT_SUCCESS)
                return null;
            else
                return output_array;
        }

        /// <summary>
        /// Writes a binary array to a SiLabs board
        /// </summary>
        /// <param name="table_addr">address of table to write to (usually 0xA0 or 0xA2)</param>
        /// <param name="start_index">index of first byte to write to (takes values 0x0 to 0xFF)</param>
        /// <param name="write_buf">Array of bytes to write (max length of 0x3C)</param>
        /// <returns>returns boolean FALSE if the procedure failed and TRUE if write was successful</returns>
        protected override bool BoardWrite(uint table_addr, uint start_index, byte[] input_buf)
        {
            // IntPtr device = new IntPtr();
            byte[] write_buf = new byte[input_buf.Length + 1];
            table_addr = Convert.ToByte(table_addr / 2);

            write_buf[0] = (byte)start_index;
            //add the input values to the write buffer
            for (int i = 0; i < input_buf.Length; i++)
            {
                write_buf[i + 1] = input_buf[i];
            }

            //open device
            //int status = FT_Open(device_index, ref device);
            //if (status != FT_SUCCESS) { updateDeviceStatus(status); return false; }

            //configure device
            //status = FT4222_I2CMaster_Init(device, BIT_RATE);
            //if (status != FT_SUCCESS) { updateDeviceStatus(status); return false; }

            int status = FT_SUCCESS;

            //request a write
            byte write_len = 0x0;
            if (write_buf.Length < 0x3D)
            {
                write_len = Convert.ToByte(write_buf.Length);
            }
            else
            {
                write_len = 0x3D;
            }
            uint num_transferred = 0x0;
            uint total_transferred = 0;
            while ((total_transferred < write_buf.Length) && (status == FT_SUCCESS))
            {
                status = FT4222_I2CMaster_WriteEx(device, (byte)table_addr, 6, write_buf, Convert.ToUInt32(write_buf.Length), ref num_transferred);
                total_transferred += num_transferred;
            }
            if (status != FT_SUCCESS) { updateDeviceStatus(status); return false; }


            Thread.Sleep(READ_WRITE_DELAY);

            // FT_Close(device);
            updateDeviceStatus(status);
            return (status == FT_SUCCESS);
        }

        /// <summary>
        /// Decoding function for Read/Write return codes.
        /// </summary>
        /// <param name="status_in">return code from CP2112 read/write operation</param>
        /// <returns>String indicating the return status of the operation</returns>
        private void updateDeviceStatus(int status_in)
        {
            switch (status_in)
            {
                case 0x00:
                    DEVICE_STATUS = DeviceMaster.operation_success;
                    break;
                case 0x01:
                    DEVICE_STATUS = "Invalid Device Handle";
                    break;
                case 0x02:
                    DEVICE_STATUS = "Device Not Found";
                    break;
                case 0x03:
                    DEVICE_STATUS = "Device Not Opened";
                    break;
                case 0x04:
                    DEVICE_STATUS = "I//O Error";
                    break;
                case 0x05:
                    DEVICE_STATUS = "Insufficient Resources";
                    break;
                case 0x6:
                    DEVICE_STATUS = "Invalid Input parameter";
                    break;
                case 0x7:
                    DEVICE_STATUS = "Invalid Baud Rate";
                    break;
                case 0x8:
                    DEVICE_STATUS = "Device Not Opened For Erase";
                    break;
                case 0x9:
                    DEVICE_STATUS = "Device Not Opened For Write";
                    break;
                case 0xA:
                    DEVICE_STATUS = "Failed To Write Device";
                    break;
                case 0xB:
                    DEVICE_STATUS = "EEPROM Read Fail";
                    break;
                case 0xC:
                    DEVICE_STATUS = "EEPROM Write Fail";
                    break;
                case 0xD:
                    DEVICE_STATUS = "EEPROM Erase Fail";
                    break;
                case 0xE:
                    DEVICE_STATUS = "EEPROM Not Present";
                    break;
                case 0xF:
                    DEVICE_STATUS = "EEPROM Not Programmed";
                    break;
                case 0x10:
                    DEVICE_STATUS = "Invalid R/W Arguments";
                    break;
                case 0x11:
                    DEVICE_STATUS = "Device Not Supported";
                    break;
                case 0x12:
                    DEVICE_STATUS = "Device Not Supported";
                    break;
                default:
                    DEVICE_STATUS = "Read/Write Failure";
                    break;
            }
        }
        #endregion
    }
}
