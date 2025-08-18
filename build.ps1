dotnet tool install -g dotnet-execute

Write-Host 'dotnet-exec ./build/build.cs "--args=$ARGS"' -ForegroundColor GREEN
 
dotnet-exec ./build/build.cs --args $ARGS
