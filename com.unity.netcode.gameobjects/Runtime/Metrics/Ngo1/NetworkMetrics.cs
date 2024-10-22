using System;
using System.Collections.Generic;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;
using Unity.Profiling;

namespace Unity.Netcode
{

    internal class NetworkMetrics : INetworkMetrics
    {
        private const ulong k_MaxMetricsPerFrame = 1000L;
        private static Dictionary<uint, string> s_SceneEventTypeNames;
        private static ProfilerMarker s_FrameDispatch = new ProfilerMarker($"{nameof(NetworkMetrics)}.DispatchFrame");

        static NetworkMetrics()
        {
            s_SceneEventTypeNames = new Dictionary<uint, string>();
            foreach (SceneEventType type in Enum.GetValues(typeof(SceneEventType)))
            {
                s_SceneEventTypeNames[(uint)type] = type.ToString();
            }
        }

        private static string GetSceneEventTypeName(uint typeCode)
        {
            if (!s_SceneEventTypeNames.TryGetValue(typeCode, out string name))
            {
                name = "Unknown";
            }

            return name;
        }



        private ulong m_NumberOfMetricsThisFrame;

        public NetworkMetrics()
        {
            Collector = new DefaultMetricCollection();
            Dispatcher = new MetricDispatcher(Collector);

            // .WithCounters(m_TransportBytesSent, m_TransportBytesReceived)
            // .WithMetricEvents(m_NetworkMessageSentEvent, m_NetworkMessageReceivedEvent)
            // .WithMetricEvents(m_NamedMessageSentEvent, m_NamedMessageReceivedEvent)
            // .WithMetricEvents(m_UnnamedMessageSentEvent, m_UnnamedMessageReceivedEvent)
            // .WithMetricEvents(m_NetworkVariableDeltaSentEvent, m_NetworkVariableDeltaReceivedEvent)
            // .WithMetricEvents(m_OwnershipChangeSentEvent, m_OwnershipChangeReceivedEvent)
            // .WithMetricEvents(m_ObjectSpawnSentEvent, m_ObjectSpawnReceivedEvent)
            // .WithMetricEvents(m_ObjectDestroySentEvent, m_ObjectDestroyReceivedEvent)
            // .WithMetricEvents(m_RpcSentEvent, m_RpcReceivedEvent)
            // .WithMetricEvents(m_ServerLogSentEvent, m_ServerLogReceivedEvent)
            // .WithMetricEvents(m_SceneEventSentEvent, m_SceneEventReceivedEvent)
            // .WithCounters(m_PacketSentCounter, m_PacketReceivedCounter)
            // .WithGauges(m_RttToServerGauge)
            // .WithGauges(m_NetworkObjectsGauge)
            // .WithGauges(m_ConnectionsGauge)
            // .WithGauges(m_PacketLossGauge)
            // .Build();
        }

        internal IMetricDispatcher Dispatcher { get; }

        internal DefaultMetricCollection Collector { get; }

        private bool CanSendMetrics => m_NumberOfMetricsThisFrame < k_MaxMetricsPerFrame;

        public void SetConnectionId(ulong connectionId)
        {
            Collector.SetConnectionId(connectionId);
        }

        public void TrackTransportBytesSent(long bytesCount)
        {
            Collector.IncrementTransportBytesSent(bytesCount);
            // m_TransportBytesSent.Increment(bytesCount);
        }

        public void TrackTransportBytesReceived(long bytesCount)
        {
            Collector.IncrementTransportBytesReceived(bytesCount);
            // m_TransportBytesReceived.Increment(bytesCount);
        }

        public void TrackNetworkMessageSent(ulong receivedClientId, string messageType, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackNetworkMessageSent(receivedClientId, messageType, bytesCount);
            // m_NetworkMessageSentEvent.Mark(new NetworkMessageEvent(new ConnectionInfo(receivedClientId), messageType, bytesCount));
            IncrementMetricCount();
        }

        public void TrackNetworkMessageReceived(ulong senderClientId, string messageType, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackNetworkMessageReceived(senderClientId, StringConversionUtility.ConvertToFixedString(messageType), bytesCount);
            IncrementMetricCount();
        }

        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackNamedMessageSent(receiverClientId, messageName, bytesCount);
            IncrementMetricCount();
        }

        public void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, long bytesCount)
        {
            foreach (var receiver in receiverClientIds)
            {
                TrackNamedMessageSent(receiver, messageName, bytesCount);
            }
        }

        public void TrackNamedMessageReceived(ulong senderClientId, string messageName, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackNamedMessageReceived(senderClientId, messageName, bytesCount);
            IncrementMetricCount();
        }

        public void TrackUnnamedMessageSent(ulong receiverClientId, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackUnnamedMessageSent(receiverClientId, bytesCount);
            // m_UnnamedMessageSentEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(receiverClientId), bytesCount));
            IncrementMetricCount();
        }

        public void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackUnnamedMessageSent(receiverClientId, bytesCount);
            }
        }

        public void TrackUnnamedMessageReceived(ulong senderClientId, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackUnnamedMessageReceived(senderClientId, bytesCount);
            // m_UnnamedMessageReceivedEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(senderClientId), bytesCount));
            IncrementMetricCount();
        }

        public void TrackNetworkVariableDeltaSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackNetworkVariableDeltaSent(receiverClientId,
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                networkObject.NetworkObjectId,
                StringConversionUtility.ConvertToFixedString(variableName),
                StringConversionUtility.ConvertToFixedString(networkBehaviourName),
                bytesCount);
            // m_NetworkVariableDeltaSentEvent.Mark(
            //     new NetworkVariableEvent(
            //         new ConnectionInfo(receiverClientId),
            //         GetObjectIdentifier(networkObject),
            //         variableName,
            //         networkBehaviourName,
            //         bytesCount));
            IncrementMetricCount();
        }

        public void TrackNetworkVariableDeltaReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }
            Collector.TrackNetworkVariableDeltaReceived(senderClientId,
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                networkObject.NetworkObjectId,
                StringConversionUtility.ConvertToFixedString(variableName),
                StringConversionUtility.ConvertToFixedString(networkBehaviourName),
                bytesCount);
            // m_NetworkVariableDeltaReceivedEvent.Mark(
            //     new NetworkVariableEvent(
            //         new ConnectionInfo(senderClientId),
            //         GetObjectIdentifier(networkObject),
            //         variableName,
            //         networkBehaviourName,
            //         bytesCount));
            IncrementMetricCount();
        }

        public void TrackOwnershipChangeSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackOwnershipChangeSent(receiverClientId,
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                networkObject.NetworkObjectId,
                bytesCount);
            // m_OwnershipChangeSentEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));
            IncrementMetricCount();
        }

        public void TrackOwnershipChangeReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackOwnershipChangeReceived(senderClientId,
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                networkObject.NetworkObjectId,
                bytesCount);
            // m_OwnershipChangeReceivedEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(senderClientId),
            //     GetObjectIdentifier(networkObject), bytesCount));
            IncrementMetricCount();
        }

        public void TrackObjectSpawnSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackObjectSpawnSent(receiverClientId,
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                networkObject.NetworkObjectId,
                bytesCount);
            // m_ObjectSpawnSentEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));
            IncrementMetricCount();
        }

        public void TrackObjectSpawnReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackObjectSpawnReceived(senderClientId,
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                networkObject.NetworkObjectId,
                bytesCount);
            // m_ObjectSpawnReceivedEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(senderClientId), GetObjectIdentifier(networkObject), bytesCount));
            IncrementMetricCount();
        }

        public void TrackObjectDestroySent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackObjectDestroySent(receiverClientId,
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                networkObject.NetworkObjectId,
                bytesCount);
            // m_ObjectDestroySentEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));
            IncrementMetricCount();
        }

        public void TrackObjectDestroyReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackObjectDestroyReceived(senderClientId,
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                networkObject.NetworkObjectId,
                bytesCount);
            // m_ObjectDestroyReceivedEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(senderClientId), GetObjectIdentifier(networkObject), bytesCount));
            IncrementMetricCount();
        }

        public void TrackRpcSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackRpcSent(receiverClientId,
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                networkObject.NetworkObjectId,
                StringConversionUtility.ConvertToFixedString(rpcName),
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                bytesCount);

            // m_RpcSentEvent.Mark(
            //     new RpcEvent(
            //         new ConnectionInfo(receiverClientId),
            //         GetObjectIdentifier(networkObject),
            //         rpcName,
            //         networkBehaviourName,
            //         bytesCount));
            IncrementMetricCount();
        }

        public void TrackRpcSent(
            ulong[] receiverClientIds,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackRpcSent(receiverClientId, networkObject, rpcName, networkBehaviourName, bytesCount);
            }
        }

        public void TrackRpcReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackRpcReceived(senderClientId,
                StringConversionUtility.ConvertToFixedString(networkObject.GetNameForMetrics()),
                networkObject.NetworkObjectId,
                StringConversionUtility.ConvertToFixedString(rpcName),
                StringConversionUtility.ConvertToFixedString(networkBehaviourName),
                bytesCount);

            // m_RpcReceivedEvent.Mark(
            //     new RpcEvent(new ConnectionInfo(senderClientId),
            //         GetObjectIdentifier(networkObject),
            //         rpcName,
            //         networkBehaviourName,
            //         bytesCount));
            IncrementMetricCount();
        }

        public void TrackServerLogSent(ulong receiverClientId, uint logType, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackServerLogSent(receiverClientId, logType, bytesCount);
            IncrementMetricCount();
        }

        public void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackServerLogReceived(senderClientId, logType, bytesCount);
            IncrementMetricCount();
        }

        public void TrackSceneEventSent(IReadOnlyList<ulong> receiverClientIds, uint sceneEventType, string sceneName, long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackSceneEventSent(receiverClientId, sceneEventType, sceneName, bytesCount);
            }
        }

        public void TrackSceneEventSent(ulong receiverClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackSceneEventSent(receiverClientId,
                // TODO: overload that returns a FixedString64Bytes without conversion?
                StringConversionUtility.ConvertToFixedString(GetSceneEventTypeName(sceneEventType)),
                StringConversionUtility.ConvertToFixedString(sceneName),
                bytesCount);
            // m_SceneEventSentEvent.Mark(new SceneEventMetric(new ConnectionInfo(receiverClientId), GetSceneEventTypeName(sceneEventType), sceneName, bytesCount));
            IncrementMetricCount();
        }

        public void TrackSceneEventReceived(ulong senderClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.TrackSceneEventReceived(senderClientId,
                StringConversionUtility.ConvertToFixedString(GetSceneEventTypeName(sceneEventType)),
                StringConversionUtility.ConvertToFixedString(sceneName),
                bytesCount);
            // m_SceneEventReceivedEvent.Mark(new SceneEventMetric(new ConnectionInfo(senderClientId), GetSceneEventTypeName(sceneEventType), sceneName, bytesCount));
            IncrementMetricCount();
        }

        public void TrackPacketSent(uint packetCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.IncrementPacketSent(packetCount);
            // m_PacketSentCounter.Increment(packetCount);
            IncrementMetricCount();
        }

        public void TrackPacketReceived(uint packetCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.IncrementPacketReceived(packetCount);
            // m_PacketReceivedCounter.Increment(packetCount);
            IncrementMetricCount();
        }

        public void UpdateRttToServer(int rttMilliseconds)
        {
            if (!CanSendMetrics)
            {
                return;
            }
            var rttSeconds = rttMilliseconds * 1e-3;
            // m_RttToServerGauge.Set(rttSeconds);
            Collector.SetRttToServer(rttSeconds);
        }

        public void UpdateNetworkObjectsCount(int count)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            // m_NetworkObjectsGauge.Set(count);
            Collector.SetNetworkObjectCount(count);
        }

        public void UpdateConnectionsCount(int count)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.SetConnectionCount(count);
            // m_ConnectionsGauge.Set(count);
        }

        public void UpdatePacketLoss(float packetLoss)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            Collector.SetPacketLoss(packetLoss);
            // m_PacketLossGauge.Set(packetLoss);
        }

        public void DispatchFrame()
        {
            s_FrameDispatch.Begin();
            Dispatcher.Dispatch();
            s_FrameDispatch.End();
            m_NumberOfMetricsThisFrame = 0;
        }

        private void IncrementMetricCount()
        {
            m_NumberOfMetricsThisFrame++;
        }
    }
}
