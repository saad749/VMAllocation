using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Web;
using Microsoft.Owin;
using VMAllocation.Web.Models;
using VMAllocation.Web.Models.DataStructure;

namespace VMAllocation.Web.Services
{
    public class LoadBalancedAllocationService : IAllocation
    {
        public double AverageDistancePerRequest { get; set; }
        //Dirty Trick to gget the Path for each cloud.
        private List<Path> _paths;


        public List<string> Allocate(List<CloudSpecification> cloudSpecifications,
            List<UserRequirement> userRequirements, List<Connection> connections)
        {
            List<string> results = new List<string>();

            List<int> cloudAndUsers = new List<int>();
            cloudAndUsers.AddRange(cloudSpecifications.Select(c => c.UniversalId));
            cloudAndUsers.AddRange(userRequirements.Select(c => c.UniversalId));
            cloudAndUsers.Sort();

            int lastId = cloudAndUsers[cloudAndUsers.Count - 1];
            lastId++;
            double totalDistance = 0;
            double numberOfusers = userRequirements.Count;
            

            foreach (CloudSpecification cloudSpecification in cloudSpecifications)
            {
                cloudSpecification.AllocatedUserRequirements = new List<UserRequirement>();
            }


            foreach (UserRequirement userRequirement in userRequirements)
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

                Connection[][] adjacencyMatrix = new Connection[lastId][];
                //int i = 0;
                foreach (int row in cloudAndUsers)
                {
                    adjacencyMatrix[row] = new Connection[lastId];
                    //int j = 0;
                    foreach (int column in cloudAndUsers)
                    {
                        Connection connection = connections.FirstOrDefault(c => (c.StartPointId == row && c.EndPointId == column) ||
                                                                                (c.EndPointId == row && c.StartPointId == column));
                        adjacencyMatrix[row][column] = connection;

                    }
                }

                _paths = new List<Path>();


                //List<CloudSpecification> bfsClouds = FindPossibleCloudsBFS(adjacencyMatrix, userRequirement, cloudSpecifications);
                List<CloudSpecification> feasibleCloudSpecifications = FindPossibleCloudsBFS(adjacencyMatrix, userRequirement, cloudSpecifications);


                //List<int> possibleClouds = FindPossibleClouds(cloudAndUsers, adjacencyMatrix, userRequirement, cloudSpecifications);
                //List<CloudSpecification> feasibleCloudSpecifications =
                //    cloudSpecifications.Where(c => possibleClouds.Contains(c.UniversalId)).ToList();

                //string possibleChoices = string.Join(", ", possibleClouds);
                string possibleChoices = string.Join(", ", feasibleCloudSpecifications.Select(c => c.UniversalId));
                //CloudSpecification feasibleCloudSpecification = cloudSpecifications.Where(c => possibleClouds.Contains(c.UniversalId)).
                //    OrderByDescending(c => c.RemainCpuCount).FirstOrDefault();

                //CloudSpecification feasibleCloudSpecification = cloudSpecifications.Where(c => possibleClouds.Contains(c.UniversalId)).
                //    OrderByDescending(c => c.RemainCpuCount).FirstOrDefault();

                double minPath = 100000;
                //int minCloudId = -1;
                //Selecting Shortest PAth
                string allPaths = "";
                foreach (Path path in _paths)
                {
                    path.Distance = path.Connections.Sum(p => p.Distance);

                    

                    allPaths += $"Cloud: {path.CloudId}:  " + string.Join(" | ", path.Connections.Select(p => p.ReadablePath)) + $"   --- Distance: {Math.Round(path.Distance, 2)}  {Environment.NewLine}";

                    CloudSpecification tempCloud = feasibleCloudSpecifications.FirstOrDefault(f => f.UniversalId == path.CloudId);
                    if (tempCloud.TemporaryPathDistance == null || tempCloud.TemporaryPathDistance > path.Distance)
                        tempCloud.TemporaryPathDistance = path.Distance;

                    if (path.Distance < minPath)
                    {
                        //minCloudId = path.CloudId;
                        minPath = path.Connections.Sum(p => p.Distance);
                        //feasibleCloudSpecifications.FirstOrDefault(f => f.UniversalId == minCloudId)
                        //    .TemporaryPathDistance = minPath;

                    }
                }

                //i AM  missing shortest path here. if I can order the clouds by shortest path then the best fitness will also consider the shortest path automatically.
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

                //CloudSpecification feasibleCloudSpecification = cloudSpecifications.FirstOrDefault(c => c.UniversalId == minCloudId);
                CloudSpecification feasibleCloudSpecification = bestFitCloud;


                if (feasibleCloudSpecification != null)
                {
                    feasibleCloudSpecification.AllocateUser(userRequirement);

                    string pathInfo = "";
                    //double pathDistance = 0.0; replaced by minDistanceForCloud
                    double minDistanceForCloud = _paths.Where(p => p.CloudId == feasibleCloudSpecification.UniversalId).Min(p => p.Distance);

                    foreach (Connection connection in _paths.FirstOrDefault(p => p.CloudId == feasibleCloudSpecification.UniversalId && p.Distance == minDistanceForCloud).Connections)
                    {
                        pathInfo += $" Endpoints: {connection.StartPointId} <--> {connection.EndPointId} - ";
                        Connection realConnection =
                            connections.FirstOrDefault(
                                c => c.StartPointId == connection.StartPointId && c.EndPointId == connection.EndPointId);

                        //realConnection.AllocatedBandwidth += userRequirement.BandwidthThreshold; // REMOVING THIS ON DEVAL Requirement. THis means infinite oversubscription
                        //pathDistance += connection.Distance;
                    }

                    results.Add($"VM {userRequirement.UniversalId} | {userRequirement.LocationTitle} " +
                                $"Placed on Cloud {feasibleCloudSpecification.UniversalId} | {feasibleCloudSpecification.LocationTitle}" +
                                $" {Environment.NewLine} Via Path: {pathInfo} {Environment.NewLine} Distance: {Math.Round(minDistanceForCloud, 2)} {Environment.NewLine} Total Fitness: {feasibleCloudSpecification.TotalFitness} {Environment.NewLine} possible choices [{possibleChoices}] {Environment.NewLine} possible paths {Environment.NewLine}{allPaths}");
                    totalDistance += minDistanceForCloud;
                }
                else
                {
                    results.Add($"VM {userRequirement.UniversalId} | {userRequirement.LocationTitle}  cannot be placed on any suitable Cloud!");
                }

               
            }

            AverageDistancePerRequest = totalDistance / numberOfusers;

            return results;
        }


        public List<int> FindPossibleClouds(List<int> cloudAndUsers, Connection[][] adjacencyMatrix, UserRequirement userRequirement, List<CloudSpecification> cloudSpecifications)
        {
            int source = userRequirement.UniversalId;
            List<int> possibleClouds = new List<int>();
            List<int> tempCloudAndUsers = new List<int>(cloudAndUsers);

            List<Connection> path = new List<Connection>();
            int pathId = 0;

            //while (tempCloudAndUsers.Count > 0)
            while (true)
            {
                double previousDistance = path.Sum(p => p.Distance);
                //Find the vertex with smallest distance && with constraints
                //Connection shortestConnection =
                //adjacencyMatrix[source].FirstOrDefault(m => m != null && m.RemainBandwidth >= userRequirement.BandwidthThreshold && (m.Distance + previousDistance <= userRequirement.DistanceThreshold));

                Random random = new Random();
                List<Connection> shortestConnections =
                    adjacencyMatrix[source].Where(
                        m =>
                            m != null && m.RemainBandwidth >= userRequirement.BandwidthThreshold &&
                            (m.Distance + previousDistance <= userRequirement.DistanceThreshold)).ToList();

                Connection shortestConnection = null;
                if (shortestConnections.Count > 0)
                {
                    double shortestDistance = shortestConnections.Min(c => c.Distance);
                    shortestConnection = shortestConnections.FirstOrDefault(c => c.Distance == shortestDistance);
                    //shortestConnection = shortestConnections[random.Next(0, shortestConnections.Count)];
                }
                    
                //adjacencyMatrix[source].FirstOrDefault(m => m.RemainBandwidth >= userRequirement.ExternalBandwidth);


                if (shortestConnection != null)
                {
                    path.Add(shortestConnection);
                    //Just something due to the nature of implementation
                    int shortestPathId = shortestConnection.StartPointId == source
                        ? shortestConnection.EndPointId
                        : shortestConnection.StartPointId;

                    //Is it a cloud?
                    CloudSpecification possibleCloud = cloudSpecifications.FirstOrDefault(c => c.UniversalId == shortestPathId 
                                                        && c.RemainCpuCount >= userRequirement.CpuCount && c.RemainMemorySize >= userRequirement.MemorySize 
                                                        && c.NetworkBandwidth >= userRequirement.NetworkBandwidth
                                                        && c.CalculateCost(userRequirement) <= userRequirement.CostThreshold);
                    if (possibleCloud != null)
                    {
                        _paths.Add(new Path() { Id = pathId, CloudId = possibleCloud.UniversalId, Connections = path.ToArray() });
                        possibleClouds.Add(possibleCloud.UniversalId);
                        pathId++;
                    }
                    //Remove from cloudAndUsers
                    tempCloudAndUsers.Remove(shortestPathId);
                    //Remove this vertex from the set. Possibly to disallow cycles?
                    adjacencyMatrix[source][shortestPathId] = null;
                    adjacencyMatrix[shortestPathId][source] = null;

                    source = shortestPathId;
                }
                else
                {
                    path.Clear();
                    source = userRequirement.UniversalId;
                    Connection availableConnection =
                        adjacencyMatrix[source].FirstOrDefault(m => m != null && m.RemainBandwidth >= userRequirement.BandwidthThreshold && (m.Distance + previousDistance <= userRequirement.DistanceThreshold));
                    if (availableConnection == null)
                    {
                        break;
                    }
                    
                }
            }
            return possibleClouds;
        }


        public List<CloudSpecification> FindPossibleCloudsDFS(Connection[][] adjacencyMatrix, UserRequirement userRequirement, List<CloudSpecification> cloudSpecifications)
        {
            List<CloudSpecification> possibleClouds = new List<CloudSpecification>();
            List<Connection> pathsList = new List<Connection>();

            Stack<int> stack = new Stack<int>();
            stack.Push(userRequirement.UniversalId);
            int pathId = 0;
            while (stack.Count > 0)
            {
                int vertex = stack.Pop();

                Connection connection = adjacencyMatrix[vertex].FirstOrDefault(c => c != null && !c.Visited);
                if (connection != null)
                {
                    connection.Visited = true;
                    int newVertex = connection.StartPointId == vertex ? connection.EndPointId : connection.StartPointId;

                    stack.Push(newVertex);
                    pathsList.Add(connection);

                    CloudSpecification possibleCloud = cloudSpecifications.FirstOrDefault(c => c.UniversalId == newVertex);
                    if (possibleCloud != null)
                    {
                        _paths.Add(new Path() { Id = pathId, CloudId = possibleCloud.UniversalId, Connections = pathsList.ToArray()});
                        pathId++;
                    }
                }
            }

            return possibleClouds;
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
            //TreeNode<int> currentNode = rootNode;

            Queue<TreeNode<int>> queue = new Queue<TreeNode<int>>();
            queue.Enqueue(rootNode);
            while (queue.Count > 0)
            {
                TreeNode<int> current = queue.Dequeue();

                
                foreach (Connection connection in adjacencyMatrix[current.Node].Where(c => c != null))
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

            return possibleClouds;
        }

    }

    public class Path
    {
        public int Id { get; set; }
        public int CloudId { get; set; }
        public Connection[] Connections { get; set; }
        public double Distance { get; set; }    
    }



    //if (currentNode.ChildrenNodes.Count > 0)
    //    currentNode = currentNode.ChildrenNodes.FirstOrDefault(c => c.Node == current);
    //else
    //    currentNode = currentNode.ParentNode;

    //if(currentNode.Node != current)
    //    break; //FREAK OUT!

    //while (currentNode.Node != current)
    //{
    //    if (currentNode.ChildrenNodes.Count > 0)
    //        currentNode = currentNode != null && currentNode.ChildrenNodes.FirstOrDefault(c => c.Node == current) == null ? 
    //            currentNode.ParentNode : currentNode.ChildrenNodes.FirstOrDefault(c => c.Node == current);
    //    else
    //        currentNode = currentNode.ParentNode;
    //}


}