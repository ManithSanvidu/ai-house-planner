using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Data;
using Microsoft.Extensions.DependencyInjection;

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql("Host=localhost;Database=houseplanner;Username=postgres;Password=postgres")
    .Options;
using var db = new ApplicationDbContext(options);
var reqs = db.ValidationRequests.Include(v => v.WorkflowState).ThenInclude(w => w.LandSubmission).ToList();
foreach(var r in reqs) {
    Console.WriteLine($"VR Status: {r.Status}, VR ClientId: {r.ClientId}, Workflow ClientId: {r.WorkflowState?.LandSubmission?.ClientId}, Match: {r.ClientId == r.WorkflowState?.LandSubmission?.ClientId}");
}
