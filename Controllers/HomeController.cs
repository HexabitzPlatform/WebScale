using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebScale.Models;
using DOT_NET_COMS_LIB;
using System.IO.Ports;
using System.Globalization;

namespace WebScale.Controllers
{
    public class HomeController : Controller
    {

        private static SerialPort Port;
        private static HexaInterface HexaInter;

        private static byte DestinationID, SourceID;
        private static byte Options = 0x22;
        private static int Code;

        private static byte channel = 0x01;
        private static byte modulePort = 0x03;
        private static byte module = 0x01;

        private static string receivedValue;

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public ActionResult Connect(string COM, int BaudRate)
        {
            Port = new SerialPort("COM" + COM, BaudRate, Parity.None, 8, StopBits.One);
            HexaInter = new HexaInterface(COM, BaudRate);
            return Json(new { COM, BaudRate });
        }

        [HttpPost]
        public void Disconnect()
        {
            Code = (int)HexaInterface.Message_Codes.CODE_H26R0_STOP;
            byte[] Payload = new byte[0];

            HexaInter.SendMessage(DestinationID, SourceID, Code, Payload);
            Port.Dispose();
        }
        

        [HttpPost]
        public void Read(string TimeOut, string Period, string Unit)
        {
            byte[] periodBytes = BitConverter.GetBytes(int.Parse(Period));
            byte[] timeBytes = BitConverter.GetBytes(int.Parse(TimeOut));

            byte[] Payload = {
                            channel,
                            periodBytes[3],
                            periodBytes[2],
                            periodBytes[1],
                            periodBytes[0],

                            timeBytes[3],
                            timeBytes[2],
                            timeBytes[1],
                            timeBytes[0],

                            modulePort,
                            module};

            Code = (int)HexaInterface.Message_Codes.CODE_H26R0_STREAM_PORT_GRAM;
            switch (Unit)
            {
                case "Gram": Code = (int)HexaInterface.Message_Codes.CODE_H26R0_STREAM_PORT_GRAM; break;
                case "KG": Code = (int)HexaInterface.Message_Codes.CODE_H26R0_STREAM_PORT_KGRAM; break;
                case "Ounces": Code = (int)HexaInterface.Message_Codes.CODE_H26R0_STREAM_PORT_OUNCE; break;
                case "Pounds": Code = (int)HexaInterface.Message_Codes.CODE_H26R0_STREAM_PORT_POUND; break;
                default: break;
            }

            HexaInter.SendMessage(DestinationID, SourceID, Code, Payload);

            Receive();
        }

        // The receiving method to listen to the port if we got any respond from it.
        private void Receive()
        {
            Port.DataReceived += new SerialDataReceivedEventHandler(Port_DataReceived);
            try { Port.Open(); } catch { }
        }

        // Method the .Net platform provide to responed to recieved data from SerialPort.
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[4];
            Port.Read(buffer, 0, 4);

            string D0 = to_right_hex(buffer[3].ToString("X"));
            string D1 = to_right_hex(buffer[2].ToString("X"));
            string D2 = to_right_hex(buffer[1].ToString("X"));
            string D3 = to_right_hex(buffer[0].ToString("X"));
            string weight = D3 + D2 + D1 + D0;

            int IntRep = int.Parse(weight, NumberStyles.AllowHexSpecifier);
            receivedValue = BitConverter.ToSingle(BitConverter.GetBytes(IntRep), 0) + "";
        }

        [HttpGet]
        public ActionResult GetValue()
        {
            return Json(new { receivedValue });
        }

        [HttpPost]
        public void Zero()
        {
            Code = (int)HexaInterface.Message_Codes.CODE_H26R0_ZEROCAL;
            byte[] Payload = { channel };
            HexaInter.SendMessage(DestinationID, SourceID, Code, Payload);
        }

        private string to_right_hex(string hex)
        {
            switch (hex)
            {
                case "A":
                case "B":
                case "C":
                case "D":
                case "E":
                case "F": hex = "0" + hex; break;
            }
            return hex;
        }

    }
}
