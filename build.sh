#!/bin/sh

dnx dotnet-execute -y ./build/build.cs --args "$@"
