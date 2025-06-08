var versionPropsFile = "./build/version.props";
var doc = System.Xml.Linq.XDocument.Load(versionPropsFile);
var propertyGroupNode = doc.Element("Project").Element("PropertyGroup");

var version = $"{propertyGroupNode.Element("VersionMajor").Value}.{propertyGroupNode.Element("VersionMinor").Value}.{propertyGroupNode.Element("VersionPatch").Value}";
Console.WriteLine($"Version: {version}");

var dockerBuildCommand = $"""docker buildx build --push -f Dockerfile --platform="linux/amd64,linux/arm64" --output="type=image" -t weihanli/dotnet-httpie:{version} .""";
Console.WriteLine($"Executing command: {dockerBuildCommand}");
CommandExecutor.ExecuteCommandAndOutput(dockerBuildCommand);
