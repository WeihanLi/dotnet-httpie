// Copyright (c) 2022-2023 Weihan Li. All rights reserved.
// Licensed under the Apache license version 2.0 http://www.apache.org/licenses/LICENSE-2.0

var solutionPath = "./dotnet-httpie.slnx";
string[] srcProjects = ["./src/HTTPie/HTTPie.csproj"];
string[] testProjects = [ "./tests/HTTPie.UnitTest/HTTPie.UnitTest.csproj", "./tests/HTTPie.IntegrationTest/HTTPie.IntegrationTest.csproj" ];

await DotNetPackageBuildProcess
    .Create(options => 
    {
        options.SolutionPath = solutionPath;
        options.SrcProjects = srcProjects;
        options.TestProjects = testProjects;
    })
    .ExecuteAsync(args);
