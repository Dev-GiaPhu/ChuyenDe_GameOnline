using Fusion;
using UnityEngine;

public class PawnSpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("Prefab Quân cờ")]
    public GameObject pawnPrefab;

    // Hàm tự động chạy khi có người chơi kết nối vào phòng
    public void PlayerJoined(PlayerRef player)
    {
        // Chỉ sinh quân cờ cho chính máy cục bộ vừa tham gia
        if (player == Runner.LocalPlayer)
        {
            Runner.Spawn(pawnPrefab, new Vector3(0, 1, 0), Quaternion.identity, player);
        }
    }
}
