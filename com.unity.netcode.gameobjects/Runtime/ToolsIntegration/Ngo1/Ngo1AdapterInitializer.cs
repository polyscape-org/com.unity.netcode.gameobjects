using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]
namespace Unity.Multiplayer.Tools.Adapters.Ngo1
{
    internal static class Ngo1AdapterInitializer
    {
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeAdapter()
        {
            InitializeAdapterAsync().Forget();
        }

        private static async Task InitializeAdapterAsync()
        {
            var networkManager = await GetNetworkManagerAsync();
            if(networkManager.NetworkMetrics is NetworkMetrics)
            {
                Debug.LogWarning("Ngo1AdapterInitializer: NetworkMetrics already initialized. Skipping initialization.");
                return;
            }

            // metrics will notify the adapter directly
            networkManager.NetworkMetrics = new NetworkMetrics();
            var ngo1Adapter = new Ngo1Adapter(networkManager);
            NetworkAdapters.AddAdapter(ngo1Adapter);

            NetworkSolutionInterface.SetInterface(new NetworkSolutionInterfaceParameters
            {
                NetworkObjectProvider = new NetworkObjectProvider(networkManager),
            });

            // We need the OnInstantiated callback because the NetworkManager could get destroyed and recreated when we change scenes
            // OnInstantiated is called in Awake, and the GetNetworkManagerAsync only returns at least after OnEnable
            // therefore the initialization is not called twice
            NetworkManager.OnInstantiated += async _ =>
            {
                // We need to wait for the NetworkTickSystem to be ready as well
                var newNetworkManager = await GetNetworkManagerAsync();
                networkManager.NetworkMetrics = new NetworkMetrics();
                ngo1Adapter.ReplaceNetworkManager(newNetworkManager);
            };

            NetworkManager.OnDestroying += _ =>
            {
                ngo1Adapter.Deinitialize();
            };
        }

        private static async Task<NetworkManager> GetNetworkManagerAsync()
        {
            while (NetworkManager.Singleton == null || NetworkManager.Singleton.NetworkTickSystem == null)
            {
                await Task.Yield();
            }

            return NetworkManager.Singleton;
        }


        // Copied from Tools.Common to avoid remove dependency for a single function
         public static void Forget(this Task task)
        {
            if (!task.IsCompleted || task.IsFaulted)
            {
                _ = ForgetAwaited(task);
            }

            static async Task ForgetAwaited(Task task, bool logCanceledTask = false)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (TaskCanceledException exception)
                {
                    if (logCanceledTask)
                    {
                        Debug.LogException(exception);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
    }
}
