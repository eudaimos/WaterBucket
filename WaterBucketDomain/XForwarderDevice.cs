using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;
using ZeroMQ.Devices;

namespace WaterBucket.Domain
{
    public class XForwarderDevice : Device
    {
        public const SocketType FrontendType = SocketType.XSUB;

        public const SocketType BackendType = SocketType.XPUB;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwarderDevice"/> class that will run in a
        /// self-managed thread.
        /// </summary>
        /// <param name="context">The <see cref="ZmqContext"/> to use when creating the sockets.</param>
        /// <param name="frontendBindAddr">The address used to bind the frontend socket.</param>
        /// <param name="backendBindAddr">The endpoint used to bind the backend socket.</param>
        public XForwarderDevice(ZmqContext context, string frontendBindAddr, string backendBindAddr)
            : this(context)
        {
            FrontendSetup.Bind(frontendBindAddr);
            FrontendSetup.SubscribeAll();
            BackendSetup.Bind(backendBindAddr);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwarderDevice"/> class.
        /// </summary>
        /// <param name="context">The <see cref="ZmqContext"/> to use when creating the sockets.</param>
        /// <param name="frontendBindAddr">The address used to bind the frontend socket.</param>
        /// <param name="backendBindAddr">The endpoint used to bind the backend socket.</param>
        /// <param name="mode">The <see cref="DeviceMode"/> for the current device.</param>
        public XForwarderDevice(ZmqContext context, string frontendBindAddr, string backendBindAddr, DeviceMode mode)
            : this(context, mode)
        {
            FrontendSetup.Bind(frontendBindAddr);
            FrontendSetup.SubscribeAll();
            BackendSetup.Bind(backendBindAddr);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwarderDevice"/> class that will run in a
        /// self-managed thread.
        /// </summary>
        /// <param name="context">The <see cref="ZmqContext"/> to use when creating the sockets.</param>
        public XForwarderDevice(ZmqContext context)
            : base(context.CreateSocket(FrontendType), context.CreateSocket(BackendType), DeviceMode.Threaded)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwarderDevice"/> class.
        /// </summary>
        /// <param name="context">The <see cref="ZmqContext"/> to use when creating the sockets.</param>
        /// <param name="mode">The <see cref="DeviceMode"/> for the current device.</param>
        public XForwarderDevice(ZmqContext context, DeviceMode mode)
            : base(context.CreateSocket(FrontendType), context.CreateSocket(BackendType), mode)
        {
        }

        /// <summary>
        /// Forwards requests from the frontend socket to the backend socket.
        /// </summary>
        /// <param name="args">A <see cref="SocketEventArgs"/> object containing the poll event args.</param>
        protected override void FrontendHandler(SocketEventArgs args)
        {
            FrontendSocket.Forward(BackendSocket);
        }

        /// <summary>
        /// Not implemented for the <see cref="ForwarderDevice"/>.
        /// </summary>
        /// <param name="args">A <see cref="SocketEventArgs"/> object containing the poll event args.</param>
        protected override void BackendHandler(SocketEventArgs args)
        {
        }
    }
}
