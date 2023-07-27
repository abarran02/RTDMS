#!/bin/bash
# MUST be run from RTDMS-trigger/ directory!

# use latest WebJobs package version:
dotnet add package Microsoft.Azure.WebJobs.Extensions.EventHubs
# C2D communication package:
dotnet add package Microsoft.Azure.Devices
