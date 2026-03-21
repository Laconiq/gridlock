using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace AIWE.Network
{
    public static class RelayManager
    {
        public static async Task<string> CreateRelay(int maxPlayers)
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            var joinCode = (await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId)).ToUpperInvariant();

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            var relayServerData = allocation.ToRelayServerData("dtls");
            transport.SetRelayServerData(relayServerData);

            Debug.Log($"[RelayManager] Relay created. Join code: {joinCode}");
            return joinCode;
        }

        public static async Task JoinRelay(string joinCode)
        {
            joinCode = joinCode.ToUpperInvariant();
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            var relayServerData = joinAllocation.ToRelayServerData("dtls");
            transport.SetRelayServerData(relayServerData);

            Debug.Log($"[RelayManager] Joined relay with code: {joinCode}");
        }
    }
}
