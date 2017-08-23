using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistentDataStructures
{
    // Listing 3.16 Immutable B-tree representation in C#
    public class BinaryTree<T>
    {
        public BinaryTree(T value, BinaryTree<T> left, BinaryTree<T> right)
        {
            Value = value;
            Left = left;
            Right = right;
        }
        public T Value { get; }
        public BinaryTree<T> Left { get; }
        public BinaryTree<T> Right { get; }
    }

    public static class BinaryTreeExtension
    {
        // Listing 3.17 B-tree helper recursive functions
        public static bool Contains<T>(this BinaryTree<T> tree, T value)
        {
            if (tree == null) return false;
            var compare = Comparer<T>.Default.Compare(value, tree.Value);
            if (compare == 0) return true;
            if (compare < 0)
                return tree.Left != null && tree.Left.Contains(value);
            return tree.Right != null && tree.Right.Contains(value);
        }

        public static BinaryTree<T> Insert<T>(this BinaryTree<T> tree, T value)
        {
            if (tree == null)
                return new BinaryTree<T>(value, null, null);
            var compare = Comparer<T>.Default.Compare(value, tree.Value);
            if (compare == 0) return tree;
            if (compare < 0)
                return new BinaryTree<T>(tree.Value,
                    tree.Left.Insert(value), tree.Right);
            return new BinaryTree<T>(tree.Value,
                    tree.Left, tree.Right.Insert(value));
        }

        // Listing 3.18 In-Order navigation function
        public static void InOrder<T>(this BinaryTree<T> tree, Action<T> action)
        {
            if (tree == null)
                return;
            tree.Left.InOrder(action);
            action(tree.Value);
            tree.Right.InOrder(action);
        }
    }
}