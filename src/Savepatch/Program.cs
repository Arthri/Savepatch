using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

if (!Locator.LocateTerraria(out string? terrariaPath))
{
    Console.WriteLine("Unable to locate Terraria.");
    goto exit;
}

try
{
    File.Copy(
        terrariaPath,
        Path.Combine(
            Path.GetDirectoryName(terrariaPath) ?? "",
            $"{Path.GetFileNameWithoutExtension(terrariaPath)}-original{Path.GetExtension(terrariaPath)}"
        )
    );
}
catch (IOException)
{
    Console.WriteLine("Unable to write to Terraria-original.exe, because of insufficient permissions, it already exists, or some other reason.");
    goto exit;
}

var assembly = AssemblyDefinition.FromFile(terrariaPath);
var module = assembly.ManifestModule;
if (module is null)
{
    Console.WriteLine("Unable to find module.");
    goto exit;
}

var type = module.TopLevelTypes.FirstOrDefault(t => t.FullName == "Terraria.Player");
if (type is null)
{
    Console.WriteLine("Unsupported client(Player type could not be found).");
    goto exit;
}

var method = type.Methods.FirstOrDefault(m => m.Name == "InternalSavePlayerFile");
if (method is null)
{
    Console.WriteLine("Unsupported client(Save method could not be found).");
    goto exit;
}

var methodBody = method.CilMethodBody;
if (methodBody is null)
{
    Console.WriteLine("Unsupported client(Save method is not valid).");
    goto exit;
}

var instructions = methodBody.Instructions;
if (instructions is
[
    { OpCode.Code: CilCode.Ldarg_0 },
    {
        OpCode.Code: CilCode.Callvirt,
        Operand: MethodDefinition
        {
            FullName: "System.Boolean Terraria.IO.PlayerFileData::get_ServerSideCharacter()",
        },
    },
    { OpCode.Code: CilCode.Brfalse or CilCode.Brfalse_S },
    { OpCode.Code: CilCode.Ret },
    ..
])
{
    instructions.RemoveRange(0, 4);

    assembly.Write(terrariaPath);

    Console.WriteLine("Successfully patched.");

    goto exit;
}

Console.WriteLine("Unsupported client(Save method contains unexpected sequence).");
goto exit;

exit:
Console.WriteLine("Press any key to exit...");
Console.ReadKey(true);
