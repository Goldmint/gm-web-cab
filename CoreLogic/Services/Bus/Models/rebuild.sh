#!/bin/bash

protoc --csharp_out=./Core --csharp_opt=file_extension=.pb.cs proto/core/*.proto
protoc --csharp_out=./Eth2Gold --csharp_opt=file_extension=.pb.cs proto/eth2gold/*.proto
protoc --csharp_out=./MintSender/Sender --csharp_opt=file_extension=.pb.cs proto/mintsender/sender/*.proto
protoc --csharp_out=./MintSender/Watcher --csharp_opt=file_extension=.pb.cs proto/mintsender/watcher/*.proto
