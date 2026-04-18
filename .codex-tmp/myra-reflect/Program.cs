using System.Reflection;
using Myra.Graphics2D.UI;

static void Dump(Type type)
{
    Console.WriteLine($"TYPE {type.FullName}");
    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name))
    {
        Console.WriteLine($"PROP {prop.PropertyType.Name} {prop.Name}");
    }
}

Dump(typeof(ImageTextButton));
