using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoardWriteServer
{
    /// <summary>
    /// Definitions for interfacing with all I2C board types in BoardRW.
    /// </summary>
    /// <remarks>
    /// This is an abstract class and does not implement any board communications on
    /// its own. Boards which communicate via I2C should inherit from this class and
    /// implement all abstract methods.
    /// </remarks>
    /// <seealso cref="MDIOProgrammer"/>
    public abstract class I2CProgrammer : IProgrammer
    {
        public static int READ_WRITE_DELAY = DeviceMaster.rw_data_delay;
        public static uint BIT_RATE = DeviceMaster.data_rate;
        public static uint MAX_WRITE_LEN = 8;
        public static uint MAX_READ_LEN = 128;
        public string DEVICE_STATUS = "Device Disconnected";
        public uint device_index;

        /// <summary>
        /// Gets whether the board supports GPIO status indicators.
        /// </summary>
        public abstract bool GPIO_SUPPORT
        {
            get;
            protected set;
        }

        /// <summary>
        /// Checks the GPIO status indicators, if enabled.
        /// </summary>
        /// <returns>An object containing the status of various GPIO signals.</returns>
        public abstract GPIOStatus PullGPIOStatus();

        /// <summary>
        /// An identification string for the board type.
        /// </summary>
        public string device_id
        {
            get;
            set;
        }

        /// <summary>
        /// Searches for connected boards of the board type.
        /// </summary>
        /// <returns>A list of device IDs.</returns>
        public abstract uint[] searchDevices();

        /// <summary>
        /// Reads in data from an optic in the board at the specified page and address.
        /// </summary>
        /// <param name="table_addr">The EEPROM page to read from.</param>
        /// <param name="read_size">The number of bytes to read.</param>
        /// <param name="read_index">The address to start reading from.</param>
        /// <returns>The bytes read from the optic.</returns>
        /// <exception cref="BoardRWException"/>
        public virtual byte[] BoardSafeRead(uint table_addr, uint read_size, uint read_index)
        {
            //ensure max read length is possible
            if (MAX_READ_LEN > 0x100)
            {
                MAX_READ_LEN = 0x100;
            }

            uint read_length = MAX_READ_LEN;
            if (read_size < MAX_READ_LEN)
            {
                read_length = read_size;
            }

            byte[] output_array = new byte[read_size];

            uint iterations = read_size / read_length;
            InitializeDevice();
            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    byte[] chunk = BoardRead(table_addr, (ushort)read_length, BitConverter.GetBytes(read_index + read_length * i)[0]);
                    if (chunk != null)
                        Array.Copy(chunk, 0, output_array, read_length * i, read_length);
                }
            }
            catch (Exception e)
            {
                // System.Windows.MessageBox.Show("ERROR: Exception occurred during read. Please replug board and optic " + 
                //                                "and try again.");
                //ErrorLogger.LogError(e.ToString());
                throw new BoardRWException("Exception occurred during read. See inner exception for further details.", e);
                // return null;
            }
            CloseDevice();
            return output_array;
        }

        /// <summary>
        /// An internal read method for calling the necessary external library functions to
        /// interface with the board and read an optic.
        /// </summary>
        /// <param name="table_addr">The EEPROM page to read from.</param>
        /// <param name="read_size">The number of bytes to read.</param>
        /// <param name="read_index">The address t ostart reading from.</param>
        /// <returns>The bytes read from the optic.</returns>
        protected abstract byte[] BoardRead(uint table_addr, uint read_size, uint read_index);

        /// <summary>
        /// Writes an array of bytes to an optic at the specified page and address. 
        /// </summary>
        /// <param name="table_addr">The EEPROM page to write to.</param>
        /// <param name="start_index">The address to start writing to.</param>
        /// <param name="input_buf">The array of bytes to write.</param>
        /// <returns>Whether the write operation succeeded.</returns>
        /// <exception cref="BoardRWException"/>
        public virtual bool BoardSafeWrite(uint table_addr, uint start_index, byte[] input_buf)
        {
            if (input_buf == null)
            {
                return false;
            }
            else
            {
                //adjust input array size to fit table size
                int write_buf_len = 0;
                if (256 < input_buf.Length)
                {
                    write_buf_len = 256;
                }
                else
                {
                    write_buf_len = input_buf.Length;
                }
                byte[] write_buf = new byte[write_buf_len];
                Array.Copy(input_buf, write_buf, write_buf_len);

                //split write buffer into 16 byte chunks and write
                int num_write_arrays = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(write_buf_len) / MAX_WRITE_LEN));
                if (!InitializeDevice())
                {
                    return false;
                }
                try
                {
                    for (int i = 0; i < num_write_arrays; i++)
                    {
                        int temp_array_len = 0;
                        int byte_lvl_offset = Convert.ToInt32(i * MAX_WRITE_LEN);
                        byte temp_start_idx = Convert.ToByte(start_index + byte_lvl_offset);
                        if (MAX_WRITE_LEN + byte_lvl_offset - 1 < write_buf_len)
                        {
                            temp_array_len = Convert.ToInt32(MAX_WRITE_LEN);
                        }
                        else
                        {
                            temp_array_len = write_buf_len - byte_lvl_offset;
                        }

                        byte[] temp_array = new byte[temp_array_len];
                        Array.Copy(write_buf, byte_lvl_offset, temp_array, 0, temp_array_len);

                        if (!BoardWrite(table_addr, temp_start_idx, temp_array))
                        {
                            CloseDevice();
                            return false;
                        }
                    }
                }
                catch (Exception e)
                {
                    //System.Windows.MessageBox.Show("ERROR: Exception occurred during write. Please replug board and optic and " +
                    //                               "try again.");
                    //ErrorLogger.LogError(e.ToString());
                    CloseDevice();
                    throw new BoardRWException("Exception occurred during write. See inner exception for further details.", e);
                    // return false;
                }
                CloseDevice();
                return true;
            }
        }

        /// <summary>
        /// Writes a single byte to an optic at the specified page and address.
        /// </summary>
        /// <param name="table_addr">The EEPROM page to write to.</param>
        /// <param name="start_index">The address to write to.</param>
        /// <param name="input_val">The byte to write.</param>
        /// <returns>Whether the write operation succeeded.</returns>
        /// <exception cref="BoardRWException"/>
        public virtual bool BoardSafeWrite(uint table_addr, uint start_index, byte input_val)
        {
            byte[] input_array = { input_val };
            if (!InitializeDevice())
            {
                return false;
            }
            else
            {
                bool success = false;
                try
                {
                    success = BoardWrite(table_addr, start_index, input_array);
                }
                catch (Exception e)
                {
                    //System.Windows.MessageBox.Show("ERROR: Exception occurred during write. Please replug board and optic and " +
                    //                               "try again.");
                    CloseDevice();
                    //ErrorLogger.LogError(e.ToString());
                    throw new BoardRWException("Exception occurred during write. See inner exception for further details.", e);
                }
                CloseDevice();
                return success;
            }
            // return BoardWrite(table_addr, start_index, input_array);
        }

        /// <summary>
        /// An internal write method for calling the necessary external library functions to
        /// interface with the board and write to an optic.
        /// </summary>
        /// <param name="table_addr">The EEPROM page to write to.</param>
        /// <param name="start_index">The address to write to.</param>
        /// <param name="input_buf">The bytes to write.</param>
        /// <returns>Whether the write operation succeeded.</returns>
        protected abstract bool BoardWrite(uint table_addr, uint start_index, byte[] input_buf);

        /// <summary>
        /// Initializes a board for reading and writing.
        /// </summary>
        /// <returns>Whether the board initialized successfully.</returns>
        /// <remarks>This method should rarely, if ever, be called from an outside class.
        /// <c>BoardSafeRead</c> and <c>BoardSafeWrite</c> should call this method on their
        /// own when preparing a read or write.</remarks>
        public abstract bool InitializeDevice();

        /// <summary>
        /// Closes a board after a read or write.
        /// </summary>
        /// <remarks>This method should rarely, if ever, be called from an outside class.
        /// <c>BoardSafeRead</c> and <c>BoardSafeWrite</c> should call this method on their
        /// own when finishing a read or write.</remarks>
        public abstract void CloseDevice();
    }

    /// <summary>
    /// Definitions for interfacing with all MDIO board types in BoardRW.
    /// </summary>
    public abstract class MDIOProgrammer : IProgrammer
    {
        public static int READ_WRITE_DELAY = DeviceMaster.rw_data_delay;
        public static uint BIT_RATE = DeviceMaster.data_rate;
        public static uint MAX_WRITE_LEN = 8;
        public static uint MAX_READ_LEN = 128;
        public static string DEVICE_STATUS = "Device Disconnected";
        public static uint device_index;

        /// <summary>
        /// Gets whether the board supports GPIO status indicators.
        /// </summary>
        public abstract bool GPIO_SUPPORT
        {
            get;
            protected set;
        }

        /// <summary>
        /// Checks the GPIO status indicators, if enabled.
        /// </summary>
        /// <returns>An object containing the status of various GPIO signals.</returns>
        public abstract GPIOStatus PullGPIOStatus();

        /// <summary>
        /// An identification string for the board type.
        /// </summary>
        public string device_id
        {
            get;
            set;
        }

        /// <summary>
        /// Searches for connected boards of the board type.
        /// </summary>
        /// <returns>A list of device IDs.</returns>
        public abstract uint[] searchDevices();

        /// <summary>
        /// Initializes a board for reading and writing.
        /// </summary>
        /// <returns>Whether the board initialized successfully.</returns>
        /// <remarks>This method should rarely, if ever, be called from an outside class.
        /// <c>BoardSafeRead</c> and <c>BoardSafeWrite</c> should call this method on their
        /// own when preparing a read or write.</remarks>
        public abstract bool InitializeDevice();

        /// <summary>
        /// Closes a board after a read or write.
        /// </summary>
        /// <remarks>This method should rarely, if ever, be called from an outside class.
        /// <c>BoardSafeRead</c> and <c>BoardSafeWrite</c> should call this method on their
        /// own when finishing a read or write.</remarks>
        public abstract void CloseDevice();

        /// <summary>
        /// Reads in data from an optic in the board at the specified page and address.
        /// </summary>
        /// <param name="table_addr">The EEPROM page to read from.</param>
        /// <param name="read_size">The number of bytes to read.</param>
        /// <param name="read_index">The address to start reading from.</param>
        /// <returns>The bytes read from the optic.</returns>
        /// <exception cref="BoardRWException"/>
        public abstract byte[] BoardSafeRead(uint table_addr, uint read_size, uint read_index);

        /// <summary>
        /// Writes an array of bytes to an optic at the specified page and address. 
        /// </summary>
        /// <param name="table_addr">The EEPROM page to write to.</param>
        /// <param name="start_index">The address to start writing to.</param>
        /// <param name="input_buf">The array of bytes to write.</param>
        /// <returns>Whether the write operation succeeded.</returns>
        /// <exception cref="BoardRWException"/>
        public abstract bool BoardSafeWrite(uint table_addr, uint start_index, byte[] input_buf);

        /// <summary>
        /// Writes a single byte to an optic at the specified page and address.
        /// </summary>
        /// <param name="table_addr">The EEPROM page to write to.</param>
        /// <param name="start_index">The address to write to.</param>
        /// <param name="input_val">The byte to write.</param>
        /// <returns>Whether the write operation succeeded.</returns>
        /// <exception cref="BoardRWException"/>
        public abstract bool BoardSafeWrite(uint table_addr, uint start_index, byte input_val);

        /// <summary>
        /// An internal read method for calling the necessary external library functions to
        /// interface with the board and read from an optic.
        /// </summary>
        /// <param name="memory_address">The register address to start reading from.</param>
        /// <param name="read_size">The number of words to read.</param>
        /// <returns>The bytes read from the optic.</returns>
        /// <remarks>
        /// MDIO devices, unlike I2C devices, read directly from a register address that
        /// contains the "table" number as part of the address. For example, instead of
        /// reading from table <c>80</c>, address <c>0x10</c>, the device reads from register
        /// address <c>0x8010</c>.
        /// </remarks>
        protected abstract byte[] BoardRead(uint memory_address, uint read_size);

        /// <summary>
        /// An internal write method for calling the necessary external library functions to
        /// interface with the board and write to the optic.
        /// </summary>
        /// <param name="memory_address"></param>
        /// <param name="input_buf"></param>
        /// <returns></returns>
        /// <remarks>
        /// MDIO devices, unlike I2C devices, read directly from a register address that
        /// contains the "table" number as part of the address. For example, instead of
        /// reading from table <c>80</c>, address <c>0x10</c>, the device reads from register
        /// address <c>0x8010</c>.
        /// </remarks>
        protected abstract bool BoardWrite(uint memory_address, byte[] input_buf);
    }

    /// <summary>
    /// Definitions for interfacing with all board types in BoardRW.
    /// </summary>
    /// <remarks>
    /// This class contains static methods for interfacing with any board type supported by the
    /// host application. By default, <c>DeviceMaster</c> does not contain any references to any
    /// <see cref="IProgrammer"/> objects. At application initialization, the application must
    /// declare to the <c>DeviceMaster</c> what boards it supports by adding a new instance of each board
    /// type to the <see cref="i2c_devices"/> and <see cref="mdio_devices"/> containers. This must
    /// occur before the application attempts to perform any board operations through <c>DeviceMaster</c>
    /// or else the operations will fail.
    /// </remarks>
    public class DeviceMaster
    {
        #region Constant Definitions
        //file location
        public const string dll_location = "";

        //board type definitions
        public const string cp2112_device_id = "CP2112 Device";
        public const string usbx_lt_device_id = "LT USBXpress Device";
        public const string ft4222a_device_id = "FT4222-A";
        public const string ft4222b_device_id = "FT4222-B";
        public const string cyusb_device_id = "EUI Device";
        public const string sub20_device_id = "SUB20 Device";
        public const string fbhid_device_id = "FB HID Device";

        // device index definitions for searchDevices

        public const string operation_success = "Success";

        /// <summary>
        /// An enumerator for specifying the current communication protocol.
        /// </summary>
        public enum Protocol
        {
            I2C,
            MDIO
        };
        #endregion

        #region Private Read/Write Variables
        private static int READ_WRITE__DELAY = 50;
        private static uint BIT_RATE = 100000;  //bit rate in Hz
        private static uint MAX_WRITE_LEN = 8;     //In bytes
        private static uint MAX_READ_LEN = 0x80;   //In bytes
        private static string DEVICE_STATUS = "Device Disconnected";

        /// <summary>
        /// A list of all supported I2C devices.
        /// </summary>
        /// <remarks>
        /// This must be populated by the application during initialization.
        /// </remarks>
        public static List<I2CProgrammer> i2c_devices = new List<I2CProgrammer>()
        {
        };

        private static I2CProgrammer selected_i2c;

        /// <summary>
        /// A list of all supported MDIO devices.
        /// </summary>
        /// <remarks>
        /// This must be populated by the application during initialization.
        /// </remarks>
        public static List<MDIOProgrammer> mdio_devices = new List<MDIOProgrammer>
        {
        };

        private static MDIOProgrammer selected_mdio;
        #endregion

        #region Get/Set definitions for read/write constants

        public static bool gpio_sfp_mod_abs = true;
        public static bool gpio_sfp_tx_en = true;
        public static bool gpio_sfp_tx_fault = true;
        public static bool gpio_sfp_los = true;
        public static bool gpio_xfp_mod_abs = true;
        public static bool gpio_xfp_tx_en = true;
        public static bool gpio_xfp_int = true;
        public static bool gpio_xfp_rst = true;
        public static bool gpio_xfp_mod_nr = true;
        public static bool gpio_xfp_los = true;
        public static bool gpio_qfp_mod_prs = true;
        public static bool gpio_qfp_int = true;
        public static bool gpio_qfp_rst = true;
        public static bool gpio_qfp_lp = true;
        public static bool gpio_qfp_mod_sel = true;

        /// <summary>
        /// Provides access to the internal WRITE_BIT_RATE variable
        /// </summary>
        public static uint data_rate
        {
            get
            {
                return BIT_RATE;
            }
            set
            {
                BIT_RATE = value;
                I2CProgrammer.BIT_RATE = value;
                MDIOProgrammer.BIT_RATE = value;
                //CP2112Device.BIT_RATE = value;
                //FT4222ADevice.BIT_RATE = value;
            }
        }

        /// <summary>
        /// Provides access to the internal WRITE_BIT_RATE variable
        /// </summary>
        public static uint read_data_size
        {
            get
            {
                return MAX_READ_LEN;
            }
            set
            {
                MAX_READ_LEN = value;
                I2CProgrammer.MAX_READ_LEN = value;
                MDIOProgrammer.MAX_READ_LEN = value;
                //CP2112Device.MAX_READ_LEN = value;
                //FT4222ADevice.MAX_READ_LEN = value;
            }
        }

        /// <summary>
        /// Provides access to the internal WRITE_BIT_RATE variable
        /// </summary>
        public static uint write_data_size
        {
            get
            {
                return MAX_WRITE_LEN;
            }
            set
            {
                MAX_WRITE_LEN = value;
                I2CProgrammer.MAX_WRITE_LEN = value;
                MDIOProgrammer.MAX_WRITE_LEN = value;
                //CP2112Device.MAX_WRITE_LEN = value;
                //FT4222ADevice.MAX_WRITE_LEN = value;
            }
        }

        /// <summary>
        /// Provides access to the internal WRITE_DELAY variable
        /// </summary>
        public static int rw_data_delay
        {
            get
            {
                return READ_WRITE__DELAY;
            }
            set
            {
                READ_WRITE__DELAY = value;
                I2CProgrammer.READ_WRITE_DELAY = value;
                MDIOProgrammer.READ_WRITE_DELAY = value;
                //CP2112Device.READ_WRITE_DELAY = value;
                //LTUSBXpressDevice.READ_WRITE_DELAY = value;
                //FT4222ADevice.READ_WRITE_DELAY = value;
            }
        }

        /// <summary>
        /// Provides access to the internal DEVICE_STATUS variable
        /// </summary>
        public static string device_status
        {
            get
            {
                return DEVICE_STATUS;
            }
            set
            {
                DEVICE_STATUS = value;
            }
        }

        /// <summary>
        /// provides a method for changing the types of devices used by devicemaster
        /// </summary>
        public static Protocol communication_protocol
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Method for accessing MDIO and I2C board search methods
        /// </summary>
        /// <returns>array of board ID's to select from</returns>
        public static string[] searchDevices()
        {
            switch (communication_protocol)
            {
                case Protocol.MDIO:
                    return searchMDIODevices();
                default:
                    return searchI2CDevices();
            }
        }

        /// <summary>
        /// Method for accessing MDIO and I2C board selection methods
        /// </summary>
        /// <param name="selected_value">ID value generated by searchDevices used to select a board</param>
        public static void selectBoard(string selected_value)
        {
            switch (communication_protocol)
            {
                case Protocol.MDIO:
                    selectMDIOBoard(selected_value);
                    break;
                default:
                    selectI2CBoard(selected_value);
                    break;
            }
        }

        /// <summary>
        /// Method for accessing MDIO or I2C write methods
        /// </summary>
        /// <param name="table_addr">device address to be accessed</param>
        /// <param name="start_index">where to start reading from</param>
        /// <param name="input_buf">bytes to write</param>
        /// <returns></returns>
        public static bool Write(byte table_addr, int start_index, byte[] input_buf)
        {
            switch (communication_protocol)
            {
                case Protocol.MDIO:
                    return MDIOWrite(table_addr, (uint)start_index, input_buf);
                default:
                    return I2CWrite(table_addr, BitConverter.GetBytes(start_index)[0], input_buf);
            }
        }

        /// <summary>
        /// Method for accessing MDIO or I2C write methods
        /// </summary>
        /// <param name="table_addr">device address to be accessed</param>
        /// <param name="start_index">size of read in bytes</param>
        /// <param name="input_val">memory address</param>
        /// <returns></returns>
        public static bool Write(byte table_addr, int start_index, byte input_val)
        {

            switch (communication_protocol)
            {
                case Protocol.MDIO:
                    return MDIOWrite(table_addr, (uint)start_index, input_val);
                default:
                    return I2CWrite(table_addr, BitConverter.GetBytes(start_index)[0], input_val);
            }

        }

        /// <summary>
        /// Method for accessing MDIO or I2C read methods
        /// </summary>
        /// <param name="table_addr">device address to be accessed</param>
        /// <param name="read_size">size of read in bytes</param>
        /// <param name="read_index">memory address</param>
        /// <returns></returns>
        public static byte[] Read(byte table_addr, int read_size, int read_index)
        {

            switch (communication_protocol)
            {
                case Protocol.MDIO:
                    return MDIORead(table_addr, Convert.ToUInt32(read_size / 2), (uint)read_index);
                default:
                    return I2CRead(table_addr, Convert.ToUInt32(read_size), BitConverter.GetBytes(read_index)[0]);
            }

        }

        #region I2C controls
        /// <summary>
        /// Searches for the indicies of connected devices with valid vid's and pid's.
        /// </summary>
        /// <returns>array of arrays of device indicies. Each array index indicates a different board type, 
        /// and board indicies are defined by the DeviceMaster object</returns>
        private static string[] searchI2CDevices()
        {
            List<string> device_list = new List<string>();
            foreach (I2CProgrammer pro in i2c_devices)
            {
                try
                {
                    foreach (uint i in pro.searchDevices())
                    {
                        device_list.Add(pro.device_id + " " + i.ToString());
                    }
                }
                //search for devices failed, ignore device set
                catch (Exception)
                {
                    // We should probably log that we hit an error here though
                    // ErrorLogger.LogError(e.ToString());
                }
            }
            return device_list.ToArray();
        }

        /// <summary>
        /// function provided to allow the user to select the device being used for interacting with optic components.
        /// </summary>
        /// <param name="selected_value">value indicating the index of the board selected</param>
        private static void selectI2CBoard(string selected_value)
        {
            foreach (I2CProgrammer pro in i2c_devices)
            {
                if (selected_value.Contains(pro.device_id))
                {
                    selected_i2c = pro;
                    selected_i2c.device_index = Convert.ToUInt32(Char.GetNumericValue(selected_value[selected_value.Length - 1]));
                }
            }
        }

        //private async void EnumerateHidDevices()
        //{
        //    // Microsoft Input Configuration Device.
        //    ushort vendorId = 0x045E;
        //    ushort productId = 0x07CD;
        //    ushort usagePage = 0x000D;
        //    ushort usageId = 0x000E;

        //    // Create the selector.
        //    string selector =
        //        HidDevice.GetDeviceSelector(usagePage, usageId, vendorId, productId);

        //    // Enumerate devices using the selector.
        //    var devices = await DeviceInformation.FindAllAsync(selector);

        //    if (devices.Any())
        //    { }
        //}

        /// <summary>
        /// Selects a write command based on the selected board (defaults to CP2112Device)
        /// </summary>
        /// <param name="table_addr">address of table to write to (usually 0xA0 or 0xA2)</param>
        /// <param name="start_index">index of first byte to write to (takes values 0x0 to 0xFF)</param>
        /// <param name="input_buf">Array of bytes to write</param>
        /// <returns>returns boolean FALSE if the procedure failed and TRUE if write was successful</returns>
        private static bool I2CWrite(byte table_addr, byte start_index, byte[] input_buf)
        {
            if (selected_i2c != null)
            {
                bool output_val = selected_i2c.BoardSafeWrite(table_addr, start_index, input_buf);
                DEVICE_STATUS = selected_i2c.DEVICE_STATUS;

                return output_val;
            }
            else return false;
        }

        /// <summary>
        /// Selects a write command based on the selected board (defaults to CP2112Device)
        /// </summary>
        /// <param name="table_addr">address of table to write to (usually 0xA0 or 0xA2)</param>
        /// <param name="start_index">index of first byte to write to (takes values 0x0 to 0xFF)</param>
        /// <param name="input_val">Array of bytes to write</param>
        /// <returns>returns boolean FALSE if the procedure failed and TRUE if write was successful</returns>
        private static bool I2CWrite(byte table_addr, byte start_index, byte input_val)
        {
            if (selected_i2c != null)
            {
                bool output_val = selected_i2c.BoardSafeWrite(table_addr, start_index, input_val);
                DEVICE_STATUS = selected_i2c.DEVICE_STATUS;
                return output_val;
            }
            else return false;
        }

        /// <summary>
        /// Selects a write command based on the selected board (defaults to CP2112Device)
        /// </summary>
        /// <param name="table_addr">Address of table to be read (usually either 0xA0 or 0xA2</param>
        /// <param name="read_size">number of bytes to be read</param>
        /// <param name="read_index">starting index of values to be read (usually between 0x0 and 0xFF)</param>
        /// <returns>Byte array containing values read from a connected SiLabs board</returns>
        private static byte[] I2CRead(byte table_addr, uint read_size, byte read_index)
        {
            byte[] gpio_status = new byte[] { 0xFF, 0xFF, 0xFF };

            if (selected_i2c != null)
            {
                // if (selected_i2c.device_id == new FT4222BDevice().device_id)
                //     gpio_status = new FT4222BDevice().PullGPIOStatus();

                byte[] output_val = selected_i2c.BoardSafeRead(table_addr, read_size, read_index);
                DEVICE_STATUS = selected_i2c.DEVICE_STATUS;

                updateGPIOStatus(gpio_status);
                return output_val;
            }
            return new byte[0];
        }
        #endregion

        #region MDIO controls
        /// <summary>
        /// Searches for the indicies of connected devices with valid vid's and pid's.
        /// </summary>
        /// <returns>array of arrays of device indicies. Each array index indicates a different board type, 
        /// and board indicies are defined by the DeviceMaster object</returns>
        private static string[] searchMDIODevices()
        {
            List<string> device_list = new List<string>();
            foreach (MDIOProgrammer pro in mdio_devices)
            {
                try
                {
                    foreach (uint i in pro.searchDevices())
                    {
                        device_list.Add(pro.device_id + " " + i.ToString());
                    }
                }
                //search for devices failed, ignore device set
                catch (Exception)
                {
                    // ErrorLogger.LogError(e.ToString());
                    // Probably throw some sort of error back up the chain to let the application know what
                    // happened
                }
            }
            return device_list.ToArray();
        }

        /// <summary>
        /// function provided to allow the user to select the device being used for interacting with optic components.
        /// </summary>
        /// <param name="selected_value">value indicating the index of the board selected</param>
        private static void selectMDIOBoard(string selected_value)
        {
            foreach (MDIOProgrammer pro in mdio_devices)
            {
                if (selected_value.Contains(pro.device_id))
                {
                    selected_mdio = pro;
                    MDIOProgrammer.device_index = Convert.ToUInt32(Char.GetNumericValue(selected_value[selected_value.Length - 1]));
                }
            }
        }

        /// <summary>
        /// Selects a write command based on the selected board
        /// </summary>
        /// <param name="memory_address">address of table to write to (usually 0xA0 or 0xA2)</param>
        /// <param name="input_buf">Array of bytes to write</param>
        /// <returns>returns boolean FALSE if the procedure failed and TRUE if write was successful</returns>
        private static bool MDIOWrite(uint tableAddr, uint startIndex, byte[] input_buf)
        {
            if (selected_mdio != null)
            {
                bool output_val = selected_mdio.BoardSafeWrite(tableAddr, startIndex, input_buf);
                DEVICE_STATUS = MDIOProgrammer.DEVICE_STATUS;
                return output_val;
            }
            else return false;
        }

        /// <summary>
        /// Selects a write command based on the selected board (defaults to CP2112Device)
        /// </summary>
        /// <param name="memory_address">address of table to write to (usually 0xA0 or 0xA2)</param>
        /// <param name="input_val">Array of bytes to write</param>
        /// <returns>returns boolean FALSE if the procedure failed and TRUE if write was successful</returns>
        private static bool MDIOWrite(uint tableAddr, uint startIndex, byte input_val)
        {
            if (selected_mdio != null)
            {
                bool output_val = selected_mdio.BoardSafeWrite(tableAddr, startIndex, new byte[] { input_val });
                DEVICE_STATUS = selected_i2c.DEVICE_STATUS;
                return output_val;
            }
            else return false;
        }

        /// <summary>
        /// Selects a write command based on the selected board (defaults to CP2112Device)
        /// </summary>
        /// <param name="memory_address">Address of table to be read (usually either 0xA0 or 0xA2</param>
        /// <param name="read_size">number of bytes to be read</param>
        /// <returns>Byte array containing values read from a connected SiLabs board</returns>
        private static byte[] MDIORead(uint tableAddr, uint read_size, uint startIndex)
        {
            if (selected_mdio != null)
            {
                byte[] output_val = selected_mdio.BoardSafeRead(tableAddr, read_size, startIndex);
                DEVICE_STATUS = MDIOProgrammer.DEVICE_STATUS;

                return output_val;
            }
            return new byte[0];
        }
        #endregion

        /// <summary>
        /// Update GPIO status bits
        /// </summary>
        /// <param name="status_array">array of GPIO status bits</param>
        private static void updateGPIOStatus(byte[] status_array)
        {
            if (status_array != null)
            {
                gpio_sfp_mod_abs = (status_array[0] & (1 << 0)) != 0;
                gpio_sfp_tx_en = (status_array[0] & (1 << 1)) != 0;
                gpio_sfp_tx_fault = (status_array[0] & (1 << 2)) != 0;
                gpio_sfp_los = (status_array[0] & (1 << 3)) != 0;
                gpio_xfp_mod_abs = (status_array[0] & (1 << 4)) != 0;
                gpio_xfp_tx_en = (status_array[0] & (1 << 5)) != 0;
                gpio_xfp_int = (status_array[0] & (1 << 6)) != 0;
                gpio_xfp_rst = (status_array[0] & (1 << 7)) != 0;
                gpio_xfp_mod_nr = (status_array[1] & (1 << 0)) != 0;
                gpio_xfp_los = (status_array[1] & (1 << 1)) != 0;
                gpio_qfp_mod_prs = (status_array[1] & (1 << 2)) != 0;
                gpio_qfp_int = (status_array[1] & (1 << 3)) != 0;
                gpio_qfp_rst = (status_array[1] & (1 << 4)) != 0;
                gpio_qfp_lp = (status_array[1] & (1 << 5)) != 0;
                gpio_qfp_mod_sel = (status_array[1] & (1 << 6)) != 0;
            }
        }
    }

    /// <summary>
    /// Represents an exception thrown by a board during a read or write operation.
    /// </summary>
    public class BoardRWException : Exception
    {
        /// <summary>
        /// Constructs a new <c>BoardRWException</c>.
        /// </summary>
        public BoardRWException()
        {

        }

        /// <summary>
        /// Constructs a new <c>BoardRWException</c> with the specified <c>message</c>.
        /// </summary>
        /// <param name="message"></param>
        public BoardRWException(string message) : base(message)
        {

        }

        /// <summary>
        /// Constructs a new <c>BoardRWException</c> with the specified <c>message</c>
        /// along with the internal exception that caused this exception to be thrown.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public BoardRWException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}
