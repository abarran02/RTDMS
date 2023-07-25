#!/bin/bash
# MUST be run from RTDMS-driver/ directory!

# IoT Gpio package:
dotnet add package System.Device.Gpio
# IoT (Bindings) package:
dotnet add package IoT.Device.Bindings
# JSON config provider package:
dotnet add package Microsoft.Extensions.Configuration.Json
# config binder package:
dotnet add package Microsoft.Extensions.Configuration.Binder
# Azure IoT Hub device SDK package to connect devices to Azure IoT Hub:
dotnet add package Microsoft.Azure.Devices.Client
