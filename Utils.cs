using System;
using BendTime;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Utils {
    namespace ExtensionMethods {
        static class TaskExtensions {
            public static Task<TOutput> Then<TInput, TOutput>(this Task<TInput> task, Func<TInput, TOutput> func) {
                return task.ContinueWith((input) => func(input.Result));
            }
            public static Task Then(this Task task, Action<Task> func) {
                return task.ContinueWith(func);
            }
            public static Task Then<TInput>(this Task<TInput> task, Action<TInput> func) {
                return task.ContinueWith((input) => func(input.Result));
            }
            public static T GetOrAddComponent<T>(this GameObject obj) where T : Component {
                return obj.GetComponent<T>() ?? obj.AddComponent<T>();
            }
        }
    }
}

