// See https://aka.ms/new-console-template for more information
using CSharpToTypescript;

Console.WriteLine("Hello, World!");


if (args.Length >= 1)
{
    string csProj = args[0];

    //await new ProjectManager().Init(csProj);

}
else
{
    ProjectConfig config = new ProjectConfig();
    if (true)
    {
        config.csProj = @"D:\Rayuki\Core\ExampleApp\ExampleApp.csproj";
        config.outputPath = @"D:\Rayuki\Core\ExampleApp\Front\src\generated";
    }
    if (false)
    {
        config.csProj = @"D:\Rayuki\Core\BaseApp\BaseApp.csproj";
        config.outputPath = @"D:\Rayuki\Core\BaseApp\Front\src";
    }
    await new ProjectManager().Init(config);
}

Console.WriteLine("Done");