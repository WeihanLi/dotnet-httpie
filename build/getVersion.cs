var versionPropsFile = "./build/version.props";
var doc = System.Xml.Linq.XDocument.Load(versionPropsFile);
var propertyGroupNode = doc.Element("Project").Element("PropertyGroup");

var version = $"{propertyGroupNode.Element("VersionMajor").Value}.{propertyGroupNode.Element("VersionMinor").Value}.{propertyGroupNode.Element("VersionPatch").Value}";
Console.WriteLine($"Version: {version}");

Environment.SetEnvironmentVariable("RELEASE_VERSION", version);
