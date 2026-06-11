#!/bin/sh

dnx -y dotnet-execute ./build/build.cs --args "$@"
