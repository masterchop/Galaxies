﻿using NUnit.Framework;
using System;

namespace Leap.Tests
{
    [TestFixture ()]
    public class DeviceTests
    {
        Controller controller;
        Device device = new Device();

        [OneTimeSetUp]
        public void Init ()
        {
            controller = new Controller ();
            System.Threading.Thread.Sleep (500);
            if(!controller.Devices.IsEmpty)
                device = controller.Devices[0];
        }

        [Test ()]
        public void TestCase ()
        {
            Assert.True (controller.IsConnected);
        }

        [Test ()]
        public void Device_horizontalViewAngle ()
        {
            // !!!Device_horizontalViewAngle
            float angleOnLongDimension = device.HorizontalViewAngle;
            // !!!END
            Assert.AreEqual(132f, angleOnLongDimension * 180f/(float)Math.PI);
        }
        
        [Test ()]
        public void Device_range ()
        {
            // !!!Device_range
            float range = device.Range;
            // !!!END
            Assert.AreEqual(470f, range);
        }
        
        [Test ()]
        public void Device_verticalViewAngle ()
        {
            // !!!Device_verticalViewAngle
            float angleOnShortDimension = device.VerticalViewAngle;
            // !!!END
            Assert.AreEqual(115f, angleOnShortDimension * 180f/(float)Math.PI);
        }

        [Test ()]
        public void Device_isSmudged ()
        {
            // !!!Device_isSmudged
            if(device.IsSmudged){
                //Display message to user
            }
            // !!!END
            Assert.False(device.IsSmudged);
        }

        [Test ()]
        public void Device_isLightingBad ()
        {
            // !!!Device_isLightingBad
            if(device.IsLightingBad){
                //Display message to user
            }
            // !!!END
            Assert.False(device.IsLightingBad);
        }

        [Test ()]
        public void Device_operator_equals ()
        {
            Device thisDevice = new Device ();
            Device thatDevice = new Device ();
            // !!!Device_operator_equals
            Boolean isEqual = thisDevice == thatDevice;
            // !!!END
            Assert.False (isEqual);
        
        }
        
        [Test ()]
        public void DeviceList_operator_index ()
        {
            // !!!DeviceList_operator_index
            DeviceList allDevices = controller.Devices;
            for (int index = 0; index < allDevices.Count; index++) {
                Console.WriteLine (allDevices [index]);
            }
            // !!!END
        
        }
        
        [Test ()]
        public void DeviceList_isEmpty ()
        {
            // !!!DeviceList_isEmpty
            if (!controller.Devices.IsEmpty) {
                Device leapDevice = controller.Devices [0];
            }
            // !!!END
        }
    }
}

