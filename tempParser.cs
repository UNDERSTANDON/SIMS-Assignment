using System;
using System.Collections.Generic;
public class Parser {
    public static void Main() {
        var line = "COURSE-123,Automation Course,3,10,1;2";
        var parts = line.Split(',');
        Console.WriteLine(parts.Length);
        for (int i=0;i<parts.Length;i++) Console.WriteLine($"[{i}]='{parts[i]}'");
        var enrolled = new List<int>();
        var en = parts.Length > 4 ? parts[4] : string.Empty;
        Console.WriteLine($"en='{en}'");
        if (!string.IsNullOrEmpty(en)) {
            var toks = en.Split(';');
            foreach(var t in toks) {
                if (int.TryParse(t, out var ii)) enrolled.Add(ii);
            }
        }
        Console.WriteLine("count=" + enrolled.Count);
    }
}
