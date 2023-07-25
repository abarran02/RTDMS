#!/bin/bash

# MUST be run from root directory containing RTDMS-V1.sln
dotnet build && dotnet publish --sc -r linux-arm

# first argument represents user/address
# second argument represents destination directory
rsync -auv RTDMS-driver/bin/Debug/net6.0/linux-arm/publish/* RTDMS-driver/src/appsettings.json $1:$2
