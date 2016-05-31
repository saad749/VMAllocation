using System;
using System.Collections.Generic;
using System.Linq;
using VMAllocation.Web.Models;

namespace VMAllocation.Web.Services
{
    public class AllocationResult :ICloneable
    {
        public UserRequirement UserRequirement { get; set; }
        public CloudSpecification CloudSpecification { get; set; }
        public Path AllocatedPath { get; set; }
        public List<Path> FeasiblePaths { get; set; }
        public string AllocatedPathString { get; set; }
        public string FeasiblePathsString { get; set; }
        public double Distance { get; set; }
        public double Fitness { get; set; }
        public List<CloudSpecification> FeasibleAllocations { get; set; }
        public string ResultString { get; set; }
        public int MigrationCount { get; set; } 
        public int MigrationAbortCount { get; set; }
        public List<AllocationResult> InitialAllocationResults { get; set; }



        public object Clone()
        {
            AllocationResult newAllocationResult = new AllocationResult();
            newAllocationResult.UserRequirement = UserRequirement;
            newAllocationResult.CloudSpecification = CloudSpecification;
            //newAllocationResult.AllocatedPath = (Path)AllocatedPath.Clone();
            newAllocationResult.FeasiblePaths = new List<Path>();
            foreach (Path feasiblePath in FeasiblePaths)
            {
                newAllocationResult.FeasiblePaths.Add(feasiblePath);
            }
            //newAllocationResult.AllocatedPathString = AllocatedPathString.Clone().ToString();
            //newAllocationResult.FeasiblePathsString = FeasiblePathsString.Clone().ToString();
            newAllocationResult.Distance = Distance;
            newAllocationResult.Fitness = Fitness;
            newAllocationResult.FeasibleAllocations = new List<CloudSpecification>();
            foreach (CloudSpecification feasibleAllocation in FeasibleAllocations)
            {
                newAllocationResult.FeasibleAllocations.Add(feasibleAllocation);
            }
            //newAllocationResult.ResultString = ResultString.Clone().ToString();
            newAllocationResult.MigrationAbortCount = MigrationAbortCount;
            newAllocationResult.MigrationCount = MigrationCount;
            newAllocationResult.InitialAllocationResults = new List<AllocationResult>();
            foreach (AllocationResult initialAllocationResult in InitialAllocationResults)
            {
                newAllocationResult.InitialAllocationResults.Add(initialAllocationResult);
            }
            return newAllocationResult;
        }

        public void ResetAllocation()
        {
            FeasibleAllocations.RemoveAll(a => a.UniversalId == CloudSpecification.UniversalId);
            FeasiblePaths.RemoveAll(p => p.CloudId == CloudSpecification.UniversalId);
            FeasiblePathsString = null;
            Distance = 0;
            Fitness = 0;
            CloudSpecification = null;
            AllocatedPath = null;
            AllocatedPathString = null;
            ResultString = null;
        }

        public string GetResultString()
        {
            if (CloudSpecification != null)
            {
                ResultString = $"VM {UserRequirement.UniversalId} | {UserRequirement.LocationTitle} ({UserRequirement.CpuCount},{UserRequirement.MemorySize},{UserRequirement.NetworkBandwidth}) {Environment.NewLine}" +
                               $"Placed on Cloud {CloudSpecification.UniversalId} | {CloudSpecification.LocationTitle} ({CloudSpecification.CpuCount},{CloudSpecification.MemorySize},{CloudSpecification.NetworkBandwidth}) {Environment.NewLine}" +
                               $"Via Path: {GetAllocatedPathAsString()} {Environment.NewLine}" +
                               //$"Distance: {Math.Round(Distance, 2)} {Environment.NewLine}" +
                               $"Total Fitness: {Fitness} {Environment.NewLine}" +
                               $"Possible choices [{string.Join(", ", FeasibleAllocations.Select(c => c.UniversalId))}] {Environment.NewLine}" +
                               $"Possible paths {Environment.NewLine}{GetAllPathsAsString()}";
            }
            else
            {
                ResultString = $"VM {UserRequirement.UniversalId} | {UserRequirement.LocationTitle} ({UserRequirement.CpuCount},{UserRequirement.MemorySize}," +
                               $"{UserRequirement.NetworkBandwidth},{UserRequirement.DistanceThreshold},{UserRequirement.CostThreshold}) {Environment.NewLine}" +
                               $"cannot be placed on any suitable Cloud! {Environment.NewLine}" +
                               $"Possible choices [{string.Join(", ", FeasibleAllocations.Select(c => c.UniversalId))}] {Environment.NewLine}" +
                               $"Possible paths {Environment.NewLine}{GetAllPathsAsString()}";
            }
            
            return ResultString;
        }

        private string GetAllocatedPathAsString()
        {
            foreach (Connection connection in FeasiblePaths.FirstOrDefault(p => p.CloudId == CloudSpecification.UniversalId && p.Connections.Sum(c => c.Distance) == Distance).Connections)
            {
                AllocatedPathString += $" {connection.StartPointId} <--> {connection.EndPointId} - ";
            }
            return AllocatedPathString;
        }

        private string GetAllPathsAsString()
        {
            double minPath = 100000;//No distance can be greater than this on earth //Is not really being used
            //string allPaths = "";
            foreach (Path path in FeasiblePaths)
            {
                CloudSpecification tempCloud = FeasibleAllocations.FirstOrDefault(f => f.UniversalId == path.CloudId);
                path.Distance = path.Connections.Sum(p => p.Distance);
                FeasiblePathsString += $"Cloud: {path.CloudId}:  " + string.Join(" | ", path.Connections.Select(p => p.ReadablePath)) +
                            $"   --- Distance: {Math.Round(path.Distance.Value, 2)}, --- Cost: {tempCloud.CalculateCost(UserRequirement)}  {Environment.NewLine}";

                
                if (tempCloud.TemporaryPathDistance == null || tempCloud.TemporaryPathDistance > path.Distance)
                    tempCloud.TemporaryPathDistance = path.Distance;

                if (path.Distance < minPath)
                {
                    minPath = path.Connections.Sum(p => p.Distance);
                }
            }

            return FeasiblePathsString;
        }
    }
}