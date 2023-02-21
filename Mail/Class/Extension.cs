using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;

namespace Mail.Class
{
    internal static class Extension
    {
        public static void AddRange<T>(this ICollection<T> Collection, IEnumerable<T> InputCollection)
        {
            foreach (T Item in InputCollection.ToArray())
            {
                Collection.Add(Item);
            }
        }

        public static void RemoveRange<T>(this ICollection<T> Collection, IEnumerable<T> InputCollection)
        {
            foreach (T Item in InputCollection.ToArray())
            {
                Collection.Remove(Item);
            }
        }

        public static T FindChildOfType<T>(this DependencyObject root) where T : DependencyObject
        {
            Queue<DependencyObject> ObjectQueue = new Queue<DependencyObject>();

            ObjectQueue.Enqueue(root);

            while (ObjectQueue.Count > 0)
            {
                DependencyObject Current = ObjectQueue.Dequeue();

                if (Current != null)
                {
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(Current); i++)
                    {
                        DependencyObject ChildObject = VisualTreeHelper.GetChild(Current, i);

                        if (ChildObject is T TypedChild)
                        {
                            return TypedChild;
                        }
                        else
                        {
                            ObjectQueue.Enqueue(ChildObject);
                        }
                    }
                }
            }

            return null;
        }

        public static T FindChildOfName<T>(this DependencyObject Parent, string Name) where T : DependencyObject
        {
            Queue<DependencyObject> ObjectQueue = new Queue<DependencyObject>();

            ObjectQueue.Enqueue(Parent);

            while (ObjectQueue.Count > 0)
            {
                DependencyObject Current = ObjectQueue.Dequeue();

                if (Current != null)
                {
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(Current); i++)
                    {
                        DependencyObject ChildObject = VisualTreeHelper.GetChild(Current, i);

                        if (ChildObject is T TypedChild && (TypedChild as FrameworkElement)?.Name == Name)
                        {
                            return TypedChild;
                        }
                        else
                        {
                            ObjectQueue.Enqueue(ChildObject);
                        }
                    }
                }
            }

            return null;
        }

        public static T FindParentOfName<T>(this DependencyObject Child, string Name) where T : DependencyObject
        {
            DependencyObject CurrentParent = VisualTreeHelper.GetParent(Child);

            while (CurrentParent != null)
            {
                if (CurrentParent is T TypedParent && (TypedParent as FrameworkElement)?.Name == Name)
                {
                    return TypedParent;
                }
                else
                {
                    CurrentParent = VisualTreeHelper.GetParent(CurrentParent);
                }
            }

            return null;
        }

        public static T FindParentOfType<T>(this DependencyObject Child) where T : DependencyObject
        {
            DependencyObject CurrentParent = VisualTreeHelper.GetParent(Child);

            while (CurrentParent != null)
            {
                if (CurrentParent is T CParent)
                {
                    return CParent;
                }
                else
                {
                    CurrentParent = VisualTreeHelper.GetParent(CurrentParent);
                }
            }

            return null;
        }
    }
}
