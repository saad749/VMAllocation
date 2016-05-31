using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Web;
using Microsoft.Owin;
using VMAllocation.Web.Models;
using VMAllocation.Web.Models.DataStructure;

namespace VMAllocation.Web.Services
{
    public class AllocationService : IAllocation
    {
        public double AverageDistancePerRequest { get; set; }
        //Dirty Trick to gget the Path for each cloud.
        private List<Path> _paths;
        private double _totalDistance; //SOLVE THIS!!
        //private List<string> _results;
        private int _cloudandUsersCount;
        public List<AllocationResult> AllocationResults { get; set; }


        public void Allocate(List<CloudSpecification> cloudSpecifications,
            List<UserRequirement> userRequirements, List<Connection> connections)
        {
            AllocationResults = new List<AllocationResult>();
            //_results = new List<string>();
            List<int> cloudAndUsers = new List<int>();
            cloudAndUsers.AddRange(cloudSpecifications.Select(c => c.UniversalId));
            cloudAndUsers.AddRange(userRequirements.Select(c => c.UniversalId));
            cloudAndUsers.Sort();
            _cloudandUsersCount = cloudAndUsers[cloudAndUsers.Count - 1]; //Take the lastId and increment it!
            _cloudandUsersCount++;

            foreach (CloudSpecification cloudSpecification in cloudSpecifications)
            {
                cloudSpecification.AllocatedUserRequirements = new List<UserRequirement>();
            }

            foreach (UserRequirement userRequirement in userRequirements)
            {
                AllocateCloud(userRequirement, cloudSpecifications, connections, cloudAndUsers);
            }

            AverageDistancePerRequest = _totalDistance / userRequirements.Count;

            //return _results;
        }


        private void AllocateCloud(UserRequirement userRequirement, List<CloudSpecification> cloudSpecifications, List<Connection> connections, List<int> cloudAndUsers)
        {
            _paths = new List<Path>();
            ResetTempValues(cloudSpecifications, connections);
            Connection[][] adjacencyMatrix = GetAdjacencyMatrix(cloudAndUsers, connections);
            List<CloudSpecification> feasibleCloudSpecifications = FindPossibleCloudsBFS(adjacencyMatrix, userRequirement, cloudSpecifications);
            //CloudSpecification feasibleCloudSpecification = GetFittestCloud(userRequirement, feasibleCloudSpecifications);
            

            if (AllocateToCloud(userRequirement, feasibleCloudSpecifications, _paths, false, null))
            {
                
                //string possibleChoices = string.Join(", ", feasibleCloudSpecifications.Select(c => c.UniversalId));
                //string allPaths = GetAllPathsAsString(feasibleCloudSpecifications);
                //AllocateCloudToUser(userRequirement, feasibleCloudSpecification, connections, possibleChoices, allPaths);
            }
            else
            {
                //Migration Phase:
                foreach (CloudSpecification cloudSpecification in feasibleCloudSpecifications)
                {
                    if (cloudSpecification.TotalFitnessRelaxed <= 1)
                    {
                        foreach (UserRequirement allocatedUserRequirement in cloudSpecification.AllocatedUserRequirements.ToList())
                        {
                            SimilarityRatio ratios = new SimilarityRatio();
                            ratios.GetSimilarityRatio(allocatedUserRequirement, userRequirement);
                            if (ratios.CpuCountRatio >= 1 && ratios.MemorySizeRaio >= 1 &&
                                ratios.NetworkBandwidthRatio >= 1)
                            {
                                //The currently alloted user has been DeAllocated
                                cloudSpecification.DeAllocateUser(allocatedUserRequirement);
                                AllocationResults.RemoveAll(
                                    r => r.UserRequirement.UniversalId == allocatedUserRequirement.UniversalId);
                                AllocationResult tempResult = (AllocationResult)allocatedUserRequirement.AllocationResult.Clone();
                                tempResult.InitialAllocationResults.Add(tempResult);
                                allocatedUserRequirement.AllocationResult.ResetAllocation();
                                if (AllocateToCloud(allocatedUserRequirement,
                                    allocatedUserRequirement.AllocationResult.FeasibleAllocations,
                                    allocatedUserRequirement.AllocationResult.FeasiblePaths, true, tempResult.InitialAllocationResults))
                                {
                                    //Continue Migration
                                    AllocateToCloud(userRequirement, feasibleCloudSpecifications, 
                                        _paths, false, null);
                                    return;
                                }
                                else
                                {
                                    //Abort Migrating this VM
                                    allocatedUserRequirement.AllocationResult = tempResult;
                                    allocatedUserRequirement.AllocationResult.MigrationAbortCount++;
                                    cloudSpecification.AllocateUser(allocatedUserRequirement);
                                    AllocationResults.Add(tempResult);
                                }
                            }
                        }
                    }
                }

                if (!userRequirement.Allocated)
                {
                    AllocationResult allocationResult = new AllocationResult()
                    {
                        UserRequirement = userRequirement,
                        FeasibleAllocations = feasibleCloudSpecifications,
                        FeasiblePaths = _paths,
                    };
                    AllocationResults.Add(allocationResult);
                }
                //_results.Add($"VM {userRequirement.UniversalId} | {userRequirement.LocationTitle}  cannot be placed on any suitable Cloud!");
            }


        }

        private bool AllocateToCloud(UserRequirement userRequirement, List<CloudSpecification> feasibleCloudSpecifications, List<Path> feasiblePaths, bool isMigration, List<AllocationResult> previousAllocationResults )
        {
            CloudSpecification feasibleCloudSpecification = GetFittestCloud(userRequirement, feasibleCloudSpecifications);
            if (feasibleCloudSpecification == null)
                return false;

            AllocationResult allocationResult = new AllocationResult()
            {
                UserRequirement = userRequirement,
                CloudSpecification = feasibleCloudSpecification,
                FeasibleAllocations = feasibleCloudSpecifications,
                FeasiblePaths = feasiblePaths,
                Distance = feasiblePaths.Where(p => p.CloudId == feasibleCloudSpecification.UniversalId).Min(p => p.Distance),
                Fitness = feasibleCloudSpecification.TotalFitness.Value,
                InitialAllocationResults = new List<AllocationResult>()
            };
            if (isMigration)
            {
                allocationResult.MigrationCount++;
                allocationResult.InitialAllocationResults = new List<AllocationResult>(previousAllocationResults);
            }
            AllocationResults.Add(allocationResult);
            feasibleCloudSpecification.AllocateUser(userRequirement);
            userRequirement.AllocationResult = allocationResult;

            return true;
        }
        private void ResetTempValues(List<CloudSpecification> cloudSpecifications, List<Connection> connections)
        {
            foreach (CloudSpecification cloudSpecification in cloudSpecifications)
            {
                cloudSpecification.TemporaryPathDistance = null;
                cloudSpecification.TotalFitness = null;
            }
            foreach (Connection connection in connections)
            {
                connection.Visited = false;
            }
        }

        private Connection[][] GetAdjacencyMatrix(List<int> cloudAndUsers, List<Connection> connections)
        {
            Connection[][] adjacencyMatrix = new Connection[_cloudandUsersCount][];
            //int i = 0;
            foreach (int row in cloudAndUsers)
            {
                adjacencyMatrix[row] = new Connection[_cloudandUsersCount];
                //int j = 0;
                foreach (int column in cloudAndUsers)
                {
                    Connection connection = connections.FirstOrDefault(c => (c.StartPointId == row && c.EndPointId == column) ||
                                                                            (c.EndPointId == row && c.StartPointId == column));
                    adjacencyMatrix[row][column] = connection;

                }
            }

            return adjacencyMatrix;
        }

        //private string GetAllPathsAsString(List<CloudSpecification> feasibleCloudSpecifications)
        //{
        //    double minPath = 100000;//No distance can be greater than this on earth //Is not really being used
        //    string allPaths = "";
        //    foreach (Path path in _paths)
        //    {
        //        path.Distance = path.Connections.Sum(p => p.Distance);
        //        allPaths += $"Cloud: {path.CloudId}:  " + string.Join(" | ", path.Connections.Select(p => p.ReadablePath)) + 
        //            $"   --- Distance: {Math.Round(path.Distance, 2)}  {Environment.NewLine}";

        //        CloudSpecification tempCloud = feasibleCloudSpecifications.FirstOrDefault(f => f.UniversalId == path.CloudId);
        //        if (tempCloud.TemporaryPathDistance == null || tempCloud.TemporaryPathDistance > path.Distance)
        //            tempCloud.TemporaryPathDistance = path.Distance;

        //        if (path.Distance < minPath)
        //        {
        //            minPath = path.Connections.Sum(p => p.Distance);
        //        }
        //    }

        //    return allPaths;
        //}

        //private void AllocateCloudToUser(UserRequirement userRequirement, CloudSpecification feasibleCloudSpecification, List<Connection> connections, string possibleChoices, string allPaths)
        //{
        //    feasibleCloudSpecification.AllocateUser(userRequirement);

        //    string pathInfo = "";
        //    double minDistanceForCloud = _paths.Where(p => p.CloudId == feasibleCloudSpecification.UniversalId).Min(p => p.Distance);

        //    foreach (Connection connection in _paths.FirstOrDefault(p => p.CloudId == feasibleCloudSpecification.UniversalId && p.Distance == minDistanceForCloud).Connections)
        //    {
        //        pathInfo += $" Endpoints: {connection.StartPointId} <--> {connection.EndPointId} - ";
        //        Connection realConnection =
        //            connections.FirstOrDefault(
        //                c => c.StartPointId == connection.StartPointId && c.EndPointId == connection.EndPointId);
        //    }

        //    //_results.Add($"VM {userRequirement.UniversalId} | {userRequirement.LocationTitle} " +
        //    //            $" Placed on Cloud {feasibleCloudSpecification.UniversalId} | {feasibleCloudSpecification.LocationTitle}" +
        //    //            $" {Environment.NewLine} Via Path: {pathInfo} {Environment.NewLine} Distance: {Math.Round(minDistanceForCloud, 2)} {Environment.NewLine} " +
        //    //            $" Total Fitness: {feasibleCloudSpecification.TotalFitness} {Environment.NewLine} possible choices [{possibleChoices}] {Environment.NewLine} " +
        //    //            $" possible paths {Environment.NewLine}{allPaths}");
        //    _totalDistance += minDistanceForCloud;
        //}

        private CloudSpecification GetFittestCloud(UserRequirement userRequirement, List<CloudSpecification> feasibleCloudSpecifications)
        {
            double bestFitnessRatio = 0;
            CloudSpecification bestFitCloud = null;
            feasibleCloudSpecifications = feasibleCloudSpecifications.OrderBy(f => f.TemporaryPathDistance).ToList();
            foreach (CloudSpecification cloudSpecification in feasibleCloudSpecifications)
            {
                cloudSpecification.CalculateTotalFitness(userRequirement);
                if (cloudSpecification.TotalFitness.HasValue && cloudSpecification.TotalFitness > bestFitnessRatio && cloudSpecification.TotalFitness <= 1)
                {
                    bestFitnessRatio = cloudSpecification.TotalFitness.Value;
                    bestFitCloud = cloudSpecification;
                }
            }
            return bestFitCloud;
        }

        public List<CloudSpecification> FindPossibleCloudsBFS(Connection[][] adjacencyMatrix, UserRequirement userRequirement, List<CloudSpecification> cloudSpecifications)
        {
            List<CloudSpecification> possibleClouds = new List<CloudSpecification>();
            int pathId = 0;
            
            TreeNode<int> rootNode =  new TreeNode<int>
            {
                Node = userRequirement.UniversalId,
                ChildrenNodes = new List<TreeNode<int>>()
            }; ;

            Queue<TreeNode<int>> queue = new Queue<TreeNode<int>>();
            queue.Enqueue(rootNode);
            while (queue.Count > 0)
            {
                TreeNode<int> current = queue.Dequeue();

                
                foreach (Connection connection in adjacencyMatrix[current.Node].Where(c => c != null && c.LinkBandwidth >= userRequirement.BandwidthThreshold))
                {
                     if (!connection.Visited)
                    {
                        connection.Visited = true;
                        int newVertex = connection.StartPointId == current.Node ? connection.EndPointId : connection.StartPointId;
                        
                        TreeNode<int> newNode = new TreeNode<int>
                        {
                            Node = newVertex,
                            ParentNode = current,
                            ChildrenNodes = new List<TreeNode<int>>(),
                            Connection = connection
                        };
                        current.ChildrenNodes.Add(newNode);
                        queue.Enqueue(newNode);

                        CloudSpecification possibleCloud = cloudSpecifications.FirstOrDefault(c => c.UniversalId == newVertex);
                        if (possibleCloud != null)
                        {
                            possibleCloud.CalculateTotalFitness(userRequirement); // Checks if the cloud can handle the req or Not. Constraint Checking
                            if (possibleCloud.TotalFitnessRelaxed <= 1)
                            {
                                possibleClouds.Add(possibleCloud);
                                TreeNode<int> pathNode = newNode;
                                List<Connection> pathConnections = new List<Connection>();
                                while (pathNode.ParentNode != null)
                                {
                                    pathConnections.Add(pathNode.Connection);
                                    pathNode = pathNode.ParentNode;
                                }
                                _paths.Add(new Path() { Id = pathId, CloudId = possibleCloud.UniversalId, Connections = pathConnections.ToArray() });
                                pathId++;
                            }
                        }
                    }
                }

            }

            return possibleClouds;
        }

    }

    public class Path : ICloneable
    {
        public int Id { get; set; }
        public int CloudId { get; set; }
        public Connection[] Connections { get; set; }
        public double Distance { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
            //return new Path()
            //{
            //    Id = Id,
            //    CloudId = CloudId,
            //    Connections = (Connection[])Connections.Clone(),
            //    Distance = Distance
            //};
        }
    }
}