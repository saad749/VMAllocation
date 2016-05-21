using System.Collections.Generic;

namespace VMAllocation.Web.Models.DataStructure
{
    public class TreeNode<T>
    {
        public T Node { get; set; }
        public Connection Connection { get; set; }
        public TreeNode<T> ParentNode { get; set; } 
        public List<TreeNode<T>> ChildrenNodes { get; set; }


    }
}
