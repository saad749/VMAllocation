using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Web;
using VMAllocation.Web.Models;

namespace VMAllocation.Web.Services
{
    public class LoadBalancedAllocationService : IAllocation
    {

        //Dirty Trick to gget the Path for each cloud.
        private List<Tuple<int, Connection[]>> Paths;

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
            double distperReq = 0.0;
            

            foreach (CloudSpecification cloudSpecification in cloudSpecifications)
            {
                cloudSpecification.AllocatedUserRequirements = new List<UserRequirement>();
            }


            foreach (UserRequirement userRequirement in userRequirements)
            {

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

                Paths = new List<Tuple<int, Connection[]>>();
                List<int> possibleClouds = Dijkstra(cloudAndUsers, adjacencyMatrix, userRequirement, cloudSpecifications);

                //CloudSpecification feasibleCloudSpecification = cloudSpecifications.Where(c => possibleClouds.Contains(c.UniversalId)).
                //    OrderByDescending(c => c.RemainCpuCount).FirstOrDefault();

                CloudSpecification feasibleCloudSpecification = cloudSpecifications.Where(c => possibleClouds.Contains(c.UniversalId)).
                    OrderByDescending(c => c.RemainCpuCount).FirstOrDefault();

                double minPath = 100000;
                int minCloudId = -1;
                //Selecting Shortest PAth
                foreach (Tuple<int, Connection[]> path in Paths)
                {
                    if (path.Item2.Sum(p => p.Distance) < minPath)
                    {
                        minCloudId = path.Item1;
                        minPath = path.Item2.Sum(p => p.Distance);
                    }
                }

                feasibleCloudSpecification = cloudSpecifications.FirstOrDefault(c => c.UniversalId == minCloudId);


                if (feasibleCloudSpecification != null)
                {
                    feasibleCloudSpecification.AllocateUser(userRequirement);

                    string pathInfo = "";
                    double pathDistance = 0.0;
                    foreach (Connection connection in Paths.FirstOrDefault(p => p.Item1 == feasibleCloudSpecification.UniversalId).Item2)
                    {
                        pathInfo += $" Endpoints: {connection.StartPointId} <-> {connection.EndPointId} - ";
                        Connection realConnection =
                            connections.FirstOrDefault(
                                c => c.StartPointId == connection.StartPointId && c.EndPointId == connection.EndPointId);

                        realConnection.AllocatedBandwidth += userRequirement.BandwidthThreshold;
                        pathDistance += connection.Distance;
                    }

                    results.Add($"VM id: {userRequirement.UniversalId} | title: {userRequirement.LocationTitle} " +
                                $"placed on Cloud Id {feasibleCloudSpecification.UniversalId} | title: {feasibleCloudSpecification.LocationTitle}" +
                                $" | Via Path: {pathInfo} | Distance: {pathDistance}");
                    totalDistance += pathDistance;
                }
                else
                {
                    results.Add($"VM id: {userRequirement.UniversalId} cannot be placed on any suitable Cloud!");
                }

               
            }

            distperReq = totalDistance/numberOfusers;
            results.Add($"AVG DIST PER REQ: {distperReq}");

            return results;
        }


        public List<int> Dijkstra(List<int> cloudAndUsers, Connection[][] adjacencyMatrix, UserRequirement userRequirement, List<CloudSpecification> cloudSpecifications)
        {
            int source = userRequirement.UniversalId;
            List<int> possibleClouds = new List<int>();
            List<int> tempCloudAndUsers = new List<int>(cloudAndUsers);

            List<Connection> path = new List<Connection>();


            while (tempCloudAndUsers.Count > 0)
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
                    shortestConnection = shortestConnections[random.Next(0, shortestConnections.Count)];
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
                        Paths.Add(new Tuple<int, Connection[]>(possibleCloud.UniversalId, path.ToArray()));
                        possibleClouds.Add(possibleCloud.UniversalId);
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
        
    }
}