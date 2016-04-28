using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMAllocation.Web.Models;

namespace VMAllocation.Web.Services
{
    public class LoadBalancedAllocationService : IAllocation
    {

        public List<string> Allocate(List<CloudSpecification> cloudSpecifications,
            List<UserRequirement> userRequirements, List<Connection> connections)
        {
            List<string> results = new List<string>();
            List<UserCloudAllocation> allocations = new List<UserCloudAllocation>();


            int i = 0;
            foreach (UserRequirement userRequirement in userRequirements)
            {
                cloudSpecifications = cloudSpecifications.OrderByDescending(c => c.RemainCpuCount).ToList();


                if (userRequirement.CpuCount < cloudSpecifications[i].RemainCpuCount)
                {
                    if (userRequirement.MemorySize < cloudSpecifications[i].RemainMemorySize)
                    {

                        int cloudId = cloudSpecifications[i].UniversalId;
                        List<Connection> tempConnections =
                            connections.Where(c => c.StartPointId == cloudId || c.EndPointId == cloudId).ToList();
                        if (tempConnections.Count > 0)
                        {
                            Connection tempConnection =
                                tempConnections.FirstOrDefault(c =>
                                    c.StartPointId == userRequirement.UniversalId ||
                                    c.EndPointId == userRequirement.UniversalId);

                            if (tempConnection != null)
                            {
                                // You are done! Connection Found
                                Console.WriteLine("Found on First Level!");
                                Console.WriteLine($"Success! {userRequirement.UniversalId} VM Successfully Placed!");
                            }
                            else
                            {
                                List<int> neighbouringIds =
                                    tempConnections.Where(t => t.StartPointId != cloudId)
                                        .Select(t => t.StartPointId)
                                        .ToList();

                                neighbouringIds.AddRange(tempConnections.Where(t => t.EndPointId != cloudId)
                                        .Select(t => t.EndPointId)
                                        .ToList());

                                List<int> visitedIds = new List<int>(cloudId);


                                bool vmPlaced = FindConnection(connections, tempConnections, cloudId, userRequirement.UniversalId, neighbouringIds, visitedIds);
                                if (vmPlaced)
                                {
                                    Console.WriteLine($"Success! {userRequirement.UniversalId} VM Successfully Placed!");
                                }

                            }


                        }
                        else
                        {
                            Console.WriteLine($"{cloudId} has no Connections");
                        }

                        //First load the connection...
                        if (userRequirement.ExternalBandwidth < connections[i].RemainBandwidth)
                        {

                        }
                    }
                }
                else
                {
                    userRequirements.Remove(userRequirement);
                    results.Add(
                        $"{userRequirement.UniversalId} Cannot be placed on any VM. CPU requirements not feasible");
                }
            }



            return results;
        }

        //DONT HAVE TEMP CONNECTIONS HERE!> Have the specific Node that will lead to it!!
        private bool FindConnection(List<Connection> connections, List<Connection> tempConnections, int cloudId, int userId, List<int> neighbouringIds, List<int> visitedIds  )
        {
            List<List<Connection>> path = new List<List<Connection>>();

            int i = 0;
            while (true)
            {
                List<Connection> deepConnections =
                    connections.Where(
                        d =>
                            tempConnections.Any(
                                t =>
                                    t.StartPointId == d.StartPointId || t.EndPointId == d.StartPointId ||
                                    t.StartPointId == d.EndPointId || t.EndPointId == d.EndPointId)).ToList();

                //List<Connection> deepConnections = connections.Where(d => neighbouringIds.Any(t => (t == d.StartPointId || t == d.EndPointId) && (t != visitedIds)) ).ToList();


                //This is just to remove the already visited connections
                //This is needed because we dont know if the start or the end was the point which matched!
                //foreach (Connection tempConnection in tempConnections)
                //{
                //    deepConnections.Remove(tempConnection);
                //}


                if (deepConnections.Count > 0)
                {
                    Connection deepConnection =
                        deepConnections.FirstOrDefault(c => c.StartPointId == userId || c.EndPointId == userId);

                    if (deepConnection != null)
                    {
                        // You are done! Connection Found
                        //But track the way!!
                        Console.WriteLine("Found!!");

                        //Tracking the Path!!
                        path.Add(new List<Connection>() { deepConnection });
                        //path[i] = new List<Connection>() {deepConnection};

                        for (int j = i - 1; j >= 0; j--)
                        {
                            for (int k = 0; k < path[j].Count; k++)
                            {
                                if (path[j][k].StartPointId != path[j + 1][0].StartPointId &&
                                    path[j][k].EndPointId != path[j + 1][0].StartPointId &&
                                    path[j][k].StartPointId != path[j + 1][0].EndPointId &&
                                    path[j][k].EndPointId != path[j + 1][0].EndPointId)
                                {
                                    path[j].Remove(path[j][k]);
                                }
                            }
                        }

                        return true;
                        //break;
                    }
                    else
                    {
                        //CONTINUEE!!!
                        //foreach (Connection tempConnection in tempConnections)
                        //{
                        //    deepConnections.Remove(tempConnection);
                        //}
                        path.Add(new List<Connection>(tempConnections));

                        tempConnections = deepConnections;
                        i++;
                        continue;
                    }
                }
                else
                {
                    Console.WriteLine("No Further Connections Found!!");
                    return false;
                    //break;
                }

                
            }


        }
    }
}