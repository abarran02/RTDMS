// Description: Interim (version 1) IoT bootcamp development project (Remote Data Center Monitoring) prototype
// Skill Level:  university engineering or comp sci ( sophomore and higher)

// This file implements the primary RDTMS driver code functionality
// sources:  derived from original example by Cam Soper: https://www.linkedin.com/in/camthegeek

// MQTT / Security refs 
// IOT Hub Security: https://docs.microsoft.com/en-us/azure/iot-hub/iot-concepts-and-iot-hub#device-identity-and-authentication
// MQTT:  https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support

using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Text.Json;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.ReadResult;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;

#nullable disable

namespace viceroy
{
    public class RTDMS_Driver : IDisposable
    {
        public bool HVAC_On = false;
        private I2cConnectionSettings i2cSettings;
        private I2cDevice i2cDevice;
        private GpioController gpio;
        private Bme280 bme280;
        private LCDWriter lcd_writer;
        private IotHubClient hub_client;
        private Task driverTask;
        private Task transmitTask;
        private object transmitLockObj;
        private CancellationTokenSource ctsTransmitTelem;
        
        // properties
        public RDTMS_Settings Settings { get; set; }
        public int HVAC_Pin { get; set; }
        public int LED_Pin { get; set; }
        public int TransmitInterval;

        int transmitCount = 0;

        // constructor 
        public RTDMS_Driver()
        {
            try
            {
                // Build/read a configuration object, using JSON providers.
                // https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration
                IConfiguration config = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json")
                   .Build();

                // Get values from the appsettings.json configuration given their key and target value type.
                Settings = config.GetRequiredSection("Settings").Get<RDTMS_Settings>();

                // set the HVAC and LED GPIO pins 
                HVAC_Pin = Settings.HVACPin;
                LED_Pin = Settings.LEDPin;

                // Get a reference to a device on the I2C bus
                i2cSettings = new I2cConnectionSettings(1, Bme280.DefaultI2cAddress);

                // initialize the device (instance) using, the BME280 default bus address
                i2cDevice = I2cDevice.Create(i2cSettings);

                // Finally, create an instance of BME280, using device settings configured above)
                bme280 = new Bme280(i2cDevice);

                // Initialize the GPIO controller for communication with Rasspberry Pi, using logical pin numbering scheme
                gpio = new GpioController(PinNumberingScheme.Logical);

                // Open the HVAC pin for output, set to off (low voltage)
                gpio.OpenPin(Settings.HVACPin, PinMode.Output);
                gpio.Write(Settings.HVACPin, PinValue.Low);

                // Open the transmit LED indicator pin for output, set to off (low voltage)
                gpio.OpenPin(Settings.LEDPin, PinMode.Output);
                gpio.Write(Settings.LEDPin, PinValue.Low);

                //construct an instance of the LCD_writer (MaxLines, and LineLen are set in appsettings.json)
                //note: i2c bus address is 0x27
                lcd_writer = new LCDWriter(1, 0x27, Settings.LCDMaxLines, Settings.LCDLineLen);
                lcd_writer.On(true); // turn the LCD display on

                // construct an instance of the IoTHub to enable dbidirectional MQTT communication the Azure Iot hub service
                // Note: this depends on Microsoft.Azure.Devices.Client package
                hub_client = new IotHubClient(Settings, TransportType.Mqtt, this);

                // cancelation token for stopping the transmit task
                ctsTransmitTelem = new CancellationTokenSource();

                // critical section (lock), used for setting the interval rate
                transmitLockObj = new Object();

                // set the default transmit interval (ms -- millseconds)
                TransmitInterval = 1000;

                // trap ctrl-c press
                Console.CancelKeyPress += (s, e) =>
                {
                    Console.WriteLine("Choose menu option \"X\" to exit the program");
                    e.Cancel = true;
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{ex}");
            }
        }

        /* complete this function */
        public (double, double, double) ReadBME280()
        {
            // *** 1. write the code to Read the BME280 sensor, and return 3-tuple (temp, hum, pressure)   
            double temperatureF, humidityPercent, decapascals;
            temperatureF = humidityPercent = decapascals = 0.0;
            return (temperatureF, humidityPercent, decapascals);
        }

        // this a cleanup function, runs when shutting thge device down
        public void HandleCloseCommmand()
        {
            if (HVAC_On)
            {
                Console.WriteLine("Shuting down HVAC unit ");
                gpio.Write(HVAC_Pin, PinValue.Low);
            }

            // if the transmit LED is high, shut it down
            if (gpio.Read(LED_Pin) == PinValue.High)
            {
                gpio.Write(LED_Pin, PinValue.Low);
            }

            // Close the pin before exit
            gpio.ClosePin(HVAC_Pin);
            gpio.ClosePin(LED_Pin);

            // turn off lcd display
            lcd_writer.On(false);
        }

       /*  complete this function */
        void UpdateSendInterval()
        {
            // this is a basic lock called a monitor (synchonization primative --critical section) used to guard concurrent access to data
            // sharted between threads. In this case, the data transmission rate variable: "TransmitInterval" 

            // Note see resource: https://www.c-sharpcorner.com/UploadFile/de41d6/monitor-and-lock-in-C-Sharp/
            //                    https://dotnettutorials.net/lesson/multithreading-using-monitor/
            
            // thread acquires the lock, enters the  critcal section  
            lock (transmitLockObj)
            {
                // *** thread is with critical section  ***
                // write the code to perfrom the following steps...
                
                /* 1. prompt user for new interval rate (in milliseconds (ms) e.g. 1 sec == 1000 ms)
                   2. Read the new the rate from the user via command line (STDIN)
                   3.  verify what the user input is valid 
                   4.  set the TransitInterval variable with value read from the user
                   5. write a message on the console (STDOUT), indicating the messaage rate has been updated
                */ 

                // *** note: thread exiting the critical section
            }
        }

         /*  complete this function */
        void SendAzureCompatibleTempHumMessage(string device_id, double temp, double hum)
        {    
            // 1.  capture the telemetry in an instance of the telemetry model variable
            Telem telemetryDataPoint = new Telem
            {
                deviceId = device_id,
                temperature = temp,
                humidity = hum,
                messageId = transmitCount++
            };

            // *** 2.  write the code to Create JSON message: serialize the telemetry data
            // var telemetryDataString = 
                   
            // *** 3. write the code to Encode the serialized object using UTF-8 so it can be parsed by IoT Hub when
            //         processing messaging rules.

            // *** 4. write code to Send the message to the IoT hub.
           
            // Console.WriteLine(string.Format("[{0}] {1}", transmitCount, telemetryDataString));

            /* Note: use this online resource to complete 1-4 of thgs function:
            https://github.com/Azure/azure-iot-sdk-csharp/blob/main/iothub/device/samples/getting%20started/SimulatedDevice/Program.cs

            
            /*  Exameple Building raw messages (don't use this):
             string message_template = string.Format("{\"messageId\":{0},\"deviceId\":\"{1}\",\"temperature\":{2},\"humidity\":{3}}", ++msgCount, device_id,  temp, hum );
            Microsoft.Azure.Devices.Client.Message m = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(message_template));
            iotClient.SendDeviceToCloudMessagesAsync(m).Wait();
            Debug.WriteLine(message_template);
            */
        }

        public void TransmitTelemety()
        {
            // a task is like a thread, but a higher level abstraction
            transmitTask = Task.Run(() =>
            {
                while (!ctsTransmitTelem.IsCancellationRequested)
                {
                    var telem = ReadBME280();

                    //flash the transmit LED, turn on
                    gpio.Write(LED_Pin, PinValue.High);

                    // send telemetry to Azure IoT Hub
                    SendAzureCompatibleTempHumMessage(Settings.DeviceId, telem.Item1, telem.Item2);

                    // thread acquires the lock, enters the  critcal section
                    // Note see resource: https://www.c-sharpcorner.com/UploadFile/de41d6/monitor-and-lock-in-C-Sharp/
                    //                    https://dotnettutorials.net/lesson/multithreading-using-monitor/
                    lock (transmitLockObj)
                    {
                        // flash the transmit LED, turn on
                        gpio.Write(LED_Pin, PinValue.Low);
                        
                        // sleep for the for duration od TransmitInterval
                        Thread.Sleep(TransmitInterval);
                    }

                    gpio.Write(LED_Pin, PinValue.Low);
                }

                // dispose of cancelation token, and instiate a new for next time the function runs
                ctsTransmitTelem.Dispose();
                ctsTransmitTelem = new CancellationTokenSource();
            });
        }

        bool IsTransmiting()
        {
            if(transmitTask != null)
               return !transmitTask.IsCompleted;
            return false;
        }

        // *** Complete this function *** /
        public void External_HVAC(bool onoff, string LCD_message)
        {
            HVAC_On = onoff;
            if(HVAC_On)
            {
                 //  *** 1. write the the code to turn On the HVAC
                Console.WriteLine("HVAC On not implementated");
            }
            else
            {
                //  *** 2. write the the code to turn On the HVAC
                 gpio.Write(HVAC_Pin, PinValue.Low);
            }

            Console.WriteLine($"{LCD_message}");
            //  *** 3. write the code to display the message on the LCD character display 
        }

        bool ExecuteCommand(string commandText)
        {
            switch (commandText.ToLower())
            {
                case "x":
                    Console.WriteLine("Exiting RTDMS...");
                    HandleCloseCommmand();
                    return true;

                case "h":
                    {
                        string HVAC_msg = "HVAC Off";
                        if (HVAC_On = !HVAC_On)
                        {
                           HVAC_msg = "HVAC On";
                        }
                       
                        External_HVAC(HVAC_On, HVAC_msg);    
                    }
                    break;
                case "t":
                    {
                        if (IsTransmiting())
                        {
                            ctsTransmitTelem.Cancel();
                            transmitTask.Wait();
                        }
                        else
                        {
                            TransmitTelemety();
                        }
                    }
                    break;
                case "i":
                    UpdateSendInterval();
                    break;
                case "s":
                    {
                        var output = ReadBME280();
                        Console.WriteLine("DEVICE STATUS");
                        Console.WriteLine("-------------");
                        Console.WriteLine($"HVAC: {(HVAC_On ? "ON" : "OFF")}");
                        Console.WriteLine($"Temperature: {output.Item1:0.#}dF");
                        Console.WriteLine($"Relative humidity: {output.Item2:#.##}%");
                        string lcd_msg = $"T:{output.Item1:0.#}dF " + $"H:{output.Item2:#.#}%";

                        lcd_writer.WriteMessage(lcd_msg);
                    }
                    break;
                default:
                    Console.WriteLine("Unknown command");
                    break;
            }
            return false;
        }

        void DisplayMenu()
        {
            string hvac_status = HVAC_On ? "Off" : "On";
            string transmit_status = (!IsTransmiting()) ? "Transmit" : "Stop Transmiting";
            Console.WriteLine("---------------");
            Console.WriteLine("RDTMS v1.0 Menu");
            Console.WriteLine("---------------");
            Console.WriteLine("\"S\": - display current temperature/humidity");
            Console.WriteLine($"\"H\": - turn HVAC {hvac_status}");
            Console.WriteLine($"\"T\": - {transmit_status} Telemetry To Azure IoT Hub service");
            Console.WriteLine("\"I\": - Change telemetry transmit interval (1000ms -default )");
            Console.WriteLine("\"X:\" - Close the  RDTMS driver");
            Console.Write("Command:-> ");
        }

        public void Start()
        {
            // create a Task to connect to asychronously to the IoT hub:
            // see background info here: https://dotnettutorials.net/lesson/asynchronous-programming-in-csharp/
            driverTask = Task.Run(() =>
            {
                // prior to displaying the menu for the first time connect, established TLS connection to Azure IoT Cloud 
                hub_client.Start();

                if (hub_client.Connected)
                {
                    Console.WriteLine("Using Shared Access Signature (SAS - connectionString) to authenticate with Azure IoT service, please wait...");
                    Console.WriteLine("Successfully Connected To Azure Cloud (IoT Hub) Service, using MQTT over TLS.");
                }
                else
                {
                    Console.WriteLine("Error connecting (or already connected) to the Azure Cloud");
                }

                bool exit = false;
                while (!exit)
                {
                    DisplayMenu();
                    string commandText = Console.ReadLine();
                    exit = ExecuteCommand(commandText);
                }

                Console.WriteLine("Closing Azure IoT Connection...");
                hub_client.Close();
                if (!hub_client.Connected)
                    Console.WriteLine("Disconnected from Azure Cloud");
                else
                    Console.WriteLine("Eror disconnecting from the Azure Cloud");
            });
        }

        public void Stop()
        {
            // wait on the driver task to make the main thread (task) block
            driverTask.Wait();
        }

        // diespose frees any native platform (non CLR managed) resources
        public void Dispose()
        {
            if (bme280 != null)
                bme280.Dispose();

            if (lcd_writer != null)
                lcd_writer.Dispose();

            if (i2cDevice != null)
                i2cDevice.Dispose();

            if (gpio != null)
                gpio.Dispose();

            if (hub_client != null)
                hub_client.Dispose();

            if (ctsTransmitTelem != null)
                ctsTransmitTelem.Dispose();

            Console.WriteLine("All Disposed");
        }
    }
}
