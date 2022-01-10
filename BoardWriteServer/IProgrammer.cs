using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoardWriteServer
{
    /// <summary>
    /// An interface for different programming board types. Standardizes various function names used across
    /// all board types so that DeviceMaster can keep a single list of all boards. Also will help to create
    /// support for boards that have both I2C and MDIO interfaces.
    /// </summary>
    /// <remarks>
    /// Ideally, this interface should not be used directly by a specific class for a specific board type. 
    /// Instead, an abstract class for a group of boards, such as <see cref="I2CProgrammer"/> or
    /// <see cref="MDIOProgrammer"/>, should implement this interface first. This will allow the
    /// <see cref="DeviceMaster"/> to perform nearly all operations based on a single list of <c>IProgrammer</c>
    /// objects while also being able to differentiate and call specific functionality for different protocols.
    /// </remarks>
    /// <seealso cref="I2CProgrammer"/>
    /// <seealso cref="MDIOProgrammer"/>
    public interface IProgrammer
    {
        /// <summary>
        /// Gets whether the board supports GPIO status indicators.
        /// </summary>
        bool GPIO_SUPPORT { get; }

        /// <summary>
        /// Checks the GPIO status indicators, if enabled.
        /// </summary>
        /// <returns>An array containing GPIO status indicators. If not supported, returns an empty array.</returns>
        /// <exception cref="NotImplementedException">Thrown when the board does not have GPIO status indicators.</exception>
        /// <remarks>
        /// The application should always check <see cref="GPIO_SUPPORT"/> before calling this method, as it will throw
        /// an error on any board that does not support this feature.
        /// </remarks>
        /// <example>
        /// <code>
        /// if(exampleProgrammer.GPIO_SUPPORT)
        /// {
        ///     GPIOStatus status = exampleProgrammer.PullGPIOStatus();
        /// }
        /// </code>
        /// </example>
        GPIOStatus PullGPIOStatus();

        /// <summary>
        /// The ID string of the board type.
        /// </summary>
        string device_id { get; }
        /// <summary>
        /// Searches for all attached devices of the board type.
        /// </summary>
        /// <returns>A list of device IDs for the selected board type.</returns>
        uint[] searchDevices();

        /// <summary>
        /// Reads in data from an optic in the board at the specified page and address.
        /// </summary>
        /// <param name="table_addr">The EEPROM page to read from.</param>
        /// <param name="read_size">The number of bytes to read.</param>
        /// <param name="read_index">The address to start reading from.</param>
        /// <returns>The bytes read from the optic.</returns>
        /// <exception cref="BoardRWException">Thrown when the board encounters a critical
        /// exception during the write.</exception>
        byte[] BoardSafeRead(uint table_addr, uint read_size, uint read_index);

        /// <summary>
        /// Writes an array of bytes to an optic at the specified page and address. 
        /// </summary>
        /// <param name="table_addr">The EEPROM page to write to.</param>
        /// <param name="start_index">The address to start writing to.</param>
        /// <param name="input_buf">The array of bytes to write.</param>
        /// <returns>Whether the write operation succeeded.</returns>
        /// <exception cref="BoardRWException">Thrown when the board encounters a critical
        /// exception during the write.</exception>
        bool BoardSafeWrite(uint table_addr, uint start_index, byte[] input_buf);
        /// <summary>
        /// Writes a single byte to an optic at the specified page and address.
        /// </summary>
        /// <param name="table_addr">The EEPROM page to write to.</param>
        /// <param name="start_index">The address to write to.</param>
        /// <param name="input_val">The byte to write.</param>
        /// <returns>Whether the write operation succeeded.</returns>
        /// <exception cref="BoardRWException">Thrown when the board encounters a critical
        /// exception during the write.</exception>
        bool BoardSafeWrite(uint table_addr, uint start_index, byte input_val);

        /// <summary>
        /// Initializes a board for reading and writing.
        /// </summary>
        /// <returns>Whether the board initialized successfully.</returns>
        /// <remarks>This method should rarely, if ever, be called from an outside class.
        /// <c>BoardSafeRead</c> and <c>BoardSafeWrite</c> should call this method on their
        /// own when preparing a read or write.</remarks>
        bool InitializeDevice();
        /// <summary>
        /// Closes a board after a read or write.
        /// </summary>
        /// <remarks>This method should rarely, if ever, be called from an outside class.
        /// <c>BoardSafeRead</c> and <c>BoardSafeWrite</c> should call this method on their
        /// own when finishing a read or write.</remarks>
        void CloseDevice();
    }
}
