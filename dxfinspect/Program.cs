﻿using Dxf;

try
{
    switch (args.Length)
    {
        case 1:
            Console.WriteLine(DxfInspect.ToHtml(args[0]));
            break;
        case 2:
            DxfInspect.Convert(args[0], args[1]);
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);
}
