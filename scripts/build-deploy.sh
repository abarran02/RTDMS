#!/bin/bash

cd RTDMS-V1
dotnet build && dotnet publish --sc -r linux-arm

# first argument represents user/address
# second argument represents destination directory
rsync -auv RTDMS-driver/bin/Debug/net6.0/linux-arm/publish/* $1:$2
rsync -auv RTDMS-driver/src/appsettings.json $1:$2
