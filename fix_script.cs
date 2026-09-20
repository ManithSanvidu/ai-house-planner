using System;
using System.IO;

var path = "/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Controllers/CustomerConstructionController.cs";
var lines = File.ReadAllLines(path);
for(int i = 0; i < lines.Length; i++) {
    if (lines[i].Contains(".Where(v => v.ClientId == customerId && v.Status == \"Approved\")")) {
        lines[i] = "            .Where(v => v.WorkflowState.LandSubmission.ClientId == customerId && v.Status == \"Approved\")";
    }
}
File.WriteAllLines(path, lines);
